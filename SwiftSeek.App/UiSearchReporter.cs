using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using SwiftSeek;

namespace SwiftSeek.App
{
    public class UiSearchReporter : ISearchReporter
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly ObservableCollection<SearchResultItem> _results;
        private readonly System.Action<string> _statusUpdate;
        private readonly System.Action<SearchStatistics> _progressUpdate;

        public UiSearchReporter(
            DispatcherQueue dispatcher,
            ObservableCollection<SearchResultItem> results,
            System.Action<string> statusUpdate,
            System.Action<SearchStatistics> progressUpdate)
        {
            _dispatcher = dispatcher;
            _results = results;
            _statusUpdate = statusUpdate;
            _progressUpdate = progressUpdate;
        }

        public void OnStart(SearchOptions options)
        {
            UpdateStatus("Searching...");
        }

        public void OnStatus(string message)
        {
            UpdateStatus(message);
        }

        public void OnWarning(string message)
        {
            UpdateStatus(message);
        }

        public void OnVerbose(string message)
        {
        }

        public void OnMatch(SearchResult result)
        {
            _dispatcher.TryEnqueue(() =>
            {
                _results.Add(new SearchResultItem
                {
                    Path = result.Path,
                    Snippet = result.Snippet,
                    Source = result.Source
                });
            });
        }

        public void OnProgress(SearchStatistics statistics)
        {
            _dispatcher.TryEnqueue(() => _progressUpdate(statistics));
        }

        public void OnComplete(SearchStatistics statistics)
        {
            UpdateStatus($"Search complete. Matches: {statistics.MatchesFound}");
        }

        private void UpdateStatus(string message)
        {
            _dispatcher.TryEnqueue(() => _statusUpdate(message));
        }
    }
}
