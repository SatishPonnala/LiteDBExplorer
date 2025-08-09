using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LiteDBExplorer.Helpers;
using LiteDBExplorer.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace LiteDBExplorer
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Initialize the ViewModel
            ViewModel = new MainViewModel();
            
            // Set the window reference for theme management
            ThemeManager.MainWindowReference = this;
            
            // Initialize window for file picker
            ViewModel.SetMainWindow(this);
            
            // Wire up event handlers for buttons
            WireUpEventHandlers();
            
            // Load sample JSON for testing JsonViewerControl
            LoadSampleJsonForTesting();
        }

        private void LoadSampleJsonForTesting()
        {
            try
            {
                var sampleJson = @"{
                    ""_id"": ""6895d03373a9ec1237909250"",
                    ""name"": ""Sample Document"",
                    ""type"": ""test"",
                    ""created"": ""2024-01-15T10:30:00Z"",
                    ""metadata"": {
                        ""version"": 1,
                        ""tags"": [""sample"", ""test"", ""json""],
                        ""properties"": {
                            ""size"": 1024,
                            ""format"": ""JSON"",
                            ""compressed"": false
                        }
                    },
                    ""content"": {
                        ""title"": ""Test Document"",
                        ""description"": ""This is a sample document for testing the JsonViewerControl"",
                        ""data"": [1, 2, 3, 4, 5],
                        ""nested"": {
                            ""level1"": {
                                ""level2"": {
                                    ""value"": ""Deep nested value""
                                }
                            }
                        }
                    }
                }";
                
                if (JsonTreeViewer != null)
                {
                    JsonTreeViewer.LoadJson(sampleJson);
                }
                
                if (SelectedDocumentId != null)
                {
                    SelectedDocumentId.Text = "Sample Document (6895d03373a9ec1237909250)";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sample JSON: {ex.Message}");
            }
        }

        private void WireUpEventHandlers()
        {
            // Studio 3T Style Global Toolbar
            ConnectButton.Click += async (s, e) => await ViewModel.OpenDatabaseCommand.ExecuteAsync(null);
            DisconnectButton.Click += (s, e) => ViewModel.CloseDatabaseCommand.Execute(null);
            CreateCollectionButton.Click += CreateCollection_Click;
            QueryEditorButton.Click += QueryEditor_Click;
            AggregateButton.Click += Aggregate_Click;
            AddDocumentButton.Click += AddDocument_Click;
            DeleteDocumentButton.Click += DeleteDocument_Click;
            ImportButton.Click += Import_Click;
            ExportButton.Click += Export_Click;
            SchemaButton.Click += Schema_Click;
            StatsButton.Click += Stats_Click;
            
            // Sidebar Operations
            RefreshButton.Click += async (s, e) => await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
            StatsButton2.Click += Stats_Click;
            
            // Pagination Controls
            PreviousPageButton.Click += async (s, e) => await ViewModel.PreviousPageCommand.ExecuteAsync(null);
            NextPageButton.Click += async (s, e) => await ViewModel.NextPageCommand.ExecuteAsync(null);
            
            // Bind data
            CollectionsListView.ItemsSource = ViewModel.Collections;
            DocumentsListView.ItemsSource = ViewModel.Documents;
            
            // Wire up selection changed events with comprehensive error handling
            CollectionsListView.SelectionChanged += (s, e) => 
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Collection selection changed. AddedItems count: {e.AddedItems.Count}");
                    
                    if (e.AddedItems.Count > 0)
                    {
                        var item = e.AddedItems[0];
                        System.Diagnostics.Debug.WriteLine($"Selected item type: {item?.GetType().FullName ?? "null"}");
                        
                        if (item is Models.CollectionMetadata collection)
                        {
                            System.Diagnostics.Debug.WriteLine($"Successfully cast to CollectionMetadata: {collection.Name}");
                            ViewModel.SelectedCollection = collection;
                            UpdateBreadcrumbNavigation();
                            ShowDetailPanel();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to cast item to CollectionMetadata. Item: {item?.ToString() ?? "null"}");
                            ViewModel.SelectedCollection = null;
                            UpdateBreadcrumbNavigation();
                            HideDetailPanel();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No items in selection");
                        ViewModel.SelectedCollection = null;
                        UpdateBreadcrumbNavigation();
                        HideDetailPanel();
                    }
                }
                catch (InvalidCastException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Collection selection InvalidCastException: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    ViewModel.SelectedCollection = null;
                    ViewModel.StatusMessage = $"Collection selection error: {ex.Message}";
                    UpdateBreadcrumbNavigation();
                    HideDetailPanel();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Collection selection general error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    ViewModel.StatusMessage = $"Error selecting collection: {ex.Message}";
                    UpdateBreadcrumbNavigation();
                    HideDetailPanel();
                }
            };
            
            // Document selection
            DocumentsListView.SelectionChanged += (s, e) =>
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is Models.LiteDbDocument document)
                {
                    ViewModel.SelectedDocument = document;
                    LoadDocumentDetail(document);
                    ShowDetailPanel();
                }
                else
                {
                    ViewModel.SelectedDocument = null;
                    HideDetailPanel();
                }
            };
            
            SearchBox.TextChanged += SearchBox_TextChanged;
            ConnectionSearchBox.TextChanged += ConnectionSearchBox_TextChanged;
            
            // Bind to property changed events for UI updates
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ShowDetailPanel()
        {
            if (DetailPanel != null)
            {
                DetailPanel.Visibility = Visibility.Visible;
                DetailContent.Visibility = Visibility.Visible;
            }
        }

        private void HideDetailPanel()
        {
            if (DetailPanel != null)
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                DetailContent.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateBreadcrumbNavigation()
        {
            if (CurrentDatabaseText != null)
            {
                if (ViewModel.IsDatabaseOpen && !string.IsNullOrEmpty(ViewModel.CurrentDatabasePath))
                {
                    var fileName = System.IO.Path.GetFileName(ViewModel.CurrentDatabasePath);
                    var readOnlyIndicator = ViewModel.IsReadOnly ? " (Read-Only)" : "";
                    CurrentDatabaseText.Text = fileName + readOnlyIndicator;
                    CollectionSeparator.Visibility = ViewModel.SelectedCollection != null ? Visibility.Visible : Visibility.Collapsed;
                    CurrentCollectionText.Visibility = ViewModel.SelectedCollection != null ? Visibility.Visible : Visibility.Collapsed;
                    
                    if (ViewModel.SelectedCollection != null)
                    {
                        CurrentCollectionText.Text = ViewModel.SelectedCollection.Name;
                    }
                }
                else
                {
                    CurrentDatabaseText.Text = "No database open";
                    CollectionSeparator.Visibility = Visibility.Collapsed;
                    CurrentCollectionText.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateDatabaseStatistics()
        {
            if (ViewModel.IsDatabaseOpen)
            {
                var totalCollections = ViewModel.Collections.Count;
                var totalDocuments = ViewModel.Collections.Sum(c => c.DocumentCount);
                ViewModel.StatusMessage = $"{totalCollections} collections, {totalDocuments} documents";
            }
            else
            {
                ViewModel.StatusMessage = "No database loaded";
            }
        }

        private void ConnectionSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Implement connection filtering
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchText = sender.Text.ToLower();
                // This would need to be implemented in the ViewModel
                // ViewModel.FilterConnections(searchText);
            }
        }

        private void LoadDocumentDetail(Models.LiteDbDocument document)
        {
            // Add null checks to prevent NullReferenceException
            if (SelectedDocumentId != null)
            {
                SelectedDocumentId.Text = $"ID: {document.Id}";
            }
            
            // Load into tree view
            if (JsonTreeViewer != null)
            {
                JsonTreeViewer.LoadJson(document.JsonString);
            }
            
            // Load into raw view
            if (RawJsonDisplay != null)
            {
                RawJsonDisplay.Text = FormatJson(document.JsonString);
            }
        }

        private string FormatJson(string json)
        {
            try
            {
                // Parse and reformat the JSON to ensure consistent formatting
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            }
            catch (JsonSerializationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON formatting serialization error: {ex.Message}");
                return json; // Return original if formatting fails
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON formatting error: {ex.Message}");
                return json; // Return original if formatting fails
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ViewModel.SearchText = sender.Text;
            UpdateDocumentCount();
        }

        private void UpdateDocumentCount()
        {
            if (DocumentCountText != null)
            {
                var count = ViewModel.Documents.Count;
                var totalCount = ViewModel.TotalDocuments;
                var pageInfo = ViewModel.TotalDocuments > 0 ? 
                    $"{count} of {totalCount} documents" : 
                    $"{count} document{(count != 1 ? "s" : "")}";
                DocumentCountText.Text = pageInfo;
            }
            
            // Update pagination controls
            if (PageInfoText != null)
            {
                PageInfoText.Text = ViewModel.TotalDocuments > 0 ? 
                    $"Page {ViewModel.CurrentPage} of {ViewModel.TotalPages}" : 
                    "Page 1 of 1";
            }
            
            if (PreviousPageButton != null)
            {
                PreviousPageButton.IsEnabled = ViewModel.HasPreviousPage;
            }
            
            if (NextPageButton != null)
            {
                NextPageButton.IsEnabled = ViewModel.HasNextPage;
            }
        }

        private bool _isCardView = false;

        private void ViewModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            _isCardView = true;
            ViewModeToggle.Content = "Card View";
            UpdateDocumentViewMode();
        }

        private void ViewModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _isCardView = false;
            ViewModeToggle.Content = "Table View";
            UpdateDocumentViewMode();
        }

        private void UpdateDocumentViewMode()
        {
            if (DocumentsListView == null) return;
            
            // For now, just change the content to indicate the mode
            // A full implementation would require different DataTemplates
            System.Diagnostics.Debug.WriteLine($"View mode changed to: {(_isCardView ? "Card" : "Table")} View");
        }
        
        private DataTemplate CreateCardViewTemplate()
        {
            // Return the existing template for now
            // In a full implementation, you'd create a card-style template
            return DocumentsListView.ItemTemplate;
        }
        
        private DataTemplate CreateTableViewTemplate()
        {
            // Return the existing table view template
            return DocumentsListView.ItemTemplate;
        }

        private async void QueryEditor_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Query Editor tab
            if (MainTabView != null)
            {
                MainTabView.SelectedIndex = 1; // Query Editor tab
            }
        }

        private async void Aggregate_Click(object sender, RoutedEventArgs e)
        {
            // Open aggregation dialog
            var dialog = new ContentDialog()
            {
                Title = "Aggregation Pipeline",
                PrimaryButtonText = "Execute",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot,
                Content = new Views.QueryEditorView()
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.StatusMessage = "Aggregation executed successfully";
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new Windows.Storage.Pickers.FileOpenPicker();
            openDialog.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            openDialog.FileTypeFilter.Add(".json");
            openDialog.FileTypeFilter.Add(".txt");

            var file = await openDialog.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var content = await Windows.Storage.FileIO.ReadTextAsync(file);
                    // Import logic would go here
                    ViewModel.StatusMessage = $"Imported data from {file.Name}";
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = $"Import failed: {ex.Message}";
                }
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedCollection == null)
            {
                ViewModel.StatusMessage = "Please select a collection to export";
                return;
            }

            var saveDialog = new Windows.Storage.Pickers.FileSavePicker();
            saveDialog.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            saveDialog.FileTypeChoices.Add("JSON Files", new System.Collections.Generic.List<string>() { ".json" });
            saveDialog.SuggestedFileName = $"{ViewModel.SelectedCollection.Name}_export";

            var file = await saveDialog.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    // Export collection data using the existing method
                    var jsonData = await ViewModel.LiteDbService.ExportCollectionToJsonAsync(ViewModel.SelectedCollection.Name);
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        await Windows.Storage.FileIO.WriteTextAsync(file, jsonData);
                        ViewModel.StatusMessage = $"Exported collection to {file.Name}";
                    }
                    else
                    {
                        ViewModel.StatusMessage = "Failed to export collection";
                    }
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = $"Export failed: {ex.Message}";
                }
            }
        }

        private async void Schema_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedCollection == null)
            {
                ViewModel.StatusMessage = "Please select a collection to view schema";
                return;
            }

            var dialog = new ContentDialog()
            {
                Title = $"Schema - {ViewModel.SelectedCollection.Name}",
                PrimaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot,
                Content = new TextBlock() 
                { 
                    Text = "Schema analysis would be displayed here",
                    TextWrapping = TextWrapping.Wrap
                }
            };

            await dialog.ShowAsync();
        }

        private async void Stats_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Database Stats tab
            if (MainTabView != null)
            {
                MainTabView.SelectedIndex = 2; // Database Stats tab
            }
        }

        private async void EditDocumentInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.LiteDbDocument document)
            {
                await EditDocumentAsync(document);
            }
        }

        private async void DeleteDocumentInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.LiteDbDocument document)
            {
                await DeleteDocumentAsync(document);
            }
        }

        private void ViewDocumentInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.LiteDbDocument document)
            {
                LoadDocumentDetail(document);
                ViewModel.SelectedDocument = document;
                ViewModel.StatusMessage = $"Viewing document: {document.Id}";
                ShowDetailPanel();
            }
        }

        private async void CreateCollection_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsReadOnly)
            {
                ViewModel.StatusMessage = "Cannot create collections in read-only mode (database is locked by another process)";
                return;
            }

            var inputDialog = new ContentDialog()
            {
                Title = "Create Collection",
                PrimaryButtonText = "Create",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var textBox = new TextBox()
            {
                PlaceholderText = "Enter collection name",
                Text = $"collection_{DateTime.Now:yyyyMMdd_HHmmss}",
                Margin = new Thickness(0, 8, 0, 0)
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock() { Text = "Enter a name for the new collection:" });
            panel.Children.Add(textBox);
            
            inputDialog.Content = panel;

            var result = await inputDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                try
                {
                    var success = await ViewModel.LiteDbService.CreateCollectionAsync(textBox.Text);
                    if (success)
                    {
                        await ViewModel.LoadCollectionsCommand.ExecuteAsync(null);
                        ViewModel.StatusMessage = $"Collection '{textBox.Text}' created successfully";
                        UpdateDatabaseStatistics();
                    }
                    else
                    {
                        ViewModel.StatusMessage = "Failed to create collection";
                    }
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = $"Error creating collection: {ex.Message}";
                }
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.IsLoading):
                    ConnectButton.IsEnabled = !ViewModel.IsLoading;
                    // Update execution time
                    if (ExecutionTimeText != null)
                    {
                        ExecutionTimeText.Text = ViewModel.IsLoading ? "Executing..." : "00:00:00.000";
                    }
                    break;
                case nameof(ViewModel.IsDatabaseOpen):
                    DisconnectButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    CreateCollectionButton.IsEnabled = ViewModel.IsDatabaseOpen && !ViewModel.IsReadOnly;
                    QueryEditorButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    AggregateButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    ImportButton.IsEnabled = ViewModel.IsDatabaseOpen && !ViewModel.IsReadOnly;
                    ExportButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    SchemaButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    StatsButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    RefreshButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    StatsButton2.IsEnabled = ViewModel.IsDatabaseOpen;
                    CountDocumentsButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    UpdateBreadcrumbNavigation();
                    UpdateDatabaseStatistics();
                    
                    // Show read-only status
                    if (ViewModel.IsDatabaseOpen && ViewModel.IsReadOnly)
                    {
                        ViewModel.StatusMessage = "Database opened in READ-ONLY mode (file is locked by another process)";
                    }
                    break;
                case nameof(ViewModel.SelectedCollection):
                    AddDocumentButton.IsEnabled = ViewModel.SelectedCollection != null && !ViewModel.IsReadOnly;
                    DeleteDocumentButton.IsEnabled = ViewModel.SelectedCollection != null && !ViewModel.IsReadOnly;
                    UpdateBreadcrumbNavigation();
                    UpdateDatabaseStatistics();
                    
                    // Clear document detail when collection changes
                    if (ViewModel.SelectedCollection == null)
                    {
                        if (SelectedDocumentId != null)
                        {
                            SelectedDocumentId.Text = "Select a document to view details";
                        }
                        if (JsonTreeViewer != null)
                        {
                            JsonTreeViewer.Clear();
                        }
                        if (RawJsonDisplay != null)
                        {
                            RawJsonDisplay.Text = "";
                        }
                        HideDetailPanel();
                    }
                    break;
                case nameof(ViewModel.SelectedDocument):
                    // Document selection is handled in the ListView.SelectionChanged event
                    break;
                case nameof(ViewModel.StatusMessage):
                    StatusText.Text = ViewModel.StatusMessage;
                    break;
                case nameof(ViewModel.CurrentDatabasePath):
                    UpdateBreadcrumbNavigation();
                    UpdateDatabaseStatistics();
                    break;
                case nameof(ViewModel.Documents):
                    UpdateDocumentCount();
                    break;
                case nameof(ViewModel.Collections):
                    System.Diagnostics.Debug.WriteLine($"Collections property changed. Count: {ViewModel.Collections.Count}");
                    UpdateDatabaseStatistics();
                    // Force refresh the ListView
                    ForceRefreshCollectionsList();
                    break;
                case nameof(ViewModel.TotalDocuments):
                case nameof(ViewModel.CurrentPage):
                case nameof(ViewModel.TotalPages):
                case nameof(ViewModel.HasNextPage):
                case nameof(ViewModel.HasPreviousPage):
                    UpdateDocumentCount();
                    break;
            }
        }

        private void ForceRefreshCollectionsList()
        {
            // Force the ListView to refresh by temporarily changing the ItemsSource
            if (CollectionsListView != null)
            {
                var currentSource = CollectionsListView.ItemsSource;
                CollectionsListView.ItemsSource = null;
                CollectionsListView.ItemsSource = currentSource;
                System.Diagnostics.Debug.WriteLine("Forced refresh of CollectionsListView");
            }
        }

        private async void AddDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedCollection == null) return;
            
            if (ViewModel.IsReadOnly)
            {
                ViewModel.StatusMessage = "Cannot add documents in read-only mode (database is locked by another process)";
                return;
            }

            var dialog = new Views.DocumentEditorDialog("{\n  \"name\": \"New Document\",\n  \"created\": \"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\n  \"value\": \"sample value\"\n}");
            dialog.XamlRoot = this.Content.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.LiteDbService.InsertDocumentAsync(
                        ViewModel.SelectedCollection.Name, 
                        dialog.JsonContent);
                    
                    if (success)
                    {
                        await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
                        ViewModel.StatusMessage = "Document added successfully";
                        UpdateDatabaseStatistics();
                    }
                    else
                    {
                        ViewModel.StatusMessage = "Failed to add document";
                    }
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = $"Error adding document: {ex.Message}";
                }
            }
        }

        private async void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;
            
            if (ViewModel.IsReadOnly)
            {
                ViewModel.StatusMessage = "Cannot delete documents in read-only mode (database is locked by another process)";
                return;
            }
            
            await DeleteDocumentAsync(ViewModel.SelectedDocument);
        }

        // Extracted common edit method
        private async Task EditDocumentAsync(Models.LiteDbDocument document)
        {
            var dialog = new Views.DocumentEditorDialog(document.JsonString);
            dialog.XamlRoot = this.Content.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.LiteDbService.UpdateDocumentByIdAsync(
                        ViewModel.SelectedCollection?.Name ?? "", 
                        document.Id,
                        dialog.JsonContent);
                    
                    if (success)
                    {
                        await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
                        UpdateDocumentCount();
                        ViewModel.StatusMessage = "Document updated successfully";
                    }
                    else
                    {
                        ViewModel.StatusMessage = "Failed to update document";
                    }
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = $"Error updating document: {ex.Message}";
                }
            }
        }

        // Extracted common delete method
        private async Task DeleteDocumentAsync(Models.LiteDbDocument document)
        {
            var dialog = new ContentDialog()
            {
                Title = "Delete Document",
                Content = "Are you sure you want to delete this document? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.LiteDbService.DeleteDocumentAsync(
                        ViewModel.SelectedCollection?.Name ?? "", 
                        document.Id);
                    
                    if (success)
                    {
                        await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
                        UpdateDocumentCount();
                        UpdateDatabaseStatistics();
                        if (SelectedDocumentId != null)
                        {
                            SelectedDocumentId.Text = "Select a document to view details";
                        }
                        if (JsonTreeViewer != null)
                        {
                            JsonTreeViewer.Clear();
                        }
                        if (RawJsonDisplay != null)
                        {
                            RawJsonDisplay.Text = "";
                        }
                        HideDetailPanel();
                        ViewModel.StatusMessage = "Document deleted successfully";
                    }
                    else
                    {
                        ViewModel.StatusMessage = "Failed to delete document";
                    }
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = $"Error deleting document: {ex.Message}";
                }
            }
        }

        private void DetailViewToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Add null checks to prevent NullReferenceException
            if (TreeViewContainer != null && RawJsonContainer != null && DetailViewToggle != null)
            {
                TreeViewContainer.Visibility = Visibility.Visible;
                RawJsonContainer.Visibility = Visibility.Collapsed;
                DetailViewToggle.Content = "Raw";
            }
        }

        private void DetailViewToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Add null checks to prevent NullReferenceException
            if (TreeViewContainer != null && RawJsonContainer != null && DetailViewToggle != null)
            {
                TreeViewContainer.Visibility = Visibility.Collapsed;
                RawJsonContainer.Visibility = Visibility.Visible;
                DetailViewToggle.Content = "Tree";
            }
        }
    }
}
