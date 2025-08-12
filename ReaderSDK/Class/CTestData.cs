using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class CTestData
    {
        [JsonProperty("NguoiKT")]
        public string NguoiKT { get; set; }

        [JsonProperty("StartTime")]
        public string StartTime { get; set; }

        [JsonProperty("EndTime")]
        public string EndTime { get; set; }

        [JsonProperty("Notes")]
        public string Notes { get; set; }

        [JsonProperty("device")]
        public CDevice device { get; set; }

        [JsonProperty("software")]
        public CSoftware software { get; set; }

        [JsonProperty("deviceInfo")]
        public DeviceInfo deviceInfo { get; set; }

        [JsonProperty("items")]
        public List<CTestItem> items;

        public CTestData()
        {
            NguoiKT = "";
            StartTime = "";
            EndTime = "";
            Notes = "";
            device = new CDevice();
            software = new CSoftware();
            deviceInfo = new DeviceInfo();
            items = new List<CTestItem>();
        }

        public bool GetData()
        {
            return true;
        }
        public bool SaveData()
        {
            try
            {
                string SerializedJsonResult = JsonConvert.SerializeObject(this);

                string jsonpath = "TestData\\" + this.deviceInfo.serial_device + "_" + DateTime.Now.ToString("dd.MM.yyyy.HH.mm.ss") + ".json";
                if (File.Exists(jsonpath))
                {
                    File.Delete(jsonpath);
                    using (var st = new StreamWriter(jsonpath, true))
                    {
                        st.WriteLine(SerializedJsonResult.ToString());
                        st.Close();
                    }
                }
                else if (!File.Exists(jsonpath))
                {
                    using (var st = new StreamWriter(jsonpath, true))
                    {
                        st.WriteLine(SerializedJsonResult.ToString());
                        st.Close();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void Clear()
        {
            NguoiKT = "";
            StartTime = "";
            EndTime = "";
            Notes = "";
            device = new CDevice();
            software = new CSoftware();
            deviceInfo = new DeviceInfo();
            items.Clear();
        }
    }
}
