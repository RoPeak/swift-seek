using System;
using System.Threading;
using System.Threading.Tasks;
using SwiftSeek.Lucene;

namespace SwiftSeek
{
    public class SearchCoordinator
    {
        public async Task<SearchStatistics> RunAsync(SearchOptions options, ISearchReporter reporter, CancellationToken cancellationToken)
        {
            var activeReporter = reporter ?? new ConsoleSearchReporter();
            activeReporter.OnStart(options);

            var contentIndexPath = options.SearchContent
                ? ContentIndexPaths.ResolveIndexPath(options.RootDirectory, options.ContentIndexPath)
                : null;

            if (options.SearchContent && options.ContentSearchMode != ContentSearchMode.Scan)
            {
                if (options.UseRegex)
                {
                    if (options.ContentSearchMode == ContentSearchMode.Index)
                    {
                        activeReporter.OnWarning("Regex search is not available when using the content index.");
                        return new SearchStatistics();
                    }

                    activeReporter.OnStatus("Regex search requested. Falling back to scan mode.");
                    options.ContentSearchMode = ContentSearchMode.Scan;
                }
                else if (!ContentIndexPaths.IndexExists(contentIndexPath))
                {
                    if (options.ContentSearchMode == ContentSearchMode.Index)
                    {
                        activeReporter.OnWarning($"No content index found at '{contentIndexPath}'.");
                        return new SearchStatistics();
                    }

                    activeReporter.OnStatus("No content index found. Falling back to scan mode.");
                    options.ContentSearchMode = ContentSearchMode.Scan;
                }
            }

            if (options.SearchContent && options.ContentSearchMode != ContentSearchMode.Scan)
            {
                return RunIndexSearch(options, contentIndexPath, activeReporter);
            }

            var searcher = new Searcher(options, activeReporter);
            return await searcher.SearchAsync(cancellationToken);
        }

        private static SearchStatistics RunIndexSearch(SearchOptions options, string indexPath, ISearchReporter reporter)
        {
            reporter.OnStatus($"Searching content index: {indexPath}");

            var contentSearcher = new ContentSearcher(indexPath);
            var query = new ContentSearchQuery
            {
                QueryText = options.SearchTerm,
                CaseSensitive = options.CaseSensitive,
                FuzzySearch = options.FuzzySearch,
                ExactPhrase = options.ExactPhrase,
                MaxResults = 200
            };

            var stats = new SearchStatistics();
            foreach (var result in contentSearcher.SearchStreaming(query))
            {
                stats.MatchesFound++;
                reporter.OnMatch(new SearchResult
                {
                    Path = result.Path,
                    Snippet = result.Snippet,
                    Source = SearchResultSource.Index
                });
            }

            reporter.OnComplete(stats);
            return stats;
        }
    }
}
