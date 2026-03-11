using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AndromedaDnsFirewall;

internal enum RuleBlockType {
	Null = 0,
	Block,
	Allow
}
internal static class UserLists {

	static readonly string path = Path.Combine(ProgramUtils.BinFolder, "user_list.json");

	public static void Save() {
		using var stream = File.Create(path);
		JsonSerializer.Serialize(stream, list); // blocks this thread!
	}
	public static void Load() {
		if (!File.Exists(path)) return;
		using var stream = File.OpenRead(path);
		var res = JsonSerializer.Deserialize<Dictionary<string, RuleBlockType>>(stream);
		if (res != null)
			list = res;
	}

	public static void Delete(string host) {
		list.Remove(host);
		Save();
	}

	public static void Block(string host) {
		list[host] = RuleBlockType.Block;
		Save();
	}

	public static void Allow(string host) {
		list[host] = RuleBlockType.Allow;
		Save();
	}

	static public Dictionary<string, RuleBlockType> list = new();

}
