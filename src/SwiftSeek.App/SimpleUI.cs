using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SwiftSeek;

namespace SwiftSeek.App
{
    public class UISearchReporter : ISearchReporter
    {
        private ListBox _resultsList;

        public UISearchReporter(ListBox resultsList)
        {
            _resultsList = resultsList;
        }

        public void OnStart(SearchOptions options) { }
        public void OnMatch(SearchResult result)
        {
            if (_resultsList.InvokeRequired)
            {
                _resultsList.Invoke(new Action(() => _resultsList.Items.Add(result.Path)));
            }
            else
            {
                _resultsList.Items.Add(result.Path);
            }
        }
        public void OnStatus(string status) { }
        public void OnWarning(string warning) { }
        public void OnVerbose(string message) { }
        public void OnProgress(SearchStatistics statistics) { }
        public void OnComplete(SearchStatistics statistics) { }
    }

    public class SimpleUI : Form
    {
        private TextBox searchTermBox;
        private Button searchButton;
        private Button cancelButton;
        private ListBox resultsList;
        private TextBox previewContent;
        private Label statusText;
        private TextBox rootDirectoryBox;
        private CheckBox contentSearchBox;
        private ProgressBar progressBar;
        private CancellationTokenSource? cts;

        public SimpleUI()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "SwiftSeek";
            this.Size = new System.Drawing.Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = null;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10),
                AutoSize = false
            };

            // Search box row
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 50, AutoSize = false };
            searchTermBox = new TextBox { Width = 400, Location = new System.Drawing.Point(10, 5) };
            searchButton = new Button { Text = "Search", Width = 80, Location = new System.Drawing.Point(420, 5) };
            searchButton.Click += OnSearchClicked;
            rootDirectoryBox = new TextBox { Text = ".", Width = 400, Location = new System.Drawing.Point(510, 5) };
            contentSearchBox = new CheckBox { Text = "Search contents", Location = new System.Drawing.Point(920, 5) };

            searchPanel.Controls.Add(new Label { Text = "Search:", Location = new System.Drawing.Point(10, -3), AutoSize = true });
            searchPanel.Controls.Add(searchTermBox);
            searchPanel.Controls.Add(searchButton);
            searchPanel.Controls.Add(new Label { Text = "Path:", Location = new System.Drawing.Point(510, -3), AutoSize = true });
            searchPanel.Controls.Add(rootDirectoryBox);
            searchPanel.Controls.Add(contentSearchBox);

            mainPanel.Controls.Add(searchPanel, 0, 0);
            mainPanel.SetColumnSpan(searchPanel, 2);

            // Results panel
            var resultsPanel = new Panel { Dock = DockStyle.Fill };
            resultsPanel.Controls.Add(new Label { Text = "Results", Location = new System.Drawing.Point(5, 5), AutoSize = true });
            resultsList = new ListBox { Dock = DockStyle.Fill, Top = 25 };
            resultsList.SelectedIndexChanged += OnResultSelected;
            resultsPanel.Controls.Add(resultsList);
            mainPanel.Controls.Add(resultsPanel, 0, 1);

            // Preview panel
            var previewPanel = new Panel { Dock = DockStyle.Fill };
            previewPanel.Controls.Add(new Label { Text = "Preview", Location = new System.Drawing.Point(5, 5), AutoSize = true });
            previewContent = new TextBox { Dock = DockStyle.Fill, Top = 25, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both };
            previewPanel.Controls.Add(previewContent);
            mainPanel.Controls.Add(previewPanel, 1, 1);

            // Status bar
            var statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            statusText = new Label { Text = "Ready", AutoSize = true, Location = new System.Drawing.Point(10, 5) };
            cancelButton = new Button { Text = "Cancel", Width = 80, Location = new System.Drawing.Point(1100, 3), Enabled = false };
            cancelButton.Click += OnCancelClicked;
            progressBar = new ProgressBar { Width = 300, Location = new System.Drawing.Point(200, 5), Style = ProgressBarStyle.Marquee, Visible = false };
            statusPanel.Controls.Add(statusText);
            statusPanel.Controls.Add(progressBar);
            statusPanel.Controls.Add(cancelButton);
            mainPanel.Controls.Add(statusPanel, 0, 2);
            mainPanel.SetColumnSpan(statusPanel, 2);

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            this.Controls.Add(mainPanel);
        }

        private async void OnSearchClicked(object? sender, EventArgs e)
        {
            resultsList.Items.Clear();
            previewContent.Text = "";
            statusText.Text = "Searching...";
            progressBar.Visible = true;
            cancelButton.Enabled = true;
            searchButton.Enabled = false;

            cts = new CancellationTokenSource();

            try
            {
                var options = new SearchOptions
                {
                    SearchTerm = searchTermBox.Text,
                    RootDirectory = rootDirectoryBox.Text,
                    SearchContent = contentSearchBox.Checked,
                    ContentIndexPath = Path.Combine(rootDirectoryBox.Text, ".swiftseek", "content-index")
                };

                var reporter = new UISearchReporter(resultsList);
                var searcher = new Searcher(options, reporter);
                var stats = await searcher.SearchAsync(cts.Token);

                statusText.Text = $"Search complete: {stats.MatchesFound} matches in {stats.FilesScanned} files";
            }
            catch (OperationCanceledException)
            {
                statusText.Text = "Search cancelled";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error: {ex.Message}";
                MessageBox.Show(ex.Message, "Search Error");
            }
            finally
            {
                progressBar.Visible = false;
                cancelButton.Enabled = false;
                searchButton.Enabled = true;
                cts?.Dispose();
            }
        }

        private void OnResultSelected(object? sender, EventArgs e)
        {
            if (resultsList.SelectedItem is string path && File.Exists(path))
            {
                try
                {
                    var content = File.ReadAllText(path);
                    previewContent.Text = content.Length > 2000 ? content.Substring(0, 2000) + "...(truncated)" : content;
                }
                catch
                {
                    previewContent.Text = "(Binary file or cannot read)";
                }
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            cts?.Cancel();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new SimpleUI());
        }
    }
}
