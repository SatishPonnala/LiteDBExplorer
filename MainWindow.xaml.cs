using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LiteDBExplorer.Helpers;
using LiteDBExplorer.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        }

        private void WireUpEventHandlers()
        {
            OpenDatabaseButton.Click += async (s, e) => await ViewModel.OpenDatabaseCommand.ExecuteAsync(null);
            CloseDatabaseButton.Click += (s, e) => ViewModel.CloseDatabaseCommand.Execute(null);
            AddCollectionButton.Click += CreateCollection_Click;
            
            AddDocumentButton.Click += AddDocument_Click;
            DeleteDocumentButton.Click += DeleteDocument_Click;
            DeleteCollectionButton.Click += DeleteCollection_Click;
            RefreshButton.Click += async (s, e) => await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
            
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
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to cast item to CollectionMetadata. Item: {item?.ToString() ?? "null"}");
                            ViewModel.SelectedCollection = null;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No items in selection");
                        ViewModel.SelectedCollection = null;
                    }
                }
                catch (InvalidCastException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Collection selection InvalidCastException: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    ViewModel.SelectedCollection = null;
                    ViewModel.StatusMessage = $"Collection selection error: {ex.Message}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Collection selection general error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    ViewModel.StatusMessage = $"Error selecting collection: {ex.Message}";
                }
            };
            
            SearchBox.TextChanged += SearchBox_TextChanged;
            
            // Bind to property changed events for UI updates
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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
                DocumentCountText.Text = $"{count} document{(count != 1 ? "s" : "")}";
            }
        }

        private void ViewModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ViewModeToggle.Content = "Card View";
        }

        private void ViewModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModeToggle.Content = "Tree View";
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

        private async void EditDocumentContext_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument != null)
            {
                await EditDocumentAsync(ViewModel.SelectedDocument);
            }
        }

        private async void DeleteDocumentContext_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument != null)
            {
                await DeleteDocumentAsync(ViewModel.SelectedDocument);
            }
        }

        private void CopyJsonContext_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument != null)
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(ViewModel.SelectedDocument.JsonString);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                ViewModel.StatusMessage = "JSON copied to clipboard";
            }
        }

        private async void CreateCollection_Click(object sender, RoutedEventArgs e)
        {
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
                    OpenDatabaseButton.IsEnabled = !ViewModel.IsLoading;
                    StatusProgressRing.IsActive = ViewModel.IsLoading;
                    StatusProgressRing.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(ViewModel.IsDatabaseOpen):
                    CloseDatabaseButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    AddCollectionButton.IsEnabled = ViewModel.IsDatabaseOpen;
                    break;
                case nameof(ViewModel.SelectedCollection):
                    AddDocumentButton.IsEnabled = ViewModel.SelectedCollection != null;
                    DeleteCollectionButton.IsEnabled = ViewModel.SelectedCollection != null;
                    RefreshButton.IsEnabled = ViewModel.SelectedCollection != null;
                    
                    // Clear document detail when collection changes
                    if (ViewModel.SelectedCollection == null)
                    {
                        if (SelectedDocumentId != null)
                        {
                            SelectedDocumentId.Text = "Select a document";
                        }
                        if (JsonTreeViewer != null)
                        {
                            JsonTreeViewer.Clear();
                        }
                        if (RawJsonDisplay != null)
                        {
                            RawJsonDisplay.Text = "";
                        }
                    }
                    break;
                case nameof(ViewModel.SelectedDocument):
                    DeleteDocumentButton.IsEnabled = ViewModel.SelectedDocument != null;
                    break;
                case nameof(ViewModel.StatusMessage):
                    StatusText.Text = ViewModel.StatusMessage;
                    break;
                case nameof(ViewModel.CurrentDatabasePath):
                    DatabasePathText.Text = ViewModel.CurrentDatabasePath;
                    break;
                case nameof(ViewModel.Documents):
                    UpdateDocumentCount();
                    break;
            }
        }

        private async void AddDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedCollection == null) return;

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
            await DeleteDocumentAsync(ViewModel.SelectedDocument);
        }

        private async void DeleteCollection_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedCollection == null) return;

            var dialog = new ContentDialog()
            {
                Title = "Delete Collection",
                Content = $"Are you sure you want to delete the collection '{ViewModel.SelectedCollection.Name}' and all its documents? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteCollectionCommand.ExecuteAsync(null);
            }
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
                        if (SelectedDocumentId != null)
                        {
                            SelectedDocumentId.Text = "Select a document";
                        }
                        if (JsonTreeViewer != null)
                        {
                            JsonTreeViewer.Clear();
                        }
                        if (RawJsonDisplay != null)
                        {
                            RawJsonDisplay.Text = "";
                        }
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

        private void ViewDocumentInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.LiteDbDocument document)
            {
                LoadDocumentDetail(document);
                ViewModel.SelectedDocument = document;
                ViewModel.StatusMessage = $"Viewing document: {document.Id}";
            }
        }

        private void ViewDocumentDetails_Click(object sender, RoutedEventArgs e)
        {
            // Get the document from the context - this works for both context menu and flyout
            Models.LiteDbDocument? document = null;
            
            if (sender is MenuFlyoutItem menuItem)
            {
                // For context menu, get document from the ListView's selected item or from the menu's parent
                var parent = menuItem.Parent;
                while (parent != null && document == null)
                {
                    if (parent is MenuFlyout flyout && flyout.Target is FrameworkElement target)
                    {
                        document = target.DataContext as Models.LiteDbDocument;
                        break;
                    }
                    parent = (parent as FrameworkElement)?.Parent;
                }
            }
            
            if (document != null)
            {
                LoadDocumentDetail(document);
                ViewModel.SelectedDocument = document;
                ViewModel.StatusMessage = $"Viewing document: {document.Id}";
            }
        }
    }
}
