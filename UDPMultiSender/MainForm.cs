using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDPTestApp
{
    public partial class MainForm : Form
    {
        private UdpClient? udpClient;
        private bool isConnected = false;
        private readonly object lockObject = new object();
        private List<int> targetPorts = new List<int>();
        private Dictionary<int, bool> portReceiveStatus = new Dictionary<int, bool>();

        public MainForm()
        {
            InitializeComponent();
            
            // 폼 아이콘 설정
            try
            {
                if (File.Exists("app.ico"))
                {
                    this.Icon = new Icon("app.ico");
                }
            }
            catch
            {
                // 아이콘 파일이 없거나 로드할 수 없는 경우 무시
            }
            
            // 폼이 로드된 후에 UDP 클라이언트 초기화
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            InitializeUDPClient();
        }

        private void InitializeUDPClient()
        {
            try
            {
                // 로컬 포트 0을 사용하여 자동으로 사용 가능한 포트 할당
                udpClient = new UdpClient(0);
                
                // Windows에서 지원하는 안전한 소켓 옵션만 설정
                try
                {
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                catch
                {
                    // ReuseAddress가 지원되지 않으면 무시
                }
                
                LogMessage("UDP 클라이언트가 초기화되었습니다.");
                LogMessage($"로컬 포트: {((IPEndPoint)udpClient.Client.LocalEndPoint!).Port}");
                
                // 비동기 수신 시작
                StartReceiving();
            }
            catch (Exception ex)
            {
                LogMessage($"UDP 클라이언트 초기화 실패: {ex.Message}");
            }
        }

        private async void StartReceiving()
        {
            try
            {
                while (udpClient != null && !this.IsDisposed)
                {
                    var result = await udpClient.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    var remoteEndPoint = result.RemoteEndPoint;
                    
                    // 폼이 로드되었는지 확인 후 Invoke 사용
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        Invoke(() =>
                        {
                            if (!this.IsDisposed)
                            {
                                LogMessage($"수신: {remoteEndPoint} -> {message}");
                                
                                // 수신 확인 메시지인 경우 포트별 상태 업데이트
                                if (message.Contains("RECEIVED") || message.Contains("ACK") || message.Contains("OK"))
                                {
                                    var port = remoteEndPoint.Port;
                                    if (portReceiveStatus.ContainsKey(port))
                                    {
                                        portReceiveStatus[port] = true;
                                        LogMessage($"✅ 포트 {port}에서 수신 확인됨");
                                    }
                                }
                            }
                        });
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // UDP 클라이언트가 정상적으로 해제된 경우
                LogMessage("UDP 수신이 종료되었습니다.");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // 연결이 중단된 경우
                LogMessage("UDP 수신이 중단되었습니다.");
            }
            catch (Exception ex)
            {
                if (udpClient != null && this.IsHandleCreated && !this.IsDisposed)
                {
                    Invoke(() =>
                    {
                        if (!this.IsDisposed)
                        {
                            LogMessage($"수신 오류: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        private void btnGetLocalIP_Click(object sender, EventArgs e)
        {
            try
            {
                var localIPs = GetLocalIPAddresses();
                if (localIPs.Count > 0)
                {
                    var ipList = string.Join(", ", localIPs);
                    txtServerIP.Text = localIPs[0]; // 첫 번째 IP를 기본값으로 설정
                    LogMessage($"기본 IP로 설정됨: {localIPs[0]}");
                }
                else
                {
                    LogMessage("로컬 IP 주소를 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"IP 주소 가져오기 실패: {ex.Message}");
            }
        }

        private void Connect()
        {
            try
            {
                if (string.IsNullOrEmpty(txtServerIP.Text) || string.IsNullOrEmpty(txtServerPorts.Text))
                {
                    MessageBox.Show("서버 IP와 포트들을 입력해주세요.", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!IPAddress.TryParse(txtServerIP.Text, out _))
                {
                    MessageBox.Show("올바른 IP 주소를 입력해주세요.", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 포트 목록 파싱 (개별 포트와 범위 지원)
                targetPorts.Clear();
                portReceiveStatus.Clear();
                var portStrings = txtServerPorts.Text.Split(',', ';', ' ', '\t', '\n', '\r')
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim());

                foreach (var portString in portStrings)
                {
                    if (portString.Contains('-'))
                    {
                        // 포트 범위 처리 (예: 7777-7710)
                        var rangeParts = portString.Split('-');
                        if (rangeParts.Length == 2)
                        {
                            if (int.TryParse(rangeParts[0].Trim(), out int startPort) && 
                                int.TryParse(rangeParts[1].Trim(), out int endPort))
                            {
                                if (startPort >= 1 && startPort <= 65535 && 
                                    endPort >= 1 && endPort <= 65535)
                                {
                                    if (startPort <= endPort)
                                    {
                                        // 정방향 범위 (7777-7710)
                                        for (int port = startPort; port <= endPort; port++)
                                        {
                                            targetPorts.Add(port);
                                            portReceiveStatus[port] = false;
                                        }
                                        LogMessage($"포트 범위 추가: {startPort}-{endPort} ({endPort - startPort + 1}개 포트)");
                                    }
                                    else
                                    {
                                        // 역방향 범위 (7710-7777)
                                        for (int port = startPort; port >= endPort; port--)
                                        {
                                            targetPorts.Add(port);
                                            portReceiveStatus[port] = false;
                                        }
                                        LogMessage($"포트 범위 추가: {startPort}-{endPort} ({startPort - endPort + 1}개 포트)");
                                    }
                                }
                                else
                                {
                                    MessageBox.Show($"포트 범위가 유효하지 않습니다: {portString} (1-65535 범위)", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show($"포트 범위 형식이 올바르지 않습니다: {portString}", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show($"포트 범위 형식이 올바르지 않습니다: {portString}", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        // 개별 포트 처리
                        if (int.TryParse(portString, out int port) && port >= 1 && port <= 65535)
                        {
                            targetPorts.Add(port);
                            portReceiveStatus[port] = false; // 초기화
                        }
                        else
                        {
                            MessageBox.Show($"올바르지 않은 포트 번호입니다: {portString}", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                if (targetPorts.Count == 0)
                {
                    MessageBox.Show("최소 하나의 유효한 포트를 입력해주세요.", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 포트 개수가 너무 많으면 경고
                if (targetPorts.Count > 100)
                {
                    var result = MessageBox.Show($"포트 개수가 많습니다 ({targetPorts.Count}개). 계속 진행하시겠습니까?", 
                        "포트 개수 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }

                // UDP 클라이언트가 없으면 새로 초기화
                if (udpClient == null)
                {
                    InitializeUDPClient();
                }
                
                // 연결 테스트를 위한 간단한 메시지 전송
                var testMessage = "CONNECTION_TEST";
                var data = Encoding.UTF8.GetBytes(testMessage);
                
                foreach (var port in targetPorts)
                {
                    var serverEndPoint = new IPEndPoint(IPAddress.Parse(txtServerIP.Text), port);
                    udpClient?.Send(data, data.Length, serverEndPoint);
                }
                
                // 수신 확인을 위한 타이머 시작 (3초 후)
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 3000;
                timer.Tick += (s, e) => {
                    timer.Stop();
                    CheckReceiveStatus();
                };
                timer.Start();
                
                isConnected = true;
                btnConnect.Text = "연결 해제";
                btnConnect.BackColor = Color.LightCoral;
                
                txtServerIP.Enabled = false;
                txtServerPorts.Enabled = false;
                
                if (targetPorts.Count <= 10)
                {
                    LogMessage($"서버에 연결되었습니다: {txtServerIP.Text} -> 포트들: {string.Join(", ", targetPorts)}");
                }
                else
                {
                    LogMessage($"서버에 연결되었습니다: {txtServerIP.Text} -> 포트들: {targetPorts.Count}개 ({targetPorts.Min()}-{targetPorts.Max()})");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"연결 실패: {ex.Message}");
                MessageBox.Show($"연결에 실패했습니다: {ex.Message}", "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Disconnect()
        {
            isConnected = false;
            btnConnect.Text = "연결";
            btnConnect.BackColor = SystemColors.Control;
            
            txtServerIP.Enabled = true;
            txtServerPorts.Enabled = true;
            
            targetPorts.Clear();
            portReceiveStatus.Clear();
            
            // UDP 클라이언트 안전하게 해제
            try
            {
                udpClient?.Close();
                udpClient?.Dispose();
            }
            catch (Exception ex)
            {
                LogMessage($"UDP 클라이언트 해제 중 오류: {ex.Message}");
            }
            finally
            {
                udpClient = null;
            }
            
            LogMessage("서버 연결이 해제되었습니다.");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("먼저 서버에 연결해주세요.", "전송 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtMessage.Text))
            {
                MessageBox.Show("전송할 메시지를 입력해주세요.", "전송 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SendMessage();
        }

        private void SendMessage()
        {
            try
            {
                var message = txtMessage.Text;
                var data = Encoding.UTF8.GetBytes(message);
                var serverIP = IPAddress.Parse(txtServerIP.Text);
                
                int successCount = 0;
                int failCount = 0;
                
                foreach (var port in targetPorts)
                {
                    try
                    {
                        var serverEndPoint = new IPEndPoint(serverIP, port);
                        udpClient?.Send(data, data.Length, serverEndPoint);
                        successCount++;
                        LogMessage($"전송 성공 -> {serverIP}:{port}: {message}");
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        LogMessage($"전송 실패 -> {serverIP}:{port}: {ex.Message}");
                    }
                }
                
                LogMessage($"전송 완료: 성공 {successCount}개, 실패 {failCount}개");
                
                // 수신 확인을 위한 타이머 시작 (2초 후)
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) => {
                    timer.Stop();
                    CheckReceiveStatus();
                };
                timer.Start();
                
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                LogMessage($"전송 실패: {ex.Message}");
                MessageBox.Show($"메시지 전송에 실패했습니다: {ex.Message}", "전송 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSendTestData_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("먼저 서버에 연결해주세요.", "전송 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SendTestData();
        }

        private void SendTestData()
        {
            try
            {
                var testData = new
                {
                    type = "test",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    message = "UDP 테스트 데이터",
                    randomValue = new Random().Next(1, 1000)
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(testData);
                var data = Encoding.UTF8.GetBytes(jsonData);
                var serverIP = IPAddress.Parse(txtServerIP.Text);
                
                int successCount = 0;
                int failCount = 0;
                
                foreach (var port in targetPorts)
                {
                    try
                    {
                        var serverEndPoint = new IPEndPoint(serverIP, port);
                        udpClient?.Send(data, data.Length, serverEndPoint);
                        successCount++;
                        LogMessage($"테스트 데이터 전송 성공 -> {serverIP}:{port}");
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        LogMessage($"테스트 데이터 전송 실패 -> {serverIP}:{port}: {ex.Message}");
                    }
                }
                
                LogMessage($"테스트 데이터 전송 완료: 성공 {successCount}개, 실패 {failCount}개");
                LogMessage($"테스트 데이터 내용: {jsonData}");
                
                // 수신 확인을 위한 타이머 시작 (2초 후)
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) => {
                    timer.Stop();
                    CheckReceiveStatus();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                LogMessage($"테스트 데이터 전송 실패: {ex.Message}");
                MessageBox.Show($"테스트 데이터 전송에 실패했습니다: {ex.Message}", "전송 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                if (isConnected)
                {
                    SendMessage();
                }
            }
        }

        private List<string> GetLocalIPAddresses()
        {
            var ipAddresses = new List<string>();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddresses.Add(ip.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"IP 주소 조회 중 오류: {ex.Message}");
            }
            return ipAddresses;
        }

        private void CheckReceiveStatus()
        {
            if (!isConnected || targetPorts.Count == 0)
                return;

            var receivedPorts = new List<int>();
            var notReceivedPorts = new List<int>();

            foreach (var port in targetPorts)
            {
                if (portReceiveStatus.ContainsKey(port) && portReceiveStatus[port])
                {
                    receivedPorts.Add(port);
                }
                else
                {
                    notReceivedPorts.Add(port);
                }
            }

            // 상태 초기화
            foreach (var port in targetPorts)
            {
                portReceiveStatus[port] = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            udpClient?.Close();
            base.OnFormClosing(e);
        }
    }
}
