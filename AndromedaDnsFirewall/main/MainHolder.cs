using AndromedaDnsFirewall.dns_server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
    internal class MainHolder
    {
        DnsServer server = new();

        public void Init()
        {
            server.Start();
        }
    }
}
