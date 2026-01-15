using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

            string searchTerm = args[0];
            string rootDirectory = ".";
            bool searchContent = false;
            bool useRegex = false;
            bool caseSensitive = false;
            string[] includeExtensions = Array.Empty<string>();
            string[] excludeExtensions = Array.Empty<string>();
            long minSize = 0;
            long maxSize = 25 * 1024 * 1024; // Default max size: 25 MB

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--root":
                        rootDirectory = args[++i];
                        break;
                    case "--content":
                        searchContent = true;
                        break;
                    case "--regex":
                        useRegex = true;
                        break;
                    case "--case-sensitive":
                        caseSensitive = true;
                        break;
                    case "--ext-include":
                        includeExtensions = args[++i].Split(',');
                        break;
                    case "--ext-exclude":
                        excludeExtensions = args[++i].Split(',');
                        break;
                    case "--min-size":
                        minSize = long.Parse(args[++i]);
                        break;
                    case "--max-size":
                        maxSize = long.Parse(args[++i]);
                        break;
                }
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var searcher = new Searcher(
                    searchTerm,
                    rootDirectory,
                    searchContent,
                    useRegex,
                    caseSensitive,
                    includeExtensions,
                    excludeExtensions,
                    minSize,
                    maxSize
                );

                await searcher.SearchAsync(cts.Token);
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
        }
    }
}