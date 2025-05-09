using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;
internal enum RuleBlockType {
	Null = 0,
	Block,
	Allow
}
internal class UserLists {

	static readonly string path = $"{ProgramUtils.BinFolder}/user_list.json";

	

	public static void Save() {
		string jsonString = JsonSerializer.Serialize(list);
		File.WriteAllText(path, jsonString);// blocks this thread!
		return;

	}
	public static void Load() {
		if (!File.Exists(path)) return;
		var jsonString = File.ReadAllText(path);
		list = JsonSerializer.Deserialize<Dictionary<string, RuleBlockType>>(jsonString);
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
