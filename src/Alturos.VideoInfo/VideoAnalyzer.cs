using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Alturos.VideoInfo
{
    public class VideoAnalyzer
    {
        private readonly string _ffprobePath;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly int _timeout = 5000;

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
        }

        /// <summary>
        /// GetVideoInfo
        /// </summary>
        /// <param name="videoFilePath">path to the video</param>
        /// <returns></returns>
        public AnalyzeResult GetVideoInfo(string videoFilePath)
        {
            if (!File.Exists(videoFilePath))
            {
                return new AnalyzeResult { Successful = false, ErrorMessage = "File does not exist" };
            }

            if (!File.Exists(this._ffprobePath))
            {
                return new AnalyzeResult { Successful = false, ErrorMessage = $"ffprobe could not be found {this._ffprobePath}" };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = this._ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };

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

                var process = new Process();

                try
                {
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += dataReceived;

                    process.Start();
                    process.BeginOutputReadLine();

                    if (!process.WaitForExit(this._timeout) || !outputWaitHandle.WaitOne(this._timeout))
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
                catch (Exception exception)
                {
                    return new AnalyzeResult { Successful = false, ErrorMessage = exception.ToString() };
                }
                finally
                {
                    process.OutputDataReceived -= dataReceived;
                    process?.Dispose();
                }
            }
        }

        /// <summary>
        /// GetVideoInfo
        /// </summary>
        /// <param name="videoFilePath">path to the video</param>
        /// <returns></returns>
        public AnalyzeResult GetVideoInfo(byte[] data)
        {
            if (!File.Exists(this._ffprobePath))
            {
                return new AnalyzeResult { Successful = false, ErrorMessage = $"ffprobe could not be found {this._ffprobePath}" };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = this._ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams -",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

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

                var process = new Process();

                try
                {
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += dataReceived;

                    process.Start();
                    process.BeginOutputReadLine();

                    using (var ffprobeIn = process.StandardInput.BaseStream)
                    {
                        ffprobeIn.Write(data, 0, data.Length);
                        ffprobeIn.Flush();
                        ffprobeIn.Close();
                    }

                    if (!process.WaitForExit(this._timeout) || !outputWaitHandle.WaitOne(this._timeout))
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
                catch (Exception exception)
                {
                    return new AnalyzeResult { Successful = false, ErrorMessage = exception.ToString() };
                }
                finally
                {
                    process.OutputDataReceived -= dataReceived;
                    process?.Dispose();
                }
            }
        }
    }
}
