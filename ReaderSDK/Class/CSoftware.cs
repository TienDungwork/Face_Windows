using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class CSoftware
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string version { get; set; }

        [JsonProperty("Infor")]
        public string Infor { get; set; }

        [JsonProperty("OS")]
        public string OS { get; set; }

        [JsonProperty("device")]
        public string device { get; set; }

        [JsonProperty("MFG")]
        public DateTime MFG { get; set; }// Manufacturing Date

        [JsonProperty("EXP")]
        public DateTime EXP { get; set; }// Expiry date

        [JsonProperty("DoiTacDatHang")]
        public string DoiTacDatHang { get; set; }

        [JsonProperty("createDate")]
        public DateTime createDate { get; set; }

        public CSoftware()
        {
            Name = "AIot Software";
            version = "V1.0";
            Infor = "AIoT JSC createed.";
            OS = "Windows 10 64 bit or higher.";
            device = "PC";
            MFG = DateTime.Now;
            EXP = DateTime.Now;
            DoiTacDatHang = "AIoT JSC";
        }
        public CSoftware(CSoftware software)
        {
            Name = software.Name;
            version = software.version;
            Infor = software.Infor;
            OS = software.OS;
            device = software.device;
            createDate = software.createDate;
            DoiTacDatHang = software.DoiTacDatHang;
        }
    }
}
