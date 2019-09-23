using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Alturos.VideoInfo.UnitTest
{
    [TestClass]
    public class VideoInfoAnalyzerTest
    {
        private async Task<bool> DownloadFileAsync(string downloadUrl, string localPath)
        {
            if (File.Exists(localPath))
            {
                return true;
            }

            using (var httpClient = new HttpClient())
            {
                var httpResponseMessage = await httpClient.GetAsync(downloadUrl);
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    return false;
                }

                using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(localPath, FileMode.CreateNew, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                    fileStream.Close();
                }
            }

            return true;
        }

        private async Task DownloadFfprobeAsync(string ffmpegPath)
        {
            var ffmpegPackageUrl = "https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.1.3-win64-static.zip";
            var ffmpegZipFilePath = "ffmpeg.zip";

            if (!File.Exists(ffmpegZipFilePath))
            {
                if (!await this.DownloadFileAsync(ffmpegPackageUrl, ffmpegZipFilePath))
                {
                    Assert.Fail("Cannot download ffmpeg package");
                }

                ZipFile.ExtractToDirectory(ffmpegZipFilePath, ffmpegPath, overwriteFiles: true);
            }
        }

        private async Task<string> GetTestVideoPathAsync()
        {
            var testVideoZipFilePath = "TestVideos.zip";
            var testVideosPath = "TestVideos";
            var testVideoPath = Path.Combine(testVideosPath, "video1.mp4");

            if (!File.Exists(testVideoPath))
            {
                await this.DownloadFileAsync("https://skimovies.s3-eu-west-1.amazonaws.com/VideoPostProcessing/TestVideos.zip", testVideoZipFilePath);
                ZipFile.ExtractToDirectory(testVideoZipFilePath, testVideosPath, overwriteFiles: true);
            }

            return testVideoPath;
        }

        [TestMethod]
        public async Task CheckFFprobeWrapperFile()
        {
            #region Prepare FFprobe

            var ffmpegPath = "ffmpeg";
            await this.DownloadFfprobeAsync(ffmpegPath);

            var files = Directory.GetFiles(ffmpegPath, "ffprobe.exe", SearchOption.AllDirectories);
            var ffprobePath = files.FirstOrDefault();
            if (string.IsNullOrEmpty(ffprobePath))
            {
                Assert.Fail("Cannot found ffprobe");
            }

            #endregion

            var testVideoPath = await this.GetTestVideoPathAsync();

            var videoAnalyzer = new VideoAnalyzer(ffprobePath);
            var anazlyeResult = videoAnalyzer.GetVideoInfo(testVideoPath);
            Assert.IsTrue(anazlyeResult.Successful, "Get VideoInfo is not successful");
            Assert.AreEqual(120, anazlyeResult.VideoInfo.Format.Duration);
        }

        [TestMethod]
        public async Task CheckFFprobeWrapperStream()
        {
            #region Prepare FFprobe

            var ffmpegPath = "ffmpeg";
            await this.DownloadFfprobeAsync(ffmpegPath);

            var files = Directory.GetFiles(ffmpegPath, "ffprobe.exe", SearchOption.AllDirectories);
            var ffprobePath = files.FirstOrDefault();
            if (string.IsNullOrEmpty(ffprobePath))
            {
                Assert.Fail("Cannot found ffprobe");
            }

            #endregion

            var testVideoPath = await this.GetTestVideoPathAsync();
            var videoData = await File.ReadAllBytesAsync(testVideoPath);

            var videoAnalyzer = new VideoAnalyzer(ffprobePath);
            var anazlyeResult = videoAnalyzer.GetVideoInfo(videoData);
            Assert.IsTrue(anazlyeResult.Successful, $"Get VideoInfo is not successful {anazlyeResult.ErrorMessage}");
            Assert.AreEqual(120, anazlyeResult.VideoInfo.Format.Duration);
        }
    }
}
