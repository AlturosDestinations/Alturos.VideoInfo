using System.Collections.Generic;

namespace Alturos.VideoInfo.Model
{
    public class Format
    {
        public string Filename { get; set; }
        public int NbStreams { get; set; }
        public int NbPrograms { get; set; }
        public string FormatName { get; set; }
        public string FormatLongName { get; set; }
        public double StartTime { get; set; }
        public double Duration { get; set; }
        public long Size { get; set; }
        public int BitRate { get; set; }
        public int ProbeScore { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
