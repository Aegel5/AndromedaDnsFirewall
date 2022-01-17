using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
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

    internal class DnsResolver
    {

        HttpClient httpClient;



        public DnsResolver()
        {
            httpClient = new() { Timeout = 5.sec() };
        }

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



        async public Task<LazyMessage> Resolve(LazyMessage msg)
        {
            var res = await resolveInt(NextServ, msg.BuffGet);
            return new() { buf = res };
        }


    }
}
