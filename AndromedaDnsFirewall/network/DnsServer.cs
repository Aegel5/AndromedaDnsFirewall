using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
            listener = new UdpClient(IPEndPoint.Parse(Config.Inst.ServerAddress));
            answener = new UdpClient();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }
            //listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            while (!GlobalData.QuitPending)
            {
                try
                {
                    var client = await listener.ReceiveAsync();

                    var buffer = client.Buffer;

                    var item = new ServerItem()
                    {
                        endPoint = client.RemoteEndPoint,
                        req = buffer
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
            await answener.SendAsync(req.answ, req.endPoint);
        }
    }
    class ServerItem
    {
        public IPEndPoint endPoint { get; init; }

        public byte[] req;
        public byte[] answ;
    }
}
