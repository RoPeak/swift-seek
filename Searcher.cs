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
        private readonly string _searchTerm;
        private readonly string _rootDirectory;
        private readonly bool _searchContent;
        private readonly bool _useRegex;
        private readonly bool _caseSensitive;
        private readonly HashSet<string> _includeExtensions;
        private readonly HashSet<string> _excludeExtensions;
        private readonly long _minSize;
        private readonly long _maxSize;

        private int _filesScanned;
        private int _matchesFound;
        private int _filesSkipped;

        public Searcher(
            string searchTerm,
            string rootDirectory,
            bool searchContent,
            bool useRegex,
            bool caseSensitive,
            string[] includeExtensions,
            string[] excludeExtensions,
            long minSize,
            long maxSize)
        {
            _searchTerm = searchTerm;
            _rootDirectory = rootDirectory;
            _searchContent = searchContent;
            _useRegex = useRegex;
            _caseSensitive = caseSensitive;
            _includeExtensions = new HashSet<string>(includeExtensions, StringComparer.OrdinalIgnoreCase);
            _excludeExtensions = new HashSet<string>(excludeExtensions, StringComparer.OrdinalIgnoreCase);
            _minSize = minSize;
            _maxSize = maxSize;
        }

        public async Task SearchAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => SearchDirectory(_rootDirectory, cancellationToken), cancellationToken);

            Console.WriteLine("\nSearch complete.");
            Console.WriteLine($"Files scanned: {_filesScanned}");
            Console.WriteLine($"Matches found: {_matchesFound}");
            Console.WriteLine($"Files skipped: {_filesSkipped}");
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
            catch (UnauthorizedAccessException)
            {
                _filesSkipped++;
            }
        }

        private void ProcessFile(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > _maxSize || fileInfo.Length < _minSize)
                {
                    _filesSkipped++;
                    return;
                }

                if (_excludeExtensions.Contains(fileInfo.Extension))
                {
                    _filesSkipped++;
                    return;
                }

                if (_includeExtensions.Count > 0 && !_includeExtensions.Contains(fileInfo.Extension))
                {
                    _filesSkipped++;
                    return;
                }

                if (FileUtils.IsBinary(filePath))
                {
                    _filesSkipped++;
                    return;
                }

                _filesScanned++;

                if (_searchContent)
                {
                    if (SearchFileContent(filePath, cancellationToken))
                    {
                        Console.WriteLine(filePath);
                        _matchesFound++;
                    }
                }
                else if (SearchFileName(filePath))
                {
                    Console.WriteLine(filePath);
                    _matchesFound++;
                }
            }
            catch (Exception)
            {
                _filesSkipped++;
            }
        }

        private bool SearchFileName(string filePath)
        {
            var comparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (_useRegex)
            {
                var regex = new Regex(_searchTerm, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                return regex.IsMatch(Path.GetFileName(filePath));
            }

            return Path.GetFileName(filePath).IndexOf(_searchTerm, comparison) >= 0;
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

                    if (_useRegex)
                    {
                        var regex = new Regex(_searchTerm, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        if (regex.IsMatch(line)) return true;
                    }
                    else
                    {
                        var comparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                        if (line.IndexOf(_searchTerm, comparison) >= 0) return true;
                    }
                }
            }
            catch
            {
                _filesSkipped++;
            }

            return false;
        }
    }
}