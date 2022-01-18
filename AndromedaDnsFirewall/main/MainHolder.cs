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

    record LogItem (LogType type, DnsElem elem, int count)
    {
    }

    enum LogType
    {
        //Pending, // пока не поддерживаем.
        Blocked,
        Allowed,
        Error,
        //DenyNotSupported,
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
    class Storage
    {


        public HashSet<string> whiteList = new();
        public HashSet<string> blackList = new();

        public StoreCache cache = new();

    }

    internal class MainHolder
    {


        DnsResolver resolver = new();
        DnsServer server = new();
        public Storage storage { get; private set; } =  new();

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
                    logitem = new LogItem(LogType.Blocked, dnsElem, 1);

                    if (Quickst.Mode == WorkMode.AllowAll) {
                        logitem = logitem with { type = LogType.Allowed };
                    } else if (Quickst.Mode == WorkMode.OnlyWhileList) {
                        if (storage.whiteList.Contains(name)) {
                            logitem = logitem with { type = LogType.Allowed };
                        }
                    } else if (Quickst.Mode == WorkMode.AllExceptBlackList) {
                        if (!storage.blackList.Contains(name)) {
                            logitem = logitem with { type = LogType.Allowed };
                        }
                    }

                    if (logitem.type != LogType.Allowed) {
                        req.Questions.RemoveAt(i);
                        wasRemoved = true;
                    }

                    if (logLst.Any()) {
                        var first = logLst[0];
                        if (first with { count = 1 } == logitem) {
                            logitem = logitem with { count = first.count + 1 };
                            logLst[0] = logitem;
                        } else {
                            logLst.AddToFront(logitem);
                        }
                    } else {
                        logLst.AddToFront(logitem);
                    }

                    Log.Info($"New log entry: {logitem}");

                    while (logLst.Count > 1000) {
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
                    logitem = logitem with { type = LogType.Error };
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
            server.ProcessRequest = ProcessRequest;
            server.Start();
        }
    }
}
