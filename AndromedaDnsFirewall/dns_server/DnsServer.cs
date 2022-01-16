using AndromedaDnsFirewall.Utils;
using ARSoft.Tools.Net.Dns;
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
    static class TcpClientExt{
        public static bool IsConnected(this TcpClient client)
        {
            if (!client.Connected)
                return false;

            //if (client.Client.Poll(0, SelectMode.SelectRead))
            //{
            //    if (client.Connected)
            //    {
            //        byte[] b = new byte[1];
            //        try
            //        {
            //            if (client.Client.Receive(b, SocketFlags.Peek) == 0)
            //            {
            //                return false;
            //            }
            //        }
            //        catch
            //        {
            //            return false;
            //        }
            //    }
            //}

            return true;
        }
    }

    internal class DnsServer
    {

        internal static ushort ParseUShort(byte[] resultData, ref int currentPosition)
        {
            ushort res;

            if (BitConverter.IsLittleEndian)
            {
                res = (ushort)((resultData[currentPosition++] << 8) | resultData[currentPosition++]);
            }
            else
            {
                res = (ushort)(resultData[currentPosition++] | (resultData[currentPosition++] << 8));
            }

            return res;
        }

        private async Task<bool> TryReadAsync(TcpClient client, NetworkStream stream, byte[] buffer, int length, CancellationToken token)
        {
            int readBytes = 0;

            while (readBytes < length)
            {
                if (token.IsCancellationRequested || !client.IsConnected())
                    return false;

                readBytes += await stream.ReadAsync(buffer, readBytes, length - readBytes, token);
            }

            return true;
        }

        private async Task<byte[]?> ReadIntoBufferAsync(TcpClient client, NetworkStream stream, int count)
        {
            CancellationToken token = new CancellationTokenSource(10.sec()).Token;

            byte[] buffer = new byte[count];

            if (await TryReadAsync(client, stream, buffer, count, token))
                return buffer;

            return null;
        }
        async void ProcessClient(UdpReceiveResult client)
        {
            //using var stream = client.GetStream();
            //var buf = new byte[1024];
            //await stream.ReadAsync(buf, 0, buf.Length);

            var buffer = client.Buffer;

            //var buffer = await ReadIntoBufferAsync(client, stream, 2);
            //if (buffer == null) // client disconneted while reading or timeout
            //    return;

            //int offset = 0;
            //int length = ParseUShort(buffer, ref offset);

            //buffer = await ReadIntoBufferAsync(client, stream, length);
            //if (buffer == null) // client disconneted while reading or timeout
            //    return;

            var msg = DnsMessage.Parse(buffer);



        }
        async public void Start()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53);
            UdpClient listener = new UdpClient(endPoint);

            while (true)
            {
                var client = await listener.ReceiveAsync();

                ProcessClient(client); // no await, spawn new route!
            }
        }

        public Action<DnsRequest> ProcessRequest;

        async public void CompleteRequest(DnsRequest req)
        {

        }
    }
    record DnsRequest
    {

    }
}
