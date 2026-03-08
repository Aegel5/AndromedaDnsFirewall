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
			bool wasBlocked = false;

			Question fortest = null;

			for (int iQuest = req.Questions.Count - 1; iQuest >= 0; iQuest--) {
				var quest = req.Questions[iQuest];
				fortest = quest;
				var name = quest.Name.ToString();

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

				logitem = new LogItem(name, quest.Type, quest.Class) { type = calcLogType() };

				if (logitem.type
					is LogType.BlockedByPublicList
					or LogType.BlockedByUserList
					or LogType.Block_PublicBlockListNotReady
					) {
					req.Questions.RemoveAt(iQuest);
					wasBlocked = true;
				}

				bool edited = false;

				for (int iLogs = 0; iLogs < Math.Min(6, logSource.Count); iLogs++) {
					var cur = logSource[iLogs];
					if (iLogs > 0 && (logitem.dt - cur.dt).TotalSeconds > 10) 
						break;
					if (cur.IsSame(logitem)) {
						cur.count++;
						cur.dt = logitem.dt;
						cur.AddQuestInfo(logitem);
						if (iLogs == 0) {
							logSource.NotifyUpdated(0);
						} else{
							logSource.Move(iLogs, 0);
						} 
						edited = true;
						break;
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

			if (!wasBlocked) {
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
