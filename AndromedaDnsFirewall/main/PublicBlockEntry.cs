using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AndromedaDnsFirewall; 
internal partial class PublicBlockEntry : ObservableObject {

	public PublicBlockEntry(string url) {
		_url = url;
	}

	public int UpdateHour { get; set; } = 5;

	string _url;
	public string Url {
		get => _url;
		set {
			SetProperty(ref _url, value);
			Config.NeedSave();
		}
	}

	bool _enabled = true;
	public bool Enabled {
		get => _enabled;
		set {
			SetProperty(ref _enabled, value);
			Config.NeedSave();
		}
	}

	[ObservableProperty][property: JsonIgnore] public int count;

	[ObservableProperty][property: JsonIgnore] public int lastUpdHour = -1;

	public TimePoint dtLastLoad;
	TimePoint try_load = TimePoint.MaxValue;
	public bool Loaded => dtLastLoad != default;

}
