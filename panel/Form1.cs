
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace ddospanel
{
    public partial class Form1 : Form
    {
        private Chart chartTraffic, chartDdos, chartAttackSpeed, chartIPAnalysis, chartSystemUsage;
        private DataGridView dataGridBlockedIPs;
        private Label lblCpu, lblRam, lblClock, lblStatus, lblDfaStates, lblUptime;
        private ProgressBar progressAttack;
        private Timer attackSimTimer, systemTimer;
        private Random rnd = new Random();
        private GroupBox grpDfaInfo, grpSystemInfo, grpStatus;
        private Panel panelNodeMap;
        private List<string> incomingIPs = new List<string>();
        private Dictionary<string, (string Tip, string Analiz)> ipDetails = new Dictionary<string, (string, string)>();
        private ToolTip ipToolTip = new ToolTip();
        private string currentDetectState = "q0";
        private string currentDefenseState = "s0";
        private DateTime startTime;
        private Dictionary<string, int> ipCategories = new Dictionary<string, int> { { "Saldırgan", 0 }, { "Şüpheli", 0 }, { "Temiz", 0 } };
        private DataGridView dataGridBannedIPs;
        private static readonly HttpClient httpClient = new HttpClient();
        private System.Windows.Forms.Timer banRefreshTimer;
        private Timer dataRefreshTimer;





        public Form1()
        {
            InitializeComponent();
            SetupUI();
            StartClock();
            //SimulateAttack();
        }


        /*
        private void Form1_Load(object sender, EventArgs e)
        {
            // Eğer başlangıçta yapılacak bir işlem varsa buraya yazılır.
        }
        */

        private async void Form1_Load(object sender, EventArgs e)
        {
            await Task.Delay(500); // GUI render süresine saygı
            await RefreshBannedIPList(); // Flask'tan banlı IP’leri çek
        }


        private async Task<List<string>> FetchBannedIPsAsync()
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("http://192.168.1.172:5000/status");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                JObject data = JObject.Parse(json);
                JArray ipArray = (JArray)data["blocked_ips"];
                return ipArray.ToObject<List<string>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
                return new List<string>();
            }
        }

        private async Task RefreshBannedIPList()
        {
            var ipList = await FetchBannedIPsAsync();
            dataGridBannedIPs.Rows.Clear();
            foreach (var ip in ipList)
            {
                dataGridBannedIPs.Rows.Add(ip, DateTime.Now.ToString("HH:mm:ss"));
            }
        }





        private Chart CreateChart(string title, string xTitle, string yTitle)
        {
            Chart chart = new Chart { BackColor = Color.FromArgb(30, 30, 30) };
            ChartArea area = new ChartArea();
            area.AxisX.Minimum = 0;
            area.AxisX.Maximum = 20;

            area.AxisX.Title = xTitle;
            area.AxisY.Title = yTitle;
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisX.IsLabelAutoFit = true;
            area.AxisX.LabelStyle.ForeColor = Color.White;
            area.AxisY.LabelStyle.ForeColor = Color.White;
            area.AxisX.TitleForeColor = Color.White;
            area.AxisY.TitleForeColor = Color.White;
            area.BackColor = Color.Transparent;
            
            
            
            chart.ChartAreas.Add(area);

            Series series = new Series
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Lime
            };
            //series.IsValueShownAsLabel = true;
            series.XValueType = ChartValueType.String; // Bu şart, yoksa string etiketler çizilmez

            chart.Series.Add(series);

            Title chartTitle = new Title
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10, FontStyle.Bold)
            };
            chart.Titles.Add(chartTitle);

            chart.BorderlineDashStyle = ChartDashStyle.Solid;
            chart.BorderlineColor = Color.DimGray;
            chart.BorderlineWidth = 1;

            return chart;
        }

        private void SetupUI()
        {
            this.Text = "ERK DDoS Panel";
            this.ClientSize = new Size(1280, 900);
            this.BackColor = Color.FromArgb(10, 15, 30);
            this.Font = new Font("Consolas", 10, FontStyle.Regular);



            banRefreshTimer = new System.Windows.Forms.Timer();
            banRefreshTimer.Interval = 5000; // 5 saniyede bir
            banRefreshTimer.Tick += BanRefreshTimer_Tick; // Aşağıda tanımlayacağın fonksiyona bağlanır
            banRefreshTimer.Start();





            chartAttackSpeed = CreateChart("Saldırı Hızı", "Time", "Packets/s");
            chartAttackSpeed.Location = new Point(20, 20);
            chartAttackSpeed.Size = new Size(400, 180);
            this.Controls.Add(chartAttackSpeed);

            chartDdos = CreateChart("Toplam Paket Sayısı", "Time", "Score");
            chartDdos.Location = new Point(20, 210);
            chartDdos.Size = new Size(400, 180);
            this.Controls.Add(chartDdos);

            chartTraffic = chartAttackSpeed;

            progressAttack = new ProgressBar
            {
                Location = new Point(20, 400),
                Size = new Size(400, 20),
                ForeColor = Color.Lime,
                Maximum = 100
            };
            this.Controls.Add(progressAttack);

            dataGridBlockedIPs = new DataGridView
            {
                Location = new Point(20, 440),
                Size = new Size(400, 220),
                BackgroundColor = Color.Black,
                ForeColor = Color.White,
                ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.LightGray },
                RowHeadersVisible = false,
                ReadOnly = true,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                DefaultCellStyle = { BackColor = Color.Black, ForeColor = Color.White, WrapMode = DataGridViewTriState.False, Alignment = DataGridViewContentAlignment.MiddleLeft },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dataGridBlockedIPs.Columns.Add("ip", "IP Address");
            dataGridBlockedIPs.Columns.Add("reason", "Reason");
            dataGridBlockedIPs.Columns.Add("time", "Time");
            this.Controls.Add(dataGridBlockedIPs);

            grpDfaInfo = new GroupBox
            {
                Text = "DFA State Info",
                ForeColor = Color.LightGreen,
                Location = new Point(440, 20),
                Size = new Size(360, 70)
            };
            lblDfaStates = new Label
            {
                Text = "Tespit DFA ----> Geçerli State: q0\nSavunma DFA ----> Geçerli State: s0",
                ForeColor = Color.LightGreen,
                AutoSize = true,
                Location = new Point(10, 20)
            };
            grpDfaInfo.Controls.Add(lblDfaStates);
            this.Controls.Add(grpDfaInfo);

            grpSystemInfo = new GroupBox
            {
                Text = "Sistem Kullanımı",
                ForeColor = Color.Cyan,
                Location = new Point(440, 100),
                Size = new Size(360, 60)
            };
            lblCpu = new Label
            {
                Text = "CPU: 0%",
                ForeColor = Color.Cyan,
                Location = new Point(20, 25),
                AutoSize = true
            };
            lblRam = new Label
            {
                Text = "RAM: 0 GB",
                ForeColor = Color.Cyan,
                Location = new Point(160, 25),
                AutoSize = true
            };
            grpSystemInfo.Controls.Add(lblCpu);
            grpSystemInfo.Controls.Add(lblRam);
            this.Controls.Add(grpSystemInfo);

            grpStatus = new GroupBox
            {
                Text = "Durum",
                ForeColor = Color.White,
                Location = new Point(820, 20),
                Size = new Size(360, 70)
            };
            lblStatus = new Label
            {
                Text = "🟢 Sistem Aktif",
                ForeColor = Color.LimeGreen,
                Location = new Point(20, 25),
                AutoSize = true
            };
            lblClock = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                ForeColor = Color.LightGray,
                Location = new Point(220, 25),
                AutoSize = true
            };
            lblUptime = new Label
            {
                Text = "Uptime: 00:00:00",
                ForeColor = Color.LightGray,
                Location = new Point(20, 45),
                AutoSize = true
            };
            grpStatus.Controls.Add(lblStatus);
            grpStatus.Controls.Add(lblClock);
            grpStatus.Controls.Add(lblUptime);
            this.Controls.Add(grpStatus);

            chartIPAnalysis = CreateChart("IP Analiz ve Kategori Dağılımı", "Kategori", "IP Sayısı");
            chartIPAnalysis.Location = new Point(820, 100);
            chartIPAnalysis.Size = new Size(360, 200);
            this.Controls.Add(chartIPAnalysis);
            chartIPAnalysis.Series[0].XValueType = ChartValueType.String;
            chartIPAnalysis.Series[0].IsValueShownAsLabel = true;
            chartIPAnalysis.ChartAreas[0].AxisX.Interval = 1;
            chartIPAnalysis.ChartAreas[0].AxisX.LabelStyle.Angle = 90;

            // IP Kategori Grafiği için sabit eksen etiketleri tanımla
            string[] categories = { "Saldırgan", "Şüpheli", "Temiz" };
            chartIPAnalysis.Series[0].Points.Clear(); // Başlangıçta sıfırla
            foreach (string category in categories)
            {
                chartIPAnalysis.Series[0].Points.AddXY(category, 0); // Başlangıç değeri 0
            }


            chartSystemUsage = new Chart { BackColor = Color.FromArgb(30, 30, 30) };
            ChartArea usageArea = new ChartArea();
            usageArea.AxisX.Title = "Zaman";
            usageArea.AxisY.Title = "% Kullanım";
            usageArea.AxisX.LabelStyle.ForeColor = Color.White;
            usageArea.AxisY.LabelStyle.ForeColor = Color.White;
            usageArea.AxisX.TitleForeColor = Color.White;
            usageArea.AxisY.TitleForeColor = Color.White;
            usageArea.BackColor = Color.Transparent;
            chartSystemUsage.ChartAreas.Add(usageArea);

            // CPU serisi (yeşil)
            Series cpuSeries = new Series("CPU")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Lime
            };
            chartSystemUsage.Series.Add(cpuSeries);

            // RAM serisi (turuncu)
            Series ramSeries = new Series("RAM")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Orange
            };
            chartSystemUsage.Series.Add(ramSeries);

            // Başlık ve kenarlık
            chartSystemUsage.Titles.Add(new Title
            {
                Text = "Sistem Kaynak Kullanımı",
                ForeColor = Color.White,
                Font = new Font("Consolas", 10, FontStyle.Bold)
            });
            chartSystemUsage.BorderlineDashStyle = ChartDashStyle.Solid;
            chartSystemUsage.BorderlineColor = Color.DimGray;
            chartSystemUsage.BorderlineWidth = 1;

            chartSystemUsage.Location = new Point(820, 310);
            chartSystemUsage.Size = new Size(360, 200);
            this.Controls.Add(chartSystemUsage);

            Legend legend = new Legend
            {
                ForeColor = Color.White,
                Font = new Font("Consolas", 8, FontStyle.Bold),
                Docking = Docking.Top,
                Alignment = StringAlignment.Near,
                BackColor = Color.Transparent
            };
            chartSystemUsage.Legends.Add(legend);



            dataGridBannedIPs = new DataGridView
            {
                Location = new Point(1210, 20),
                Size = new Size(300, 490),
                BackgroundColor = Color.Black,
                ForeColor = Color.White,
                ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.LightGray },
                RowHeadersVisible = false,
                ReadOnly = true,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                DefaultCellStyle = { BackColor = Color.Black, ForeColor = Color.White, WrapMode = DataGridViewTriState.False, Alignment = DataGridViewContentAlignment.MiddleLeft },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dataGridBannedIPs.Columns.Add("ip", "Banned IP");
            dataGridBannedIPs.Columns.Add("time", "Ban Time");
            this.Controls.Add(dataGridBannedIPs);

            panelNodeMap = new Panel
            {
                Location = new Point(440, 180),
                Size = new Size(360, 480),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelNodeMap.Paint += PanelNodeMap_Paint;
            panelNodeMap.MouseMove += PanelNodeMap_MouseMove;

            this.Controls.Add(panelNodeMap);

            startTime = DateTime.Now;

            


            dataRefreshTimer = new Timer();
            dataRefreshTimer.Interval = 2000; // 2 saniyede bir verileri çek
            dataRefreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    var response = await httpClient.GetAsync("http://192.168.1.172:5000/status");
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();

                    JObject data = JObject.Parse(json);

                    // 1) DFA Durumları
                    string detectState = data["dfa_detect_state"]?.ToString();
                    string defenseState = data["dfa_defense_state"]?.ToString();
                    lblDfaStates.Text = $"Tespit DFA ----> Geçerli State: {detectState}\nSavunma DFA ----> Geçerli State: {defenseState}";

                    // 2) IP Node Grafiği
                    JArray ipList = (JArray)data["ip_nodes"];
                    incomingIPs.Clear();
                    foreach (var ip in ipList)
                        incomingIPs.Add(ip.ToString());
                    panelNodeMap.Invalidate();

                    // 3) Kategori Dağılımı
                    JObject stats = (JObject)data["ip_category_stats"];
                    ipCategories["Saldırgan"] = stats["Saldırgan"]?.ToObject<int>() ?? 0;
                    ipCategories["Şüpheli"] = stats["Şüpheli"]?.ToObject<int>() ?? 0;
                    ipCategories["Temiz"] = stats["Temiz"]?.ToObject<int>() ?? 0;

                    chartIPAnalysis.Series[0].Points.Clear();
                    foreach (var pair in ipCategories)
                        chartIPAnalysis.Series[0].Points.AddXY(pair.Key, pair.Value);

                    // 4) Sistem Durumu
                    string traffic = data["traffic_level"]?.ToString();
                    if (ipList.Count == 0 || traffic == "low")
                    {
                        lblStatus.Text = "🟢 Sistem Aktif";
                        lblStatus.ForeColor = Color.LimeGreen;
                    }
                    else if (traffic == "mid")
                    {
                        lblStatus.Text = "🟡 Şüpheli Trafik";
                        lblStatus.ForeColor = Color.Orange;
                    }
                    else
                    {
                        lblStatus.Text = "🔴 DDoS Algılandı";
                        lblStatus.ForeColor = Color.Red;
                    }

                    // 5) Saldırı Hızı ve Toplam Paket Grafikleri
                    int packetRate = data["packet_rate"]?.ToObject<int>() ?? 0;
                    int packetTotal = data["packet_total"]?.ToObject<int>() ?? 0;
                    progressAttack.Value = Math.Min(packetRate, 100); // bar maksimumu 100


                    chartAttackSpeed.Series[0].Points.AddY(packetRate);
                    if (chartAttackSpeed.Series[0].Points.Count > 20)
                        chartAttackSpeed.Series[0].Points.RemoveAt(0);
                        chartAttackSpeed.ResetAutoValues();

                    chartDdos.Series[0].Points.AddY(packetTotal);
                    if (chartDdos.Series[0].Points.Count > 20)
                        chartDdos.Series[0].Points.RemoveAt(0);
                        chartDdos.ResetAutoValues();

                    // 6) IP Log Tablosu (Alt Kısım)
                    JArray logs = (JArray)data["ip_logs"];
                    dataGridBlockedIPs.Rows.Clear();
                    foreach (JObject log in logs)
                    {
                        string ip = log["ip"]?.ToString();
                        string reason = log["reason"]?.ToString();
                        string time = log["time"]?.ToString();

                        dataGridBlockedIPs.Rows.Add(ip, reason, time);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Veri çekme hatası: " + ex.Message);
                }
            };
            dataRefreshTimer.Start();










        }

        void BanIP(string ip)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            dataGridBannedIPs.Rows.Insert(0, ip, time);
            if (dataGridBannedIPs.Rows.Count > 20)
                dataGridBannedIPs.Rows.RemoveAt(19);
        }

        private async void BanRefreshTimer_Tick(object sender, EventArgs e)
        {
            await RefreshBannedIPList();
        }





        private void PanelNodeMap_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int centerX = panelNodeMap.Width / 2;
            int centerY = panelNodeMap.Height / 2;
            int radius = 180;

            g.FillEllipse(Brushes.Red, centerX - 20, centerY - 20, 40, 40);
            g.DrawString("SERVER", new Font("Consolas", 9), Brushes.White, centerX - 30, centerY - 35);

            for (int i = 0; i < incomingIPs.Count; i++)
            {
                double angle = i * 2 * Math.PI / incomingIPs.Count;
                int x = centerX + (int)(radius * Math.Cos(angle));
                int y = centerY + (int)(radius * Math.Sin(angle));
                Pen pulsePen = new Pen(Color.LimeGreen, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                g.DrawLine(pulsePen, centerX, centerY, x, y);
                g.FillEllipse(Brushes.DodgerBlue, x - 10, y - 10, 20, 20);
                g.DrawString(incomingIPs[i], new Font("Consolas", 8), Brushes.White, x + 10, y);
            }
        }

        private void PanelNodeMap_MouseMove(object sender, MouseEventArgs e)
        {
            int centerX = panelNodeMap.Width / 2;
            int centerY = panelNodeMap.Height / 2;
            int radius = 180;

            for (int i = 0; i < incomingIPs.Count; i++)
            {
                double angle = i * 2 * Math.PI / incomingIPs.Count;
                int x = centerX + (int)(radius * Math.Cos(angle));
                int y = centerY + (int)(radius * Math.Sin(angle));
                Rectangle rect = new Rectangle(x - 10, y - 10, 20, 20);

                if (rect.Contains(e.Location))
                {
                    if (ipDetails.ContainsKey(incomingIPs[i]))
                    {
                        var (tip, analiz) = ipDetails[incomingIPs[i]];
                        ipToolTip.SetToolTip(panelNodeMap, $"IP: {incomingIPs[i]}\nTip: {tip}\nAnaliz: {analiz}");
                        return;
                    }
                }
            }

            ipToolTip.Hide(panelNodeMap);
        }



        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

        private void StartClock()
        {
            cpuCounter.NextValue(); // İlk değerleri çekip hazırlıyor
            ramCounter.NextValue();

            systemTimer = new Timer();
            systemTimer.Interval = 1000;
            systemTimer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
                TimeSpan uptime = DateTime.Now - startTime;
                lblUptime.Text = $"Uptime: {uptime:hh\\:mm\\:ss}";

                float cpu = cpuCounter.NextValue();
                float ram = ramCounter.NextValue();
                float usedRam = ((16 - (ram / 1024)) / 16) * 100;


                lblCpu.Text = $"CPU: {cpu:F1}%";
                lblRam.Text = $"RAM: {usedRam:F1}%";


                chartSystemUsage.Series[0].Points.AddY(cpu);     // CPU yeşil
                chartSystemUsage.Series[1].Points.AddY(usedRam); // RAM turuncu

                if (chartSystemUsage.Series[0].Points.Count > 50)
                {
                    chartSystemUsage.Series[0].Points.RemoveAt(0);
                    chartSystemUsage.Series[1].Points.RemoveAt(0);
                }
            };
            systemTimer.Start();
        }



    }
}

