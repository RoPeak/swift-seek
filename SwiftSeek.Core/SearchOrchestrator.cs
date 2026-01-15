using System;
using System.Collections.Generic;
using System.Linq;
using SwiftSeek.Data.Repositories;

namespace SwiftSeek.Core
{
    public class SearchOrchestrator
    {
        private readonly EntryRepository _entryRepository;

        public SearchOrchestrator(EntryRepository entryRepository)
        {
            _entryRepository = entryRepository;
        }

        public IEnumerable<SearchResult> Search(string query, SearchOptions options)
        {
            var entries = _entryRepository.GetAllEntries();

            var results = entries.Where(entry =>
                (options.SearchFileNames && entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (options.SearchFolderNames && entry.DirectoryPath.Contains(query, StringComparison.OrdinalIgnoreCase))
            );

            return results.Select(entry => new SearchResult
            {
                FullPath = entry.FullPath,
                Name = entry.Name,
                DirectoryPath = entry.DirectoryPath,
                MatchType = options.SearchFileNames ? "File Name" : "Folder Name"
            });
        }
    }

    public class SearchOptions
    {
        public bool SearchFileNames { get; set; }
        public bool SearchFolderNames { get; set; }
    }

    public class SearchResult
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string DirectoryPath { get; set; }
        public string MatchType { get; set; }
    }
}