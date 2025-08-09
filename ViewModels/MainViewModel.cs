using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LiteDBExplorer.Models;
using LiteDBExplorer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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

        [ObservableProperty]
        private bool _isReadOnly;

        // Add pagination properties
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 25; // Reduced from 50 to make scrolling more apparent

        [ObservableProperty]
        private int _totalDocuments = 0;

        [ObservableProperty]
        private bool _hasNextPage = false;

        [ObservableProperty]
        private bool _hasPreviousPage = false;

        [ObservableProperty]
        private int _totalPages = 1;

        // Add search-specific pagination
        [ObservableProperty]
        private bool _isSearching = false;

        private CancellationTokenSource? _searchCancellationTokenSource;

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
                    try
                    {
                        var success = await _liteDbService.OpenDatabaseAsync(file.Path);
                        if (success)
                        {
                            IsDatabaseOpen = true;
                            CurrentDatabasePath = file.Path;
                            IsReadOnly = _liteDbService.IsReadOnly;
                            await LoadCollectionsAsync();
                            StatusMessage = $"Database opened: {Path.GetFileName(file.Path)}" + (IsReadOnly ? " (Read-Only)" : "");
                        }
                        else
                        {
                            StatusMessage = "Failed to open database - unknown error";
                        }
                    }
                    catch (InvalidOperationException ioEx)
                    {
                        StatusMessage = $"Database Error: {ioEx.Message}";
                        System.Diagnostics.Debug.WriteLine($"Database open error: {ioEx.Message}");
                        if (ioEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner exception: {ioEx.InnerException.Message}");
                        }
                        
                        // Generate comprehensive troubleshooting guide
                        System.Diagnostics.Debug.WriteLine("Generating troubleshooting guide...");
                        try
                        {
                            var actualException = ioEx.InnerException ?? ioEx;
                            var troubleshootingGuide = await _liteDbService.GetTroubleshootingGuideAsync(actualException);
                            System.Diagnostics.Debug.WriteLine(troubleshootingGuide);
                            
                            // Also run diagnostics
                            var diagnostics = await _liteDbService.DiagnoseDatabaseAsync(file.Path);
                            System.Diagnostics.Debug.WriteLine("=== FILE DIAGNOSTICS ===");
                            System.Diagnostics.Debug.WriteLine(diagnostics);
                            System.Diagnostics.Debug.WriteLine("========================");
                        }
                        catch (Exception troubleshootingEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Troubleshooting guide generation failed: {troubleshootingEx.Message}");
                        }
                    }
                    catch (Exception dbEx)
                    {
                        StatusMessage = $"Unexpected Error: {dbEx.Message}";
                        System.Diagnostics.Debug.WriteLine($"Unexpected database error: {dbEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Exception type: {dbEx.GetType().Name}");
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
                System.Diagnostics.Debug.WriteLine($"OpenDatabaseAsync error: {ex.Message}");
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
            if (!IsDatabaseOpen) 
            {
                System.Diagnostics.Debug.WriteLine("LoadCollectionsAsync: Database not open");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Loading collections...";
                
                System.Diagnostics.Debug.WriteLine("LoadCollectionsAsync: Starting to load collections");
                var collections = await _liteDbService.GetCollectionsAsync();
                System.Diagnostics.Debug.WriteLine($"LoadCollectionsAsync: Retrieved {collections.Count} collections from service");
                
                Collections.Clear();
                System.Diagnostics.Debug.WriteLine("LoadCollectionsAsync: Cleared existing collections");
                
                foreach (var collection in collections)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadCollectionsAsync: Adding collection '{collection.Name}' with {collection.DocumentCount} documents");
                    Collections.Add(collection);
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadCollectionsAsync: Final collection count: {Collections.Count}");
                StatusMessage = $"Loaded {Collections.Count} collections";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCollectionsAsync: Error loading collections: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadCollectionsAsync: Stack trace: {ex.StackTrace}");
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
                
                // Calculate skip based on current page
                var skip = (CurrentPage - 1) * PageSize;
                
                // Get total count first
                TotalDocuments = await _liteDbService.GetDocumentCountAsync(SelectedCollection.Name);
                
                // Load documents with pagination
                var documents = await _liteDbService.GetDocumentsAsync(SelectedCollection.Name, skip, PageSize);
                
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
                
                // Update pagination state
                TotalPages = TotalDocuments > 0 ? (int)Math.Ceiling((double)TotalDocuments / PageSize) : 1;
                HasPreviousPage = CurrentPage > 1;
                HasNextPage = CurrentPage < TotalPages;
                
                StatusMessage = $"Loaded {Documents.Count} documents (Page {CurrentPage} of {TotalPages}) from {SelectedCollection.Name}";
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
        private async Task NextPageAsync()
        {
            if (HasNextPage)
            {
                CurrentPage++;
                await LoadDocumentsAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (HasPreviousPage)
            {
                CurrentPage--;
                await LoadDocumentsAsync();
            }
        }

        [RelayCommand]
        private async Task GoToPageAsync(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= TotalPages)
            {
                CurrentPage = pageNumber;
                await LoadDocumentsAsync();
            }
        }

        [RelayCommand]
        private async Task CreateCollectionAsync()
        {
            if (!IsDatabaseOpen || IsReadOnly) return;

            // Generate a unique collection name
            var collectionName = $"collection_{DateTime.Now:yyyyMMdd_HHmms}";
            
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
            if (SelectedCollection == null || IsReadOnly) return;

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
            if (SelectedCollection == null || IsReadOnly) return;

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
            if (SelectedDocument == null || SelectedCollection == null || IsReadOnly) return;

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
            if (SelectedCollection == null || documents == null || !documents.Any() || IsReadOnly) 
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
            if (SelectedCollection == null || IsReadOnly) return;

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
            if (SelectedCollection == null || documentId == null || IsReadOnly) return;

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
                // Reset to first page when switching collections
                CurrentPage = 1;
                
                IsLoading = true;
                StatusMessage = $"Loading documents from {collection.Name}...";
                
                // Calculate skip based on current page
                var skip = (CurrentPage - 1) * PageSize;
                
                // Get total count first
                TotalDocuments = await _liteDbService.GetDocumentCountAsync(collection.Name);
                
                // Load documents with pagination
                var documents = await _liteDbService.GetDocumentsAsync(collection.Name, skip, PageSize);
                
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
                        
                        // Update pagination state
                        TotalPages = TotalDocuments > 0 ? (int)Math.Ceiling((double)TotalDocuments / PageSize) : 1;
                        HasPreviousPage = CurrentPage > 1;
                        HasNextPage = CurrentPage < TotalPages;
                        
                        StatusMessage = $"Loaded {Documents.Count} documents (Page {CurrentPage} of {TotalPages}) from {collection.Name}";
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
            // Cancel previous search
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            
            var cancellationToken = _searchCancellationTokenSource.Token;
            
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    IsSearching = false;
                    // Reset to first page and reload all documents
                    CurrentPage = 1;
                    _ = LoadDocumentsAsync();
                }
                else
                {
                    IsSearching = true;
                    // Debounce search - wait 300ms before executing
                    _ = Task.Delay(300, cancellationToken).ContinueWith(async _ =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await PerformSearchAsync(value, cancellationToken);
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search initialization error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Search initialization error: {ex.Message}");
            }
        }

        private async Task PerformSearchAsync(string searchText, CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedCollection == null) return;

                // Reset to first page for search
                CurrentPage = 1;
                
                // Load current page of documents and filter client-side
                // Note: For better performance, this should be moved to server-side filtering in LiteDbService
                var skip = (CurrentPage - 1) * PageSize;
                var documents = await _liteDbService.GetDocumentsAsync(SelectedCollection.Name, skip, PageSize * 5); // Load more for search
                
                if (cancellationToken.IsCancellationRequested) return;

                var matchingDocuments = documents
                    .Where(doc => doc.JsonString.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .Take(PageSize)
                    .ToList();

                // Update UI on the UI thread
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Documents.Clear();
                            foreach (var doc in matchingDocuments)
                            {
                                Documents.Add(doc);
                            }
                            StatusMessage = $"Found {Documents.Count} documents matching '{searchText}'";
                        }
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
        }

        [RelayCommand]
        private async Task DiagnoseDatabaseAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Running database diagnostics...";

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
                    var diagnostics = await _liteDbService.DiagnoseDatabaseAsync(file.Path);
                    StatusMessage = "Diagnostics completed - check debug output";
                    
                    // Log to debug output
                    System.Diagnostics.Debug.WriteLine("=== DATABASE DIAGNOSTICS ===");
                    System.Diagnostics.Debug.WriteLine(diagnostics);
                    System.Diagnostics.Debug.WriteLine("============================");
                    
                    // Also display in status (first line only)
                    var firstLine = diagnostics.Split('\n').FirstOrDefault() ?? "Diagnostics completed";
                    StatusMessage = firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine;
                }
                else
                {
                    StatusMessage = "No file selected for diagnostics";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Diagnostics error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"DiagnoseDatabaseAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ShowTroubleshootingInfoAsync()
        {
            try
            {
                var info = await _liteDbService.GetTroubleshootingGuideAsync(new Exception("General troubleshooting"));
                var dialog = new ContentDialog()
                {
                    Title = "Troubleshooting Guide",
                    PrimaryButtonText = "Close",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = _mainWindow?.Content.XamlRoot,
                    Content = new ScrollViewer()
                    {
                        Content = new TextBlock()
                        {
                            Text = info,
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 11
                        }
                    }
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error showing troubleshooting info: {ex.Message}";
            }
        }
    }
}