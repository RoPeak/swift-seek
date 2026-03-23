using SwiftSeek;

namespace SwiftSeek.App
{
    public class SearchResultItem
    {
        public string Path { get; init; }
        public string Snippet { get; init; }
        public SearchResultSource Source { get; init; }
    }
}
