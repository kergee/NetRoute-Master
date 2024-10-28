using RouteManager;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

namespace NetRouteMaster
{
    public partial class MainWindow : Window
    {
        private readonly Settings settings;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConfiguring = false;

        public MainWindow()
        {
            InitializeComponent();
            CheckAdminRights();
            settings = Settings.Load();
            LoadNetworkInterfaces();
            InitializeUI();
        }

        private void CheckAdminRights()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show("请以管理员身份运行此程序！\n\n路由配置需要管理员权限。",
                    "需要管理员权限",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // 重启为管理员权限
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    Process.Start(startInfo);
                    Application.Current.Shutdown();
                    return;
                }
                catch (Exception)
                {
                    MessageBox.Show("无法获取管理员权限，程序将退出。",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
        }

        private void UpdateProgressUI(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                ConfigProgress.Value = (double)current / total * 100;
                ProgressText.Text = $"{current}/{total}";
            });
        }

        private void EnableConfigurationControls(bool enable)
        {
            StartButton.IsEnabled = enable;
            StopButton.IsEnabled = !enable;
            NetworkInterfacesComboBox.IsEnabled = enable;
            _isConfiguring = !enable;
        }

        private async void StartConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_isConfiguring) return;

            if (NetworkInterfacesComboBox.SelectedItem is not NetworkInterfaceInfo selectedInterface)
            {
                MessageBox.Show("请选择网卡接口！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                EnableConfigurationControls(false);
                ConfigProgress.Value = 0;

                await ConfigureRoutesAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                LogMessage("配置已停止");
            }
            catch (Exception ex)
            {
                LogMessage($"错误: {ex.Message}");
            }
            finally
            {
                EnableConfigurationControls(true);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }


        private void StopConfig_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task ConfigureRoutesAsync(CancellationToken cancellationToken)
        {
            var urls = settings.Urls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList();
            var ips = settings.IPs.Where(ip => !string.IsNullOrWhiteSpace(ip)).ToList();
            int total = urls.Count + ips.Count;
            int current = 0;

            // Configure URLs
            foreach (string url in urls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LogMessage($"正在处理URL: {url}");
                string ip = await ResolveHostnameAsync(url);

                if (!string.IsNullOrWhiteSpace(ip))
                {
                    await AddRouteAsync(ip);
                }

                current++;
                UpdateProgressUI(current, total);
            }

            // Configure IPs
            foreach (string ip in ips)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await AddRouteAsync(ip);
                current++;
                UpdateProgressUI(current, total);
            }

            LogMessage("路由配置完成");
        }

        private async Task<bool> AddRouteAsync(string ip)
        {
            try
            {
                string command = $"route ADD {ip} MASK 255.255.255.255 {settings.DefaultInterface}";
                LogMessage($"执行: {command}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas"  // 使用管理员权限运行
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        LogMessage($"添加路由失败 {ip}: {error}");
                        return false;
                    }

                    if (!string.IsNullOrEmpty(output))
                        LogMessage(output.Trim());

                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"添加路由失败 {ip}: {ex.Message}");
                return false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isConfiguring)
            {
                var result = MessageBox.Show(
                    "正在配置路由，确定要退出吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _cancellationTokenSource?.Cancel();
            base.OnClosing(e);
        }



        private void LoadNetworkInterfaces()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                           ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(ni => new NetworkInterfaceInfo
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    IPAddress = ni.GetIPProperties()
                        .UnicastAddresses
                        .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        ?.Address.ToString()
                })
                .Where(nii => !string.IsNullOrEmpty(nii.IPAddress))
                .ToList();

            NetworkInterfacesComboBox.ItemsSource = interfaces;

            // Select the previously saved interface if it exists
            var savedInterface = interfaces.FirstOrDefault(i => i.IPAddress == settings.DefaultInterface);
            if (savedInterface != null)
            {
                NetworkInterfacesComboBox.SelectedItem = savedInterface;
            }
            else if (interfaces.Any())
            {
                NetworkInterfacesComboBox.SelectedIndex = 0;
            }
        }

        private void NetworkInterfacesComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (NetworkInterfacesComboBox.SelectedItem is NetworkInterfaceInfo selectedInterface)
            {
                settings.DefaultInterface = selectedInterface.IPAddress;
                UpdateSelectedInterfaceInfo(selectedInterface);
            }
        }

        private void UpdateSelectedInterfaceInfo(NetworkInterfaceInfo interfaceInfo)
        {
            SelectedInterfaceInfo.Text = $"已选择: {interfaceInfo.Description} ({interfaceInfo.IPAddress})";
        }

        private void RefreshInterfaces_Click(object sender, RoutedEventArgs e)
        {
            LoadNetworkInterfaces();
            LogMessage("网卡列表已刷新");
        }

        private void InitializeUI()
        {
            UrlListTextBox.Text = string.Join(Environment.NewLine, settings.Urls);
            IpListTextBox.Text = string.Join(Environment.NewLine, settings.IPs);
        }

        private void NewUrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddUrl_Click(sender, null);
            }
        }

        private void NewIpTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddIp_Click(sender, null);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkInterfacesComboBox.SelectedItem is NetworkInterfaceInfo selectedInterface)
            {
                settings.DefaultInterface = selectedInterface.IPAddress;
            }

            settings.Urls = UrlListTextBox.Text
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .ToList();

            settings.IPs = IpListTextBox.Text
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .ToList();

            settings.Save();
            LogMessage("设置已保存");
        }

        private void AddUrl_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewUrlTextBox.Text))
            {
                UrlListTextBox.Text += Environment.NewLine + NewUrlTextBox.Text;
                NewUrlTextBox.Clear();
            }
        }

        private void AddIp_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewIpTextBox.Text))
            {
                IpListTextBox.Text += Environment.NewLine + NewIpTextBox.Text;
                NewIpTextBox.Clear();
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private void LogMessage(string message)
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        }

        private async Task ConfigureRoutesAsync()
        {
            try
            {
                // Configure URLs
                foreach (string url in settings.Urls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    LogMessage($"正在处理URL: {url}");
                    string ip = await ResolveHostnameAsync(url);

                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        await AddRouteAsync(ip);
                    }
                }

                // Configure IPs
                foreach (string ip in settings.IPs)
                {
                    if (string.IsNullOrWhiteSpace(ip)) continue;

                    await AddRouteAsync(ip);
                }

                LogMessage("路由配置完成");
            }
            catch (Exception ex)
            {
                LogMessage($"错误: {ex.Message}");
            }
        }

        private async Task<string> ResolveHostnameAsync(string hostname)
        {
            try
            {
                var pingReply = await new Ping().SendPingAsync(hostname);
                string ip = pingReply.Address.ToString();
                LogMessage($"解析 {hostname} => {ip}");
                return ip;
            }
            catch (Exception ex)
            {
                LogMessage($"解析 {hostname} 失败: {ex.Message}");
                return null;
            }
        }
        
    }
    public class NetworkInterfaceInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
    }
}
