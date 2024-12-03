using System.Collections.Generic;
using System.Net;

namespace SharedResources
{
    public class WebRequestResponse
    {
        public string RequestUri { get; set; }
        public string ResponseBody { get; set; }
        public IDictionary<string, IEnumerable<string>> ResponseHeaders { get; set; }
        public byte[] ResponseBytes { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Reason { get; set; }
        public bool IsError { get; set; }
        public bool IsEncoded { get; set; }
    }
}
