using System.IO;
using System.Text.Json;

namespace RouteManager
{
    public class Settings
    {
        private static readonly string SettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");

        // 默认URL列表
        private static readonly string[] DefaultUrls = new[]
        {
            "files.oaiusercontent.com",
            "discord.com",
            "ab.chatgpt.com",
            "zh.z-lib.gs",
            "www.perplexity.ai",
            "o4504136533147648.ingest.us.sentry.io",
            "static.cloudflareinsights.com",
            "s.gravatar.com",
            "platform.openai.com",
            "poe.com",
            "www.duolingo.com",
            "auth0.openai.com",
            "auth.openai.com",
            "store.steampowered.com",
            "www.notion.so",
            "www.perplexity.ai",
            "dash.cloudflare.com",
            "anthropic.com",
            "claude.ai",
            "vercel.app",
            "chromewebstore.google.com",
            "objects.githubusercontent.com",
            "googleusercontent.com",
            "bbc.com",
            "www.goodreads.com",
            "proxy.golang.org",
            "mlook.mobi",
            "status.openai.com",
            "chatgpt.com",
            "cdn.oaistatic.com",
            "tcr9i.chat.openai.com",
            "chat.openai.com",
            "feedly.com",
            "github.com",
            "v2ex.com",
            "www.google.com",
            "www.google.com.hk",
            "gmail.com",
            "signaler-pa.clients6.google.com",
            "lh3.googleusercontent.com",
            "apis.google.com",
            "ci3.googleusercontent.com",
            "mail.google.com",
            "ssl.gstatic.com",
            "play.google.com",
            "fonts.google.com",
            "accounts.google.com",
            "people-pa.clients6.google.com",
            "signaler-pa.clients6.google.com",
            "taskassist-pa.clients6.google.com",
            "waa-pa.clients6.google.com",
            "ogs.google.com",
            "huggingface.co",
            "cdn-lfs-us-1.huggingface.co",
            "cdn-lfs.huggingface.co",
            "browser-intake-datadoghq.com"
        };

        // 默认IP列表
        private static readonly string[] DefaultIPs = new[]
        {
            "112.83.140.14",
            "180.210.212.138",
            "116.153.80.1",
            "120.46.84.174",
            "60.28.214.185",
            "120.131.12.37",
            "103.52.155.88",
            "140.82.113.26",
            "35.186.227.140",
            "203.119.245.36",
            "104.21.3.198",
            "210.22.248.229",
            "204.79.197.200",
            "61.241.122.189",
            "218.98.41.10",
            "218.98.41.18",
            "172.64.154.167",
            "52.109.52.2",
            "111.206.149.205",
            "40.99.34.2",
            "34.110.207.168",
            "204.79.197.239",
            "157.148.50.184",
            "140.207.62.227",
            "221.194.131.176",
            "123.182.48.117",
            "59.82.23.82",
            "34.120.208.123",
            "112.83.164.235",
            "34.107.243.93",
            "34.149.100.209",
            "20.69.137.228",
            "124.223.145.123",
            "112.65.195.240",
            "140.207.236.194",
            "20.198.162.76",
            "91.108.56.190",
            "8.8.4.4",
            "162.159.61.4",
            "142.250.191.74",
            "149.154.175.52",
            "149.154.175.50",
            "116.128.171.238",
            "20.42.144.52",
            "140.205.70.177",
            "122.195.90.189",
            "192.0.84.247",
            "124.223.145.148",
            "35.190.80.1",
            "3.164.110.79",
            "61.133.101.58",
            "180.111.196.239",
            "180.210.212.142"
        };

        public string DefaultInterface { get; set; } = "192.168.200.1";
        public List<string> Urls { get; set; }
        public List<string> IPs { get; set; }

        public Settings()
        {
            // 初始化默认值
            Urls = new List<string>(DefaultUrls);
            IPs = new List<string>(DefaultIPs);
        }

        public void Save()
        {
            string directoryPath = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);

                    // 确保从文件加载的设置包含所有默认值
                    if (settings != null)
                    {
                        // 合并默认URL和已保存的URL，去重
                        settings.Urls = settings.Urls
                            .Union(DefaultUrls)
                            .Distinct()
                            .ToList();

                        // 合并默认IP和已保存的IP，去重
                        settings.IPs = settings.IPs
                            .Union(DefaultIPs)
                            .Distinct()
                            .ToList();

                        return settings;
                    }
                }
            }
            catch (Exception)
            {
                // 如果加载失败，返回默认设置
            }

            return new Settings();
        }
    }
}
