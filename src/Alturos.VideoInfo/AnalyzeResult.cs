namespace Alturos.VideoInfo
{
    public class AnalyzeResult
    {
        public bool Successful { get; set; }
        public string ErrorMessage { get; set; }
        public VideoInfoResult VideoInfo { get; set; }
    }
}
