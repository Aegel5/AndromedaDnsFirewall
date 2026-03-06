using System.Net.Http;

namespace AndromedaDnsFirewall.network {
	static class HttpClientHolder {
		public static HttpClient Inst = new() { Timeout = 30.sec() };
	}

}
