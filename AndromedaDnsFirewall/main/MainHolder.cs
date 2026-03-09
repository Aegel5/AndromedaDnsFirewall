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
		LogItem logitem_last = null;
		int logitem_indexLast = 0;

		try {

			if (Config.Inst.mode == WorkMode.AllowAll) {
				// simple bypass request
				dnsItem.answ = await DnsResolver.Inst.RawResolveBypass(dnsItem.req);
				return;
			}

			var lazy_req = new LazyMessage(dnsItem.req);
			reqId = lazy_req.Msg.Id;
			bool wasEdited = false;

			for (int iQuest = lazy_req.Msg.Questions.Count - 1; iQuest >= 0; iQuest--) {
				var quest = lazy_req.Msg.Questions[iQuest];
				//fortest = quest;
				var name = quest.Name.ToCanonical().ToString();

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

				logitem_last = new LogItem(name, quest.Type, quest.Class, calcLogType());

				if (logitem_last.type
					is LogType.BlockedByPublicList
					or LogType.BlockedByUserList
					or LogType.Block_PublicBlockListNotReady
					) {
					lazy_req.Msg.Questions.RemoveAt(iQuest);
					wasEdited = true;
				}

				bool edited = false;

				for (int iLog = 0; iLog < Math.Min(6, logSource.Count); iLog++) {
					var cur = logSource[iLog];
					if (iLog > 0 && (logitem_last.dt - cur.dt).Duration().TotalSeconds > 10) 
						break;
					if (cur.IsSame(logitem_last)) {
						cur.count++;
						cur.dt = logitem_last.dt;
						cur.UnionWith(logitem_last);
						logSource.NotifyUpdated(iLog);
						logitem_last = cur;
						edited = true;
						logitem_indexLast = iLog;
						break;
					}
				}

				if (!edited) {
					//for (int i = 0; i < logSource.Count; i++) {
					//	if (logitem.IsSame(logSource[i])) {
					//		int k = 0;
					//	}
					//}
					logSource.PushFrontNotify(logitem_last);
					while (logSource.Count > 150) {
						logSource.PopBackNofity();
					}
				}

				logChangeId++;

				Log.Info($"New log entry: {logitem_last}");
			}

			if (wasEdited) {
				lazy_req.ClearBuf(); // были изменения, буфер больше не валиден, нужно пересоздавать.
			}

			if (lazy_req.Msg.Questions.Any()) {
				var (lazyres, fromCache) = await DnsResolver.Inst.ResolveWithCache(lazy_req);
				if (fromCache && logitem_last != null) {
					// если from cache, значит await до сих пор не было, а значит индекс все еще валиден
					// не очень хорошо полагаться на это, но ладно.
					logitem_last.SetFromCache();
					logSource.NotifyUpdated(logitem_indexLast);
				}

				dnsItem.answ = lazyres.Buf;
			} else {
				Message msg = lazy_req.Msg.CreateResponse();
				msg.Status = MessageStatus.Refused; 
				//msg.Answers.Add(new ARecord() { Name = fortest.Name, Address = IPAddress.Parse("127.0.0.1"), TTL=7.sec() }); // todo
				dnsItem.answ = msg.ToByteArray();
			}
		} catch (Exception ex) {
			Log.Err(ex);
			if (logitem_last != null) {
				logitem_last.type = LogType.Exception;
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
