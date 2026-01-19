using System;

namespace SwiftSeek
{
    public class ConsoleSearchReporter : ISearchReporter
    {
        public void OnStart(SearchOptions options)
        {
            Console.WriteLine($"Starting search in directory: {options.RootDirectory}");

            if (!options.Verbose)
            {
                return;
            }

            Console.WriteLine("[VERBOSE] Search options:");
            Console.WriteLine($"  Search Term: {options.SearchTerm}");
            Console.WriteLine($"  Case Sensitive: {options.CaseSensitive}");
            Console.WriteLine($"  Use Regex: {options.UseRegex}");
            Console.WriteLine($"  Include Extensions: {string.Join(", ", options.IncludeExtensions)}");
            Console.WriteLine($"  Exclude Extensions: {string.Join(", ", options.ExcludeExtensions)}");
            Console.WriteLine($"  Min Size: {options.MinSize} bytes");
            Console.WriteLine($"  Max Size: {options.MaxSize} bytes");
        }

        public void OnStatus(string message)
        {
            Console.WriteLine(message);
        }

        public void OnWarning(string message)
        {
            Console.WriteLine(message);
        }

        public void OnVerbose(string message)
        {
            Console.WriteLine(message);
        }

        public void OnMatch(SearchResult result)
        {
            Console.WriteLine(result.Path);
        }

        public void OnProgress(SearchStatistics statistics)
        {
        }

        public void OnComplete(SearchStatistics statistics)
        {
            Console.WriteLine("\nSearch complete.");
            Console.WriteLine($"Files scanned: {statistics.FilesScanned}");
            Console.WriteLine($"Matches found: {statistics.MatchesFound}");
            Console.WriteLine($"Files skipped: {statistics.FilesSkipped}");
        }
    }
}
