using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.IO;

namespace Alturos.VideoInfo
{
    public class VideoAnalyzer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(VideoAnalyzer));
        private readonly string _ffprobePath;

        public VideoAnalyzer(string ffprobePath = @"ffmpeg\ffprobe.exe")
        {
            this._ffprobePath = ffprobePath;
        }

        public VideoInfoResult GetVideoInfo(string videoFilePath)
        {
            if (!File.Exists(videoFilePath))
            {
                Log.Error($"{nameof(GetVideoInfo)} - File does not exist");
                return null;
            }

            if (!File.Exists(this._ffprobePath))
            {
                Log.Error($"{nameof(GetVideoInfo)} - FFprobe could not be found {this._ffprobePath}");
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = this._ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams {videoFilePath}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    var json = process.StandardOutput.ReadToEnd();

                    var contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver,
                        Formatting = Formatting.Indented
                    };

                    return JsonConvert.DeserializeObject<VideoInfoResult>(json, settings);
                }
            }
            catch (Exception exception)
            {
                Log.Error(nameof(GetVideoInfo), exception);
            }

            return null;
        }
    }
}
