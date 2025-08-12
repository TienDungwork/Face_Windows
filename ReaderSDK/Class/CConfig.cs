using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class CConfig
    {
        [JsonProperty("AddressData")]
        public string AddressData { get; set; }

        [JsonProperty("AddressCamera")]
        public string AddressCamera { get; set; }
        public bool SaveData()
        {
            try
            {
                string SerializedJsonResult = JsonConvert.SerializeObject(this);

                string jsonpath = "config.json";
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
        public bool ReadConfig()
        {
            try
            {
                string jsonpath = "config.json";

                string json_string = File.ReadAllText(jsonpath);

                CConfig device = JsonConvert.DeserializeObject<CConfig>(json_string);
                AddressData = device.AddressData;
                AddressCamera = device.AddressCamera;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
