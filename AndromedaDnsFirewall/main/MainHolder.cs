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
        BlockedByRules,
        AllowedByRules,
        Error,
        BlockedByPublicList,
        PublicBlockListNotReady
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

    enum RuleBlockType {
        Null = 0,
        Block,
        Allow
    }

    // serialized to file

    internal class MainHolder
    {
        public StoreCache cache = new();


        public Dictionary<string, RuleBlockType> myRules = new();

        DnsServer server = new();

        public long logChangeId { get; private set; }

        public Deque<LogItem> logLst = new();

        async void ProcessRequest(ServerItem dnsItem) {

            ushort reqId = 0;
            LogItem logitem = null;

            try {

                if (!Quickst.Inst.LogEnable && !Quickst.Inst.UseCache && Quickst.Inst.mode == WorkMode.AllowAll) {
                    // simple bypass request
                    dnsItem.answ = await DnsResolver.Inst.ResolveBypass(dnsItem.req);
                    return;
                }

                var req = new Message();
                req.Read(dnsItem.req);
                reqId = req.Id;
                bool wasRemoved = false;

                //Question fortest = null;

                for (int i = req.Questions.Count - 1; i >= 0; i--) {
                    var quest = req.Questions[i];
                    //fortest = quest;
                    var name = quest.Name.ToString();
                    var dnsElem = new DnsElem(quest.Type, quest.Class, name);

                    logitem = new LogItem { type = LogType.Blocked, elem = dnsElem };

                    myRules.TryGetValue(name, out RuleBlockType block);

                    LogType calcLogType() {
                        if (Quickst.Mode == WorkMode.AllowAll)
                            return LogType.Allowed;
                        if (block == RuleBlockType.Block)
                            return LogType.BlockedByRules;
                        if (block == RuleBlockType.Allow)
                            return LogType.AllowedByRules;
                        if (!PublicBlockListHolder.Inst.BlockListReady)
                            return LogType.PublicBlockListNotReady;
                        if (PublicBlockListHolder.Inst.IsNeedBlock(name))
                            return LogType.BlockedByPublicList;
                        if (Quickst.Mode == WorkMode.OnlyWhiteList)
                            return LogType.Blocked;
                        return LogType.Allowed;
                    }

                    logitem = new LogItem { type = calcLogType(), elem = dnsElem };

                    if (logitem.type != LogType.Allowed && logitem.type != LogType.AllowedByRules) {
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
                    var lazyres = await DnsResolver.Inst.Resolve(lazy);
                    dnsItem.answ = lazyres.BuffGet;
                } else {
                    Message msg = new() { Id = reqId };
                    msg.Status = MessageStatus.Refused; // nobody check this :(
                    //msg.Answers.Add(new ARecord() { Name = fortest.Name, Address = IPAddress.Parse("0.0.0.0") });
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
                myRules.Add(elem.name, elem.block ? RuleBlockType.Block : RuleBlockType.Allow);
            }

            PublicBlockListHolder.Init();

            server.ProcessRequest = ProcessRequest;
            server.Start();
        }
    }
}
