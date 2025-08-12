using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SkiaSharp;
using SocketIOClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViewFaceCore;
using ViewFaceCore.Core;
using ViewFaceCore.Model;

namespace ReaderSDK
{
    public partial class frm : Form
    {
        private const string ipAddress = "127.0.0.1";
        private const int port = 8000;
        public bool IsConnected { get; set; }
        public bool IsScan { get; set; }
        public bool Isfail { get; set; }
        public string resultScan { get; set; }
        public event Action getResult;
        int countThanhCong = 0;
        int countThatBai = 0;
        public event Action GetInfoConnectDivice;
        private SocketIOClient.SocketIO clientScan;
        private SocketIOClient.SocketIO clientCamera;
        public bool isNewCard = false;
        public bool isReadSuccessfully = false;
        public bool isReadRawDataSuccessfully = false;
        public int readStatus = 1;
        public int ReadCount = 0;
        public int Tot_Count = 0;
        public int Xau_Count = 0;
        public int readMaxTime = 3;
        long startRead = 0;
        long ReadDg1 = 0;
        long ReadOther = 0;
        long ReadAll = 0;
        public List<Log> LogList = new List<Log>();

        // Thêm biến đếm chu kỳ
        public int cycleCount = 0;
        public bool isVerificationInProgress = false;

        // ViewFaceCore
        private FaceDetector faceDetector = new FaceDetector();
        private FaceLandmarker faceLandmarker = new FaceLandmarker();
        private FaceAntiSpoofing faceAntiSpoofing = new FaceAntiSpoofing();
        private FaceRecognizer faceRecognizer = new FaceRecognizer();
        private byte[] referenceImageBytes = null;
        private float[] referenceFeatures = null;
        private float bestSimilarity = 0f;
        private readonly ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
        private bool isProcessingQueue = false;
        private long lastProcessedTime = 0;
        private const int maxQueueSize = 5; // Giới hạn queue
        private const int minProcessInterval = 200;
        private bool hasSpoofingDetected = false; // Thêm biến cờ phát hiện giả mạo

        // Thêm để theo dõi khuôn mặt với pid
        //private Dictionary<int, TrackedFace> trackedFaces = new Dictionary<int, TrackedFace>();

        // public class TrackedFace
        // {
        //     public int Pid { get; set; }
        //     public float[] Features { get; set; }
        //     public long LastUpdated { get; set; }
        //     public float BestSimilarity { get; set; }
        //     public bool IsVerified { get; set; }
        //     public bool IsReal { get; set; } // Thêm để lưu trạng thái chống giả mạo
        // }

        private VideoCapture capture; // Thêm biến capture cho webcam
        private bool isCameraRunning = false;

        public frm()
        {
            InitializeComponent();
            // Khởi tạo FaceTracker với cấu hình cơ bản
            //faceTracker = new FaceTracker(new FaceTrackerConfig(640, 480)); // Giả sử độ phân giải webcam là 640x480
        }

        private void reset()
        {
            txtHoTen.Text = "";
            txtNgaySinh.Text = "";
            txtGioiTinh.Text = "";
            txtDanToc.Text = "";
            txtQuocTich.Text = "";
            txtQueQuan.Text = "";
            txtSoCCCD.Text = "";
            txtNgayCap.Text = "";
            txtNgayHetHan.Text = "";
            txtSoCMND.Text = "";
            txtTonGiao.Text = "";
            txtThuongTru.Text = "";
            txtSoCMND.Text = "";
            txtTenBo.Text = "";
            txtTenMe.Text = "";
            txtTenVoChong.Text = "";
            picAnh.Image = null;
            // ViewFaceCore
            referenceImageBytes = null;
            referenceFeatures = null;
            bestSimilarity = 0f;
            lblSimilarity.Text = "Vui lòng đặt thẻ";
            hasSpoofingDetected = false; // Reset cờ khi reset
        }

        private void frm_Load(object sender, EventArgs e)
        {
            clientScan = new SocketIOClient.SocketIO($"http://{ipAddress}:{port}/");
            // Bỏ clientCamera, thay bằng webcam local
            _ = ScanStart();
            // Không tự động bật camera, chỉ bật khi có thẻ CCCD
            Task.Run(() => ProcessFrameQueue()); // Đảm bảo luôn chạy pipeline xử lý frame
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Tắt camera khi đóng form
            StopCamera();
            isProcessingQueue = false;
        }

