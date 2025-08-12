using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderSDK.Class
{
    public class CPersion
    {
        [JsonProperty("SoCCCD")]
        public string SoCCCD { get; set; }

        [JsonProperty("SoCMND")]
        public string SoCMND { get; set; }

        [JsonProperty("HoVaTen")]
        public string HoVaTen { get; set; }

        [JsonProperty("NgaySinh")]
        public string NgaySinh { get; set; }

        [JsonProperty("GioiTinh")]
        public string GioiTinh { get; set; }

        [JsonProperty("NgayCap")]
        public string NgayCap { get; set; }

        [JsonProperty("NgayHetHan")]
        public string NgayHetHan { get; set; }

        [JsonProperty("QuocTich")]
        public string QuocTich { get; set; }

        [JsonProperty("DanToc")]
        public string DanToc { get; set; }

        [JsonProperty("TonGiao")]
        public string TonGiao { get; set; }

        [JsonProperty("HoVaTenBo")]
        public string HoVaTenBo { get; set; }

        [JsonProperty("HoVaTenMe")]
        public string HoVaTenMe { get; set; }

        [JsonProperty("QueQuan")]
        public string QueQuan { get; set; }

        [JsonProperty("NoiThuongTru")]
        public string NoiThuongTru { get; set; }

        [JsonProperty("DacDiemNhanDang")]
        public string DacDiemNhanDang { get; set; }

        [JsonProperty("imgData")]
        public string imgData { get; set; }// Base64

        public CPersion()
        {
            SoCCCD = " ";
            SoCMND = " ";
            HoVaTen = " ";
            NgaySinh = " ";
            GioiTinh = " ";
            NgayHetHan = " ";
            NgayCap = "";
            QuocTich = "";
            DanToc = " ";
            TonGiao = " ";
            HoVaTenBo = " ";
            HoVaTenMe = " ";
            QueQuan = " ";
            NoiThuongTru = " ";
            DacDiemNhanDang = " ";
            imgData = "";
        }
        public CPersion(CPersion persion)
        {
            SoCCCD = persion.SoCCCD;
            SoCMND = persion.SoCMND;
            HoVaTen = persion.HoVaTen;
            NgaySinh = persion.NgaySinh;
            GioiTinh = persion.GioiTinh;
            NgayHetHan = persion.NgayHetHan;
            NgayCap = persion.NgayCap;
            QuocTich = persion.QuocTich;
            DanToc = persion.DanToc;
            TonGiao = persion.TonGiao;
            HoVaTenBo = persion.HoVaTenBo;
            HoVaTenMe = persion.HoVaTenMe;
            QueQuan = persion.QueQuan;
            NoiThuongTru = persion.NoiThuongTru;
            DacDiemNhanDang = persion.DacDiemNhanDang;
            imgData = persion.imgData;
        }
    }
}
