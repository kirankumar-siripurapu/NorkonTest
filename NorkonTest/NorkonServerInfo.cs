using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NorkonTest
{
    class NorkonServerInfo
    {
        [JsonProperty("Connections")]
        public int totalConnected { get; set; }
        [JsonProperty("uptime")]
        public int uptime { get; set; }

        [JsonProperty("frag Updtes")]
        public int frag { get; set; }

        [JsonProperty("http request Count")]
        public int httpRecCount { get; set; }

        [JsonProperty("fallback ready")]
        public bool fallbackReady { get; set; }

        [JsonProperty("serverName")]
        public string serverName { get; set; }
    }
}
