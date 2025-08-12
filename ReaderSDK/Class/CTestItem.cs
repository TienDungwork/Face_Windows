using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class TimeInfo
    {
        public string strTime { get; set; }
        public long longTime { get; set; }
        public TimeInfo()
        {
            strTime = " ";
            longTime = 0;
        }
        public TimeInfo(TimeInfo time)
        {
            strTime = time.strTime;
            longTime = time.longTime;
        }
    }
    public class CTestItem
    {
        [JsonProperty("StartTime")]
        public TimeInfo StartTime { get; set; }

        [JsonProperty("DG13Time")]
        public long DG13Time { get; set; }

        [JsonProperty("OtherTime")]
        public long OtherTime { get; set; }

        [JsonProperty("AllTime")]
        public long AllTime { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("persion")]
        public CPersion persion { get; set; }

        public CTestItem()
        {
            StartTime = new TimeInfo();
            DG13Time = 0;
            OtherTime = 0;
            AllTime = 0;
            status = " ";
            persion = new CPersion();
        }
        public CTestItem(CTestItem item)
        {
            StartTime = new TimeInfo(item.StartTime);
            DG13Time = item.DG13Time;
            OtherTime = item.OtherTime;
            AllTime = item.AllTime;
            status = item.status;
            persion = new CPersion(item.persion);
        }
    }
}
