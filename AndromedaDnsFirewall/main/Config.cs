using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

internal enum BlockListType {
    RawString = 0,
    //WildString = 1,
    //RegularString = 2
}

record PublicBlockListRec(string url, double updateHour, BlockListType type);

class Config {
	public static Config Inst { get; private set; }
	static readonly string path = $"{ProgramUtils.BinFolder}/config.json";
	static public void Load() {
		if (!File.Exists(path)) {
			Inst = new();
			return;
		}
		var cont = File.ReadAllText(path); // blocks this thread!
		Inst = JsonSerializer.Deserialize<Config>(cont,
			new JsonSerializerOptions { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip });
	}

	static public void Save() {
		var cont = JsonSerializer.Serialize(Inst);
		File.WriteAllText(path, cont);// blocks this thread!
	}

	public List<PublicBlockListRec> PublicBlockLists { get; set; } = [
		new ("https://github.com/notracking/hosts-blocklists/raw/master/dnscrypt-proxy/dnscrypt-proxy.blacklist.txt", 5, BlockListType.RawString)
		];
	public bool useServers_DOH { get; set; } = true;
	public List<string> DnsResolvers_DOH { get; set; } = [
		"https://1.0.0.1/dns-query",
		"https://1.1.1.1/dns-query",
		"https://8.8.4.4/dns-query",
		"https://8.8.8.8/dns-query"
		];
	public bool useServers_UDP { get; set; } = false;
	public List<string> DnsResolvers_UDP { get; set; } = [
		"default_gateway",
		"1.0.0.1",
		"1.1.1.1",
		"8.8.4.4",
		"8.8.8.8"
		];
        public string ServerAddress { get; set; } = "127.0.0.1:53";

	public WorkMode mode { get; set; } = WorkMode.AllExceptBlockList;
	static public WorkMode Mode => Inst.mode;
}
