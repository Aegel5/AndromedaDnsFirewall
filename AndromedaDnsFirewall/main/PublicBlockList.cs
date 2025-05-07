using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.network;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall; 
internal class PublicBlockList {
    public static PublicBlockList Inst { get; private set; }

	IEnumerable<PublicBlockEntry> records => Config.Inst.PublicBlockLists;

    public bool AllLoaded => records.All(x => x.Loaded);

    async void Update() {

        while (!GlobalData.QuitPending) {
			foreach (var elem in records) {
				await elem.Reload(); // todo parallel
			}
            await Task.Delay(10.min());
        }

    }

    static public void Init() {
        Inst.Update();
    }

    public bool IsNeedBlock(string name) {
        return records.Any(x => x.IsNeedBlock(name));
    }

}
