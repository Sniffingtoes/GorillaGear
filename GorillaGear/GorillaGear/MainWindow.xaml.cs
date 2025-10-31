using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GorillaGear
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient { DefaultRequestHeaders = { { "User-Agent", "GorillaGear/1.0" } } };
        private readonly string configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GorillaGear");
        private readonly string configPath;
        private readonly List<ModInfo> _allMods = new List<ModInfo>
        {
            new ModInfo { Name = "Gorilla Shirts", Description = "wear custom shirts in gorilla tag or even create them", Repo="developer9998/GorillaShirts"},
            new ModInfo { Name = "Stupid Menu", Description = "A gorilla tag mod menu with 1000+ mods", Repo="iiDk-the-actual/iis.Stupid.Menu"},
            new ModInfo { Name = "Emote Wheel", Description = "A fortnite emote wheel for gorilla tag with a wide variety of emotes from the game fortnite", Repo="iiDk-the-actual/FortniteEmoteWheel"},
            new ModInfo { Name = "Monke Phone", Description = "A phone in gorilla tag with a wide variety of features", Repo="developer9998/MonkePhone"},
            new ModInfo { Name = "Gorilla Luau", Description = "This mod unlocks the ability to execute Luau code directly within Gorilla Tag", Repo="severedcli/GorillaLuau"},
            new ModInfo { Name = "Shirt Pad", Description = "Mod that lets you shirt it from everywhere", Repo="ZlothY29IQ/ShirtsPad"},
            new ModInfo { Name = "Dingus", Description = "Dingus", Repo="ZlothY29IQ/dingus"},
            new ModInfo { Name = "Oculus Report Menu", Description = "Access the OculusReportMenu (a portable leaderboard) on SteamVR and Oculus Rift headsets by pressing the right thumbstick in and pressing the left secondary", Repo="sirkingbinx/OculusReportMenu"},
            new ModInfo { Name = "Too Much Info", Description = "A mod for Gorilla Tag that gives you too much info about a person", Repo="iiDk-the-actual/TooMuchInfo"},
            new ModInfo { Name = "Gorilla Hands", Description = "GorillaHands is a plugin/mod for Gorilla Tag. It adds two large, human-like hands that the player can use to grab surfaces and move around the map", Repo="CrafterBotOfficial/GorillaHands"},
        };

        private ModInfo _currentMod;
        private string GtFolder => txtGtPath.Text.Trim();

        public MainWindow()
        {
            configPath = Path.Combine(configFolder, "config.json");
            InitializeComponent();
            Directory.CreateDirectory(configFolder);
            LoadConfig();
            BuildModButtons();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = JObject.Parse(File.ReadAllText(configPath));
                    string path = (string)json["GtFolder"];
                    if (Directory.Exists(path)) txtGtPath.Text = path;
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                var json = new JObject { ["GtFolder"] = GtFolder };
                File.WriteAllText(configPath, json.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void BuildModButtons()
        {
            modButtonsPanel.Children.Clear();
            foreach (var mod in _allMods)
            {
                var btn = new Button
                {
                    Width = 130,
                    Height = 90,
                    Style = (Style)FindResource("SquareModButton"),
                    Tag = mod,
                    Content = mod.Name
                };
                btn.Click += ModButton_Click;
                modButtonsPanel.Children.Add(btn);
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog { Description = "Select the folder that contains GorillaTag.exe", UseDescriptionForTitle = true };
            if (dlg.ShowDialog() == true)
            {
                txtGtPath.Text = dlg.SelectedPath;
                SaveConfig();
            }
        }

        private async void InstallBepInEx_Click(object sender, RoutedEventArgs e) => await InstallBepInExAsync();
        private async void RepairCore_Click(object sender, RoutedEventArgs e) => await InstallBepInExAsync(true);

        private void ModButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMod = (sender as Button)?.Tag as ModInfo;
            if (_currentMod == null) return;

            txtModName.Text = _currentMod.Name;
            txtModDesc.Text = _currentMod.Description;
            btnInstall.Content = "Install";
            txtInstallStatus.Text = "";
            infoPanel.Visibility = Visibility.Visible;
        }

        private void CloseInfo_Click(object sender, RoutedEventArgs e) => infoPanel.Visibility = Visibility.Collapsed;

        private async void InstallMod_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMod != null)
                await InstallModFromRepo(_currentMod);
        }

        private async Task InstallBepInExAsync(bool repair = false)
        {
            if (!Directory.Exists(GtFolder)) { MessageBox.Show("Select your Gorilla Tag folder first."); return; }

            progressBar.Visibility = Visibility.Visible;
            txtStatus.Text = "Installing BepInEx and Utilla...";

            string tempZip = Path.GetTempFileName() + ".zip";
            string tempExtract = Path.Combine(Path.GetTempPath(), "BepInExExtract");
            if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);

            string bepUrl = "https://github.com/BepInEx/BepInEx/releases/latest/download/BepInEx_win_x64_5.4.23.4.zip";
            using (var fs = new FileStream(tempZip, FileMode.Create))
            {
                var resp = await _http.GetAsync(bepUrl);
                await resp.Content.CopyToAsync(fs);
            }

            ZipFile.ExtractToDirectory(tempZip, tempExtract);

            string topBepInEx = Path.Combine(tempExtract, "BepInEx");
            if (!Directory.Exists(topBepInEx)) topBepInEx = tempExtract;

            string coreDir = Path.Combine(GtFolder, "BepInEx");
            Directory.CreateDirectory(coreDir);

            foreach (string entry in Directory.GetFileSystemEntries(topBepInEx))
            {
                string destName = Path.GetFileName(entry);
                if (destName.Equals("plugins", StringComparison.OrdinalIgnoreCase)) continue;
                if (Directory.Exists(entry))
                    CopyDirectory(entry, Path.Combine(coreDir, destName));
                else
                    File.Copy(entry, Path.Combine(coreDir, destName), true);
            }

            string pluginsDir = Path.Combine(GtFolder, "BepInEx", "Plugins");
            string utillaDir = Path.Combine(pluginsDir, "Utilla");
            Directory.CreateDirectory(utillaDir);

            string utillaApiUrl = "https://api.github.com/repos/legoandmars/Utilla/releases/latest";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "GorillaGear/1.0");
                var json = await client.GetStringAsync(utillaApiUrl);
                var data = JObject.Parse(json);
                var assets = data["assets"] as JArray;

                var zipAsset = assets?.FirstOrDefault(a => a["name"].ToString().EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (zipAsset != null)
                {
                    string zipUrl = zipAsset["browser_download_url"].ToString();
                    string utillaZip = Path.GetTempFileName() + ".zip";  
                    using (var fs = new FileStream(utillaZip, FileMode.Create))
                    {
                        var resp = await client.GetAsync(zipUrl);
                        await resp.Content.CopyToAsync(fs);
                    }

                    string utillaExtract = Path.Combine(Path.GetTempPath(), "UtillaExtract"); // renamed
                    if (Directory.Exists(utillaExtract)) Directory.Delete(utillaExtract, true);
                    ZipFile.ExtractToDirectory(utillaZip, utillaExtract);

                    var dllFiles = Directory.GetFiles(utillaExtract, "Utilla.dll", SearchOption.AllDirectories);
                    foreach (var dll in dllFiles)
                    {
                        File.Copy(dll, Path.Combine(utillaDir, "Utilla.dll"), true);
                    }

                    File.Delete(utillaZip);
                    Directory.Delete(utillaExtract, true);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);

            foreach (string folder in Directory.GetDirectories(sourceDir))
                CopyDirectory(folder, Path.Combine(destDir, Path.GetFileName(folder)));
        }

        private async Task InstallModFromRepo(ModInfo mod)
        {
            if (!Directory.Exists(GtFolder)) return;
            progressBar.Visibility = Visibility.Visible;
            txtStatus.Text = "Installing " + mod.Name + "...";

            string apiUrl = $"https://api.github.com/repos/{mod.Repo}/releases/latest";
            var json = await _http.GetStringAsync(apiUrl);
            var data = JObject.Parse(json);
            var assets = data["assets"] as JArray;
            var dllAsset = assets.FirstOrDefault(a => a["name"].ToString().EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            if (dllAsset == null) { progressBar.Visibility = Visibility.Collapsed; return; }

            string dllUrl = dllAsset["browser_download_url"].ToString();
            string fileName = dllAsset["name"].ToString();
            string coreDir = Path.Combine(GtFolder, "BepInEx", "Plugins");
            Directory.CreateDirectory(coreDir);
            string dest = Path.Combine(coreDir, fileName);

            using (var fs = new FileStream(dest, FileMode.Create))
            {
                var resp = await _http.GetAsync(dllUrl);
                await resp.Content.CopyToAsync(fs);
            }

            progressBar.Visibility = Visibility.Collapsed;
            txtInstallStatus.Text = "Installed";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
    }

    public class ModInfo
    {
        public string Name { get; set; }
        public string Repo { get; set; }
        public string Description { get; set; }
    }
}
