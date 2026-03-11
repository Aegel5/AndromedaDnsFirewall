using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

internal partial class PublicBlockEntry {

	QuickHashType[] cache = [];

	static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken) {

		// use our own resolver!
		var ip = await DnsResolver.Inst.ResolveNoCache(context.DnsEndPoint.Host);
		var endPoint = new DnsEndPoint(ip.ToString(), context.DnsEndPoint.Port);

		var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		await socket.ConnectAsync(endPoint, cancellationToken);
		return new NetworkStream(socket, ownsSocket: true);
	}

	HttpClient httpClient = new HttpClient(new SocketsHttpHandler { ConnectCallback = ConnectCallback }) { Timeout = 20.sec() };


	void Apply(byte[] cont) { // no await

		List<QuickHashType> temp = new(8192);
		Utf8Utils.Split(cont, (byte)'\n', x => {
			if (x[0] == '#') return;
			temp.Add(HashUtils.QuickHash(x));
		});
		cache = temp.ToArray();
		Array.Sort(cache);

		//var ok = cache.AsEnumerable().Distinct().Count() == cache.Length;

		dtLastLoad = TimePoint.Now;
		Count = cache.Length;
	}

	public async Task LoadFromUrl() {
		try {

			var resp = await httpClient.GetAsync(Url);
			resp.EnsureSuccessStatusCode();
			using var cont = resp.Content;
			var res = await cont.ReadAsByteArrayAsync();
			Apply(res);

		} catch (Exception ex) {
			Log.Err(ex);
		}
	}

	TimePoint dtLastLoad {
		get => field;
		set {
			field = value;
			UpdateH();
		}
	}
	TimePoint stopLoadingUntil;
	bool IsCacheActual => dtLastLoad.DeltToNow.TotalHours <= UpdateHour;
	public bool Inited => dtLastLoad != default;

	void UpdateH() {
		LastUpdated = dtLastLoad == default ? "never" : $"{(int)dtLastLoad.DeltToNow.TotalHours} hours ago";
	}

	void Clear() {
		cache = [];
		Count = 0;
		dtLastLoad = default;
	}
	ACounter reload = new();
	void UpdateLabel() {
	}
	public async Task UpdateReload() {

		try {

			if (reload.IsTaked) return;
			using var loc = reload.Take();

			UpdateH();

			if (!Enabled) {
				Clear();
				return;
			}

			if (stopLoadingUntil > TimePoint.Now)
				return;

			if (IsCacheActual) return;

			for (int i = 0; i < 3; i++) {
				await LoadFromUrl();
				if (IsCacheActual) return;
				if (Inited) break;
			}

			// произошла ошибка

			if (!Inited) {
				// пробуем через 5 секунд
				stopLoadingUntil = TimePoint.Now.Add(10.sec());
			} else {
				// есть старая версия, поэтому задержка больше.
				stopLoadingUntil = TimePoint.Now.Add(10.min());
			}
		} catch (Exception ex) {
			Log.Err(ex);
		}
	}

	public bool IsNeedBlock(string name) {
		if (!Enabled) return false;
		var key = HashUtils.QuickHash(name.ToUtf8());
		return HashUtils.UltraFastSearch(cache, key) >= 0;
		//return cache.BinarySearch(key) >= 0;
	}

}
