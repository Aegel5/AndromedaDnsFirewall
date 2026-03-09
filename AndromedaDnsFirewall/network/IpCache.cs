using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
//using CacheData = System.Collections.Generic.List<Makaretu.Dns.ResourceRecord>;
using CacheData = byte[];

namespace AndromedaDnsFirewall.network; 
internal class IpCache {

	Deque<((string, int) key, TimePoint time)> items = new();
	Dictionary<(string,int), CacheData> dict = new();

	void DelOld() {
		while (!items.IsEmpty && (items.Count > 5000 || items.Front.time.DeltToNow > Config.Inst.IpCacheTimeMinutes.min())) {
			var it = items.PopFront();
			if (!dict.Remove(it.key)) {
				throw new Exception("bad implementation");
			}
		}
		if (items.Count != dict.Count) 
			throw new Exception("bad implementation");
	}

	public void Clear() {
		items.Clear();
		dict.Clear();
	}

	public void Add(string host, int type, CacheData arr) {
		DelOld();
		ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, (host, type), out bool exists);
		entry = arr;
		if (exists) {
			// если запись существует, время будет старое, но
			// так как мы проверяем кеш перед обращением, то сюда мы можем попасть только из-за await и одновременных запросов по одному и тоже же хосту
			// поэтому не критично
			int k = 0;
		} else {
			items.Add(((host, type), TimePoint.Now));
		}
	}
	public CacheData? Get(string host, int type) {
		DelOld();
		dict.TryGetValue((host, type), out var arr);
		return arr;
	}
}
