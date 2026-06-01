using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MP3Converter
{
    public partial class MainWindow : Window
    {
        private readonly string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        private readonly string enginePath = AppDomain.CurrentDomain.BaseDirectory;

        private System.Timers.Timer ceviriTimer;
        private double fakeCeviriProgress = 50;
        private DispatcherTimer previewTimer;
        private Encoding turkishEncoding;
        private Process activeProcess;

        private bool isFirstActivation = true;
        private bool isUpdatingMotor = false;
        private bool isCancelled = false;
        private bool isPlaylistMode = false;
        private int currentPlaylistItem = 1;
        private int totalPlaylistItems = 1;
        private DateTime processStartTime;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                MessageBox.Show($"Kritik Sistem Hatası:\n{e.ExceptionObject}", "Sistem Çökmesi", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            InitializeComponent();

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                turkishEncoding = Encoding.GetEncoding(1254);
            }
            catch
            {
                turkishEncoding = Encoding.UTF8;
            }

            previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            previewTimer.Tick += PreviewTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                isUpdatingMotor = true;
                ToggleControls(false);

                statusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"));
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                statusBar.IsIndeterminate = true;

                await CheckAndUpdateMotorAsync();
            }
            catch (Exception ex)
            {
                ShowCustomMessage("Açılış Hatası", ex.Message, true);
            }
        }

        private void ToggleControls(bool isEnabled)
        {
            txtUrl.IsEnabled = isEnabled;
            btnDownload.IsEnabled = isEnabled;
            cb.IsEnabled = isEnabled;
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e) => infoOverlay.Visibility = Visibility.Visible;
        private void btnCloseInfo_Click(object sender, RoutedEventArgs e) => infoOverlay.Visibility = Visibility.Collapsed;
        private void btnMessageOk_Click(object sender, RoutedEventArgs e) => messageOverlay.Visibility = Visibility.Collapsed;

        private void ShowCustomMessage(string title, string message, bool isError = false)
        {
            lblMessageTitle.Text = isError ? $"❌ {title}" : $"⚠️ {title}";
            lblMessageTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#FF3333" : "#FFA500"));
            lblMessageText.Text = message;
            messageOverlay.Visibility = Visibility.Visible;
        }

        private async Task CheckAndUpdateMotorAsync()
        {
            try
            {
                lblStatus.Text = "Altyapı güncelliği kontrol ediliyor...";
                string ytdlpPath = Path.Combine(enginePath, "yt-dlp.exe");

                if (!File.Exists(ytdlpPath)) throw new FileNotFoundException("yt-dlp.exe motoru klasörde bulunamadı!");

                await Task.Run(() =>
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = ytdlpPath,
                        Arguments = "-U",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = turkishEncoding
                    };
                    using (Process process = Process.Start(startInfo)) { process.WaitForExit(); }
                });

                statusBar.IsIndeterminate = false;
                statusBar.Value = 100;
                statusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1DB954"));
                lblStatus.Text = "Sistem güncel. Link yapıştırılmaya hazır!";
            }
            catch (Exception ex)
            {
                statusBar.IsIndeterminate = false;
                statusBar.Value = 0;
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                lblStatus.Text = "Sistem internetsiz çalışıyor.";
                if (ex is FileNotFoundException) ShowCustomMessage("Hata", ex.Message, true);
            }
            finally
            {
                isUpdatingMotor = false;
                ToggleControls(true);
            }
        }

        private bool IsYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            url = url.Trim();
            if (url.Length > 300 || url.Contains("\n") || url.Contains("\r")) return false;

            bool isHttp = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                          url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                          url.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

            return isHttp && (url.Contains("youtube.com/") || url.Contains("youtu.be/"));
        }

        private bool IsPlaylistUrl(string url) => IsYouTubeUrl(url) && (url.Contains("list=") || url.Contains("playlist?"));

        private async void Window_Activated(object sender, EventArgs e)
        {
            if (isFirstActivation) { isFirstActivation = false; return; }
            if (isUpdatingMotor) return;

            try
            {
                if (Clipboard.ContainsText())
                {
                    string pano = Clipboard.GetText().Trim();
                    if (IsYouTubeUrl(pano) && txtUrl.Text != pano)
                    {
                        txtUrl.Text = pano;
                        previewTimer.Stop();
                        await FetchPreviewAsync(pano);
                    }
                }
            }
            catch { }
        }

        private void txtUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingMotor) return;
            string url = txtUrl.Text.Trim();

            if (IsYouTubeUrl(url))
            {
                previewTimer.Stop();
                previewTimer.Start();
            }
            else
            {
                previewTimer.Stop();
                previewPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void PreviewTimer_Tick(object sender, EventArgs e)
        {
            previewTimer.Stop();
            if (IsYouTubeUrl(txtUrl.Text.Trim())) await FetchPreviewAsync(txtUrl.Text.Trim());
        }

        private async Task FetchPreviewAsync(string url)
        {
            previewPanel.Visibility = Visibility.Visible;
            lblVideoTitle.Text = "Bilgiler çekiliyor...";
            lblVideoChannel.Text = "Lütfen bekleyin...";
            lblVideoDuration.Text = "";
            imgThumbnail.Source = null;

            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(enginePath, "yt-dlp.exe"),
                        Arguments = $"--playlist-items 1 --no-warnings --print \"%(title)s|||%(uploader)s|||%(duration_string)s|||%(thumbnail)s\" \"{url}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = turkishEncoding
                    };

                    using (Process process = Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            string[] parts = output.Trim().Split(new string[] { "|||" }, StringSplitOptions.None);
                            if (parts.Length >= 4)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    lblVideoTitle.Text = parts[0];
                                    lblVideoChannel.Text = parts[1];
                                    lblVideoDuration.Text = "Süre: " + parts[2];
                                    try { imgThumbnail.Source = new BitmapImage(new Uri(parts[3])); } catch { }
                                });
                            }
                        }
                        else
                        {
                            Dispatcher.Invoke(() => { lblVideoTitle.Text = "Bilgi alınamadı."; lblVideoChannel.Text = "Gizli veya kısıtlamalı video."; });
                        }
                    }
                }
                catch
                {
                    Dispatcher.Invoke(() => previewPanel.Visibility = Visibility.Collapsed);
                }
            });
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = txtUrl.Text.Trim();

            if (!IsYouTubeUrl(videoUrl))
            {
                ShowCustomMessage("Geçersiz Link", "Lütfen indirme işlemine başlamadan önce geçerli bir YouTube linki yapıştırın.", false);
                return;
            }

            int format = cb.SelectedIndex;
            isPlaylistMode = IsPlaylistUrl(videoUrl);
            currentPlaylistItem = 1;
            totalPlaylistItems = 1;
            isCancelled = false;

            processStartTime = DateTime.Now.AddSeconds(-1);

            btnDownload.Visibility = Visibility.Collapsed;
            btnStop.Visibility = Visibility.Visible;
            ToggleControls(false);

            statusBar.Value = 0;
            fakeCeviriProgress = 50;
            statusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
            lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            lblStatus.Text = "Bağlantı kuruluyor...";

            bool basariliMi = await IndirVeCevirAsync(videoUrl, downloadsPath, format);

            if (ceviriTimer != null) { ceviriTimer.Stop(); ceviriTimer.Dispose(); ceviriTimer = null; }

            btnDownload.Visibility = Visibility.Visible;
            btnStop.Visibility = Visibility.Collapsed;
            ToggleControls(true);

            if (isCancelled)
            {
                lblStatus.Text = "Kalıntı dosyalar temizleniyor...";
                await CleanTempFilesAsync(downloadsPath, processStartTime);

                statusBar.Value = 0;
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                lblStatus.Text = "❌ İşlem iptal edildi, sistem temizlendi.";
            }
            else if (basariliMi)
            {
                while (statusBar.Value < 100) { statusBar.Value += 2; await Task.Delay(10); }

                statusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1DB954"));
                lblStatus.Text = "✓ İşlem Başarıyla Tamamlandı! Dosya İndirilenlerde.";
                txtUrl.Clear();
                previewPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                statusBar.Value = 0;
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                lblStatus.Text = "❌ Bir hata oluştu! Linki kontrol edin.";
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            isCancelled = true;
            lblStatus.Text = "İşlem durduruluyor...";
            try { activeProcess?.Kill(true); } catch { }
        }

        private async Task CleanTempFilesAsync(string folderPath, DateTime startTime)
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Threading.Thread.Sleep(1500);
                    string[] nukeExtensions = { "*.mp3", "*.mp4", "*.m4a", "*.webm", "*.part", "*.ytdl", "*.temp", "*.tmp", "*.webp", "*.jpg" };

                    foreach (string ext in nukeExtensions)
                    {
                        string[] files = Directory.GetFiles(folderPath, ext);
                        foreach (string file in files)
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(file);
                                if (fi.CreationTime >= startTime || fi.LastWriteTime >= startTime) File.Delete(file);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            });
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(downloadsPath)) Directory.CreateDirectory(downloadsPath);
                Process.Start(new ProcessStartInfo { FileName = downloadsPath, UseShellExecute = true, Verb = "open" });
            }
            catch (Exception ex)
            {
                ShowCustomMessage("Klasör Hata", "İndirilenler klasörü açılamadı: " + ex.Message, true);
            }
        }

        private Task<bool> IndirVeCevirAsync(string url, string outputFolder, int selectedFormat)
        {
            return Task.Run(() =>
            {
                try
                {
                    string safeEnginePath = enginePath.TrimEnd(Path.DirectorySeparatorChar);
                    string safeOutputFolder = outputFolder.TrimEnd(Path.DirectorySeparatorChar);

                    string playlistArg = isPlaylistMode ? "--yes-playlist" : "--no-playlist";

                    string ytDlpArgs = selectedFormat == 0
                        ? $"{playlistArg} --newline --progress-template \"[PROGRESS] %(progress._percent_str)s\" -x --audio-format mp3 --audio-quality 4 --postprocessor-args \"ExtractAudio:-b:a 160k\" --embed-metadata --embed-thumbnail --ffmpeg-location \"{safeEnginePath}\" --windows-filenames -o \"{safeOutputFolder}\\%(title)s.%(ext)s\" \"{url}\""
                        : $"{playlistArg} --newline --progress-template \"[PROGRESS] %(progress._percent_str)s\" -f \"bestvideo+bestaudio[ext=mp4]/best\" --merge-output-format mp4 --embed-metadata --embed-thumbnail --ffmpeg-location \"{safeEnginePath}\" --windows-filenames -o \"{safeOutputFolder}\\%(title)s.%(ext)s\" \"{url}\"";

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(enginePath, "yt-dlp.exe"),
                        Arguments = ytDlpArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = turkishEncoding,
                        StandardErrorEncoding = turkishEncoding
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;

                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (isCancelled || string.IsNullOrEmpty(e.Data)) return;

                            string cleanLine = Regex.Replace(e.Data, @"\x1B\[[^@-_]*[0-9A-PRZcf-ntqry=><]", "");

                            Match playlistMatch = Regex.Match(cleanLine, @"\[download\] Downloading (?:video|item) (\d+) of (\d+)");
                            if (playlistMatch.Success)
                            {
                                currentPlaylistItem = int.Parse(playlistMatch.Groups[1].Value);
                                totalPlaylistItems = int.Parse(playlistMatch.Groups[2].Value);

                                if (ceviriTimer != null) { ceviriTimer.Stop(); ceviriTimer.Dispose(); ceviriTimer = null; }
                                fakeCeviriProgress = 50;

                                Dispatcher.BeginInvoke(new Action(() => {
                                    statusBar.Value = 0;
                                    lblStatus.Text = $"[{currentPlaylistItem}/{totalPlaylistItems}] İndirme başlıyor...";
                                }));
                                return;
                            }

                            double originalPercentage = ExtractPercentage(cleanLine);

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (isCancelled) return;
                                string prefix = isPlaylistMode ? $"[{currentPlaylistItem}/{totalPlaylistItems}] " : "";

                                if (originalPercentage >= 0)
                                {
                                    statusBar.Value = originalPercentage / 2;
                                    lblStatus.Text = $"{prefix}İndiriliyor... (%{originalPercentage:0})";
                                }
                                else if (!cleanLine.Contains("[PROGRESS]"))
                                {
                                    if ((cleanLine.Contains("[ExtractAudio]") || cleanLine.Contains("[VideoConvertor]") || cleanLine.Contains("[Merger]") || cleanLine.Contains("[ThumbnailsConvertor]") || cleanLine.Contains("[Metadata]") || cleanLine.Contains("ffmpeg")) && ceviriTimer == null)
                                    {
                                        ceviriTimer = new System.Timers.Timer(30);
                                        ceviriTimer.Elapsed += (s, ev) =>
                                        {
                                            Dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                if (isCancelled) return;
                                                if (fakeCeviriProgress < 99) fakeCeviriProgress += 0.2;

                                                statusBar.Value = fakeCeviriProgress;
                                                double yansiyanYuzde = Math.Min(100, (fakeCeviriProgress - 50) * 2);
                                                lblStatus.Text = $"{prefix}Çevriliyor... (%{yansiyanYuzde:0})";
                                            }));
                                        };
                                        ceviriTimer.Start();
                                    }
                                }
                            }));
                        };

                        process.Start();
                        activeProcess = process;
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();

                        return process.ExitCode == 0;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private double ExtractPercentage(string input)
        {
            try
            {
                Match match = Regex.Match(input, @"(\d+(\.\d+)?)%");
                if (match.Success) return double.Parse(match.Groups[1].Value.Replace("%", "").Trim(), CultureInfo.InvariantCulture);
            }
            catch { }
            return -1;
        }

        private void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (btnDownload == null) return;
            btnDownload.Content = cb.SelectedIndex == 0 ? "MP3 Olarak İndir" : "MP4 Olarak İndir";
        }
    }
}