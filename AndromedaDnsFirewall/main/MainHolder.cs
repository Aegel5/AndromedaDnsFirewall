using AndromedaDnsFirewall.dns_server;
using DNS.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
    class StoreItem
    {
        public string Name;
        public string[] ip;
        DateTime dtLastUse;
        int next;
    }

    class LogItem
    {
        public LogType type;
        public string name;
        public DnsItem dnsItem;
    }

    enum LogType
    {
        //Pending, // пока не поддерживаем.
        Blocked,
        Allowed,
        DenyNotSupported,
    }

    enum WorkMode
    {
        AllowWhiteList,
        AllowExceptBlackList,
        AllowAll
    }

    class StoreCache
    {
        public Dictionary<string, StoreItem> storeItems = new();
    }

    // serialized to file
    class Storage
    {
        public WorkMode mode = WorkMode.AllowAll;

        public HashSet<string> whiteList = new();
        public HashSet<string> blackList = new();

        public StoreCache cache = new();

    }

    internal class MainHolder
    {


        DnsResolver resolver = new();
        DnsServer server = new();

        Storage storage = new();

        public List<LogItem> logLst = new();

        async void ProcessRequest(DnsItem dnsItem)
        {
            var answ = Response.FromRequest(dnsItem.request);
            //var questions = dnsItem.request.Questions;
            foreach (var quest in dnsItem.request.Questions)
            {
                var logitem = new LogItem();
                logitem.name = quest.Name.ToString();

                if (quest.Type != DNS.Protocol.RecordType.A)
                {
                    logitem.type = LogType.DenyNotSupported;
                }
                else
                {
                    logitem.type = LogType.Blocked;
                    if(storage.mode == WorkMode.AllowAll)
                    {
                        logitem.type = LogType.Allowed;
                    }else if(storage.mode == WorkMode.AllowWhiteList)
                    {
                        if (storage.whiteList.Contains(logitem.name))
                        {
                            logitem.type = LogType.Allowed;
                        }
                    }else if(storage.mode == WorkMode.AllowExceptBlackList)
                    {
                        if (!storage.blackList.Contains(logitem.name))
                        {
                            logitem.type = LogType.Allowed;
                        }
                    }

                    var ip = "127.0.0.1";
                    if(logitem.type == LogType.Allowed)
                    {
                        ip = await resolver.Resolve(storage.cache, logitem.name);
                    }

                    //answ.AnswerRecords.Add(new Are)
                    //server.CompleteRequest();
                }
                logLst.Add(logitem);
            }

            dnsItem.answer = answ;

            server.CompleteRequest(dnsItem);
        }


        public void Init()
        {
            //resolver.cache = st
            server.ProcessRequest = ProcessRequest;
            server.Start();
        }
    }
}
