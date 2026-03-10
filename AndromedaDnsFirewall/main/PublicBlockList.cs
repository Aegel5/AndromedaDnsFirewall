using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

internal class PublicBlockList {
	static IEnumerable<PublicBlockEntry> Records => Config.Inst.PublicBlockLists;

	static public bool AllRecordsOk => Records.All(x => !x.Enabled || x.Inited);

	static async void Update() {
		while (!GlobalData.QuitPending) {
			await Task.WhenAll(Records.Select(x => x.UpdateReload()));
			await Task.Delay(10.sec());
		}

	}

	static public void Init() {
		// пулим загрузку списков
		Update();
	}

	static public bool IsNeedBlock(string name) {
		return Records.Any(x => x.IsNeedBlock(name));
	}

}
