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
        /// <summary>
        /// The duration of the file
        /// </summary>
        public double Duration { get; set; }
        /// <summary>
        /// The size of the file in bytes
        /// </summary>
        public long Size { get; set; }
        public int BitRate { get; set; }
        /// <summary>
        /// ProbeScore 0-100
        /// 100 is the maximum score, meaning that FFmpeg is sure that the format is the real one.
        /// With a score below 25, it is recommanded to increase probe duration.
        /// </summary>
        public int ProbeScore { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
