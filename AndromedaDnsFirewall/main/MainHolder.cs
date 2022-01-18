using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.Utils;
using Makaretu.Dns;
using Nito.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
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

    }

    enum LogType
    {
        Blocked,
        Allowed,
        BlockedByBlackList,
        AllowedByWhiteList,
        Error,
    }

    enum WorkMode
    {
        OnlyWhileList = 0,
        AllExceptBlackList = 1,
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

    // serialized to file

    internal class MainHolder
    {
        public StoreCache cache = new();

        public HashSet<string> whiteList = new();
        public HashSet<string> blackList = new();

        internal DnsResolver resolver { get; private set; } = new();
        DnsServer server = new();

        public long logChangeId { get; private set; }

        public Deque<LogItem> logLst = new();

        async void ProcessRequest(ServerItem dnsItem) {

            ushort reqId = 0;
            LogItem logitem = null;

            try {

                if (!Quickst.Inst.LogEnable && !Quickst.Inst.UseCache && Quickst.Inst.mode == WorkMode.AllowAll) {
                    // simple bypass request
                    dnsItem.answ = await resolver.ResolveBypass(dnsItem.req);
                    return;
                }

                var req = new Message();
                req.Read(dnsItem.req);
                reqId = req.Id;
                bool wasRemoved = false;
                for (int i = req.Questions.Count - 1; i >= 0; i--) {
                    var quest = req.Questions[i];
                    var name = quest.Name.ToString();
                    var dnsElem = new DnsElem(quest.Type, quest.Class, name);

                    logitem = new LogItem { type = LogType.Blocked, elem = dnsElem };

                    if(Quickst.Mode != WorkMode.AllowAll && blackList.Contains(name)) {
                        logitem.type = LogType.BlockedByBlackList ;
                    } else {
                        if (whiteList.Contains(name)) {
                            logitem.type  = LogType.AllowedByWhiteList ;
                        } else {
                            if(Quickst.Mode == WorkMode.OnlyWhileList) {
                                logitem.type = LogType.Blocked ;
                            } else {
                                logitem.type = LogType.Allowed ;
                            }
                        }
                    }

                    //if (Quickst.Mode == WorkMode.AllowAll) {
                    //    logitem = logitem with { type = LogType.Allowed };
                    //} else if (Quickst.Mode == WorkMode.OnlyWhileList) {
                    //    if (whiteList.Contains(name)) {
                    //        logitem = logitem with { type = LogType.AllowedByWhiteList };
                    //    }
                    //} else if (Quickst.Mode == WorkMode.AllExceptBlackList) {
                    //    if (!blackList.Contains(name)) {
                    //        logitem = logitem with { type = LogType.BlockedByBlackList };
                    //    }
                    //}

                    if (logitem.type != LogType.Allowed && logitem.type != LogType.AllowedByWhiteList) {
                        req.Questions.RemoveAt(i);
                        wasRemoved = true;
                    }

                    if (logLst.Any()) {
                        var first = logLst[0];
                        if (first with { count = 1 } == logitem) {
                            first.count += 1;
                            logitem = first;
                        } else {
                            logLst.AddToFront(logitem);
                        }
                    } else {
                        logLst.AddToFront(logitem);
                    }
                    logChangeId++;

                    Log.Info($"New log entry: {logitem}");

                    while (logLst.Count > 500) {
                        logLst.RemoveFromBack();
                    }
                }

                LazyMessage lazy = new() { msg = req };

                if (!wasRemoved) {
                    lazy.buf = dnsItem.req; // optimize serialize
                }


                if (req.Questions.Any()) {
                    dnsItem.answ = (await resolver.Resolve(lazy)).BuffGet;
                } else {
                    Message msg = new() { Id = reqId, Status = MessageStatus.Refused };
                    dnsItem.answ = msg.ToByteArray();
                }

            } catch (Exception ex) {
                Log.Err(ex);
                if (logitem != null) {
                    logitem.type = LogType.Error ;
                }
            } finally {
                if (dnsItem.answ == null) {
                    Message msg = new() { Id = reqId, Status = MessageStatus.ServerFailure };
                    dnsItem.answ = msg.ToByteArray();
                }
                server.CompleteRequest(dnsItem);
            }

        }


        public void Init()
        {
            NameRulesStore.Inst.Init();

            foreach(var elem in NameRulesStore.Inst.Load()) {
                if (elem.block) {
                    blackList.Add(elem.name);
                } else {
                    whiteList.Add(elem.name);
                }
            }

            server.ProcessRequest = ProcessRequest;
            server.Start();
        }
    }
}
