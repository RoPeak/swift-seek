namespace SwiftSeek
{
    class SearchOptions
    {
        public string SearchTerm { get; set; }
        public string RootDirectory { get; set; } = ".";
        public bool SearchContent { get; set; } = false;
        public bool UseRegex { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public string[] IncludeExtensions { get; set; } = new string[0];
        public string[] ExcludeExtensions { get; set; } = new string[0];
        public long MinSize { get; set; } = 0;
        public long MaxSize { get; set; } = 25 * 1024 * 1024; // Default 25 MB
        public bool Verbose { get; set; } = false;
    }
}