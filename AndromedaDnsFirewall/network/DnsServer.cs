using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.dns_server {

	internal class DnsServer {
		UdpClient? listener;
		//UdpClient answener;

		async public void Start() {
			listener = new UdpClient(IPEndPoint.Parse(Config.Inst.ServerAddress));
			//listener.Client.SendTimeout = 3000;
			//answener = new UdpClient();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				const int SIO_UDP_CONNRESET = -1744830452;
				listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
			}
			//listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			while (!GlobalData.QuitPending) {
				try {
					var client = await listener.ReceiveAsync();

					var buffer = client.Buffer;

					var item = new ServerItem() {
						endPoint = client.RemoteEndPoint,
						req = buffer
					};

					ProcessRequest(item);
				} catch (Exception ex) {
					Log.Err(ex);
					await Task.Delay(1.sec());
				}
			}
		}

		required public Action<ServerItem> ProcessRequest { get; init; }

		public ValueTask<int> CompleteRequest(ServerItem req) {
			return listener.SendAsync(req.answ, req.endPoint);
		}
	}
	class ServerItem {
		required public IPEndPoint endPoint { get; init; }
		required public byte[] req { get; init; }
		public byte[]? answ;
	}
}
