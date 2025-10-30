// Version: 0.0.0.3
// HlsCreator.cs
// .NET Framework 4.8 (WPF) — klasa rozszerzona o:
// - Obsługę anulowania (CancellationToken)
// - Opcję transkodowania do H.264/AAC (jeśli copy stream nie jest zgodny z HLS)
// - Możliwość generowania prostego M3U8 bez segmentacji
// - Dostosowanie formatu i nazewnictwa segmentów

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Creators
{
    /// <summary>
    /// Klasa do tworzenia HLS (HTTP Live Streaming) z plików wideo za pomocą ffmpeg.
    /// Pozwala na segmentację wideo na mniejsze części oraz generowanie playlisty M3U8.
    /// Obsługuje anulowanie operacji oraz logowanie postępu.
    /// </summary>
    public class HlsCreator
    {
        /// <summary>
        /// Ścieżka do pliku wykonywalnego ffmpeg.
        /// </summary>
        public string FfmpegPath { get; set; }
        /// <summary>
        /// Czas trwania pojedynczego segmentu w sekundach.
        /// </summary>
        public int SegmentDurationSeconds { get; set; } = 10;
        /// <summary>
        /// Nazwa pliku playlisty M3U8.
        /// </summary>
        public string PlaylistFileName { get; set; } = "playlist.m3u8";
        /// <summary>
        /// Wzorzec nazwy plików segmentów.
        /// </summary>
        public string SegmentPattern { get; set; } = "segment_{0:D3}.ts"; // format nazwy segmentów
        /// <summary>
        /// Jeśli true, wymusza transkodowanie do H.264/AAC zamiast kopiowania strumieni.
        /// </summary>
        public bool ForceTranscode { get; set; } = false; // jeśli true -> transkoduje do H.264/AAC
        /// <summary>
        /// Jeśli true, generuje prostą playlistę M3U8 bez segmentacji (jeden plik).
        /// </summary>
        public bool SimplePlaylist { get; set; } = false; // jeśli true -> generuje jedno M3U8 bez segmentacji

        /// <summary>
        /// Delegat do logowania komunikatów z ffmpeg.
        /// </summary>
        public Action<string> Log { get; set; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy HlsCreator.
        /// </summary>
        /// <param name="ffmpegPath">ścieżka do kompilacji ffmpeg</param>
        public HlsCreator(string ffmpegPath = "ffmpeg")
        {
            FfmpegPath = ffmpegPath;
        }

        /// <summary>
        /// Tworzy HLS z podanego pliku wideo i zapisuje segmenty oraz playlistę w określonym katalogu.
        /// </summary>
        /// <param name="inputFile">Plik wejściowy</param>
        /// <param name="outputDirectory">Folder docelowy</param>
        /// <param name="token">Token anulowania operacji</param>
        /// <returns>Wykonuje zadanie tworzenia pliku .m3u8 z pliku multimedialnego</returns>
        /// <exception cref="ArgumentException">Wyjątek inputFile: null albo spacja</exception>
        /// <exception cref="FileNotFoundException">Wyjątek nie można odnaleźć ścieżki do folderu</exception>
        public Task CreateFromFileAsync(string inputFile, string outputDirectory, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(inputFile)) throw new ArgumentException("inputFile");
            if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("outputDirectory");
            if (!File.Exists(inputFile)) throw new FileNotFoundException("Plik wejściowy nie istnieje", inputFile);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string playlistPath = Path.Combine(outputDirectory, PlaylistFileName);
            string args;

            if (SimplePlaylist)
            {
                // Tworzy jedno .m3u8 wskazujące na cały plik (bez segmentacji)
                // ffmpeg generuje kontener HLS bez dzielenia (dla kompatybilności)
                args = $"-y -i \"{inputFile}\" -codec copy -f hls -hls_list_size 1 -hls_segment_filename \"{Path.Combine(outputDirectory, SegmentPattern)}\" \"{playlistPath}\"";
            }
            else
            {
                string segmentPatternFull = Path.Combine(outputDirectory, SegmentPattern);
                var codecParam = ForceTranscode ? "-c:v h264 -c:a aac -b:a 128k " : "-codec copy ";

                args = new StringBuilder()
                    .AppendFormat("-y -i \"{0}\" ", inputFile)
                    .Append(codecParam)
                    .AppendFormat("-start_number 0 -hls_time {0} -hls_list_size 0 ", SegmentDurationSeconds)
                    .AppendFormat("-hls_segment_filename \"{0}\" \"{1}\"", segmentPatternFull, playlistPath)
                    .ToString();
            }

            return RunProcessAsync(FfmpegPath, args, token);
        }

        private Task RunProcessAsync(string fileName, string arguments, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            proc.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log?.Invoke(e.Data); };
            proc.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log?.Invoke(e.Data); };

            proc.Exited += (s, e) =>
            {
                try
                {
                    if (proc.ExitCode == 0)
                        tcs.TrySetResult(true);
                    else
                        tcs.TrySetException(new Exception($"ffmpeg zakończył się z kodem {proc.ExitCode}"));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    proc.Dispose();
                }
            };

            token.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                        Log?.Invoke("Proces ffmpeg został anulowany.");
                    }
                    tcs.TrySetCanceled();
                }
                catch { }
            });

            try
            {
                if (!proc.Start())
                {
                    tcs.SetException(new Exception("Nie udało się uruchomić procesu ffmpeg."));
                }
                else
                {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }
    }
}

/*
UŻYCIE (przykład w WPF):

private CancellationTokenSource _cts;

private async void Button_Start_Click(object sender, RoutedEventArgs e)
{
    _cts = new CancellationTokenSource();
    var creator = new MyApp.Utils.HlsCreator("ffmpeg")
    {
        SegmentDurationSeconds = 6,
        ForceTranscode = true, // transkodowanie do H.264/AAC
        SimplePlaylist = false, // false = segmentacja, true = pojedynczy plik
        SegmentPattern = "vid_part_{0:D4}.ts",
        Log = s => Dispatcher.Invoke(() => Debug.WriteLine(s))
    };

    try
    {
        await Task.Run(() => creator.CreateFromFileAsync("C:\\video.mp4", "C:\\hls_output", _cts.Token)).Unwrap();
        MessageBox.Show("Gotowe!");
    }
    catch (TaskCanceledException)
    {
        MessageBox.Show("Anulowano.");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Błąd: {ex.Message}");
    }
}

private void Button_Cancel_Click(object sender, RoutedEventArgs e)
{
    _cts?.Cancel();
}
*/
