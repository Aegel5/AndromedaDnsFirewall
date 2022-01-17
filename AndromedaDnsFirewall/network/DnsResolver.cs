using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using DNS.Protocol;
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

        async public Task<Response> resolveInt(string server, Request req)
        {
            var url = $"{server}";
            var msg = new HttpRequestMessage(new HttpMethod("POST"), url);
            msg.Content = new ByteArrayContent(req.ToArray());
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
            using var resp = await httpClient.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            using var cont = resp.Content;
            var res = await cont.ReadAsByteArrayAsync();
            var answ = Response.FromArray(res);
            return answ;
        }

        //async public Task<Response> Resolve(IEnumerable<DnsElem> query)
        //{
        //    return await resolveInt(Config.Servers[0], query);
        //}

        async public Task<Response> ResolveBypass(Request query)
        {
            return await resolveInt(Config.Servers[3], query);
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
