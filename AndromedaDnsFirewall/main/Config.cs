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

        enum BlockListType {
            RawString = 0,
            //WildString = 1,
            //RegularString = 2
        }

        record PublicBlockList(string url, double updateHour, BlockListType type);

        List<PublicBlockList> PublicBlockLists { get; set; } = new();
        public List<string> Servers { get; set; }
        public string ListenAddress { get; set; } = "127.0.0.1:53";
    }
}
