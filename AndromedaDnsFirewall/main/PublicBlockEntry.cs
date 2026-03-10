using AndromedaDnsFirewall.gui;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
namespace AndromedaDnsFirewall;


internal partial class PublicBlockEntry : ViewModelBase {

	// Универсальный метод для обновления значения
	protected bool SetPropertySaveConf<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) {
		var res = SetProperty(ref storage, value, propertyName);
		if (res) {
			Config.NeedSave();
		}
		return res;
	}

	public int UpdateHour { get; set; } = 5;
	public string Url { get => field; set => SetPropertySaveConf(ref field, value); } = "URL";
	public bool Enabled { get => field; set { SetPropertySaveConf(ref field, value); UpdateReload(); } }
	[JsonIgnore] public int Count { get => field; set => SetProperty(ref field, value); }
	[JsonIgnore] public string LastUpdated { get => field; set => SetProperty(ref field, value); }



}
