using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{

    internal enum BlockListType {
        RawString = 0,
        //WildString = 1,
        //RegularString = 2
    }

    record PublicBlockListRec(string url, double updateHour, BlockListType type);

    class Config
    {
        public static Config Inst { get; private set; }
        static readonly string path = $"{ProgramUtils.BinFolder}/config.json";
        static public void Load()
        {
            var cont = File.ReadAllText(path); // blocks this thread!
            Inst = JsonSerializer.Deserialize<Config>(cont, 
                new JsonSerializerOptions { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip});
        }

        public List<PublicBlockListRec> PublicBlockLists { get; set; } = new();
        public List<string> DnsResolvers { get; set; }
        public string ServerAddress { get; set; } = "127.0.0.1:53";
    }
}
