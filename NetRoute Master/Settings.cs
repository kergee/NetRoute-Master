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
            
        };

        // 默认IP列表
        private static readonly string[] DefaultIPs = new[]
        {
            
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
