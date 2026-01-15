using System;
using System.Collections.Generic;
using System.IO;
using SwiftSeek.Data.Repositories;

namespace SwiftSeek.Indexing
{
    public class FileSystemCrawler
    {
        private readonly EntryRepository _entryRepository;

        public FileSystemCrawler(EntryRepository entryRepository)
        {
            _entryRepository = entryRepository;
        }

        public void CrawlDirectory(string rootPath, bool includeSubdirectories = true)
        {
            var directories = new Stack<string>();
            directories.Push(rootPath);

            while (directories.Count > 0)
            {
                var currentDirectory = directories.Pop();

                try
                {
                    foreach (var file in Directory.GetFiles(currentDirectory))
                    {
                        ProcessFile(file);
                    }

                    if (includeSubdirectories)
                    {
                        foreach (var directory in Directory.GetDirectories(currentDirectory))
                        {
                            directories.Push(directory);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Log and skip directories/files with access issues
                    Console.WriteLine($"Access denied: {currentDirectory}");
                }
                catch (Exception ex)
                {
                    // Log other exceptions
                    Console.WriteLine($"Error processing directory {currentDirectory}: {ex.Message}");
                }
            }
        }

        private void ProcessFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                var entry = new Entry
                {
                    FullPath = fileInfo.FullName,
                    Name = fileInfo.Name,
                    DirectoryPath = fileInfo.DirectoryName,
                    Extension = fileInfo.Extension,
                    IsDirectory = false,
                    SizeBytes = fileInfo.Length,
                    CreatedUtc = fileInfo.CreationTimeUtc,
                    ModifiedUtc = fileInfo.LastWriteTimeUtc
                };

                _entryRepository.AddEntry(entry);
            }
            catch (Exception ex)
            {
                // Log and skip problematic files
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }
    }
}