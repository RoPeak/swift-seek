using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SwiftSeek;
using SwiftSeek.Lucene;

namespace SwiftSeek.App
{
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<SearchResultItem> _results = new();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isSearching;

        public MainWindow()
        {
            InitializeComponent();
            ResultsList.ItemsSource = _results;
        }

        private async void OnSearchClicked(object sender, RoutedEventArgs e)
        {
            if (_isSearching)
            {
                return;
            }

            var options = BuildSearchOptions();
            if (!Directory.Exists(options.RootDirectory))
            {
                StatusText.Text = $"Directory not found: {options.RootDirectory}";
                return;
            }

            _results.Clear();
            PreviewPath.Text = "Select a result to preview";
            PreviewContent.Text = string.Empty;

            _cancellationTokenSource = new CancellationTokenSource();
            _isSearching = true;
            ToggleSearchState(true);

            var reporter = new UiSearchReporter(DispatcherQueue, _results, UpdateStatus, UpdateProgress);
            var coordinator = new SearchCoordinator();

            try
            {
                await coordinator.RunAsync(options, reporter, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Search cancelled.");
            }
            finally
            {
                _isSearching = false;
                ToggleSearchState(false);
            }
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            if (_isSearching)
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        private void OnResultSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is not SearchResultItem item)
            {
                return;
            }

            PreviewPath.Text = item.Path;
            PreviewContent.Text = LoadPreviewText(item.Path);
        }

        private void ToggleSearchState(bool isSearching)
        {
            SearchButton.IsEnabled = !isSearching;
            CancelButton.IsEnabled = isSearching;
            ProgressBar.IsIndeterminate = isSearching;
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void UpdateProgress(SearchStatistics stats)
        {
            StatusText.Text = $"Scanned: {stats.FilesScanned}  Matches: {stats.MatchesFound}  Skipped: {stats.FilesSkipped}";
        }

        private static string LoadPreviewText(string path)
        {
            try
            {
                using var reader = new StreamReader(path);
                char[] buffer = new char[4096];
                var read = reader.Read(buffer, 0, buffer.Length);
                return new string(buffer, 0, read);
            }
            catch
            {
                return "Preview unavailable.";
            }
        }

        private SearchOptions BuildSearchOptions()
        {
            var options = new SearchOptions
            {
                SearchTerm = SearchTermBox.Text?.Trim() ?? string.Empty,
                RootDirectory = string.IsNullOrWhiteSpace(RootDirectoryBox.Text) ? "." : RootDirectoryBox.Text.Trim(),
                SearchContent = ContentSearchBox.IsChecked == true,
                UseRegex = RegexBox.IsChecked == true,
                CaseSensitive = CaseSensitiveBox.IsChecked == true,
                ExactPhrase = PhraseBox.IsChecked == true,
                FuzzySearch = FuzzyBox.IsChecked == true,
                ContentSearchMode = GetContentMode(),
                IncludeExtensions = ParseExtensions(IncludeExtensionsBox.Text),
                ExcludeExtensions = ParseExtensions(ExcludeExtensionsBox.Text),
                MinSize = ParseLong(MinSizeBox.Text, 0),
                MaxSize = ParseLong(MaxSizeBox.Text, 25 * 1024 * 1024),
                Verbose = false
            };

            return options;
        }

        private ContentSearchMode GetContentMode()
        {
            var value = (ContentModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return value switch
            {
                "Index" => ContentSearchMode.Index,
                "Scan" => ContentSearchMode.Scan,
                _ => ContentSearchMode.Auto
            };
        }

        private static string[] ParseExtensions(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            return input.Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .ToArray();
        }

        private static long ParseLong(string input, long fallback)
        {
            if (long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            {
                return value;
            }

            return fallback;
        }
    }
}
