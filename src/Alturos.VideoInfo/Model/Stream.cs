namespace Alturos.VideoInfo.Model
{
    public class Stream
    {
        public int Index { get; set; }
        public string CodecName { get; set; }
        public string CodecLongName { get; set; }
        public string Profile { get; set; }
        /// <summary>
        /// Describes the type of the stream as either Audio or Video
        /// </summary>
        public string CodecType { get; set; }
        public string CodecTimeBase { get; set; }
        public string CodecTagString { get; set; }
        public string CodecTag { get; set; }
        /// <summary>
        /// Width of the video if the codec type is Video
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Height of the video if the codec type is Video
        /// </summary>
        public int Height { get; set; }
        public int CodedWidth { get; set; }
        public int CodedHeight { get; set; }
        public int HasBFrames { get; set; }
        public string SampleAspectRatio { get; set; }
        public string DisplayAspectRatio { get; set; }
        public string PixFmt { get; set; }
        public int Level { get; set; }
        public string ColorRange { get; set; }
        public string ColorSpace { get; set; }
        public string ColorTransfer { get; set; }
        public string ColorPrimaries { get; set; }
        public string ChromaLocation { get; set; }
        public int Refs { get; set; }
        public string IsAvc { get; set; }
        public string NalLengthSize { get; set; }
        public string RFrameRate { get; set; }
        public string AvgFrameRate { get; set; }
        public string TimeBase { get; set; }
        public int StartPts { get; set; }
        public string StartTime { get; set; }
        public int DurationTs { get; set; }
        public double Duration { get; set; }
        public string BitRate { get; set; }
        public string BitsPerRawSample { get; set; }
        public string NbFrames { get; set; }
        public Disposition Disposition { get; set; }
        public Tags Tags { get; set; }
        public string SampleFmt { get; set; }
        public string SampleRate { get; set; }
        public int? Channels { get; set; }
        public string Channel_Layout { get; set; }
        public int? BitsPerSample { get; set; }
    }
}
