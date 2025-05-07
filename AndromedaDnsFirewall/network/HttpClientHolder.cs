using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.network
{
    static class HttpClientHolder {
        public static HttpClient Inst = new() { Timeout = 30.sec() };
    }

}
