using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SwiftSeek.Data.Repositories
{
    public class EntryRepository
    {
        private readonly string _connectionString;

        public EntryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Entry> GetAllEntries()
        {
            var entries = new List<Entry>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Entries;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new Entry
                {
                    Id = reader.GetInt32(0),
                    FullPath = reader.GetString(1),
                    Name = reader.GetString(2),
                    DirectoryPath = reader.GetString(3),
                    Extension = reader.GetString(4),
                    IsDirectory = reader.GetBoolean(5),
                    SizeBytes = reader.GetInt64(6),
                    CreatedUtc = DateTime.Parse(reader.GetString(7)),
                    ModifiedUtc = DateTime.Parse(reader.GetString(8))
                });
            }

            return entries;
        }

        public void AddEntry(Entry entry)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Entries (FullPath, Name, DirectoryPath, Extension, IsDirectory, SizeBytes, CreatedUtc, ModifiedUtc)
                VALUES ($fullPath, $name, $directoryPath, $extension, $isDirectory, $sizeBytes, $createdUtc, $modifiedUtc);
            ";

            command.Parameters.AddWithValue("$fullPath", entry.FullPath);
            command.Parameters.AddWithValue("$name", entry.Name);
            command.Parameters.AddWithValue("$directoryPath", entry.DirectoryPath);
            command.Parameters.AddWithValue("$extension", entry.Extension);
            command.Parameters.AddWithValue("$isDirectory", entry.IsDirectory);
            command.Parameters.AddWithValue("$sizeBytes", entry.SizeBytes);
            command.Parameters.AddWithValue("$createdUtc", entry.CreatedUtc.ToString("o"));
            command.Parameters.AddWithValue("$modifiedUtc", entry.ModifiedUtc.ToString("o"));

            command.ExecuteNonQuery();
        }
    }

    public class Entry
    {
        public int Id { get; set; }
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string DirectoryPath { get; set; }
        public string Extension { get; set; }
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }
}