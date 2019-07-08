# Alturos.VideoInfo

With this project you can access the video metadata for example the duration of the video. We are use a ffprobe wrapper in the background.
[ffprobe](https://ffmpeg.org/download.html) is required, it is a part of ffmpeg.

## nuget
Quick install Alturos.VideoInfo over [nuget](https://www.nuget.org/packages/Alturos.VideoInfo)
```
PM> Install-Package Alturos.VideoInfo
```

## Example
```cs
var videoFilePath = "myVideo.mp4";

var videoAnalyer = new VideoAnalyzer();
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

