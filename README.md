![Alturos.VideoInfo](doc/logo-banner.png)

# Alturos.VideoInfo

With this project you can access the metadata of a video (e.g. its duration, bitrate, codec, etc.).

This project uses a wrapper for [ffprobe](https://ffmpeg.org/download.html) in order to achieve this. ffprobe is a part of FFmpeg and extracts information from multimedia streams in a way the output is readable by humans and machines alike. It's required for this project to work.

The project comes with a class that automatically downloads the tool for you.

## Installation via NuGet
Quickly install Alturos.VideoInfo via [NuGet](https://www.nuget.org/packages/Alturos.VideoInfo) by pasting this into the Package Manager Console:
```
PM> Install-Package Alturos.VideoInfo
```

## Usage Example
```cs
var videoFilePath = "myVideo.mp4";
var ffprobePath = @"ffmpeg\ffprobe.exe";

var videoAnalyer = new VideoAnalyzer(ffprobePath);
var analyzeResult = videoAnalyer.GetVideoInfo(videoFilePath);
var videoInfo = analyzeResult.VideoInfo;

//videoInfo.Format.Filename = "TestVideos\\video1.mp4"
//videoInfo.Format.NbStreams = 1
//videoInfo.Format.NbPrograms = 0
//videoInfo.Format.FormatName = "mov,mp4,m4a,3gp,3g2,mj2"
//videoInfo.Format.FormatLongName = "QuickTime / MOV"
//videoInfo.Format.StartTime = 0
//videoInfo.Format.Duration = 120 //seconds
//videoInfo.Format.Size = 2088470 //bytes
//videoInfo.Format.BitRate = 139231
//videoInfo.Format.ProbeScore = 100
//videoInfo.Format.Tags["encoder"] = Lavf57.76.100
//videoInfo.Streams[0].CodecType = "video" //Video, Audio
//videoInfo.Streams[0]...
```

## Downloading ffprobe via code
```cs
var fileDownloader = new FileDownloader();
var url = fileDownloader.GetFfmpegPackageUrl();
var result = await fileDownloader.DownloadAsync(url, "ffmpeg");

//if (result.Successful)
//{
//	var ffprobePath = result.FfprobePath;
//}
```
