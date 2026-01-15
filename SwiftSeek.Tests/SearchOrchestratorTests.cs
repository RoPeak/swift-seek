using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwiftSeek.Core;
using SwiftSeek.Data.Repositories;

namespace SwiftSeek.Tests
{
    [TestClass]
    public class SearchOrchestratorTests
    {
        [TestMethod]
        public void Search_ShouldReturnMatchingEntries_WhenQueryMatchesFileName()
        {
            // Arrange
            var mockEntries = new List<Entry>
            {
                new Entry { Name = "file1.txt", DirectoryPath = "C:\\Test", FullPath = "C:\\Test\\file1.txt" },
                new Entry { Name = "file2.txt", DirectoryPath = "C:\\Test", FullPath = "C:\\Test\\file2.txt" }
            };

            var mockRepository = new MockEntryRepository(mockEntries);
            var orchestrator = new SearchOrchestrator(mockRepository);

            var options = new SearchOptions { SearchFileNames = true, SearchFolderNames = false };

            // Act
            var results = orchestrator.Search("file1", options).ToList();

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("file1.txt", results[0].Name);
        }

        private class MockEntryRepository : EntryRepository
        {
            private readonly IEnumerable<Entry> _entries;

            public MockEntryRepository(IEnumerable<Entry> entries) : base(null)
            {
                _entries = entries;
            }

            public override IEnumerable<Entry> GetAllEntries()
            {
                return _entries;
            }
        }
    }
}