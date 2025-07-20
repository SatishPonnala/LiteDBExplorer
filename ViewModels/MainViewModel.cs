using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDBExplorer.Models;
using LiteDBExplorer.Services;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinUIEx;

namespace LiteDBExplorer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LiteDbService _liteDbService;
        private Window? _mainWindow;

        // Expose the service for direct access from UI
        public LiteDbService LiteDbService => _liteDbService;

        [ObservableProperty]
        private bool _isDatabaseOpen;

        [ObservableProperty]
        private string _currentDatabasePath = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<CollectionMetadata> _collections = new();

        [ObservableProperty]
        private CollectionMetadata? _selectedCollection;

        [ObservableProperty]
        private ObservableCollection<LiteDbDocument> _documents = new();

        [ObservableProperty]
        private LiteDbDocument? _selectedDocument;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isDarkMode;

        public MainViewModel()
        {
            _liteDbService = new LiteDbService();
        }

        public void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        [RelayCommand]
        private async Task OpenDatabaseAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Opening database...";

                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".db");
                picker.FileTypeFilter.Add("*");
                
                // Initialize picker with window handle for WinUI 3
                if (_mainWindow != null)
                {
                    var windowHandle = _mainWindow.GetWindowHandle();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
                }

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var success = await _liteDbService.OpenDatabaseAsync(file.Path);
                    if (success)
                    {
                        IsDatabaseOpen = true;
                        CurrentDatabasePath = file.Path;
                        await LoadCollectionsAsync();
                        StatusMessage = $"Database opened: {Path.GetFileName(file.Path)}";
                    }
                    else
                    {
                        StatusMessage = "Failed to open database";
                    }
                }
                else
                {
                    StatusMessage = "No file selected";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CloseDatabase()
        {
            _liteDbService.CloseDatabase();
            IsDatabaseOpen = false;
            CurrentDatabasePath = string.Empty;
            Collections.Clear();
            Documents.Clear();
            SelectedCollection = null;
            SelectedDocument = null;
            StatusMessage = "Database closed";
        }

        [RelayCommand]
        private async Task LoadCollectionsAsync()
        {
            if (!IsDatabaseOpen) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Loading collections...";
                
                var collections = await _liteDbService.GetCollectionsAsync();
                
                Collections.Clear();
                foreach (var collection in collections)
                {
                    Collections.Add(collection);
                }
                
                StatusMessage = $"Loaded {Collections.Count} collections";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading collections: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadDocumentsAsync()
        {
            if (SelectedCollection == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"Loading documents from {SelectedCollection.Name}...";
                
                var documents = await _liteDbService.GetDocumentsAsync(SelectedCollection.Name);
                
                Documents.Clear();
                foreach (var document in documents)
                {
                    Documents.Add(document);
                }
                
                StatusMessage = $"Loaded {Documents.Count} documents from {SelectedCollection.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading documents: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreateCollectionAsync()
        {
            if (!IsDatabaseOpen) return;

            // Generate a unique collection name
            var collectionName = $"collection_{DateTime.Now:yyyyMMdd_HHmmss}";
            
            try
            {
                StatusMessage = $"Creating collection '{collectionName}'...";
                var success = await _liteDbService.CreateCollectionAsync(collectionName);
                if (success)
                {
                    await LoadCollectionsAsync();
                    StatusMessage = $"Collection '{collectionName}' created successfully";
                }
                else
                {
                    StatusMessage = "Failed to create collection";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating collection: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteCollectionAsync()
        {
            if (SelectedCollection == null) return;

            try
            {
                StatusMessage = $"Deleting collection '{SelectedCollection.Name}'...";
                var success = await _liteDbService.DeleteCollectionAsync(SelectedCollection.Name);
                if (success)
                {
                    await LoadCollectionsAsync();
                    Documents.Clear();
                    SelectedDocument = null;
                    StatusMessage = $"Collection '{SelectedCollection.Name}' deleted successfully";
                }
                else
                {
                    StatusMessage = "Failed to delete collection";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting collection: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddDocumentAsync()
        {
            if (SelectedCollection == null) return;

            // Create a sample document
            var newDocument = new
            {
                name = "New Document",
                created = DateTime.Now,
                value = "sample value",
                type = "document"
            };

            try
            {
                StatusMessage = "Adding new document...";
                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(newDocument);
                var success = await _liteDbService.InsertDocumentAsync(SelectedCollection.Name, jsonDocument);
                if (success)
                {
                    await LoadDocumentsAsync();
                    StatusMessage = "Document added successfully";
                }
                else
                {
                    StatusMessage = "Failed to add document";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding document: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteDocumentAsync()
        {
            if (SelectedDocument == null || SelectedCollection == null) return;

            try
            {
                StatusMessage = "Deleting document...";
                var success = await _liteDbService.DeleteDocumentAsync(SelectedCollection.Name, SelectedDocument.Id);
                if (success)
                {
                    await LoadDocumentsAsync();
                    StatusMessage = "Document deleted successfully";
                }
                else
                {
                    StatusMessage = "Failed to delete document";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting document: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            StatusMessage = $"Theme changed to {(IsDarkMode ? "Dark" : "Light")} mode";
        }

        partial void OnSelectedCollectionChanged(CollectionMetadata? value)
        {
            if (value != null)
            {
                _ = LoadDocumentsAsync();
            }
            else
            {
                Documents.Clear();
                SelectedDocument = null;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            // Implement basic search functionality
            if (string.IsNullOrWhiteSpace(value))
            {
                // Show all documents if search is empty
                StatusMessage = $"Showing all {Documents.Count} documents";
            }
            else
            {
                // Filter documents based on JSON content
                var filteredDocuments = _liteDbService.GetDocumentsAsync(SelectedCollection?.Name ?? "")
                    .GetAwaiter().GetResult()
                    .Where(doc => doc.JsonString.Contains(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Documents.Clear();
                foreach (var doc in filteredDocuments)
                {
                    Documents.Add(doc);
                }
                
                StatusMessage = $"Found {Documents.Count} documents matching '{value}'";
            }
        }
    }
}