using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SwiftSeek.Lucene;

namespace SwiftSeek
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                PrintUsage();
                return;
            }

            if (args[0] == "index")
            {
                if (args.Length > 1 && args[1] == "content")
                {
                    await RunContentIndexCommandAsync(args);
                    return;
                }

                var indexer = new Indexer("swiftseek.db");
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cts.Cancel();
                };

                if (args.Length > 1 && args[1] == "build")
                {
                    string rootDirectory = args.Length > 3 && args[2] == "--root" ? args[3] : ".";
                    await indexer.BuildIndexAsync(rootDirectory, cts.Token);
                }
                else if (args.Length > 1 && args[1] == "status")
                {
                    indexer.ShowIndexStatus();
                }
                else if (args.Length > 1 && args[1] == "rebuild")
                {
                    string rootDirectory = args.Length > 3 && args[2] == "--root" ? args[3] : ".";
                    await indexer.RebuildIndexAsync(rootDirectory, cts.Token);
                }
                else
                {
                    Console.WriteLine("Invalid index command. Use 'build', 'status', or 'rebuild'.");
                }

                return;
            }

            var options = ParseArguments(args);
            if (options == null)
            {
                Console.WriteLine("Invalid arguments. Use --help for usage information.");
                return;
            }

            if (!Directory.Exists(options.RootDirectory))
            {
                Console.WriteLine($"Error: The specified root directory '{options.RootDirectory}' does not exist.");
                return;
            }

            var searchCts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                searchCts.Cancel();
            };

            var cancellationTokenSource = searchCts.Token;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var contentIndexPath = options.SearchContent
                    ? ContentIndexPaths.ResolveIndexPath(options.RootDirectory, options.ContentIndexPath)
                    : null;

                if (options.SearchContent && options.ContentSearchMode != ContentSearchMode.Scan)
                {
                    if (options.UseRegex)
                    {
                        if (options.ContentSearchMode == ContentSearchMode.Index)
                        {
                            Console.WriteLine("Regex search is not available when using the content index.");
                            return;
                        }

                        Console.WriteLine("Regex search requested. Falling back to scan mode.");
                        options.ContentSearchMode = ContentSearchMode.Scan;
                    }
                    else if (!ContentIndexPaths.IndexExists(contentIndexPath))
                    {
                        if (options.ContentSearchMode == ContentSearchMode.Index)
                        {
                            Console.WriteLine($"No content index found at '{contentIndexPath}'.");
                            return;
                        }

                        Console.WriteLine("No content index found. Falling back to scan mode.");
                        options.ContentSearchMode = ContentSearchMode.Scan;
                    }
                }

                if (options.SearchContent && options.ContentSearchMode != ContentSearchMode.Scan)
                {
                    await RunContentSearchAsync(options, contentIndexPath);
                }
                else
                {
                    var searcher = new Searcher(options);
                    await searcher.SearchAsync(cancellationTokenSource);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Search cancelled.");
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}");
            }
        }

        static async Task RunContentIndexCommandAsync(string[] args)
        {
            var rootDirectory = ".";
            string indexOverride = null;
            var rebuild = false;

            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--root":
                        rootDirectory = args[++i];
                        break;
                    case "--content-index":
                        indexOverride = args[++i];
                        break;
                    case "--rebuild":
                        rebuild = true;
                        break;
                }
            }

            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"Error: The specified root directory '{rootDirectory}' does not exist.");
                return;
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            var indexPath = ContentIndexPaths.ResolveIndexPath(rootDirectory, indexOverride);
            var indexer = new ContentIndexer(indexPath);
            var options = new ContentIndexingOptions();

            Console.WriteLine($"Indexing content under '{rootDirectory}'...");
            Console.WriteLine($"Content index location: {indexPath}");

            var stats = await indexer.IndexDirectoryAsync(rootDirectory, rebuild, options, cts.Token);
            Console.WriteLine("Content indexing complete.");
            Console.WriteLine($"Files indexed: {stats.FilesIndexed}");
            Console.WriteLine($"Files unchanged: {stats.FilesUnchanged}");
            Console.WriteLine($"Files skipped: {stats.FilesSkipped}");
        }

        static Task RunContentSearchAsync(SearchOptions options, string indexPath)
        {
            Console.WriteLine($"Searching content index: {indexPath}");

            var searcher = new ContentSearcher(indexPath);
            var query = new ContentSearchQuery
            {
                QueryText = options.SearchTerm,
                CaseSensitive = options.CaseSensitive,
                FuzzySearch = options.FuzzySearch,
                ExactPhrase = options.ExactPhrase,
                MaxResults = 20
            };

            var results = searcher.Search(query);
            foreach (var result in results)
            {
                Console.WriteLine(result.Path);
                if (!string.IsNullOrWhiteSpace(result.Snippet))
                {
                    Console.WriteLine(result.Snippet);
                }

                if (options.Verbose)
                {
                    Console.WriteLine($"[VERBOSE] Score: {result.Score:F2}");
                }
            }

            Console.WriteLine($"Matches found: {results.Count}");
            return Task.CompletedTask;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: swiftseek <search-term> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --root <directory>         Root directory to start search (default: current directory)");
            Console.WriteLine("  --content                 Search file contents");
            Console.WriteLine("  --regex                   Use regular expressions for searching");
            Console.WriteLine("  --case-sensitive          Perform case-sensitive search");
            Console.WriteLine("  --phrase                  Require an exact phrase match (content search)");
            Console.WriteLine("  --fuzzy                   Enable fuzzy matching (content index only)");
            Console.WriteLine("  --content-mode <mode>     Content search mode: auto, index, scan");
            Console.WriteLine("  --content-index <path>    Override the default content index location");
            Console.WriteLine("  --ext-include <exts>      Comma-separated list of extensions to include");
            Console.WriteLine("  --ext-exclude <exts>      Comma-separated list of extensions to exclude");
            Console.WriteLine("  --min-size <bytes>        Minimum file size in bytes");
            Console.WriteLine("  --max-size <bytes>        Maximum file size in bytes (default: 25 MB)");
            Console.WriteLine("  --verbose                 Enable verbose output");
            Console.WriteLine();
            Console.WriteLine("Content indexing:");
            Console.WriteLine("  swiftseek index content --root <directory> [--rebuild] [--content-index <path>]");
        }

        static SearchOptions ParseArguments(string[] args)
        {
            var options = new SearchOptions();

            try
            {
                options.SearchTerm = args[0];

                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--root":
                            options.RootDirectory = args[++i];
                            break;
                        case "--content":
                            options.SearchContent = true;
                            break;
                        case "--regex":
                            options.UseRegex = true;
                            break;
                        case "--case-sensitive":
                            options.CaseSensitive = true;
                            break;
                        case "--phrase":
                            options.ExactPhrase = true;
                            break;
                        case "--fuzzy":
                            options.FuzzySearch = true;
                            break;
                        case "--content-mode":
                            options.ContentSearchMode = ParseContentMode(args[++i]);
                            break;
                        case "--content-index":
                            options.ContentIndexPath = args[++i];
                            break;
                        case "--ext-include":
                            options.IncludeExtensions = args[++i].Split(',');
                            break;
                        case "--ext-exclude":
                            options.ExcludeExtensions = args[++i].Split(',');
                            break;
                        case "--min-size":
                            options.MinSize = long.Parse(args[++i]);
                            break;
                        case "--max-size":
                            options.MaxSize = long.Parse(args[++i]);
                            break;
                        case "--verbose":
                            options.Verbose = true;
                            break;
                    }
                }

                return options;
            }
            catch
            {
                return null;
            }
        }

        static ContentSearchMode ParseContentMode(string mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "index" => ContentSearchMode.Index,
                "scan" => ContentSearchMode.Scan,
                _ => ContentSearchMode.Auto
            };
        }
    }
}
