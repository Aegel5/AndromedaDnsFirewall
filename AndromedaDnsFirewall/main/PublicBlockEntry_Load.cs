using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AndromedaDnsFirewall.dns_server;
using Avalonia.Input;

namespace AndromedaDnsFirewall; 
internal partial class PublicBlockEntry {

	public HashSet<string> cache = new();

	static int GetStableHashCode(string str) {
		unchecked {
			int hash1 = 5381;
			int hash2 = hash1;

			for (int i = 0; i < str.Length && str[i] != '\0'; i += 2) {
				hash1 = ((hash1 << 5) + hash1) ^ str[i];
				if (i == str.Length - 1 || str[i + 1] == '\0')
					break;
				hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}
	static string folder => ProgramUtils.BinFolder + "/PublicBlockLists/";

	string path => folder + Math.Abs(GetStableHashCode(Url));

	static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken) {

		// use our own resolver!
		var ip = await DnsResolver.Inst.Resolve(context.DnsEndPoint.Host);
		var endPoint = new DnsEndPoint(ip.ToString(), context.DnsEndPoint.Port);

		var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		await socket.ConnectAsync(endPoint, cancellationToken);
		return new NetworkStream(socket, ownsSocket: true);
	}

	HttpClient httpClient = new HttpClient(new SocketsHttpHandler { ConnectCallback = ConnectCallback }) { Timeout = 20.sec() };

	void apply(string cont) { // no await
		cache.Clear();
		var lines = cont.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines) {
			if (line.StartsWith('#'))
				continue;
			cache.Add(line.Trim());
		}
		DtLastLoad = TimePoint.Now;
	}

	async Task LoadFromUrl() {
		try {

			var resp = await httpClient.GetAsync(Url);
			resp.EnsureSuccessStatusCode();
			using var cont = resp.Content;
			var res = await cont.ReadAsStringAsync();

			apply(res);

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
	}

	async Task LoadFromFile() {
		try {
			// если после рестарта случилась ошибка, возьмем последнее сохранение.
			var cont = await Task.Run(() => {

				if (!File.Exists(path))
					return null;
				return File.ReadAllText(path);
			});
			if (cont != null) {
				apply(cont);
			}
		} catch (Exception ex) {
			Log.Err(ex);
		}
	}
	bool ok => DtLastLoad.DeltToNow.TotalHours <= updateHour;
	void tryafter(TimeSpan ts) {
		DtLastLoad = TimePoint.Now.Add(-(TimeSpan.FromHours(updateHour) + ts));
	}
	public async Task Reload() {

		if (ok) return;

		await LoadFromUrl();

		if (ok) return;

		if(!Loaded) {
			// нет никаких данных вообще!
			await LoadFromFile();
		} 

		if(!Loaded) {
			tryafter(10.sec()); // все еще пусто!
		} else {
			tryafter(10.min()); // есть старая версия, но пробуем url
		}

	}

	public bool IsNeedBlock(string name) {
		if (!Enabled) return false;
		return cache.Contains(name);
	}

}
