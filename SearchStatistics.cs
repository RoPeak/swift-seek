namespace SwiftSeek
{
    class SearchStatistics
    {
        public int FilesScanned { get; set; } = 0;
        public int MatchesFound { get; set; } = 0;
        public int FilesSkipped { get; set; } = 0;
    }
}