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

    record LogItem (LogType type, DnsElem elem)
    {
    }

    enum LogType
    {
        //Pending, // пока не поддерживаем.
        Blocked,
        Allowed,
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

        async void ProcessRequest(ServerItem dnsItem)
        {
            ushort reqId = 0;

            try
            {
                if (!Quickst.Inst.LogEnable && !Quickst.Inst.UseCache && Quickst.Inst.mode == WorkMode.AllowAll)
                {
                    // simple bypass request
                    dnsItem.answ = await resolver.ResolveBypass(dnsItem.req);
                    return;
                }

                var req = new Message();
                req.Read(dnsItem.req);
                reqId = req.Id;
                //var cur = new List<LogItem>();
                var newQ = new List<Question>();
                foreach (var quest in req.Questions)
                {
                    var name = quest.Name.ToString();
                    var dnsElem = new DnsElem(quest.Type, quest.Class, name);
                    var logitem = new LogItem(LogType.Blocked, dnsElem);

                    if (Quickst.Mode == WorkMode.AllowAll)
                    {
                        logitem = logitem with { type = LogType.Allowed };
                    }
                    else if (Quickst.Mode == WorkMode.OnlyWhileList)
                    {
                        if (storage.whiteList.Contains(name))
                        {
                            logitem = logitem with { type = LogType.Allowed };
                        }
                    }
                    else if (Quickst.Mode == WorkMode.AllExceptBlackList)
                    {
                        if (!storage.blackList.Contains(name))
                        {
                            logitem = logitem with { type = LogType.Allowed };
                        }
                    }

                    if (logitem.type == LogType.Allowed)
                    {
                        newQ.Add(quest);
                    }

                    logLst.AddToFront(logitem);
                    Log.Info($"New log entry: {logitem}");

                    while(logLst.Count > 1000)
                    {
                        logLst.RemoveFromBack();
                    }
                }

                LazyMessage lazy = new() { msg = req };

                if (req.Questions.Count == newQ.Count)
                {
                    lazy.buf = dnsItem.req; // optimize serialize
                }
                else
                {
                    req.Questions.Clear();
                    foreach (var elem in newQ)
                        req.Questions.Add(elem);
                }

                if (req.Questions.Any())
                {
                    dnsItem.answ = (await resolver.Resolve(lazy)).BuffGet;
                }
                else
                {
                    Message msg = new() { Id = reqId, Status = MessageStatus.Refused };
                    dnsItem.answ = msg.ToByteArray();
                }

            }
            catch (Exception ex)
            {
                Log.Err(ex);
            }
            finally
            {
                if(dnsItem.answ == null)
                {
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

            Log.Info("program started");
        }
    }
}
