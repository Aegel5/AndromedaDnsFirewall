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

        public List<string> Servers = new()
        {
            "https://1.0.0.1/dns-query",
            "https://1.1.1.1/dns-query",
            "https://8.8.4.4/dns-query",
            "https://8.8.8.8/dns-query",
        };
    }
}
