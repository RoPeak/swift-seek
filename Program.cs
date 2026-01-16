using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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
                var searcher = new Searcher(options);
                await searcher.SearchAsync(cancellationTokenSource);
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

        static void PrintUsage()
        {
            Console.WriteLine("Usage: swiftseek <search-term> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --root <directory>         Root directory to start search (default: current directory)");
            Console.WriteLine("  --content                 Search file contents");
            Console.WriteLine("  --regex                   Use regular expressions for searching");
            Console.WriteLine("  --case-sensitive          Perform case-sensitive search");
            Console.WriteLine("  --ext-include <exts>      Comma-separated list of extensions to include");
            Console.WriteLine("  --ext-exclude <exts>      Comma-separated list of extensions to exclude");
            Console.WriteLine("  --min-size <bytes>        Minimum file size in bytes");
            Console.WriteLine("  --max-size <bytes>        Maximum file size in bytes (default: 25 MB)");
            Console.WriteLine("  --verbose                 Enable verbose output");
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
    }
}