# SwiftSeek

SwiftSeek is a command-line tool for searching files and directories on Windows. It supports searching by file names, directory names, and file contents with practical filters and modes.

SwiftSeek is intentionally small, readable, and dependency-light so it is easy to build and run on any Windows machine with .NET 8. It uses SQLite for durable metadata, Lucene.NET for fast full-text search, and a scan-based fallback that works even without an index.

## Quick Start

1. Ensure you have .NET 8 installed.
2. Restore and build the project using the .NET CLI:
   ```
   dotnet restore
   dotnet build
   ```
3. Run the tool:
   ```
   swiftseek <search-term> [options]
   ```

SwiftSeek creates local index files at runtime (SQLite metadata and optional Lucene content index). These are stored outside source control so no personal file paths or content are pushed to Git.

## Supported Arguments

- `<search-term>`: The term to search for.
- `--root <directory>`: Root directory to start the search (default: current directory).
- `--content`: Search file contents.
- `--regex`: Use regular expressions for searching.
- `--case-sensitive`: Perform case-sensitive search.
- `--phrase`: Require an exact phrase match (content search).
- `--fuzzy`: Enable fuzzy matching (content index only).
- `--content-mode <mode>`: Content search mode: `auto`, `index`, or `scan`.
- `--content-index <path>`: Override the default content index location.
- `--ext-include <exts>`: Comma-separated list of extensions to include.
- `--ext-exclude <exts>`: Comma-separated list of extensions to exclude.
- `--min-size <bytes>`: Minimum file size in bytes.
- `--max-size <bytes>`: Maximum file size in bytes (default: 25 MB).

## Example Usage

```sh
swiftseek "error code 503" --root "C:\Projects" --content --regex --ext-include ".log,.txt"
```

## Features

- Recursive directory search
- SQLite-based metadata indexing
- Lucene.NET-based full-text content indexing
- Advanced search options:
  - Exact phrase search
  - Case-sensitive toggle
  - Fuzzy search
  - Highlighted snippets

## Design Notes

SwiftSeek balances speed, safety, and ease of use:

- **Fast metadata queries**: SQLite gives a persistent, queryable catalogue of file system metadata without over-engineering.
- **Scalable content search**: Lucene.NET handles large text collections efficiently with well-known search primitives.
- **Reliable fallback**: Scan mode ensures content searches still work when a Lucene index is missing or out of date.
- **Privacy by default**: All indices are local and ignored by Git, avoiding accidental disclosure of personal paths or content.

## Architecture Overview

SwiftSeek is organised as three cooperating parts:

1. **Metadata indexing (SQLite)**  
   `Indexer` builds a file catalogue into `swiftseek.db`. This is used for fast filename and directory searches.

2. **Content indexing (Lucene.NET)**  
   The `SwiftSeek.Lucene` module isolates all Lucene logic. `ContentIndexer` builds a content index at `.swiftseek/content-index`, storing:
   - `Path` (unique key)
   - `Content` (case-insensitive tokens)
   - `ContentCase` (case-preserving tokens for case-sensitive search)
   - `ContentStored` (raw text for snippets)
   - `LastModifiedTicks` (used for resumable indexing)

3. **Search execution**  
   `Searcher` performs scan-based search for both names and content. When `--content` is used, the CLI selects either scan or index mode based on `--content-mode`.

## Usage

### Metadata Indexing

```bash
swiftseek index build --root "C:\"
```

Indexes metadata (file paths, sizes, etc.) in the specified directory.

### Content Indexing

```bash
swiftseek index content --root "C:\Projects"
```

Indexes the text content of supported files in the specified directory. Supported file types:

- `.txt`, `.md`, `.log`, `.json`, `.yaml`, `.yml`
- Source code files: `.cs`, `.java`, `.py`, `.js`, `.ts`, `.cpp`, `.go`, `.rs`, `.swift`, and more

The content index is resumable; re-running the command updates changed files and skips unchanged ones. Use `--rebuild` to start from scratch.

### Content Search Modes

Content searching can use either scan mode (read files on demand) or the Lucene index. Enable content search with `--content`, then control the mode with `--content-mode`:

- `auto` (default): Use the content index if it exists, otherwise fall back to scan mode.
- `index`: Search indexed content only and report an error if no index exists.
- `scan`: Always search file contents directly without the index.

### Content Search

```bash
swiftseek "error code 503" --root "C:\Projects" --content
```

Searches the indexed content for the specified query.

#### Options

- **Case-sensitive search**: Use `--case-sensitive`.
- **Exact phrase search**: Add `--phrase` to match the exact phrase.
- **Fuzzy search**: Add `--fuzzy` to enable approximate matches (index mode only).
- **Scan fallback**: Use `--content-mode auto` to fall back to scan-based search if no index exists.

### Fallback to Scan Mode

If content indexing is disabled or unavailable, SwiftSeek falls back to scan-based search when `--content-mode auto` is used.

## Known Limitations

- Only plain text files are supported for content search.
- Files larger than 25 MB are skipped by default.
- Binary files are skipped using a simple heuristic (null byte detection).
- Access-denied files are skipped.
- Regular expressions must be valid according to .NET's `System.Text.RegularExpressions`.

## Index Commands

SwiftSeek now supports persistent indexing using SQLite. The following commands are available:

### Build Index

Build an index for a given root directory:

```sh
swiftseek index build --root "C:\"
```

### Show Index Status

Display the current index status:

```sh
swiftseek index status
```

### Rebuild Index

Rebuild the index from scratch:

```sh
swiftseek index rebuild --root "C:\"
```

### Notes

- The index stores file metadata such as path, name, extension, size, creation time, and modification time.
- Filename and directory searches use the index, while content searches remain on-demand.

## Development

- Lucene.NET is used for full-text indexing.
- SQLite is used for metadata indexing.

## Performance Notes

- **Indexing**: Content indexing commits in batches for stability on large trees, and reuses stored timestamps to skip unchanged files.
- **Search**: Case-sensitive content search uses a dedicated field with a case-preserving analyser; case-insensitive search uses a normalised field.
- **Snippets**: Highlighting is performed on stored text and keeps output small for fast CLI use.

## Repository Hygiene

- The `swiftseek.db` file is dynamically generated and ignored in Git.
- Lucene index files are stored under `.swiftseek/content-index` by default.
- The `.swiftseek` directory is ignored so personal indices never enter source control.

## Build and Execute Notes

SwiftSeek is designed for quick set-up:

- No external services or configuration files required.
- NuGet dependencies are resolved during `dotnet restore`.
- Index files are created automatically on first use.

## Future Work

- Additional file type support.
- Improved snippet highlighting.
