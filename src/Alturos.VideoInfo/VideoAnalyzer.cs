using Alturos.VideoInfo.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alturos.VideoInfo
{
    public class VideoAnalyzer
    {
        private readonly string _ffprobePath;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly int _timeout;
        private Process _process;

        /// <summary>
        /// VideoAnalyzer
        /// </summary>
        /// <param name="ffprobePath"></param>
        /// <param name="timeout">max ffprobe execution time (ms)</param>
        public VideoAnalyzer(string ffprobePath = @"ffmpeg\ffprobe.exe", int timeout = 5000)
        {
            this._ffprobePath = ffprobePath;

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            this._jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            };

            this._timeout = timeout;
        }

        /// <summary>
        /// GetVideoInfo
        /// </summary>
        /// <param name="videoFilePath">Path to the video</param>
        /// <returns></returns>
        public async Task<AnalyzeResult> GetVideoInfoAsync(string videoFilePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(videoFilePath))
            {
                return new AnalyzeResult { Successful = false, ErrorMessage = "File does not exist" };
            }

            cancellationToken.Register(() => this.KillProcess());
            return await this.GetVideoInfoAsync(new MediaInput { FilePath = videoFilePath }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// GetVideoInfo
        /// </summary>
        /// <param name="data">Video data</param>
        /// <returns></returns>
        public async Task<AnalyzeResult> GetVideoInfoAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => this.KillProcess());
            return await this.GetVideoInfoAsync(new MediaInput { FileContent = data }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AnalyzeResult> GetVideoInfoAsync(MediaInput mediaInput, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(this._ffprobePath))
            {
                return new AnalyzeResult { Successful = false, ErrorMessage = $"ffprobe could not be found {this._ffprobePath}" };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = this._ffprobePath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (mediaInput.FileContent != null)
            {
                startInfo.RedirectStandardInput = true;
                startInfo.Arguments = $"-v quiet -print_format json -show_format -show_streams -";
            }
            else
            {
                startInfo.Arguments = $"-v quiet -print_format json -show_format -show_streams \"{mediaInput.FilePath}\"";
            }

            using (var outputWaitHandle = new AutoResetEvent(false))
            {
                var json = new StringBuilder();

                var dataReceived = new DataReceivedEventHandler((sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                        return;
                    }

                    json.AppendLine(e.Data);
                });

                this._process = new Process();

                try
                {
                    this._process.StartInfo = startInfo;
                    this._process.OutputDataReceived += dataReceived;

                    if (!this._process.Start())
                    {
                        return new AnalyzeResult { ErrorMessage = "Cannot start ffprobe" };
                    }

                    this._process.BeginOutputReadLine();

                    if (mediaInput.FileContent != null)
                    {
                        using (var ffprobeIn = this._process.StandardInput.BaseStream)
                        {
                            var packageSize = 100000;
                            for (var i = 0; i < mediaInput.FileContent.Length; i += packageSize)
                            {
                                var package = mediaInput.FileContent.Skip(i).Take(packageSize).ToArray();
                                await ffprobeIn.WriteAsync(package, 0, package.Length, cancellationToken);
                            }
                            await ffprobeIn.FlushAsync(cancellationToken);
                            ffprobeIn.Close();
                        }
                    }

                    if (!this._process.WaitForExit(this._timeout) || !outputWaitHandle.WaitOne(this._timeout))
                    {
                        return new AnalyzeResult { ErrorMessage = $"Timeout reached {this._timeout} (ms)" };
                    }

                    var videoInfo = JsonConvert.DeserializeObject<VideoInfoResult>(json.ToString(), this._jsonSerializerSettings);
                    if (videoInfo.Format == null && videoInfo.Streams == null)
                    {
                        return new AnalyzeResult { Successful = false, ErrorMessage = "No feedback from ffprobe" };
                    }

                    return new AnalyzeResult { Successful = true, VideoInfo = videoInfo };
                }
                catch (IOException)
                {
                    var videoInfo = JsonConvert.DeserializeObject<VideoInfoResult>(json.ToString(), this._jsonSerializerSettings);
                    if (videoInfo.Format == null && videoInfo.Streams == null)
                    {
                        return new AnalyzeResult { Successful = false, ErrorMessage = "No feedback from ffprobe (IOException)" };
                    }

                    return new AnalyzeResult { Successful = true, VideoInfo = videoInfo };
                }
                catch (Exception exception)
                {
                    return new AnalyzeResult { Successful = false, ErrorMessage = exception.ToString() };
                }
                finally
                {
                    this._process.OutputDataReceived -= dataReceived;
                    this._process?.Dispose();
                }
            }
        }

        private void KillProcess()
        {
            try
            {
                if (this._process != null && !this._process.HasExited)
                {
                    this._process.Kill();
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());
            }
        }
    }
}
