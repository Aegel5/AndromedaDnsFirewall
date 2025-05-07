using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AndromedaDnsFirewall; 
internal partial class PublicBlockEntry : ObservableObject {

	public int updateHour = 5;

	[ObservableProperty] public string url;
	[ObservableProperty] public TimePoint dtLastLoad;
	[ObservableProperty] public bool enabled;
	public bool Loaded => DtLastLoad != default;

}
