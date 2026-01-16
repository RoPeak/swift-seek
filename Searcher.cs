using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftSeek
{
    class Searcher
    {
        private readonly SearchOptions _options;
        private readonly SearchStatistics _statistics = new SearchStatistics();

        public Searcher(SearchOptions options)
        {
            _options = options;
        }

        public async Task SearchAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => SearchDirectory(_options.RootDirectory, cancellationToken), cancellationToken);

            Console.WriteLine("\nSearch complete.");
            Console.WriteLine($"Files scanned: {_statistics.FilesScanned}");
            Console.WriteLine($"Matches found: {_statistics.MatchesFound}");
            Console.WriteLine($"Files skipped: {_statistics.FilesSkipped}");
        }

        private void SearchDirectory(string directory, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ProcessFile(file, cancellationToken);
                }

                foreach (var subDirectory in Directory.EnumerateDirectories(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SearchDirectory(subDirectory, cancellationToken);
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"Warning: Directory not found: {directory}. Skipping.");
                _statistics.FilesSkipped++;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Warning: Access denied to directory: {directory}. Skipping.");
                _statistics.FilesSkipped++;
            }
        }

        private void ProcessFile(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > _options.MaxSize || fileInfo.Length < _options.MinSize)
                {
                    _statistics.FilesSkipped++;
                    return;
                }

                if (Array.Exists(_options.ExcludeExtensions, ext => ext.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    _statistics.FilesSkipped++;
                    return;
                }

                if (_options.IncludeExtensions.Length > 0 && !Array.Exists(_options.IncludeExtensions, ext => ext.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    _statistics.FilesSkipped++;
                    return;
                }

                if (FileUtils.IsBinary(filePath))
                {
                    _statistics.FilesSkipped++;
                    return;
                }

                _statistics.FilesScanned++;

                if (_options.SearchContent)
                {
                    if (SearchFileContent(filePath, cancellationToken))
                    {
                        ReportMatch(filePath);
                    }
                }
                else if (SearchFileName(filePath))
                {
                    ReportMatch(filePath);
                }
            }
            catch (Exception)
            {
                _statistics.FilesSkipped++;
            }
        }

        private bool SearchFileName(string filePath)
        {
            var comparison = _options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (_options.UseRegex)
            {
                var regex = new Regex(_options.SearchTerm, _options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                return regex.IsMatch(Path.GetFileName(filePath));
            }

            return Path.GetFileName(filePath).IndexOf(_options.SearchTerm, comparison) >= 0;
        }

        private bool SearchFileContent(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (_options.UseRegex)
                    {
                        var regex = new Regex(_options.SearchTerm, _options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        if (regex.IsMatch(line)) return true;
                    }
                    else
                    {
                        var comparison = _options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                        if (line.IndexOf(_options.SearchTerm, comparison) >= 0) return true;
                    }
                }
            }
            catch
            {
                _statistics.FilesSkipped++;
            }

            return false;
        }

        private void ReportMatch(string filePath)
        {
            _statistics.MatchesFound++;
            Console.WriteLine(filePath);

            if (_options.Verbose)
            {
                Console.WriteLine($"[VERBOSE] Match found in: {filePath}");
            }
        }
    }
}