using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Compression;
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
            var fileDownloader = new FileDownloader();
            var downloadResult = await fileDownloader.DownloadAsync("ffmpeg");
            var testVideoPath = await this.GetTestVideoPathAsync();

            var videoAnalyzer = new VideoAnalyzer(downloadResult.FfprobePath);
            var analyzeResult = await videoAnalyzer.GetVideoInfoAsync(testVideoPath);
            Assert.IsTrue(analyzeResult.Successful, "Get VideoInfo is not successful");
            Assert.AreEqual(120, analyzeResult.VideoInfo.Format.Duration);
        }

        [TestMethod]
        public async Task CheckFFprobeWrapperStream()
        {
            var fileDownloader = new FileDownloader();
            var downloadResult = await fileDownloader.DownloadAsync("ffmpeg");
            var testVideoPath = await this.GetTestVideoPathAsync();
            var videoData = await File.ReadAllBytesAsync(testVideoPath);

            var videoAnalyzer = new VideoAnalyzer(downloadResult.FfprobePath);
            var analyzeResult = await videoAnalyzer.GetVideoInfoAsync(videoData);
            Assert.IsTrue(analyzeResult.Successful, $"Get VideoInfo is not successful {analyzeResult.ErrorMessage}");
            Assert.AreEqual(120, analyzeResult.VideoInfo.Format.Duration);
        }
    }
}
