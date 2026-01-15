# SwiftSeek

SwiftSeek is a command-line tool for searching files and directories on Windows. It supports searching by file names, directory names, and file contents with various filters and modes.

## How to Run

1. Ensure you have .NET 8 installed.
2. Build the project using the .NET CLI:
   ```
   dotnet build
   ```
3. Run the tool:
   ```
   swiftseek <search-term> [options]
   ```

## Supported Arguments

- `<search-term>`: The term to search for.
- `--root <directory>`: Root directory to start the search (default: current directory).
- `--content`: Search file contents.
- `--regex`: Use regular expressions for searching.
- `--case-sensitive`: Perform case-sensitive search.
- `--ext-include <exts>`: Comma-separated list of extensions to include.
- `--ext-exclude <exts>`: Comma-separated list of extensions to exclude.
- `--min-size <bytes>`: Minimum file size in bytes.
- `--max-size <bytes>`: Maximum file size in bytes (default: 25 MB).

## Example Usage

```sh
swiftseek "error code 503" --root "C:\Projects" --content --regex --ext-include ".log,.txt"
```

## Known Limitations

- Only plain text files are supported for content search.
- Files larger than 25 MB are skipped by default.
- Binary files are skipped using a simple heuristic (null byte detection).
- Access-denied files are skipped.
- Regular expressions must be valid according to .NET's `System.Text.RegularExpressions`.
