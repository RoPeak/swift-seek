using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace SwiftSeek.Lucene
{
    public class ContentIndexingOptions
    {
        public IReadOnlyCollection<string> AllowedExtensions { get; init; } = ContentIndexPaths.DefaultExtensions;
        public long MaxFileBytes { get; init; } = 25 * 1024 * 1024;
        public int CommitEvery { get; init; } = 200;
    }

    public class ContentIndexingStats
    {
        public int FilesIndexed { get; internal set; }
        public int FilesSkipped { get; internal set; }
        public int FilesUnchanged { get; internal set; }
    }

    public static class ContentIndexPaths
    {
        public static readonly IReadOnlyCollection<string> DefaultExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".md", ".log", ".json", ".yaml", ".yml",
            ".cs", ".java", ".py", ".js", ".ts", ".tsx", ".jsx", ".cpp", ".c", ".h", ".hpp",
            ".go", ".rs", ".rb", ".php", ".swift", ".kt", ".scala", ".sql", ".html", ".css",
            ".scss", ".xml", ".xaml", ".sh", ".bat", ".ps1", ".ini", ".cfg", ".toml"
        };

        public static string ResolveIndexPath(string rootDirectory, string overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath;
            }

            return Path.Combine(rootDirectory, ".swiftseek", "content-index");
        }

        public static bool IndexExists(string indexPath)
        {
            if (!System.IO.Directory.Exists(indexPath))
            {
                return false;
            }

            try
            {
                return System.IO.Directory.EnumerateFiles(indexPath).Any();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public class ContentIndexer
    {
        private static readonly LuceneVersion LuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexPath;
        private readonly Analyzer _indexAnalyzer;

        public ContentIndexer(string indexPath)
        {
            _indexPath = indexPath;
            _indexAnalyzer = CreatePerFieldAnalyzer();
        }

        public async Task<ContentIndexingStats> IndexDirectoryAsync(string rootDirectory, bool rebuild, ContentIndexingOptions options, CancellationToken cancellationToken)
        {
            var stats = new ContentIndexingStats();
            if (rebuild && System.IO.Directory.Exists(_indexPath))
            {
                System.IO.Directory.Delete(_indexPath, recursive: true);
            }

            System.IO.Directory.CreateDirectory(_indexPath);

            using var directory = FSDirectory.Open(_indexPath);
            var config = new IndexWriterConfig(LuceneVersion, _indexAnalyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            using var writer = new IndexWriter(directory, config);
            using var reader = DirectoryReader.IndexExists(directory) ? DirectoryReader.Open(writer, true) : null;
            var searcher = reader != null ? new IndexSearcher(reader) : null;

            int pendingCommits = 0;
            foreach (var filePath in EnumerateFiles(rootDirectory, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ShouldIndexFile(filePath, options))
                {
                    stats.FilesSkipped++;
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                if (searcher != null && TryGetIndexedModifiedTicks(searcher, filePath, out long existingTicks))
                {
                    if (existingTicks == fileInfo.LastWriteTimeUtc.Ticks)
                    {
                        stats.FilesUnchanged++;
                        continue;
                    }
                }

                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var doc = new Document
                {
                    new StringField("Path", filePath, Field.Store.YES),
                    new TextField("Content", content, Field.Store.NO),
                    new TextField("ContentCase", content, Field.Store.NO),
                    new StoredField("ContentStored", content),
                    new Int64Field("LastModifiedTicks", fileInfo.LastWriteTimeUtc.Ticks, Field.Store.YES)
                };

                writer.UpdateDocument(new Term("Path", filePath), doc);
                stats.FilesIndexed++;
                pendingCommits++;

                if (pendingCommits >= options.CommitEvery)
                {
                    writer.Commit();
                    pendingCommits = 0;
                }
            }

            writer.Commit();
            return stats;
        }

        private static bool ShouldIndexFile(string filePath, ContentIndexingOptions options)
        {
            var extension = Path.GetExtension(filePath);
            if (!options.AllowedExtensions.Contains(extension))
            {
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > options.MaxFileBytes)
            {
                return false;
            }

            return !FileUtils.IsBinary(filePath);
        }

        private static bool TryGetIndexedModifiedTicks(IndexSearcher searcher, string filePath, out long ticks)
        {
            ticks = 0;
            var query = new TermQuery(new Term("Path", filePath));
            var hits = searcher.Search(query, 1);
            if (hits.TotalHits == 0)
            {
                return false;
            }

            var doc = searcher.Doc(hits.ScoreDocs[0].Doc);
            var storedTicks = doc.Get("LastModifiedTicks");
            return long.TryParse(storedTicks, out ticks);
        }

        private static IEnumerable<string> EnumerateFiles(string rootDirectory, CancellationToken cancellationToken)
        {
            var pending = new Stack<string>();
            pending.Push(rootDirectory);

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();
                IEnumerable<string> files;
                IEnumerable<string> directories;

                try
                {
                    files = System.IO.Directory.EnumerateFiles(current);
                    directories = System.IO.Directory.EnumerateDirectories(current);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                foreach (var directory in directories)
                {
                    pending.Push(directory);
                }
            }
        }

        internal static Analyzer CreateSearchAnalyzer()
        {
            return new CasePreservingAnalyzer(LuceneVersion);
        }

        private static Analyzer CreatePerFieldAnalyzer()
        {
            var standardAnalyzer = new StandardAnalyzer(LuceneVersion, CharArraySet.EMPTY_SET);
            var casePreservingAnalyzer = CreateSearchAnalyzer();
            var perField = new Dictionary<string, Analyzer>
            {
                ["ContentCase"] = casePreservingAnalyzer
            };

            return new PerFieldAnalyzerWrapper(standardAnalyzer, perField);
        }

        private sealed class CasePreservingAnalyzer : Analyzer
        {
            private readonly LuceneVersion _version;

            public CasePreservingAnalyzer(LuceneVersion version)
            {
                _version = version;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                var tokenizer = new StandardTokenizer(_version, reader);
                TokenStream stream = new StandardFilter(_version, tokenizer);
                return new TokenStreamComponents(tokenizer, stream);
            }
        }
    }
}
