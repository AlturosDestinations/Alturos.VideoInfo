using Alturos.VideoInfo.Model;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Alturos.VideoInfo
{
    public class FileDownloader
    {
        private readonly string _version;

        public FileDownloader(string version = "4.1.3")
        {
            this._version = version;
        }

        private string GetFfmpegPackageUrl()
        {
            var win64 = $"https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-{this._version}-win64-static.zip";
            var win32 = $"https://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-{this._version}-win32-static.zip";
            var macos64 = $"https://ffmpeg.zeranoe.com/builds/macos64/static/ffmpeg-{this._version}-macos64-static.zip";

#if NETSTANDARD2_0

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return macos64;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    return win64;
                }

                return win32;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new NotSupportedException("Linux binary download is not supported");
            }
#endif

#if NET461
            if (Environment.Is64BitOperatingSystem)
            {
                return win64;
            }

            return win32;
#endif

            throw new NotSupportedException("Binary download is not supported");
        }

        public async Task<FfmpegDownloadResult> DownloadAsync(string destinationPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var url = this.GetFfmpegPackageUrl();
            return await this.DownloadAsync(url, destinationPath, cancellationToken);
        }

        public async Task<FfmpegDownloadResult> DownloadAsync(string url, string destinationPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            var folderName = Path.GetFileNameWithoutExtension(fileName);
            var filePath = Path.Combine(destinationPath, fileName);
            var packagePath = Path.Combine(destinationPath, folderName, "bin");

            if (Directory.Exists(packagePath))
            {
                return this.GetPaths(packagePath);
            }

            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(10);

                using (var httpResponseMessage = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    if (!httpResponseMessage.IsSuccessStatusCode)
                    {
                        return new FfmpegDownloadResult
                        {
                            Successful = false
                        };
                    }

                    using (var fileContentStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var sourceStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        fileContentStream.Seek(0, SeekOrigin.Begin);
                        await fileContentStream.CopyToAsync(sourceStream, 4096, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            ZipFile.ExtractToDirectory(filePath, destinationPath);
            File.Delete(filePath);

            return this.GetPaths(packagePath);
        }

        private FfmpegDownloadResult GetPaths(string packagePath)
        {
            var item = new FfmpegDownloadResult
            {
                Successful = true,
                FfmpegPath = Path.Combine(packagePath, "ffmpeg.exe"),
                FfprobePath = Path.Combine(packagePath, "ffprobe.exe"),
                FfplayPath = Path.Combine(packagePath, "ffplay.exe")
            };

            if (File.Exists(item.FfmpegPath) && File.Exists(item.FfprobePath) && File.Exists(item.FfplayPath))
            {
                return item;
            }

            return new FfmpegDownloadResult
            {
                Successful = false
            };
        }
    }
}
