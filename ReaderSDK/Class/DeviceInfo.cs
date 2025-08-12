using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class DeviceInfo
    {
        [JsonProperty("version")]
        public string version { get; set; }

        [JsonProperty("serial_nfc")]
        public string serial_nfc { get; set; }

        [JsonProperty("serial_device")]
        public string serial_device { get; set; }

        [JsonProperty("date")]
        public string date { get; set; }
        [JsonProperty("version_api")]
        public string version_api { get; set; }
    }
}
