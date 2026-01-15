using System;
using Microsoft.Data.Sqlite;

namespace SwiftSeek.Data
{
    public static class SQLiteSchema
    {
        public static void InitialiseDatabase(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Entries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullPath TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    DirectoryPath TEXT NOT NULL,
                    Extension TEXT NOT NULL,
                    IsDirectory INTEGER NOT NULL,
                    SizeBytes INTEGER NOT NULL,
                    CreatedUtc TEXT NOT NULL,
                    ModifiedUtc TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Scopes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RootPath TEXT NOT NULL,
                    IncludeSubdirectories INTEGER NOT NULL,
                    IsEnabled INTEGER NOT NULL,
                    LastIndexedUtc TEXT
                );

                CREATE TABLE IF NOT EXISTS Exclusions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Pattern TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_entries_fullpath ON Entries (FullPath);
                CREATE INDEX IF NOT EXISTS idx_entries_name ON Entries (Name);
                CREATE INDEX IF NOT EXISTS idx_entries_extension ON Entries (Extension);
                CREATE INDEX IF NOT EXISTS idx_entries_createdutc ON Entries (CreatedUtc);
                CREATE INDEX IF NOT EXISTS idx_entries_modifiedutc ON Entries (ModifiedUtc);
                CREATE INDEX IF NOT EXISTS idx_entries_sizebytes ON Entries (SizeBytes);
            ";

            command.ExecuteNonQuery();
        }
    }
}