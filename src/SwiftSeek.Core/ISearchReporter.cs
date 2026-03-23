namespace SwiftSeek
{
    public interface ISearchReporter
    {
        void OnStart(SearchOptions options);
        void OnStatus(string message);
        void OnWarning(string message);
        void OnVerbose(string message);
        void OnMatch(SearchResult result);
        void OnProgress(SearchStatistics statistics);
        void OnComplete(SearchStatistics statistics);
    }
}
