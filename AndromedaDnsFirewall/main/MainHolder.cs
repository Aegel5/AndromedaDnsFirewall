using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.Utils;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndromedaDnsFirewall;

class CacheItem {
	public string Name;
	public string[] ip;
	//DateTime dtLastUse;
	//int next;
}

record LogItem {
	public bool IsSame(LogItem other) {
		return type == other.type && elem == other.elem;
	}
	public LogType type;
	public DnsElem elem;
	public int count = 1;
	public DateTime dt;

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

	public override string ToString() {
		return $"{dt.ToLocalQuick()} {type} {(count==1?"":$"({count})")} {elem.data} {elem.type}";
	}

}

enum LogType {
	Blocked,
	Allowed,
	BlockedByUserList,
	AllowedByUserList,
	Error,
	BlockedByPublicList,
	Allow_PublicBlockListNotReady,
	Block_PublicBlockListNotReady
}

//record RuleRec (string name, bool block);

enum WorkMode {
	OnlyWhiteList = 0,
	AllExceptBlockList = 1,
	AllowAll = 2
	//AllowAllWitoutLogs

}

class StoreCache {
	public Dictionary<string, CacheItem> cacheLst = new();
}

record DnsElem(DnsType type, DnsClass cls, string data) {
}

internal class MainHolder {
	public static MainHolder Inst;
	public StoreCache cache = new();
	DnsServer server = new();

	public static void Create() {
		if (Inst == null) { Inst = new(); }
	}

	public long logChangeId { get; private set; }

	public ObservableDeque<LogItem> logSource = new();

	public MainHolder() {
		Inst = this;
	}

	async void ProcessRequest(ServerItem dnsItem) {

		ushort reqId = 0;
		LogItem logitem = null;

		try {

			if (Config.Inst.mode == WorkMode.AllowAll) {
				// simple bypass request
				dnsItem.answ = await DnsResolver.Inst.ResolveBypass(dnsItem.req);
				return;
			}

			var req = new Message();
			req.Read(dnsItem.req);
			reqId = req.Id;
			bool wasRemoved = false;

			Question fortest = null;

			for (int i = req.Questions.Count - 1; i >= 0; i--) {
				var quest = req.Questions[i];
				fortest = quest;
				var name = quest.Name.ToString();
				var dnsElem = new DnsElem(quest.Type, quest.Class, name);

				logitem = new LogItem { type = LogType.Blocked, elem = dnsElem };

				UserLists.list.TryGetValue(name, out RuleBlockType block);

				LogType calcLogType() {
					if (Config.Mode == WorkMode.AllowAll)
						return LogType.Allowed;
					if (block == RuleBlockType.Block)
						return LogType.BlockedByUserList;
					if (block == RuleBlockType.Allow)
						return LogType.AllowedByUserList;
					if (PublicBlockList.IsNeedBlock(name))
						return LogType.BlockedByPublicList;
					if (Config.Mode == WorkMode.OnlyWhiteList)
						return LogType.Blocked;

					if (!PublicBlockList.AllLoaded)
						return LogType.Block_PublicBlockListNotReady;

					return LogType.Allowed;
				}

				logitem = new LogItem { type = calcLogType(), elem = dnsElem, dt = DateTime.UtcNow };

				if (logitem.type
					is LogType.BlockedByPublicList
					or LogType.BlockedByUserList
					or LogType.Block_PublicBlockListNotReady
					) {
					req.Questions.RemoveAt(i);
					wasRemoved = true;
				}

				bool edited = false;

				if (logSource.Count != 0) {
					var first = logSource.Front;
					if (first.IsSame(logitem)) {
						first.count += 1;
						first.dt = DateTime.UtcNow;
						logitem = first;
						logSource.FrontUpdated();
						edited = true;
					}
				}

				if (!edited) {
					logSource.PushFront(logitem);
					while (logSource.Count > 150) {
						logSource.PopBack();
					}
				}

				logChangeId++;

				Log.Info($"New log entry: {logitem}");


			}

			LazyMessage lazy = new() { msg = req };

			if (!wasRemoved) {
				lazy.buf = dnsItem.req; // optimize serialize
			}


			if (req.Questions.Any()) {
				var lazyres = await DnsResolver.Inst.Resolve(lazy);
				dnsItem.answ = lazyres.BuffGet;
			} else {
				Message msg = new() { Id = reqId };
				msg.Status = MessageStatus.Refused; // nobody check this :(
													//msg.Answers.Add(new CNAMERecord() { Name = fortest.Name, Target = fortest.Name });
													//msg.Answers.Add(new ARecord() { Name = fortest.Name, Address = IPAddress.Parse("127.0.0.1"), TTL=7.sec() }); // todo
				dnsItem.answ = msg.ToByteArray();
			}


		} catch (Exception ex) {
			Log.Err(ex);
			if (logitem != null) {
				logitem.type = LogType.Error;
			}
		} finally {
			if (dnsItem.answ == null) {
				Message msg = new() { Id = reqId, Status = MessageStatus.ServerFailure };
				dnsItem.answ = msg.ToByteArray();
			}
			server.CompleteRequest(dnsItem);
		}

	}


	public void Init() {
		server.ProcessRequest = ProcessRequest;
		server.Start();
	}
}
