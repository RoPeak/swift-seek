using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SwiftSeek
{
    class Indexer
    {
        private readonly string _databasePath;

        public Indexer(string databasePath)
        {
            _databasePath = databasePath;
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS FileIndex (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Path TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Extension TEXT,
                    Size INTEGER,
                    CreatedTime TEXT,
                    ModifiedTime TEXT,
                    IsDirectory INTEGER
                );
            ";
            command.ExecuteNonQuery();
        }

        public async Task BuildIndexAsync(string rootDirectory, CancellationToken cancellationToken)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM FileIndex;";
            command.ExecuteNonQuery();

            Console.WriteLine("Building index...");

            await IndexDirectoryAsync(rootDirectory, connection, transaction, cancellationToken);

            transaction.Commit();
            Console.WriteLine("Index build complete.");
        }

        private async Task IndexDirectoryAsync(string directory, SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var file in Directory.EnumerateFileSystemEntries(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileInfo = new FileInfo(file);
                    var isDirectory = Directory.Exists(file);

                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO FileIndex (Path, Name, Extension, Size, CreatedTime, ModifiedTime, IsDirectory)
                        VALUES ($path, $name, $extension, $size, $createdTime, $modifiedTime, $isDirectory);
                    ";

                    command.Parameters.AddWithValue("$path", file);
                    command.Parameters.AddWithValue("$name", fileInfo.Name);
                    command.Parameters.AddWithValue("$extension", isDirectory ? (object)DBNull.Value : fileInfo.Extension);
                    command.Parameters.AddWithValue("$size", isDirectory ? (object)DBNull.Value : fileInfo.Length);
                    command.Parameters.AddWithValue("$createdTime", fileInfo.CreationTimeUtc.ToString("o"));
                    command.Parameters.AddWithValue("$modifiedTime", fileInfo.LastWriteTimeUtc.ToString("o"));
                    command.Parameters.AddWithValue("$isDirectory", isDirectory ? 1 : 0);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    Console.WriteLine($"[VERBOSE] Indexing file: {file}");

                    if (isDirectory)
                    {
                        await IndexDirectoryAsync(file, connection, transaction, cancellationToken);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Warning: Access denied to directory: {directory}. Skipping.");
            }
        }

        public void ShowIndexStatus()
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM FileIndex;";

            var count = Convert.ToInt32(command.ExecuteScalar());
            Console.WriteLine($"Index contains {count} entries.");
        }

        public async Task RebuildIndexAsync(string rootDirectory, CancellationToken cancellationToken)
        {
            Console.WriteLine("Rebuilding index...");
            await BuildIndexAsync(rootDirectory, cancellationToken);
        }
    }
}