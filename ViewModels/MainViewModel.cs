using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDBExplorer.Models;
using LiteDBExplorer.Services;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
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
                    try
                    {
                        Documents.Add(document);
                    }
                    catch (InvalidCastException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Document add cast error: {ex.Message}");
                        // Skip this document but continue with others
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Document add error: {ex.Message}");
                        // Skip this document but continue with others
                    }
                }
                
                StatusMessage = $"Loaded {Documents.Count} documents from {SelectedCollection.Name}";
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDocumentsAsync cast error: {ex.Message}");
                StatusMessage = $"Invalid data format in collection: {ex.Message}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDocumentsAsync error: {ex.Message}");
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
        private async Task DeleteMultipleDocumentsAsync(IEnumerable<LiteDbDocument> documents)
        {
            if (SelectedCollection == null || documents == null || !documents.Any()) 
                return;

            var documentsList = documents.ToList();
            var documentIds = documentsList.Select(d => d.Id).ToList();

            try
            {
                StatusMessage = $"Deleting {documentsList.Count} documents...";
                var success = await _liteDbService.DeleteMultipleDocumentsAsync(SelectedCollection.Name, documentIds);
                
                if (success)
                {
                    await LoadDocumentsAsync();
                    StatusMessage = $"Successfully deleted {documentsList.Count} documents";
                }
                else
                {
                    StatusMessage = "Some documents could not be deleted";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting documents: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteAllDocumentsAsync()
        {
            if (SelectedCollection == null) return;

            try
            {
                StatusMessage = $"Deleting all documents from {SelectedCollection.Name}...";
                var success = await _liteDbService.DeleteAllDocumentsInCollectionAsync(SelectedCollection.Name);
                
                if (success)
                {
                    await LoadDocumentsAsync();
                    StatusMessage = $"All documents deleted from {SelectedCollection.Name}";
                }
                else
                {
                    StatusMessage = "Failed to delete all documents";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting all documents: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteDocumentByIdAsync(object documentId)
        {
            if (SelectedCollection == null || documentId == null) return;

            try
            {
                StatusMessage = $"Deleting document {documentId}...";
                var success = await _liteDbService.DeleteDocumentAsync(SelectedCollection.Name, documentId);
                
                if (success)
                {
                    await LoadDocumentsAsync();
                    StatusMessage = $"Document {documentId} deleted successfully";
                }
                else
                {
                    StatusMessage = $"Failed to delete document {documentId}";
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
            try
            {
                if (value != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Collection selected: {value.Name}");
                    _ = SafeLoadDocumentsAsync(value);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Collection selection cleared");
                    Documents.Clear();
                    SelectedDocument = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnSelectedCollectionChanged error: {ex.Message}");
                StatusMessage = $"Error selecting collection: {ex.Message}";
                Documents.Clear();
                SelectedDocument = null;
            }
        }

        private async Task SafeLoadDocumentsAsync(CollectionMetadata collection)
        {
            try
            {
                IsLoading = true;
                StatusMessage = $"Loading documents from {collection.Name}...";
                
                var documents = await _liteDbService.GetDocumentsAsync(collection.Name);
                
                // Clear and populate on UI thread
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    try
                    {
                        Documents.Clear();
                        foreach (var document in documents)
                        {
                            Documents.Add(document);
                        }
                        StatusMessage = $"Loaded {Documents.Count} documents from {collection.Name}";
                    }
                    catch (InvalidCastException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Document UI update cast error: {ex.Message}");
                        StatusMessage = $"Error updating document list: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Document UI update error: {ex.Message}");
                        StatusMessage = $"Error updating document list: {ex.Message}";
                    }
                });
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeLoadDocumentsAsync cast error: {ex.Message}");
                StatusMessage = $"Invalid data format in collection: {ex.Message}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeLoadDocumentsAsync error: {ex.Message}");
                StatusMessage = $"Error loading documents: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            try
            {
                // Implement basic search functionality
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Show all documents if search is empty
                    StatusMessage = $"Showing all {Documents.Count} documents";
                }
                else
                {
                    // Use async pattern properly to avoid blocking calls
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var filteredDocuments = await _liteDbService.GetDocumentsAsync(SelectedCollection?.Name ?? "");
                            var matchingDocuments = filteredDocuments
                                .Where(doc => doc.JsonString.Contains(value, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            // Update UI on the UI thread
                            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                            {
                                try
                                {
                                    Documents.Clear();
                                    foreach (var doc in matchingDocuments)
                                    {
                                        Documents.Add(doc);
                                    }
                                    StatusMessage = $"Found {Documents.Count} documents matching '{value}'";
                                }
                                catch (Exception ex)
                                {
                                    StatusMessage = $"Error updating search results: {ex.Message}";
                                    System.Diagnostics.Debug.WriteLine($"Search UI update error: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                            {
                                StatusMessage = $"Search error: {ex.Message}";
                                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search initialization error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Search initialization error: {ex.Message}");
            }
        }
    }
}