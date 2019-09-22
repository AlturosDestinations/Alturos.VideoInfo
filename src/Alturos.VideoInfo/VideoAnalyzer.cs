using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Alturos.VideoInfo.UnitTest")]
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
                if (!IsValidURL(videoFilePath))
                {
                    return new AnalyzeResult { Successful = false, ErrorMessage = "File does not exist" };
                }
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
        /// Validate that a URL meets the pattern for a URL, and then attempt to retrieve the resource from the URL
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns></returns>
        internal bool IsValidURL(string url)
        {
            //https://stackoverflow.com/questions/5717312/regular-expression-for-url
            string Pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
            Regex Rgx = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            //https://stackoverflow.com/questions/924679/c-sharp-how-can-i-check-if-a-url-exists-is-valid
            if (Rgx.IsMatch(url))
            {
                try
                {
                    HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                    request.Timeout = 5000; //set the timeout to 5 seconds to keep the user from waiting too long for the page to load
                    request.Method = "HEAD"; //Get only the header information -- no need to download any content

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        int statusCode = (int)response.StatusCode;
                        if (statusCode >= 100 && statusCode < 400) //Good requests
                        {
                            return true;
                        }
                        else if (statusCode >= 500 && statusCode <= 510) //Server Errors
                        {
                            return false;
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError) //400 errors
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}