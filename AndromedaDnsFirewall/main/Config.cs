using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
    internal static class Config
    {
        public static void Load()
        {

        }

        public static bool UseCache = false;

        public static bool LogEnable = true;

        public static List<string> Servers = new()
        {
            "https://1.0.0.1/dns-query",
            "https://1.1.1.1/dns-query",
            "https://8.8.4.4/dns-query",
            "https://8.8.8.8/dns-query",
        };
    }
}