        private void btnKetNoi_Click(object sender, EventArgs e) { }
        private void btnNgatKetNoi_Click(object sender, EventArgs e) { }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            reset();
            ReadCount = 0;
            Tot_Count = 0;
            Xau_Count = 0;
            cycleCount = 0; // Reset chu kỳ
            lblCycleCount.Text = "Chu kỳ: 0"; // Reset hiển thị chu kỳ
            LogList.Clear();
            // Tắt camera nếu đang chạy
            StopCamera();
        }

        // Thêm method bật camera
        private async Task StartCamera()
        {
            if (!isCameraRunning)
            {
                capture = new VideoCapture(0); // 0 là webcam mặc định
                isCameraRunning = true;
                isVerificationInProgress = true;
                
                // Tăng chu kỳ khi bắt đầu xác thực
                cycleCount++;
                
                // Cập nhật UI hiển thị chu kỳ
                this.Invoke((MethodInvoker)delegate
                {
                    lblCycleCount.Text = $"Chu kỳ: {cycleCount}";
                    Console.WriteLine($"Bắt đầu chu kỳ xác thực thứ: {cycleCount}");
                });

                Task.Run(() =>
                {
                    while (isCameraRunning)
                    {
                        using (var mat = new Mat())
                        {
                            if (capture.Read(mat) && !mat.Empty())
                            {
                                var img = BitmapConverter.ToBitmap(mat);
                                byte[] frameBytes;
                                using (var ms = new MemoryStream())
                                {
                                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    frameBytes = ms.ToArray();
                                }

                                // Hiển thị lên UI
                                if (!IsDisposed)
                                {
                                    this.BeginInvoke((MethodInvoker)delegate
                                    {
                                        try
                                        {
                                            ptb_image.Image?.Dispose();
                                            ptb_image.Image = (Bitmap)img.Clone();
                                        }
                                        catch { }
                                    });
                                }

                                // ViewFaceCore: Thêm frame vào queue FIFO
                                //byte[] frameBytes = Convert.FromBase64String(info.data);
                                if (frameQueue.Count < maxQueueSize)
                                {
                                    frameQueue.Enqueue(frameBytes);
                                }
                                else
                                {
                                    // Bỏ frame cũ nếu queue đầy
                                    frameQueue.TryDequeue(out _);
                                    frameQueue.Enqueue(frameBytes);
                                }
                            }
                        }
                    }
                });
            }
        }

        // Thêm method tắt camera
        private void StopCamera()
        {
            if (isCameraRunning)
            {
                isCameraRunning = false;
                isVerificationInProgress = false;
                capture?.Release();
                capture?.Dispose();
                capture = null;
                
                // Xóa ảnh camera trên UI
                this.Invoke((MethodInvoker)delegate
                {
                    ptb_image.Image?.Dispose();
                    ptb_image.Image = null;
                });
                
                Console.WriteLine($"Kết thúc chu kỳ xác thực thứ: {cycleCount}");
            }
        }

        public async Task TaskCamera()
        {
            // Method này giờ chỉ được gọi khi cần bật camera
            await StartCamera();
        }

