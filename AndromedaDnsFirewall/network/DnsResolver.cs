using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using DNS.Protocol;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.dns_server
{
    internal class DnsResolver
    {

        HttpClient httpClient;



        public DnsResolver()
        {
            httpClient = new() { Timeout = 5.sec() };
        }

        //async public Task<Response> resolveInt(string server, Request req)
        //{
        //    var url = $"{server}";
        //    var msg = new HttpRequestMessage(new HttpMethod("POST"), url);
        //    msg.Content = new ByteArrayContent(req.ToArray());
        //    msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
        //    using var resp = await httpClient.SendAsync(msg);
        //    resp.EnsureSuccessStatusCode();
        //    using var cont = resp.Content;
        //    var res = await cont.ReadAsByteArrayAsync();
        //    var answ = Response.FromArray(res);
        //    return answ;
        //}

        async public Task<byte[]> resolveInt(string server, byte[] req)
        {
            var url = $"{server}";
            var msg = new HttpRequestMessage(new HttpMethod("POST"), url);
            msg.Content = new ByteArrayContent(req);
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
            using var resp = await httpClient.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            using var cont = resp.Content;
            var res = await cont.ReadAsByteArrayAsync();
            return res;
        }


        //async public Task<Response> Resolve(IEnumerable<DnsElem> query)
        //{
        //    return await resolveInt(Config.Servers[0], query);
        //}

        int nextServ = 0;
        string NextServ
        {
            get
            {
                nextServ++;
                if (nextServ >= Config.Servers.Count)
                    nextServ = 0;
                return Config.Servers[nextServ];
            }
        }

        async public Task<byte[]> ResolveBypass(byte[] query)
        {
            return await resolveInt(NextServ, query);
        }

        public class ResolveRes
        {
            Message msg;
            byte[] buf;

            public ResolveRes(Message msg)
            {
                this.msg = msg;
            }

            public ResolveRes(byte[] buf)
            {
                this.buf = buf;
            }

            public Message Msg
            {
                get
                {
                    if (msg != null)
                        return msg;
                    if(buf != null)
                    {
                        msg = new Message();
                        msg.Read(buf);
                        return msg;
                    }
                    throw new Exception("no data");
                }
            }

            public byte[] Buff
            {
                get
                {
                    if (buf != null)
                        return buf;
                    if(msg != null)
                    {
                        buf = msg.ToByteArray();
                        return buf;
                    }
                    throw new Exception("no data");
                }
            }
        }

        async public Task<ResolveRes> Resolve(Message msg)
        {
            var arr = msg.ToByteArray();
            var res = await resolveInt(NextServ, arr);
            return new(res);
        }

        //async Task<Response> resolveInt(string server, IEnumerable<DnsElem> query)
        //{
        //    var req = new Request();
        //    foreach (var elem in query) {
        //        req.Questions.Add(new Question(Domain.FromString(elem.data), elem.type, elem.cls));
        //    }
        //    return await resolveInt(server, req);
        //}
    }
}
