using AndromedaDnsFirewall.dns_server;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Makaretu.Dns;
using Nito.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

class CacheItem
{
    public string Name;
    public string[] ip;
    DateTime dtLastUse;
    //int next;
}

record LogItem 
{
    public LogType type;
    public DnsElem elem;
    public int count  = 1;

	static IImmutableSolidColorBrush c_block1 = new ImmutableSolidColorBrush(Color.FromRgb(100,0,0));

	public IBrush? Background {
		get {
			return type switch { 
				LogType.BlockedByPublicList => Brushes.DarkOrchid, 
				LogType.BlockedByUserList => Brushes.Cornsilk,
				LogType.AllowedByUserList => Brushes.GreenYellow,
				LogType.Allow_PublicBlockListNotReady => Brushes.Gray,
				_ => default
			};
		}
	}

	public override string ToString() {
		return $"{type} {elem} count={count}";
	}

}

enum LogType
{
    Blocked,
    Allowed,
    BlockedByUserList,
    AllowedByUserList,
    Error,
    BlockedByPublicList,
    Allow_PublicBlockListNotReady
}

//record RuleRec (string name, bool block);

enum WorkMode
{
    OnlyWhiteList = 0,
    AllExceptBlockList = 1,
    AllowAll = 2
    //AllowAllWitoutLogs

}

class StoreCache
{
    public Dictionary<string, CacheItem> cacheLst = new();
}

record DnsElem(DnsType type, DnsClass cls, string data)
{
}

internal class MainHolder {
	public static MainHolder Inst;
	public StoreCache cache = new();
	DnsServer server = new();

	public MainHolder() {
		Inst = this;
	}

	public static void Create() {
		if (Inst == null) { Inst = new(); }
	}

	public long logChangeId { get; private set; }

	public ObservableCollection<LogItem> logLst = new(); // todo observable deque

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
						return LogType.Allow_PublicBlockListNotReady;
					return LogType.Allowed;
				}

				logitem = new LogItem { type = calcLogType(), elem = dnsElem };

				if (logitem.type == LogType.BlockedByPublicList || logitem.type == LogType.BlockedByUserList) {
					req.Questions.RemoveAt(i);
					wasRemoved = true;
				}

				if (logLst.Any()) {
					var first = logLst[0];
					if (first with { count = 1 } == logitem) {
						first.count += 1;
						logitem = first;
					} else {
						logLst.Insert(0,logitem);
					}
				} else {
					logLst.Insert(0, logitem);
				}
				logChangeId++;

				Log.Info($"New log entry: {logitem}");

				while (logLst.Count > 250) {
					logLst.RemoveLast();
				}
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
