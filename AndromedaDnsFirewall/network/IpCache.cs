using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace AndromedaDnsFirewall.network; 
internal class IpCache {

	Deque<(string s, TimePoint t)> items = new();
	Dictionary<string, byte[]> dict = new();

	void DelOld() {
		while (items.Count > 0 && (items.Count > 10000 || items[0].t.DeltToNow > 120.min())) {
			var it = items.PopFront();
			if (!dict.Remove(it.s)) {
				throw new Exception("bad implementation");
			}
		}
		if (items.Count != dict.Count) 
			throw new Exception("bad implementation");
	}

	public void Add(string host, byte[] arr) {
		if (arr.Length == 0) throw new Exception("must be > 0");
		DelOld();
		ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, host, out bool exists);
		entry = arr;
		if (exists) {
			// так как мы проверяем кеш перед обращением, то сюда мы можем попасть только из-за await и одновременных запросов по одному и тоже же хосту
			// это крайне редко и не критично
			int k = 0;
		} else {
			items.Add((host, TimePoint.Now));
		}
	}
	public byte[]? Get(string host) {
		DelOld();
		dict.TryGetValue(host, out var arr);
		return arr;
	}
}