        public async Task ScanStart()
        {
            clientScan.OnConnected += async delegate
            {
                IsConnected = true;
                this.GetInfoConnectDivice?.Invoke();
            };

            clientScan.On("/event", delegate (SocketIOResponse response)
            {
                resultScan = response.ToString();
                PayLoad payload = JsonConvert.DeserializeObject<List<PayLoad>>(response.ToString())?[0];
                switch (payload.Id)
                {
                    case "1":
                        isNewCard = true;
                        isReadSuccessfully = false;
                        isReadRawDataSuccessfully = false;
                        readStatus = 1;
                        // hasVerificationResult = false;

                        this.Invoke(new Action(() => reset()));

                        startRead = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        ReadCount++;
                        break;
                    case "2":
                        readStatus = 2;
                        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        ReadDg1 = milliseconds - startRead;

                        txtHoTen.Invoke(new Action(() => txtHoTen.Text = payload.Data.PersonName.ToUpper()));
                        txtNgaySinh.Invoke(new Action(() => txtNgaySinh.Text = payload.Data.DateOfBirth));
                        txtGioiTinh.Invoke(new Action(() => txtGioiTinh.Text = payload.Data.Gender));
                        txtDanToc.Invoke(new Action(() => txtDanToc.Text = payload.Data.Race));
                        txtQuocTich.Invoke(new Action(() => txtQuocTich.Text = payload.Data.Nationality));
                        txtQueQuan.Invoke(new Action(() => txtQueQuan.Text = payload.Data.ResidencePlace));
                        txtSoCCCD.Invoke(new Action(() => txtSoCCCD.Text = payload.Data.IdCard.ToUpper()));
                        txtNgayCap.Invoke(new Action(() => txtNgayCap.Text = payload.Data.IssueDate));
                        txtNgayHetHan.Invoke(new Action(() => txtNgayHetHan.Text = payload.Data.ExpiryDate));
                        txtSoCMND.Invoke(new Action(() => txtSoCMND.Text = payload.Data.OldIdCode));
                        txtTonGiao.Invoke(new Action(() => txtTonGiao.Text = payload.Data.Religion));
                        txtThuongTru.Invoke(new Action(() => txtThuongTru.Text = payload.Data.PersonalIdentification));
                        txtSoCMND.Invoke(new Action(() => txtSoCMND.Text = payload.Data.OriginPlace));
                        txtTenBo.Invoke(new Action(() => txtTenBo.Text = payload.Data.FatherName));
                        txtTenMe.Invoke(new Action(() => txtTenMe.Text = payload.Data.MotherName));
                        txtTenVoChong.Invoke(new Action(() => txtTenVoChong.Text = payload.Data.WifeName));
                        break;
                    case "4":
                        readStatus = 4;
                        long milliseconds_3 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        ReadAll = milliseconds_3 - startRead;
                        ReadOther = ReadAll - ReadDg1;
                        Tot_Count++;

                        if (payload.Data.ImgData == null)
                        {
                            picAnh.Invoke(new Action(() => picAnh.Image = null));
                        }
                        else
                        {
                            byte[] bytes = Convert.FromBase64String(payload.Data.ImgData);
                            Image image;
                            using (MemoryStream ms = new MemoryStream(bytes))
                            {
                                image = Image.FromStream(ms);
                            }
                            picAnh.Invoke(new Action(() => picAnh.Image = image));
                            referenceImageBytes = bytes;
                            ProcessReferenceImage(bytes);
                            
                            // Bật camera khi nhận được ảnh CCCD
                            _ = StartCamera();
                        }

                        Log log = new Log();
                        log.serial = "00000000000";
                        long unixDate = startRead;
                        DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        DateTime date = start.AddMilliseconds(unixDate).ToLocalTime();
                        log.time = date.ToString("dd/MM/yyyy HH:mm:ss");
                        log.status.ReadBeginTime = startRead;
                        log.status.ReadDg1Time = ReadDg1;
                        log.status.ReadOtherTime = ReadOther;
                        log.status.ReadAllTime = ReadAll;
                        log.status.ReadStatus = "Read all data successfully!";
                        log.persion.SoCCCD = txtSoCCCD.Text;
                        log.persion.HoVaTen = txtHoTen.Text;
                        LogList.Add(log);
                        break;
                    case "3":
                        Xau_Count++;
                        Log log1 = new Log();
                        log1.serial = "00000000000";
                        long unixDate1 = startRead;
                        DateTime start1 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        DateTime date1 = start1.AddMilliseconds(unixDate1).ToLocalTime();
                        log1.time = date1.ToString("dd/MM/yyyy HH:mm:ss");
                        log1.status.ReadBeginTime = startRead;
                        log1.status.ReadDg1Time = ReadDg1;
                        log1.status.ReadOtherTime = ReadOther;
                        log1.status.ReadAllTime = ReadAll;
                        log1.status.ReadStatus = payload.Message;
                        log1.persion.SoCCCD = txtSoCCCD.Text;
                        log1.persion.HoVaTen = txtHoTen.Text;
                        LogList.Add(log1);
                        readStatus = 3;
                        break;
                    default:
                        break;
                }
            });

            clientScan.OnError += async delegate
            {
                IsConnected = false;
                this.GetInfoConnectDivice?.Invoke();
            };

            clientScan.OnDisconnected += async delegate
            {
                IsConnected = false;
                this.GetInfoConnectDivice?.Invoke();
            };

            clientScan.ConnectAsync();
        }
        // ViewFaceCore: Xử lý ảnh CCCD
        private void ProcessReferenceImage(byte[] imageBytes)
        {
            try
            {
                SKBitmap bitmap = ByteArrayToSKBitmap(imageBytes);
                if (bitmap == null)
                {
                    if (!IsDisposed)
                    {
                        this.Invoke(new Action(() => lblSimilarity.Text = "Độ tương đồng: 0% - Ảnh CCCD không hợp lệ"));
                    }
                    return;
                }

                var faceInfos = faceDetector.Detect(bitmap);
                if (faceInfos == null || faceInfos.Length == 0)
                {
                    if (!IsDisposed)
                    {
                        this.Invoke(new Action(() => lblSimilarity.Text = "Không tìm thấy khuôn mặt CCCD"));
                    }
                    bitmap.Dispose();
                    return;
                }

                var faceInfo = faceInfos[0];
                var points = faceLandmarker.Mark(bitmap, faceInfo);
                referenceFeatures = faceRecognizer.Extract(bitmap, points);
                if (referenceFeatures == null)
                {
                    if (!IsDisposed)
                    {
                        this.Invoke(new Action(() => lblSimilarity.Text = "Độ tương đồng: 0% - Lỗi trích xuất CCCD"));
                    }
                    bitmap.Dispose();
                    return;
                }

                this.Invoke(new Action(() => lblSimilarity.Text = "Đã lưu ảnh CCCD"));
                bitmap.Dispose();
            }
            catch (Exception ex)
            {

                if (!IsDisposed)
                {
                    this.Invoke(new Action(() => lblSimilarity.Text = "Độ tương đồng: 0% - Lỗi xử lý CCCD"));
                }
            }
        }

