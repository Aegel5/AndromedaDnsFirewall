using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.network;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main {
    internal class PublicBlockListHolder {
        public static PublicBlockListHolder Inst { get; private set; }

        class HolderRec {
            public PublicBlockListRec descr;
            public HashSet<string> cache = new();

            public DateTime dtLastLoad;

            public bool loaded;

            string folder => ProgramUtils.BinFolder + "/PublicBlockLists/";
            string path => folder + Math.Abs(descr.url.GetHashCode());

            void apply(string cont) {
                cache.Clear();
                var lines = cont.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines) {
                    if (line.StartsWith('#'))
                        continue;
                    cache.Add(line.Trim());
                }
                loaded = true;
            }

            static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken) {

                // use our own resolver!
                var ip = await DnsResolver.Inst.Resolve(context.DnsEndPoint.Host);
                var endPoint = new DnsEndPoint(ip.ToString(), context.DnsEndPoint.Port);

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await socket.ConnectAsync(endPoint, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }

            HttpClient httpClient = new HttpClient(new SocketsHttpHandler { ConnectCallback = ConnectCallback }) { Timeout = 20.sec() };


            public async Task Reload() {
                bool curok = false;
                try {

                    var resp = await httpClient.GetAsync(descr.url);
                    resp.EnsureSuccessStatusCode();
                    using var cont = resp.Content;
                    var res = await cont.ReadAsStringAsync();

                    apply(res);
                    dtLastLoad = DateTime.Now;
                    curok = true;

                    // dump to file
                    var f = folder;
                    var p = path;
                    await Task.Run(() => {
                        Directory.CreateDirectory(f);
                        File.WriteAllText(p, res); // todo zip
                    });


                } catch (Exception ex) {
                    Log.Err(ex);
                }

                if(curok == false) {
                    tryafter(10.min());
                }

                if (loaded) {
                    return;
                }

                try {
                    // если после рестарта случилась ошибка, возьмем последнее сохранение.
                    var cont = await Task.Run(() => {

                        if (!File.Exists(path))
                            return null;
                        return File.ReadAllText(path); // TODO Do not use if too old
                    });
                    if (cont != null) {
                        apply(cont);
                    } 
                } catch (Exception ex) {
                    Log.Err(ex);
                }

                if (!loaded) {
                    tryafter(10.sec()); // we are do not have any data!!!
                }

            }

            void tryafter(TimeSpan ts) {
                dtLastLoad = DateTime.Now - TimeSpan.FromHours(descr.updateHour) + ts; 
            }

            public bool IsNeedBlock(string name) {
                return cache.Contains(name);
            }
        }

        List<HolderRec> lstHolders = new();

        public bool BlockListReady => lstHolders.All(x => x.loaded);

        async void Update() {

            while (!GlobalData.QuitPending) {
                foreach (var elem in lstHolders) {
                    if((DateTime.Now - elem.dtLastLoad).TotalHours >= elem.descr.updateHour) {
                        await elem.Reload(); // todo parallel
                    }
                }
                await Task.Delay(1.min());
            }

        }

        static public void Init() {

            Inst = new();
            foreach (var elem in Config.Inst.PublicBlockLists) {
                var rec = new HolderRec() { descr = elem };
                Inst.lstHolders.Add(rec);
            }

            Inst.Update();
        }

        public bool IsNeedBlock(string name) {
            return lstHolders.Any(x => x.IsNeedBlock(name));
        }

        public IEnumerable<string> ListForGui() {
            return lstHolders.SelectMany(x => x.cache).Take(500).Append("...");
        }
    }
}
