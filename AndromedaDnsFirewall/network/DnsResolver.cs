using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.network;
using AndromedaDnsFirewall.Utils;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.dns_server
{
    public class LazyMessage
    {
        public Message msg;
        public byte[] buf;


        public Message MsgGet
        {
            get
            {
                if (msg != null)
                    return msg;
                if (buf != null)
                {
                    msg = new Message();
                    msg.Read(buf);
                    return msg;
                }
                throw new Exception("no data");
            }
        }

        public byte[] BuffGet
        {
            get
            {
                if (buf != null)
                    return buf;
                if (msg != null)
                {
                    buf = msg.ToByteArray();
                    return buf;
                }
                throw new Exception("no data");
            }
        }
    }

    internal class DnsResolver {

        //HttpClient httpClient;
        public static DnsResolver Inst;

        static DnsResolver(){
            Inst = new();
        }

        public DnsResolver()
        {
            //httpClient = new() { Timeout = 5.sec() };
            srvLst = Config.Inst.DnsResolvers.Select(x => new ServerRec() { url = x }).ToList();
        }
        HttpClient httpClient = new() { Timeout = 3.sec() };

        async Task<byte[]> resolveInt(ServerRec server, byte[] req) {
            var timer = Stopwatch.StartNew();
            try {
                var msg = new HttpRequestMessage(new HttpMethod("POST"), server.url);
                msg.Content = new ByteArrayContent(req);
                msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
                //using var timeout = new CancellationTokenSource(3.sec());
                using var resp = await httpClient.SendAsync(msg);
                resp.EnsureSuccessStatusCode();
                using var cont = resp.Content;
                var res = await cont.ReadAsByteArrayAsync();
                server.cntReq++;
                server.allDur += timer.ElapsedMilliseconds;
                return res; 
            } catch  {
                server.cntErr++;
                throw;
            }
        }

        record ServerRec
        {
            public string url;
            public long cntReq;
            public double allDur;
            public long cntErr;

            public double Avr => cntReq == 0 ? 0 : allDur / cntReq;
        }

        public string ServStats => string.Join("\n", srvLst.OrderBy(x => x.Avr).Select(x => $"{x.url}: cnt={x.cntReq}, avr={x.Avr}, err={x.cntErr}"));

        List<ServerRec> srvLst;


        int nextServ = 0;
        ServerRec NextServ
        {
            get
            {
                nextServ++;
                if (nextServ >= srvLst.Count)
                    nextServ = 0;
                return srvLst[nextServ];
            }
        }

        async public Task<byte[]> ResolveBypass(byte[] query)
        {
            return await resolveInt(NextServ, query);
        }

        async public Task<LazyMessage> Resolve(LazyMessage msg)
        {
            //return await ResolveNoCrypt(msg); // for test
            var res = await resolveInt(NextServ, msg.BuffGet);
            return new() { buf = res };
        }

        async public Task<LazyMessage> ResolveNoCrypt(LazyMessage msg) {

            var rem = new IPEndPoint(IPAddress.Parse("1.1.1.3"), 53);
            var sender = new UdpClient(rem.AddressFamily);
            await sender.SendAsync(msg.MsgGet.ToByteArray(), rem);
            var result = await sender.ReceiveAsync();
            var data = result.Buffer;

            return new() { buf = data }; ;

        }

        Random rnd = new();

        async public Task<IPAddress> Resolve(string name) {
            Message msg = new();
            msg.Questions.Add(new Question { Type = DnsType.A, Name = name });

            var res = await resolveInt(NextServ, msg.ToByteArray());

            Message parsed = new();
            parsed.Read(res);
            
            var take = parsed.Answers.Where(x => x.Type == DnsType.A).Select(x => x as AddressRecord).ToArray();
            if(take.Length == 0) {
                throw new Exception($"no anser for request {name}");
            }
            return take[rnd.Next(take.Length)].Address;
        }


    }
}
