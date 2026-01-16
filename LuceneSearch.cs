using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace SwiftSeek.Lucene
{
    public class ContentSearchQuery
    {
        public string QueryText { get; init; }
        public bool CaseSensitive { get; init; }
        public bool FuzzySearch { get; init; }
        public bool ExactPhrase { get; init; }
        public int MaxResults { get; init; } = 20;
    }

    public class ContentSearchResult
    {
        public string Path { get; init; }
        public string Snippet { get; init; }
        public float Score { get; init; }
    }

    public class ContentSearcher
    {
        private static readonly LuceneVersion LuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexPath;

        public ContentSearcher(string indexPath)
        {
            _indexPath = indexPath;
        }

        public IReadOnlyList<ContentSearchResult> Search(ContentSearchQuery query)
        {
            if (!ContentIndexPaths.IndexExists(_indexPath))
            {
                return Array.Empty<ContentSearchResult>();
            }

            using var directory = FSDirectory.Open(_indexPath);
            using var reader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(reader);

            var field = query.CaseSensitive ? "ContentCase" : "Content";
            using var analyzer = CreateSearchAnalyzer(query.CaseSensitive);
            var parsedQuery = BuildQuery(query, field, analyzer);
            var hits = searcher.Search(parsedQuery, query.MaxResults);

            var results = new List<ContentSearchResult>();
            foreach (var hit in hits.ScoreDocs)
            {
                var doc = searcher.Doc(hit.Doc);
                var path = doc.Get("Path");
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    continue;
                }

                var content = doc.Get("ContentStored");
                var snippet = SnippetBuilder.Build(content, query.QueryText, query.CaseSensitive, query.ExactPhrase);
                results.Add(new ContentSearchResult
                {
                    Path = path,
                    Snippet = snippet,
                    Score = hit.Score
                });
            }

            return results;
        }

        private static Analyzer CreateSearchAnalyzer(bool caseSensitive)
        {
            if (!caseSensitive)
            {
                return new StandardAnalyzer(LuceneVersion, CharArraySet.EMPTY_SET);
            }

            return ContentIndexer.CreateSearchAnalyzer();
        }

        private static Query BuildQuery(ContentSearchQuery query, string field, Analyzer analyzer)
        {
            var parser = new QueryParser(LuceneVersion, field, analyzer)
            {
                DefaultOperator = Operator.AND
            };

            if (query.ExactPhrase)
            {
                var phrase = QueryParser.Escape(query.QueryText);
                return parser.Parse($"\"{phrase}\"");
            }

            if (query.FuzzySearch)
            {
                var fuzzy = BuildFuzzyQuery(query.QueryText);
                return parser.Parse(fuzzy);
            }

            return parser.Parse(QueryParser.Escape(query.QueryText));
        }

        private static string BuildFuzzyQuery(string input)
        {
            var terms = input.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", terms.Select(term => $"{QueryParser.Escape(term)}~"));
        }
    }

    internal static class SnippetBuilder
    {
        private const int SnippetRadius = 120;

        public static string Build(string content, string queryText, bool caseSensitive, bool exactPhrase)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var cleaned = content.Replace("\r", " ").Replace("\n", " ");
            var matchIndex = FindMatchIndex(cleaned, queryText, caseSensitive, exactPhrase);
            if (matchIndex < 0)
            {
                return Truncate(cleaned);
            }

            var start = Math.Max(0, matchIndex - SnippetRadius);
            var end = Math.Min(cleaned.Length, matchIndex + queryText.Length + SnippetRadius);
            var snippet = cleaned.Substring(start, end - start);
            return Highlight(snippet, queryText, caseSensitive, exactPhrase);
        }

        private static int FindMatchIndex(string content, string queryText, bool caseSensitive, bool exactPhrase)
        {
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (exactPhrase)
            {
                return content.IndexOf(queryText, comparison);
            }

            foreach (var term in ExtractTerms(queryText))
            {
                var index = content.IndexOf(term, comparison);
                if (index >= 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private static IEnumerable<string> ExtractTerms(string queryText)
        {
            return queryText.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Highlight(string text, string queryText, bool caseSensitive, bool exactPhrase)
        {
            var terms = exactPhrase ? new[] { queryText } : ExtractTerms(queryText).ToArray();
            if (terms.Length == 0)
            {
                return text;
            }

            var pattern = string.Join("|", terms.Select(Regex.Escape));
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(pattern, options);
            return regex.Replace(text, match => $"**{match.Value}**");
        }

        private static string Truncate(string content)
        {
            if (content.Length <= SnippetRadius * 2)
            {
                return content;
            }

            return content.Substring(0, SnippetRadius * 2);
        }
    }
}
