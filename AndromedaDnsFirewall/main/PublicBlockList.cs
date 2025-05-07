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
	static IEnumerable<PublicBlockEntry> records => Config.Inst.PublicBlockLists;

    static public bool AllLoaded => records.All(x => !x.Enabled || x.Loaded);

    static async void Update() {

        while (!GlobalData.QuitPending) {
			foreach (var elem in records) {
				await elem.UpdateReload(); // todo parallel
			}
            await Task.Delay(1.min());
        }

    }

    static public void Init() {
        Update();
    }

    static public bool IsNeedBlock(string name) {
        return records.Any(x => x.IsNeedBlock(name));
    }

}