        private void ProcessFrameQueue()
        {
            isProcessingQueue = true;
            long? startTime = null; // Thời gian bắt đầu khi có referenceFeatures
            bestSimilarity = 0f;
            hasSpoofingDetected = false; // Reset cờ khi bắt đầu xác thực mới

            while (isProcessingQueue)
            {
                long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                if (currentTime - lastProcessedTime < minProcessInterval)
                {
                    Task.Delay(10).Wait();
                    continue;
                }

                if (frameQueue.TryDequeue(out byte[] frameBytes) && referenceFeatures != null)
                {
                    try
                    {
                        // Bắt đầu đếm thời gian nếu chưa có
                        if (!startTime.HasValue)
                        {
                            startTime = currentTime;
                        }

                        SKBitmap bitmap = ByteArrayToSKBitmap(frameBytes);
                        if (bitmap == null)
                        {
                            Console.WriteLine("Ảnh webcam không hợp lệ");
                            continue;
                        }

                        var faceInfos = faceDetector.Detect(bitmap);
                        if (faceInfos == null || faceInfos.Length == 0)
                        {
                            Console.WriteLine("Không tìm thấy khuôn mặt trong ảnh webcam");
                            bitmap.Dispose();
                            continue;
                        }

                        bool foundMatchOrSpoof = false;
                        foreach (var faceInfo in faceInfos)
                        {
                            var points = faceLandmarker.Mark(bitmap, faceInfo);
                            var antiSpoofResult = faceAntiSpoofing.AntiSpoofing(bitmap, faceInfo, points);
                            if (antiSpoofResult.Status != AntiSpoofingStatus.Real)
                            {
                                Console.WriteLine($"Ảnh webcam giả mạo: Clarity: {antiSpoofResult.Clarity:F6}, Reality: {antiSpoofResult.Reality:F6}");
                                if (antiSpoofResult.Reality < 0.8 && antiSpoofResult.Clarity < 0.7)
                                    hasSpoofingDetected = true; // Đánh dấu đã phát hiện giả mạo
                                else
                                    hasSpoofingDetected = false;
                                
                                // Kiểm tra timeout ngay cả khi phát hiện giả mạo
                                if (startTime.HasValue && (currentTime - startTime.Value >= 5000))
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        if (hasSpoofingDetected)
                                        {
                                            lblSimilarity.Text = "Phát hiện giả mạo";
                                            lblSimilarity.ForeColor = Color.Red;
                                            countThatBai++;
                                        }
                                        else
                                        {
                                            lblSimilarity.Text = $"Xác thực thất bại";
                                            lblSimilarity.ForeColor = Color.Red;
                                            countThatBai++;
                                        }
                                    });

                                    // Tắt camera sau khi xác thực thất bại
                                    StopCamera();

                                    // Reset trạng thái
                                    startTime = null;
                                    bestSimilarity = 0f;
                                    referenceFeatures = null;
                                    hasSpoofingDetected = false;
                                    foundMatchOrSpoof = true;
                                    break;
                                }

                                this.Invoke((MethodInvoker)delegate
                                {
                                    if (hasSpoofingDetected)
                                    {
                                        lblSimilarity.Text = "Đang xác thực...";
                                        lblSimilarity.ForeColor = Color.Black;
                                    }
                                    else
                                    {
                                        lblSimilarity.Text = "Đang xác thực...";
                                        lblSimilarity.ForeColor = Color.Black;
                                    }
                                });
                                foundMatchOrSpoof = true;
                                break;
                            }

                            var features = faceRecognizer.Extract(bitmap, points);
                            if (features == null)
                            {
                                Console.WriteLine("Không thể trích xuất đặc trưng từ ảnh webcam");
                                continue;
                            }

                            float similarity = CalculateSimilarity(features, referenceFeatures) * 100;
                            if (similarity > bestSimilarity)
                            {
                                bestSimilarity = similarity;
                            }

                            // Kiểm tra đạt 75% ngay lập tức
                            if (bestSimilarity >= 75f)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    lblSimilarity.Text = $"Xác thực thành công";
                                    lblSimilarity.ForeColor = Color.Green;
                                    countThanhCong++;
                                });

