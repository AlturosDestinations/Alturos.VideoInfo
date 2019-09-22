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

        [TestMethod]
        public async Task CheckFFprobeWrapper()
        {
            #region Prepare FFprobe

            var ffmpegPackageUrl = "https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.1.3-win64-static.zip";
            var ffmpegZipFilePath = "ffmpeg.zip";
            var ffmpegPath = "ffmpeg";

            if (!await this.DownloadFileAsync(ffmpegPackageUrl, ffmpegZipFilePath))
            {
                Assert.Fail("Cannot download ffmpeg package");
            }

            ZipFile.ExtractToDirectory(ffmpegZipFilePath, ffmpegPath, overwriteFiles: true);

            var files = Directory.GetFiles(ffmpegPath, "ffprobe.exe", SearchOption.AllDirectories);
            var ffprobePath = files.FirstOrDefault();
            if (ffprobePath == null)
            {
                Assert.Fail("Cannot found ffprobe");
            }

            #endregion

            #region Prepare Video

            var testVideoZipFilePath = "TestVideos.zip";
            var testVideosPath = "TestVideos";

            await this.DownloadFileAsync("https://skimovies.s3-eu-west-1.amazonaws.com/VideoPostProcessing/TestVideos.zip", testVideoZipFilePath);
            ZipFile.ExtractToDirectory(testVideoZipFilePath, testVideosPath, overwriteFiles: true);

            files = Directory.GetFiles(testVideosPath, "video1.mp4", SearchOption.AllDirectories);
            var videoPath = files.FirstOrDefault();

            #endregion

            var videoAnalyzer = new VideoAnalyzer(ffprobePath);
            var anazlyeResult = videoAnalyzer.GetVideoInfo(videoPath);
            Assert.IsTrue(anazlyeResult.Successful);
            Assert.AreEqual(120, anazlyeResult.VideoInfo.Format.Duration);
        }

        [TestMethod]
        public async Task GetVideoInfo_HTTPVideoURL_IsValid()
        {
            VideoAnalyzer videoAnalyzer = new VideoAnalyzer();
            Assert.AreEqual(videoAnalyzer.IsValidURL("https://www.sample-videos.com/video123/mp4/240/big_buck_bunny_240p_1mb.mp4"), true);
        }

        [TestMethod]
        public async Task GetVideoInfo_HTTPVideoURL_IsNotValid()
        {
            VideoAnalyzer videoAnalyzer = new VideoAnalyzer();
            Assert.AreEqual(videoAnalyzer.IsValidURL("ftp://www.sample-videos.com/video123/mp4/240/big_buck_bunny_240p_1mb.mp4"), false);
        }
    }
}
