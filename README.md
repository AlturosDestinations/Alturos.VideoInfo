# Alturos.VideoInfo

[ffprobe](https://ffmpeg.org/download.html) is required, it is a part of ffmpeg.

## nuget
Quick install Alturos.VideoInfo over [nuget](https://www.nuget.org/packages/Alturos.VideoInfo)
```
PM> install-package VideoInfo
```

## Example
```cs
var videoFilePath = "myVideo.mp4";

var videoAnalyer = new VideoAnalyzer();
var videoInfo = videoAnalyer.GetVideoInfo(videoFilePath);
```

