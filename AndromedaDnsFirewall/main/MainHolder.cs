using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.Utils;
using Makaretu.Dns;
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
        AllowWhiteList,
        AllowExceptBlackList,
        AllowAll,
        SimpleBypass

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
        public WorkMode mode = WorkMode.AllowAll;

        public HashSet<DnsElem> whiteList = new();
        public HashSet<DnsElem> blackList = new();

        public StoreCache cache = new();

    }

    internal class MainHolder
    {


        DnsResolver resolver = new();
        DnsServer server = new();
        public Storage storage { get; private set; } =  new();

        public List<LogItem> logLst = new();

        async void ProcessRequest(ServerItem dnsItem)
        {
            try
            {
                //if (dnsItem.request.Questions[0].Name.ToString() == "www.bing.com")
                //{
                //    var rem = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
                //    //var my = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54343);
                //    //var listener = new UdpClient(endPoint);
                //    var sender = new UdpClient(rem.AddressFamily);

                //    await sender.SendAsync(dnsItem.request.ToArray(), dnsItem.request.Size, rem);

                //    var result = await sender.ReceiveAsync();

                //    var data = result.Buffer;


                //    var msg = new Message();
                //    msg.Read(data);

                //    var buf3 = msg.ToByteArray();

                //    //var buf2 = obj.

                //    dnsItem.rawanwer = buf3;
                //    server.CompleteRequest(dnsItem);
                //    return;

                //    var otherres = Response.FromArray(result.Buffer);
                //    var buf2 = otherres.ToArray();

                //    dnsItem.answer = otherres;
                //    server.CompleteRequest(dnsItem);

                //    return;
                //}

                //SortedSet<RecordType>

                if(storage.mode == WorkMode.SimpleBypass)
                {
                    // simple bypass request
                    dnsItem.answ = await resolver.ResolveBypass(dnsItem.req);
                    server.CompleteRequest(dnsItem);
                    return;
                }

                var req = new Message();
                req.Read(dnsItem.req);
                var cur = new List<LogItem>();
                var newQ = new List<Question>();
                foreach (var quest in req.Questions)
                {
                    var name = quest.Name.ToString();
                    var dnsElem = new DnsElem(quest.Type, quest.Class, name);
                    var logitem = new LogItem(LogType.Blocked, dnsElem);

                    if (storage.mode == WorkMode.AllowAll)
                    {
                        logitem = logitem with { type = LogType.Allowed };
                    }
                    else if (storage.mode == WorkMode.AllowWhiteList)
                    {
                        if (storage.whiteList.Contains(dnsElem))
                        {
                            logitem = logitem with { type = LogType.Allowed };
                        }
                    }
                    else if (storage.mode == WorkMode.AllowExceptBlackList)
                    {
                        if (!storage.blackList.Contains(dnsElem))
                        {
                            logitem = logitem with { type = LogType.Allowed };
                        }
                    }

                    if(logitem.type == LogType.Allowed)
                    {
                        newQ.Add(quest);
                    }

                    cur.Add(logitem);
                }

                req.Questions.Clear();
                foreach (var elem in newQ)
                    req.Questions.Add(elem);

                foreach (var elem in cur)
                {
                    Log.Info($"New log entry: {elem}");
                }

                if (req.Questions.Any())
                {
                    dnsItem.answ = (await resolver.Resolve(req)).Buff;
                }
                else
                {
                    Message msg = new();
                    msg.Id = req.Id;
                    msg.Status = MessageStatus.Refused;
                    dnsItem.answ = msg.ToByteArray();
                }

                server.CompleteRequest(dnsItem);
            }
            catch(Exception ex)
            {
                Log.Err(ex);
            }
        }


        public void Init()
        {
            //resolver.cache = st
            server.ProcessRequest = ProcessRequest;
            server.Start();

            Log.Info("program started");
        }
    }
}
