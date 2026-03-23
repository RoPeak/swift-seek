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
        private readonly ISearchReporter _reporter;
        private int _progressCounter;
        private const int ProgressInterval = 100;

        public Searcher(SearchOptions options, ISearchReporter reporter = null)
        {
            _options = options;
            _reporter = reporter ?? new ConsoleSearchReporter();
        }

        public async Task<SearchStatistics> SearchAsync(CancellationToken cancellationToken)
        {
            _reporter.OnStart(_options);

            await Task.Run(() => SearchDirectory(_options.RootDirectory, cancellationToken), cancellationToken);

            _reporter.OnComplete(_statistics);
            return _statistics;
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
                _reporter.OnWarning($"Warning: Directory not found: {directory}. Skipping.");
                _statistics.FilesSkipped++;
            }
            catch (UnauthorizedAccessException)
            {
                _reporter.OnWarning($"Warning: Access denied to directory: {directory}. Skipping.");
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
                    if (_options.Verbose)
                    {
                        _reporter.OnVerbose($"[VERBOSE] Skipping file: {filePath} (Reason: Size filter)");
                    }
                    return;
                }

                if (Array.Exists(_options.ExcludeExtensions, ext => ext.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    _statistics.FilesSkipped++;
                    if (_options.Verbose)
                    {
                        _reporter.OnVerbose($"[VERBOSE] Skipping file: {filePath} (Reason: Excluded extension)");
                    }
                    return;
                }

                if (_options.IncludeExtensions.Length > 0 && !Array.Exists(_options.IncludeExtensions, ext => ext.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    _statistics.FilesSkipped++;
                    if (_options.Verbose)
                    {
                        _reporter.OnVerbose($"[VERBOSE] Skipping file: {filePath} (Reason: Included extensions filter)");
                    }
                    return;
                }

                if (FileUtils.IsBinary(filePath))
                {
                    _statistics.FilesSkipped++;
                    if (_options.Verbose)
                    {
                        _reporter.OnVerbose($"[VERBOSE] Skipping file: {filePath} (Reason: Binary file)");
                    }
                    return;
                }

                _statistics.FilesScanned++;
                ReportProgressIfNeeded();

                if (_options.Verbose)
                {
                    _reporter.OnVerbose($"[VERBOSE] Scanning file: {filePath}");
                }

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
            _reporter.OnMatch(new SearchResult
            {
                Path = filePath,
                Snippet = string.Empty,
                Source = SearchResultSource.Scan
            });

            if (_options.Verbose)
            {
                _reporter.OnVerbose($"[VERBOSE] Match found in: {filePath}");
            }
        }

        private void ReportProgressIfNeeded()
        {
            _progressCounter++;
            if (_progressCounter >= ProgressInterval)
            {
                _progressCounter = 0;
                _reporter.OnProgress(_statistics);
            }
        }
    }
}
