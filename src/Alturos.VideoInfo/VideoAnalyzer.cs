using Alturos.VideoInfo.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Alturos.VideoInfo
{
    public class VideoAnalyzer
    {
        private readonly string _ffprobePath;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly int _timeout;

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
        /// <param name="videoFilePath">path to the video</param>
        /// <returns></returns>
        public AnalyzeResult GetVideoInfo(string videoFilePath)
        {
            if (!File.Exists(videoFilePath))
            {
                return new AnalyzeResult { Successful = false, ErrorMessage = "File does not exist" };
            }

            return this.GetVideoInfo(new MediaInput { FilePath = videoFilePath });
        }

        /// <summary>
        /// GetVideoInfo
        /// </summary>
        /// <param name="videoFilePath">path to the video</param>
        /// <returns></returns>
        public AnalyzeResult GetVideoInfo(byte[] data)
        {
            var mediaInput = new MediaInput();

            //Set probing size in bytes, i.e. the size of the data to analyze to get stream information.
            //A higher value will enable detecting more information in case it is dispersed into the stream,
            //but will increase latency. Must be an integer not lesser than 32. It is 5000000 by default.
            //TODO:5000000 not work, check why 278188 working
            var ffprobeMaxInputLength = 278188;
            if (data.Length > ffprobeMaxInputLength)
            {
                mediaInput.FileContent = data.Take(ffprobeMaxInputLength).ToArray();
            }
            else
            {
                mediaInput.FileContent = data;
            }

            return this.GetVideoInfo(mediaInput);
        }

        private AnalyzeResult GetVideoInfo(MediaInput mediaInput)
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

                var process = new Process();

                try
                {
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += dataReceived;

                    if (!process.Start())
                    {
                        return new AnalyzeResult { ErrorMessage = "Cannot start ffprobe" };
                    }

                    process.BeginOutputReadLine();

                    if (mediaInput.FileContent != null)
                    {
                        using (var ffprobeIn = process.StandardInput.BaseStream)
                        {
                            ffprobeIn.Write(mediaInput.FileContent, 0, mediaInput.FileContent.Length);
                            ffprobeIn.Flush();
                            ffprobeIn.Close();
                        }
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
