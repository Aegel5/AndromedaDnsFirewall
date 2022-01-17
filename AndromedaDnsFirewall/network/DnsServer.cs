using AndromedaDnsFirewall.Utils;
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
        UdpClient answener;

        async public void Start()
        {

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53);
            listener = new UdpClient(endPoint);
            answener = new UdpClient();

            const int SIO_UDP_CONNRESET = -1744830452;
            listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            //listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            while (true)
            {
                try
                {
                    var client = await listener.ReceiveAsync();

                    var buffer = client.Buffer;

                    Request req = Request.FromArray(buffer);
                    //var msg = DnsMessage.Parse(buffer);

                    var item = new ServerItem()
                    {
                        endPoint = client.RemoteEndPoint,
                        request = req
                    };

                    ProcessRequest(item);
                }
                catch(Exception ex)
                {
                    Log.Err(ex);
                    await Task.Delay(1.sec());
                }
            }
        }

        public Action<ServerItem> ProcessRequest;

        async public void CompleteRequest(ServerItem req)
        {
            if (req.rawanwer != null)
            {
                await answener.SendAsync(req.rawanwer, req.endPoint);
            }
            else
            {
                var answ = req.answer;
                var arr = answ.ToArray();
                await answener.SendAsync(arr, answ.Size, req.endPoint);
            }
        }

    }
    class ServerItem
    {
        public IPEndPoint endPoint { get; init; }

        public Request request { get; init; }

        public Response answer { get; set; }

        public byte[] rawanwer;

        //public string[] names { get; init; }

        //public Dictionary<string, string> answer;
    }
}