                                // Tắt camera sau khi xác thực thành công
                                StopCamera();

                                // Reset trạng thái
                                startTime = null;
                                bestSimilarity = 0f;
                                referenceFeatures = null;
                                hasSpoofingDetected = false;
                                foundMatchOrSpoof = true;
                                break;
                            }
                        }

                        if (foundMatchOrSpoof)
                        {
                            bitmap.Dispose();
                            continue;
                        }

                        // Kiểm tra hết 1 phút nếu chưa đạt 75%
                        if (startTime.HasValue && (currentTime - startTime.Value >= 5000))
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                lblSimilarity.Text = $"Xác thực thất bại";
                                lblSimilarity.ForeColor = Color.Red;
                                countThatBai++;
                            });

                            // Tắt camera sau khi xác thực thất bại
                            StopCamera();

                            // Reset trạng thái
                            startTime = null;
                            bestSimilarity = 0f;
                            referenceFeatures = null;
                            hasSpoofingDetected = false;
                            bitmap.Dispose();
                            continue;
                        }

                        // Cập nhật % mỗi frame, chưa đạt 75%
                        this.Invoke((MethodInvoker)delegate
                        {
                            lblSimilarity.Text = $"Đang xác thực...";
                            lblSimilarity.ForeColor = Color.Black; // Màu mặc định
                        });

                        lastProcessedTime = currentTime;
                        bitmap.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi xử lý frame webcam: {ex.Message}");
                    }
                }
                else
                {
                    Task.Delay(10).Wait();
                }
            }
        }

        // ViewFaceCore: Chuyển byte thành SKBitmap
        private SKBitmap ByteArrayToSKBitmap(byte[] data)
        {
            try
            {
                if (data == null || data.Length < 10) return null;
                using (MemoryStream ms = new MemoryStream(data))
                {
                    return SKBitmap.Decode(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        // ViewFaceCore: Tính Similarity
        private float CalculateSimilarity(float[] features1, float[] features2)
        {
            if (features1 == null || features2 == null || features1.Length != features2.Length) return 0f;
            float dotProduct = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < features1.Length; i++)
            {
                dotProduct += features1[i] * features2[i];
                norm1 += features1[i] * features1[i];
                norm2 += features2[i] * features2[i];
            }
            float similarity = dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
            return float.IsNaN(similarity) ? 0f : similarity;
        }

        public class PayLoad
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public IcaoInfoCard Data { get; set; }
        }

        public class IcaoResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public IcaoInfoCard Data { get; set; }
        }

        public class IcaoInfoCard
        {
            [JsonProperty("idCode")]
            public string IdCard { get; set; }

            [JsonProperty("personName")]
            public string PersonName { get; set; }

            [JsonProperty("dateOfBirth")]
            public string DateOfBirth { get; set; }

            [JsonProperty("gender")]
            public string Gender { get; set; }

            [JsonProperty("nationality")]
            public string Nationality { get; set; }

            [JsonProperty("race")]
            public string Race { get; set; }

            [JsonProperty("religion")]
            public string Religion { get; set; }

            [JsonProperty("originPlace")]
            public string OriginPlace { get; set; }

            [JsonProperty("residencePlace")]
            public string ResidencePlace { get; set; }

            [JsonProperty("personalIdentification")]
            public string PersonalIdentification { get; set; }

            [JsonProperty("issueDate")]
            public string IssueDate { get; set; }

            [JsonProperty("expiryDate")]
            public string ExpiryDate { get; set; }

            [JsonProperty("fatherName")]
            public string FatherName { get; set; }

            [JsonProperty("motherName")]
            public string MotherName { get; set; }

            [JsonProperty("wifeName")]
            public string WifeName { get; set; }

            [JsonProperty("oldIdCode")]
            public string OldIdCode { get; set; }

            [JsonProperty("img_data")]
            public string ImgData { get; set; }

            [JsonProperty("dg2")]
            public string Dg2 { get; set; }

            [JsonProperty("dg13")]
            public string Dg13 { get; set; }

            [JsonProperty("dg14")]
            public string Dg14 { get; set; }

            [JsonProperty("dg15")]
            public string Dg15 { get; set; }

            [JsonProperty("sod")]
            public string Sod { get; set; }
        }

        public class CCamera
        {
            [JsonProperty("data")]
            public string data { get; set; }
        }

        public class Persion
        {
            public string SoCCCD;
            public string SoCMND;
            public string HoVaTen;
            public string NgaySinh;
            public string GioiTinh;
            public string NgayCap;
            public string NgayHetHan;
            public string QuocTich;
            public string DanToc;
            public string TonGiao;
            public string HoVaTenBo;
            public string HoVaTenMe;
            public string QueQuan;
            public string NoiThuongTru;
            public string DacDiemNhanDang;
            public Persion()
            {
                SoCCCD = " ";
                SoCMND = " ";
                HoVaTen = " ";
                NgaySinh = " ";
                GioiTinh = " ";
                NgayHetHan = " ";
                NgayHetHan = "";
                QuocTich = "";
                DanToc = " ";
                TonGiao = " ";
                HoVaTenBo = " ";
                HoVaTenMe = " ";
                QueQuan = " ";
                NoiThuongTru = " ";
                DacDiemNhanDang = " ";
            }
        }

        public class TestStatus
        {
            public long ReadBeginTime;
            public long ReadDg1Time;
            public long ReadOtherTime;
            public long ReadAllTime;
            public string ReadStatus;
            public TestStatus()
            {
                ReadBeginTime = 0;
                ReadDg1Time = 0;
                ReadOtherTime = 0;
                ReadAllTime = 0;
                ReadStatus = " ";
            }
        }

        public class Log
        {
            public string serial;
            public string time;
            public TestStatus status;
            public Persion persion;
            public Log()
            {
                serial = "0000000000";
                time = "0";
                status = new TestStatus();
                persion = new Persion();
            }
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void label17_Click_1(object sender, EventArgs e)
        {

        }

        private void ptb_image_Click(object sender, EventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void txtTonGiao_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void txtSoCCCD_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtNgayCap_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void txtSoCMND_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtHoTen_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void txtGioiTinh_TextChanged(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void txtTenBo_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtQuocTich_TextChanged(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void txtDacDiem_TextChanged(object sender, EventArgs e)
        {

        }

        private void picAnh_Click(object sender, EventArgs e)
        {

        }

        private void lblTitle_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtTenVoChong_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtDacDiem_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void txtQueQuan_TextChanged(object sender, EventArgs e)
        {

        }
    }
}