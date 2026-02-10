using System.Collections.Generic;

namespace ChatClientCommon
{
    public class ChatSettings
    {
        public string BackendUrl { get; set; }
        public string UserName { get; set; }

        public static List<string> DefaultAddresses => new List<string>
        {
            "https://chatserver-1-0-0.onrender.com/chatHub",
            "http://localhost:7777/chatHub",
        };
    }
}
