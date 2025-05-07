using AndromedaDnsFirewall.network;
using Makaretu.Dns;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
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
            if (Config.Inst.useServers_DOH) {
                srvLst.AddRange(Config.Inst.DnsResolvers_DOH.Select(x => new ServerRec() { url = x, type = ServType.doh }));
            }

            if (Config.Inst.useServers_UDP) {
                srvLst.AddRange(Config.Inst.DnsResolvers_UDP.Select(x => new ServerRec() { url = x, type = ServType.upd }));
            }

            foreach (var elem in srvLst) {
                if(elem.url == "default_gateway")
                    elem.url = GetDefaultGateway().ToString();
            }

            //udpclient.Client.ReceiveTimeout = 3000;
            //udpclient.Client.SendTimeout = 3000;
        }
        HttpClient httpClient = new() { Timeout = 3.sec() };

        public static IPAddress GetDefaultGateway() {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
                .Select(g => g?.Address)
                .Where(a => a != null)
                 //.Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                 .Where(a => Array.FindIndex(a.GetAddressBytes(), b => b != 0) >= 0)
                .FirstOrDefault();
        }

        async Task<byte[]> doh(byte[] req, string addr) {
            var msg = new HttpRequestMessage(new HttpMethod("POST"), addr);
            msg.Content = new ByteArrayContent(req);
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
            //using var timeout = new CancellationTokenSource(3.sec());
            using var resp = await httpClient.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            using var cont = resp.Content;
            var res = await cont.ReadAsByteArrayAsync();
            return res;
        }

        //UdpClient udpclient = new();
        //AWaitVariable2 cv = new();
        //UdpReceiveResult lastres;

        //async Task<byte[]> waitudp(IPEndPoint addr) {

        //    var timeout = new CancellationTokenSource(3.sec());
        //    var task = udpclient.ReceiveAsync();
        //    while (true) {
        //        var res = await Task.WhenAny(task, cv.Wait(timeout.Token));
        //        if (res == task) {
        //            await task;
        //            lastres = task.Result;
        //        }
        //        if (lastres.RemoteEndPoint.Address == addr.Address) {
        //            return lastres.Buffer;
        //        }
        //        if (timeout.IsCancellationRequested)
        //            throw new Exception("timeout udp");
        //    }

        //}



        async Task<byte[]> udp(byte[] req, string addr) {
            using var cl = new UdpClient();
            var rem = new IPEndPoint(IPAddress.Parse(addr), 53);
            using var timeout = new CancellationTokenSource(3.sec());
            await cl.SendAsync(req, rem, timeout.Token);
            var result = await cl.ReceiveAsync(timeout.Token);
            return result.Buffer;
        }

        async Task<byte[]> resolveInt(ServerRec server, byte[] req) {
            var timer = Stopwatch.StartNew();
            try {

                byte[] res = null;
                if(server.type == ServType.doh) {
                    res = await doh(req, server.url);
                }else if(server.type == ServType.upd) {
                    res = await udp(req, server.url);
                } else {
                    throw new Exception("unknown serv type");
                }

                server.cntReq++;
                server.allDur += timer.ElapsedMilliseconds;
                return res; 
            } catch  {
                server.cntErr++;
                throw;
            }
        }

        enum ServType {
            upd,
            doh
        }

        record ServerRec
        {
            public string url;
            public long cntReq;
            public double allDur;
            public long cntErr;
            public ServType type;

            public double Avr => cntReq == 0 ? 0 : Math.Round(allDur / cntReq);
        }

        public string ServStats => string.Join("\n", srvLst.OrderBy(x => x.Avr).Select(x => $"{x.url}: cnt={x.cntReq}, avr={x.Avr}, err={x.cntErr}"));

        List<ServerRec> srvLst = new();


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
            var res = await resolveInt(NextServ, msg.BuffGet);
            return new() { buf = res };
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
