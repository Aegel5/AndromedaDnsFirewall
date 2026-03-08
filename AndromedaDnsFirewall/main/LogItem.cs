using Avalonia.Media;
using Avalonia.Media.Immutable;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Text;

namespace AndromedaDnsFirewall;

enum LogType {
	Blocked,
	Allowed,
	BlockedByUserList,
	AllowedByUserList,
	Exception,
	BlockedByPublicList,
	Allow_PublicBlockListNotReady,
	Block_PublicBlockListNotReady
}

record LogItem {
	public bool IsSame(LogItem other) {
		return type == other.type && host == other.host;
	}
	public void AddQuestInfo(LogItem other) {
		if (questInfos.Contains(other.questInfos[0])) return;
		questInfos.Add(other.questInfos[0]);
		questInfos.Sort();
	}

	public LogType type;
	public string host = "unknown";
	List<string> questInfos = new();
	public int count = 1;
	public DateTime dt;

	public LogItem(string host, DnsType t, DnsClass c, LogType type) {
		this.type = type;
		this.host = host;
		questInfos.Add(BuildQuestInfo(t, c));
		//dt = DateTime.UtcNow;
	}

	static string BuildQuestInfo(DnsType t, DnsClass c) {
		if (c == DnsClass.IN) return t.ToString();
		return $"{t}_{c}";
	}

	static IImmutableSolidColorBrush c_block1 = new ImmutableSolidColorBrush(Color.Parse("#7792d1"));
	static IImmutableSolidColorBrush c_block2 = new ImmutableSolidColorBrush(Color.Parse("#edcc9d"));

	public IBrush? Background {
		get {
			return type switch {
				LogType.BlockedByPublicList => c_block1,
				LogType.BlockedByUserList => c_block2,
				LogType.AllowedByUserList => Brushes.GreenYellow,
				LogType.Allow_PublicBlockListNotReady => Brushes.Gray,
				LogType.Block_PublicBlockListNotReady => Brushes.Gray,
				_ => default
			};
		}
	}

	string QuestsToString() {
		return string.Join(" / ", questInfos);
	}

	public override string ToString() {
		return $"{dt.ToLocalQuick()} {type} {(count == 1 ? "" : $"({count})")} {host} {QuestsToString()}";
	}

}


