using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class CDevice
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string code { get; set; }

        [JsonProperty("Serial")]
        public string Serial { get; set; }

        [JsonProperty("Infomation")]
        public string Infomation { get; set; }

        [JsonProperty("software")]
        public CSoftware software{ get; set; }

        [JsonProperty("MFG")]
        public DateTime MFG { get; set; }// Manufacturing Date

        [JsonProperty("EXP")]
        public DateTime EXP { get; set; }// Expiry date

        [JsonProperty("TieuChuan")]
        public string TieuChuan { get; set; }

        [JsonProperty("Customer")]
        public string Customer { get; set; }

        public CDevice()
        {
            Name = "E2";
            code = "000";
            Serial = "0000000000";
            Infomation = "AIoT JSC Create.";
            software = new CSoftware();
            MFG = DateTime.Now;
            EXP = DateTime.Now;
            TieuChuan = "No";
            Customer = "AIoT JSC.";            
        }
        public CDevice(CDevice device)
        {
            Name = device.Name;
            code = device.code;
            Serial = device.Serial;
            Infomation = device.Infomation;
            software = new CSoftware(device.software);
            MFG = device.MFG;
            EXP = device.EXP;
            TieuChuan = device.TieuChuan;
            Customer = device.Customer;
        }
    }
}
