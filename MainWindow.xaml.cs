using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LiteDBExplorer.Helpers;
using LiteDBExplorer.ViewModels;
using System;
using System.ComponentModel;

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
            
            // Wire up selection changed events
            CollectionsListView.SelectionChanged += (s, e) => 
            {
                if (e.AddedItems.Count > 0)
                    ViewModel.SelectedCollection = e.AddedItems[0] as Models.CollectionMetadata;
            };
            
            DocumentsListView.SelectionChanged += (s, e) =>
            {
                if (e.AddedItems.Count > 0)
                    ViewModel.SelectedDocument = e.AddedItems[0] as Models.LiteDbDocument;
            };
            
            // Add double-click to edit document
            DocumentsListView.DoubleTapped += DocumentsListView_DoubleTapped;
            
            SearchBox.TextChanged += (s, e) => ViewModel.SearchText = SearchBox.Text;
            
            // Bind to property changed events for UI updates
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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

        private async void DocumentsListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

            var dialog = new Views.DocumentEditorDialog(ViewModel.SelectedDocument.JsonString);
            dialog.XamlRoot = this.Content.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.LiteDbService.UpdateDocumentByIdAsync(
                        ViewModel.SelectedCollection?.Name ?? "", 
                        ViewModel.SelectedDocument.Id,
                        dialog.JsonContent);
                    
                    if (success)
                    {
                        await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
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

        private async void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

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
                await ViewModel.DeleteDocumentCommand.ExecuteAsync(null);
            }
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

        private async void EditDocumentContext_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

            var dialog = new Views.DocumentEditorDialog(ViewModel.SelectedDocument.JsonString);
            dialog.XamlRoot = this.Content.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.LiteDbService.UpdateDocumentByIdAsync(
                        ViewModel.SelectedCollection?.Name ?? "", 
                        ViewModel.SelectedDocument.Id,
                        dialog.JsonContent);
                    
                    if (success)
                    {
                        await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
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

        private async void DeleteDocumentContext_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

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
                await ViewModel.DeleteDocumentCommand.ExecuteAsync(null);
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
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
        }
    }
}
