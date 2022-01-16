using AndromedaDnsFirewall.Utils;
using ARSoft.Tools.Net.Dns;
using DNS.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.dns_server
{

    internal class DnsServer
    {
        UdpClient listener;
        
        async public void Start()
        {

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53);
            listener = new UdpClient(endPoint);

            while (true)
            {
                var client = await listener.ReceiveAsync();

                var buffer = client.Buffer;

                Request req = Request.FromArray(buffer);
                //var msg = DnsMessage.Parse(buffer);

                var item = new DnsItem()
                {
                    endPoint = client.RemoteEndPoint,
                    //names = msg.Questions.Where(x => x.RecordType == RecordType.A).Select(x => x.Name.ToString()).ToArray(),
                    request = req
                };

                

                ProcessRequest(item);
            }
        }

        public Action<DnsItem> ProcessRequest;

        async public void CompleteRequest(DnsItem req)
        {
            var answ = req.answer;
            await listener.SendAsync(answ.ToArray());
            //var msg = req.msg.CreateResponseInstance();
            //msg.AnswerRecords.Add(new )
            //req.answer.encode
            //listener.SendAsync
        }

    }
    class DnsItem
    {
        public IPEndPoint endPoint { get; init; }

        public Request request { get; init; }

        public Response answer { get; set; }

        //public string[] names { get; init; }

        //public Dictionary<string, string> answer;
    }
}
