using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class CCamera
    {
        [JsonProperty("data")]
        public string data { get; set; }
    }
}
