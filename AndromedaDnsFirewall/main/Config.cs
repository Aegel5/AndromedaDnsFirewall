using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

class Config {
	public static Config Inst { get; private set; } = new();
	static readonly string path = $"{ProgramUtils.BinFolder}/config.json";

	static public void Init() {

		SaveLoop();

		if (!File.Exists(path)) {
			return;
		}
		var cont = File.ReadAllText(path); // blocks this thread!
		Inst = JsonSerializer.Deserialize<Config>(cont,
			new JsonSerializerOptions { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip });


	}

	async static void SaveLoop() {
		while (!GlobalData.QuitPending) {
			if (needSave) {
				needSave = false;
				Save();
			}
			await Task.Delay(5000);
		}
	}


	static bool needSave = false;
	public static void NeedSave() {
		needSave = true;
	}

	static JsonSerializerOptions opt = new JsonSerializerOptions() {
		//IncludeFields = true,
		IgnoreReadOnlyFields = true,
		IgnoreReadOnlyProperties = true,
		WriteIndented = true
	};

	static void Save() {

		var cont = JsonSerializer.Serialize(Inst, opt);
		File.WriteAllText(path, cont);// blocks this thread!
	}

	public ObservableCollection<PublicBlockEntry> PublicBlockLists { get; set; } = [
		new PublicBlockEntry("https://raw.githubusercontent.com/hagezi/dns-blocklists/main/domains/pro.txt")
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
