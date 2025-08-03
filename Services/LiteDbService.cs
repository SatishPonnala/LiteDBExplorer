using LiteDB;
using LiteDBExplorer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDBExplorer.Services
{
    public class LiteDbService
    {
        private LiteDatabase? _database;
        private string? _currentDatabasePath;

        private bool _isReadOnly = false;
        public bool IsReadOnly => _isReadOnly;

        public bool IsDatabaseOpen => _database != null;
        public string? CurrentDatabasePath => _currentDatabasePath;

        public async Task<bool> OpenDatabaseAsync(string filePath)
        {
            _isReadOnly = false;
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Database file does not exist: {filePath}");
                    return false;
                }

                // Check file size and basic properties
                var fileInfo = new FileInfo(filePath);
                System.Diagnostics.Debug.WriteLine($"File size: {fileInfo.Length} bytes");
                System.Diagnostics.Debug.WriteLine($"File extension: {fileInfo.Extension}");
                
                if (fileInfo.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("File is empty");
                    throw new InvalidOperationException("Database file is empty");
                }

                // Check if file is accessible
                bool fileIsLocked = false;
                try
                {
                    using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // File is accessible for read/write
                        System.Diagnostics.Debug.WriteLine("File is accessible for read/write");
                    }
                }
                catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process"))
                {
                    System.Diagnostics.Debug.WriteLine("File is locked by another process, will open in read-only mode");
                    fileIsLocked = true;
                }
                catch (Exception fileEx)
                {
                    System.Diagnostics.Debug.WriteLine($"File access error: {fileEx.Message}");
                    throw new InvalidOperationException($"Cannot access database file: {fileEx.Message}", fileEx);
                }

                _database?.Dispose();
                
                // If file is locked, try read-only mode first
                if (fileIsLocked)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Attempting to open locked database with ReadOnly=true");
                        _database = new LiteDatabase($"Filename={filePath};ReadOnly=true");
                        _isReadOnly = true;
                        System.Diagnostics.Debug.WriteLine("Successfully opened locked database in read-only mode");
                    }
                    catch (LiteException liteEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"LiteDB Exception (ReadOnly=true): {liteEx.ErrorCode} - {liteEx.Message}");
                        throw new InvalidOperationException($"Cannot open database in read-only mode. The file may be corrupted or in use by another application. Error {liteEx.ErrorCode}: {liteEx.Message}", liteEx);
                    }
                }
                else
                {
                    // Try different connection string approaches for unlocked files
                    try
                    {
                        // First try with the basic connection string
                        System.Diagnostics.Debug.WriteLine("Attempting to open database with ReadOnly=false");
                        _database = new LiteDatabase($"Filename={filePath};ReadOnly=false");
                        _isReadOnly = false;
                        System.Diagnostics.Debug.WriteLine("Successfully opened database with ReadOnly=false");
                    }
                    catch (LiteException liteEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"LiteDB Exception (ReadOnly=false): {liteEx.ErrorCode} - {liteEx.Message}");
                        
                        // Try read-only mode if write access fails
                        try
                        {
                            _database?.Dispose();
                            System.Diagnostics.Debug.WriteLine("Attempting to open database with ReadOnly=true");
                            _database = new LiteDatabase($"Filename={filePath};ReadOnly=true");
                            _isReadOnly = true;
                            System.Diagnostics.Debug.WriteLine("Successfully opened database in read-only mode");
                        }
                        catch (LiteException readOnlyEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"LiteDB Exception (ReadOnly=true): {readOnlyEx.ErrorCode} - {readOnlyEx.Message}");
                            
                            // Try with specific connection parameters
                            try
                            {
                                _database?.Dispose();
                                System.Diagnostics.Debug.WriteLine("Attempting to open database with shared connection");
                                _database = new LiteDatabase($"Filename={filePath};Connection=shared");
                                _isReadOnly = false;
                                System.Diagnostics.Debug.WriteLine("Successfully opened database with shared connection");
                            }
                            catch (LiteException sharedEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"LiteDB Exception (shared): {sharedEx.ErrorCode} - {sharedEx.Message}");
                                
                                // Try with different connection string format
                                try
                                {
                                    _database?.Dispose();
                                    System.Diagnostics.Debug.WriteLine("Attempting to open database with direct filename");
                                    _database = new LiteDatabase(filePath);
                                    _isReadOnly = false;
                                    System.Diagnostics.Debug.WriteLine("Successfully opened database with direct filename");
                                }
                                catch (LiteException directEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"LiteDB Exception (direct): {directEx.ErrorCode} - {directEx.Message}");
                                    throw new InvalidOperationException($"Cannot open LiteDB database. The file may not be a valid LiteDB database or may be corrupted. Error {liteEx.ErrorCode}: {liteEx.Message}", liteEx);
                                }
                            }
                        }
                    }
                }
                
                _currentDatabasePath = filePath;
                
                // Test if we can actually access the database
                try
                {
                    var testCollection = _database.GetCollectionNames();
                    System.Diagnostics.Debug.WriteLine($"Database opened successfully. Found {testCollection.Count()} collections.");
                    if (_isReadOnly)
                    {
                        System.Diagnostics.Debug.WriteLine("Database opened in READ-ONLY mode due to file being locked by another process");
                    }
                }
                catch (Exception testEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Database test failed: {testEx.Message}");
                    throw new InvalidOperationException("Database opened but cannot be accessed. The file may be corrupted.", testEx);
                }
                
                return true;
            }
            catch (LiteException liteEx)
            {
                System.Diagnostics.Debug.WriteLine($"LiteDB Exception in OpenDatabaseAsync: {liteEx.ErrorCode} - {liteEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {liteEx.StackTrace}");
                throw new InvalidOperationException($"LiteDB Error {liteEx.ErrorCode}: {liteEx.Message}", liteEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General exception in OpenDatabaseAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Error opening database: {ex.Message}", ex);
            }
        }

        public void CloseDatabase()
        {
            _database?.Dispose();
            _database = null;
            _currentDatabasePath = null;
        }

        public async Task<List<CollectionMetadata>> GetCollectionsAsync()
        {
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("GetCollectionsAsync: Database is null");
                return new List<CollectionMetadata>();
            }

            System.Diagnostics.Debug.WriteLine("GetCollectionsAsync: Starting to get collections");
            var collections = new List<CollectionMetadata>();
            
            var collectionNames = _database.GetCollectionNames();
            System.Diagnostics.Debug.WriteLine($"GetCollectionsAsync: Found {collectionNames.Count()} collection names");
            
            foreach (var collectionName in collectionNames)
            {
                System.Diagnostics.Debug.WriteLine($"GetCollectionsAsync: Processing collection '{collectionName}'");
                var collection = _database.GetCollection(collectionName);
                var documentCount = collection.Count();
                System.Diagnostics.Debug.WriteLine($"GetCollectionsAsync: Collection '{collectionName}' has {documentCount} documents");
                // LiteDB does not provide per-collection size, so set to 0 or estimate if needed
                var size = 0L;
                
                var metadata = new CollectionMetadata(collectionName, documentCount, size);
                collections.Add(metadata);
                System.Diagnostics.Debug.WriteLine($"GetCollectionsAsync: Added collection metadata for '{collectionName}'");
            }

            System.Diagnostics.Debug.WriteLine($"GetCollectionsAsync: Returning {collections.Count} collections");
            return collections;
        }

        public async Task<List<LiteDbDocument>> GetDocumentsAsync(string collectionName, int skip = 0, int limit = 100)
        {
            if (_database == null)
                return new List<LiteDbDocument>();

            try
            {
                var collection = _database.GetCollection(collectionName);
                var documents = collection.Find(Query.All(), skip, limit);
                
                var result = new List<LiteDbDocument>();
                
                foreach (var doc in documents)
                {
                    try
                    {
                        var liteDbDoc = new LiteDbDocument(doc);
                        result.Add(liteDbDoc);
                    }
                    catch (InvalidCastException ex)
                    {
                        // Log the error but continue processing other documents
                        System.Diagnostics.Debug.WriteLine($"Document cast error in GetDocumentsAsync: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Document content: {doc?.ToString() ?? "null"}");
                        // Skip this document and continue
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing other documents
                        System.Diagnostics.Debug.WriteLine($"Error processing document in GetDocumentsAsync: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Document content: {doc?.ToString() ?? "null"}");
                        // Skip this document and continue
                        continue;
                    }
                }
                
                return result;
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Collection cast error in GetDocumentsAsync: {ex.Message}");
                return new List<LiteDbDocument>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Collection error in GetDocumentsAsync: {ex.Message}");
                return new List<LiteDbDocument>();
            }
        }

        public async Task<LiteDbDocument?> GetDocumentByIdAsync(string collectionName, ObjectId id)
        {
            if (_database == null)
                return null;

            var collection = _database.GetCollection(collectionName);
            var document = collection.FindById(id);
            
            return document != null ? new LiteDbDocument(document) : null;
        }

        public async Task<bool> InsertDocumentAsync(string collectionName, string jsonDocument)
        {
            if (_database == null)
                return false;

            try
            {
                var document = JsonConvert.DeserializeObject<BsonDocument>(jsonDocument);
                var collection = _database.GetCollection(collectionName);
                collection.Insert(document);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDocumentAsync(string collectionName, ObjectId id, string jsonDocument)
        {
            if (_database == null)
                return false;

            try
            {
                var document = JsonConvert.DeserializeObject<BsonDocument>(jsonDocument);
                var collection = _database.GetCollection(collectionName);
                document["_id"] = id;
                return collection.Update(document);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDocumentByIdAsync(string collectionName, object documentId, string jsonDocument)
        {
            if (_database == null)
                return false;

            try
            {
                var document = JsonConvert.DeserializeObject<BsonDocument>(jsonDocument);
                if (document == null) return false;
                
                var collection = _database.GetCollection(collectionName);
                
                // Handle different ID types
                if (documentId is ObjectId objectId)
                {
                    document["_id"] = objectId;
                    return collection.Update(document);
                }
                else if (documentId is string stringId)
                {
                    try
                    {
                        var parsedObjectId = new ObjectId(stringId);
                        document["_id"] = parsedObjectId;
                        return collection.Update(document);
                    }
                    catch
                    {
                        // For non-ObjectId string types, try to update by finding the document first
                        var existingDoc = collection.FindOne(Query.EQ("_id", new BsonValue(stringId)));
                        if (existingDoc != null)
                        {
                            document["_id"] = existingDoc["_id"];
                            return collection.Update(document);
                        }
                    }
                }
                else
                {
                    // For non-ObjectId types, try to update by finding the document first
                    var existingDoc = collection.FindOne(Query.EQ("_id", new BsonValue(documentId)));
                    if (existingDoc != null)
                    {
                        document["_id"] = existingDoc["_id"];
                        return collection.Update(document);
                    }
                }
                
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string collectionName, object documentId)
        {
            if (_database == null)
                return false;

            try
            {
                var collection = _database.GetCollection(collectionName);
                
                // Handle different ID types
                if (documentId is ObjectId objectId)
                {
                    return collection.Delete(objectId);
                }
                else if (documentId is string stringId)
                {
                    // Try to parse as ObjectId first
                    try
                    {
                        var parsedObjectId = new ObjectId(stringId);
                        return collection.Delete(parsedObjectId);
                    }
                    catch
                    {
                        // Delete by string ID
                        return collection.Delete(new BsonValue(stringId));
                    }
                }
                else if (documentId is int intId)
                {
                    return collection.Delete(new BsonValue(intId));
                }
                else if (documentId is long longId)
                {
                    return collection.Delete(new BsonValue(longId));
                }
                else
                {
                    // Try to convert to BsonValue
                    return collection.Delete(new BsonValue(documentId));
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteMultipleDocumentsAsync(string collectionName, IEnumerable<object> documentIds)
        {
            if (_database == null)
                return false;

            try
            {
                var collection = _database.GetCollection(collectionName);
                bool allSucceeded = true;
                
                foreach (var documentId in documentIds)
                {
                    bool result = false;
                    
                    // Handle different ID types
                    if (documentId is ObjectId objectId)
                    {
                        result = collection.Delete(objectId);
                    }
                    else if (documentId is string stringId)
                    {
                        try
                        {
                            var parsedObjectId = new ObjectId(stringId);
                            result = collection.Delete(parsedObjectId);
                        }
                        catch
                        {
                            result = collection.Delete(new BsonValue(stringId));
                        }
                    }
                    else if (documentId is int intId)
                    {
                        result = collection.Delete(new BsonValue(intId));
                    }
                    else if (documentId is long longId)
                    {
                        result = collection.Delete(new BsonValue(longId));
                    }
                    else
                    {
                        result = collection.Delete(new BsonValue(documentId));
                    }
                    
                    if (!result)
                    {
                        allSucceeded = false;
                        System.Diagnostics.Debug.WriteLine($"Failed to delete document with ID: {documentId}");
                    }
                }
                
                return allSucceeded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteMultipleDocumentsAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAllDocumentsInCollectionAsync(string collectionName)
        {
            if (_database == null)
                return false;

            try
            {
                var collection = _database.GetCollection(collectionName);
                var allDocuments = collection.FindAll();
                
                foreach (var doc in allDocuments)
                {
                    collection.Delete(doc["_id"]);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteAllDocumentsInCollectionAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<BsonDocument>> ExecuteQueryAsync(string collectionName, string query)
        {
            if (_database == null)
                return new List<BsonDocument>();

            try
            {
                var collection = _database.GetCollection(collectionName);
                // For now, we'll support basic queries
                // In a full implementation, you'd want to parse the query string
                // and convert it to appropriate LiteDB queries
                return collection.Find(Query.All()).ToList();
            }
            catch (Exception)
            {
                return new List<BsonDocument>();
            }
        }

        public async Task<string> ExportCollectionToJsonAsync(string collectionName)
        {
            if (_database == null)
                return string.Empty;

            try
            {
                var collection = _database.GetCollection(collectionName);
                var documents = collection.Find(Query.All()).ToList();
                return JsonConvert.SerializeObject(documents, Formatting.Indented);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public async Task<bool> ImportCollectionFromJsonAsync(string collectionName, string jsonData)
        {
            if (_database == null)
                return false;

            try
            {
                var documents = JsonConvert.DeserializeObject<List<BsonDocument>>(jsonData);
                var collection = _database.GetCollection(collectionName);
                
                foreach (var document in documents)
                {
                    collection.Insert(document);
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetDatabaseStatsAsync()
        {
            if (_database == null)
                return new Dictionary<string, object>();

            try
            {
                var stats = new Dictionary<string, object>();
                
                if (!string.IsNullOrEmpty(_currentDatabasePath))
                {
                    var fileInfo = new FileInfo(_currentDatabasePath);
                    stats["FileSize"] = fileInfo.Length;
                    stats["LastModified"] = fileInfo.LastWriteTime;
                }

                var collections = await GetCollectionsAsync();
                stats["TotalCollections"] = collections.Count;
                stats["TotalDocuments"] = collections.Sum(c => c.DocumentCount);
                stats["TotalSize"] = collections.Sum(c => c.Size);

                return stats;
            }
            catch (Exception)
            {
                return new Dictionary<string, object>();
            }
        }

        public async Task<int> GetDocumentCountAsync(string collectionName)
        {
            if (_database == null)
                return 0;

            try
            {
                var collection = _database.GetCollection(collectionName);
                return collection.Count();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<bool> DocumentExistsAsync(string collectionName, object documentId)
        {
            if (_database == null)
                return false;

            try
            {
                var collection = _database.GetCollection(collectionName);
                
                if (documentId is ObjectId objectId)
                {
                    return collection.FindById(objectId) != null;
                }
                else if (documentId is string stringId)
                {
                    try
                    {
                        var parsedObjectId = new ObjectId(stringId);
                        return collection.FindById(parsedObjectId) != null;
                    }
                    catch
                    {
                        return collection.FindOne(Query.EQ("_id", new BsonValue(stringId))) != null;
                    }
                }
                else
                {
                    return collection.FindOne(Query.EQ("_id", new BsonValue(documentId))) != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateCollectionAsync(string collectionName)
        {
            if (_database == null)
                return false;

            try
            {
                var collection = _database.GetCollection(collectionName);
                // Just accessing the collection creates it if it doesn't exist
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteCollectionAsync(string collectionName)
        {
            if (_database == null)
                return false;

            try
            {
                return _database.DropCollection(collectionName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            CloseDatabase();
        }

        public async Task<string> DiagnoseDatabaseAsync(string filePath)
        {
            var diagnostics = new List<string>();
            
            try
            {
                diagnostics.Add($"Database file path: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    diagnostics.Add("? File does not exist");
                    return string.Join("\n", diagnostics);
                }
                
                var fileInfo = new FileInfo(filePath);
                diagnostics.Add($"? File exists - Size: {fileInfo.Length} bytes");
                diagnostics.Add($"? Last modified: {fileInfo.LastWriteTime}");
                
                // Check file permissions
                try
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        diagnostics.Add("? File has read/write access");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    diagnostics.Add("?? File is read-only or access denied");
                    try
                    {
                        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            diagnostics.Add("? File has read access");
                        }
                    }
                    catch
                    {
                        diagnostics.Add("? No file access permissions");
                    }
                }
                catch (IOException ioEx)
                {
                    diagnostics.Add($"?? File I/O issue: {ioEx.Message}");
                }
                
                // Check if it's a valid LiteDB file by reading the header
                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        if (stream.Length < 8192) // LiteDB minimum file size
                        {
                            diagnostics.Add($"?? File too small for LiteDB ({stream.Length} bytes < 8192 bytes)");
                        }
                        else
                        {
                            var buffer = new byte[32];
                            await stream.ReadAsync(buffer, 0, 32);
                            
                            // Check for LiteDB magic number (first 4 bytes should be specific values)
                            var header = System.Text.Encoding.ASCII.GetString(buffer, 0, 8);
                            diagnostics.Add($"? File header: {BitConverter.ToString(buffer, 0, 8)}");
                        }
                    }
                }
                catch (Exception headerEx)
                {
                    diagnostics.Add($"? Cannot read file header: {headerEx.Message}");
                }
                
                // Try to open with LiteDB
                try
                {
                    using (var testDb = new LiteDatabase($"Filename={filePath};ReadOnly=true"))
                    {
                        var collections = testDb.GetCollectionNames().ToList();
                        diagnostics.Add($"? Valid LiteDB file with {collections.Count} collections");
                        if (collections.Any())
                        {
                            diagnostics.Add($"   Collections: {string.Join(", ", collections.Take(5))}");
                        }
                    }
                }
                catch (LiteException liteEx)
                {
                    diagnostics.Add($"? LiteDB Error {liteEx.ErrorCode}: {liteEx.Message}");
                    
                    // Provide specific guidance based on error code
                    switch (liteEx.ErrorCode)
                    {
                        case 103: // Invalid datafile format
                            diagnostics.Add("   ? This file is not a valid LiteDB database or is corrupted");
                            break;
                        case 104: // Wrong password
                            diagnostics.Add("   ? This database appears to be password protected");
                            break;
                        case 105: // Database is locked
                            diagnostics.Add("   ? Database is locked by another process");
                            break;
                        case 106: // Version not supported
                            diagnostics.Add("   ? Database version is not supported by this LiteDB version");
                            break;
                        default:
                            diagnostics.Add($"   ? See LiteDB documentation for error code {liteEx.ErrorCode}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"? General error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add($"? Diagnosis failed: {ex.Message}");
            }
            
            return string.Join("\n", diagnostics);
        }

        private async Task<bool> ValidateDatabaseFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fileInfo = new FileInfo(filePath);
                
                // Check minimum file size for LiteDB
                if (fileInfo.Length < 8192)
                {
                    System.Diagnostics.Debug.WriteLine($"File too small: {fileInfo.Length} bytes");
                    return false;
                }

                // Try to read file header to check if it's a valid LiteDB file
                using (var stream = File.OpenRead(filePath))
                {
                    var buffer = new byte[32];
                    await stream.ReadAsync(buffer, 0, 32);
                    
                    // Basic validation - just check if we can read the header
                    // LiteDB files typically start with specific byte patterns
                    return true; // If we can read it, assume it might be valid
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File validation error: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetLiteDbVersionInfoAsync()
        {
            var info = new Dictionary<string, string>();
            
            try
            {
                // Get LiteDB assembly version
                var assembly = typeof(LiteDatabase).Assembly;
                var version = assembly.GetName().Version;
                info["LiteDB Version"] = version?.ToString() ?? "Unknown";
                info["Assembly Location"] = assembly.Location;
                
                // Get runtime information
                info["Runtime Version"] = Environment.Version.ToString();
                info["OS Version"] = Environment.OSVersion.ToString();
                info["64-bit Process"] = Environment.Is64BitProcess.ToString();
                
                return info;
            }
            catch (Exception ex)
            {
                info["Error"] = ex.Message;
                return info;
            }
        }

        public async Task<string> GetTroubleshootingGuideAsync(Exception exception)
        {
            var guide = new List<string>();
            
            guide.Add("=== LITEDB TROUBLESHOOTING GUIDE ===");
            guide.Add($"Exception: {exception.GetType().Name}");
            guide.Add($"Message: {exception.Message}");
            guide.Add("");
            
            if (exception is LiteException liteEx)
            {
                guide.Add($"LiteDB Error Code: {liteEx.ErrorCode}");
                guide.Add("");
                
                switch (liteEx.ErrorCode)
                {
                    case 103: // Invalid datafile format
                        guide.Add("SOLUTION for Error 103 (Invalid datafile format):");
                        guide.Add("� The file is not a valid LiteDB database");
                        guide.Add("� The file may be corrupted");
                        guide.Add("� Try opening the file with a hex editor to check if it contains database content");
                        guide.Add("� If it's a text file, it might be exported JSON data instead of a database");
                        break;
                        
                    case 104: // Wrong password
                        guide.Add("SOLUTION for Error 104 (Wrong password):");
                        guide.Add("� This database is password protected");
                        guide.Add("� You need to provide the correct password in the connection string");
                        guide.Add("� Example: Filename=database.db;Password=yourpassword");
                        break;
                        
                    case 105: // Database is locked
                        guide.Add("SOLUTION for Error 105 (Database is locked):");
                        guide.Add("� Another process is using the database");
                        guide.Add("� Close any other applications that might be using the file");
                        guide.Add("� Try restarting this application");
                        guide.Add("� Check for zombie processes in Task Manager");
                        break;
                        
                    case 106: // Version not supported
                        guide.Add("SOLUTION for Error 106 (Version not supported):");
                        guide.Add("� Database was created with a different LiteDB version");
                        guide.Add($"� Current LiteDB version: {typeof(LiteDatabase).Assembly.GetName().Version}");
                        guide.Add("� Try updating LiteDB to the latest version");
                        guide.Add("� Or use the version that created the database");
                        break;
                        
                    case 200: // Index not found
                        guide.Add("SOLUTION for Error 200 (Index not found):");
                        guide.Add("� Database corruption in index structures");
                        guide.Add("� Try to rebuild the database");
                        break;
                        
                    default:
                        guide.Add($"GENERAL SOLUTION for Error {liteEx.ErrorCode}:");
                        guide.Add("� Check LiteDB documentation for this specific error code");
                        guide.Add("� Verify file permissions and access rights");
                        guide.Add("� Try opening in read-only mode first");
                        guide.Add("� Check if the file is actually a LiteDB database");
                        break;
                }
            }
            else if (exception is UnauthorizedAccessException)
            {
                guide.Add("SOLUTION for Access Denied:");
                guide.Add("� Run the application as Administrator");
                guide.Add("� Check file permissions");
                guide.Add("� Make sure the file is not read-only");
                guide.Add("� Verify the file is not in use by another process");
            }
            else if (exception is FileNotFoundException)
            {
                guide.Add("SOLUTION for File Not Found:");
                guide.Add("� Verify the file path is correct");
                guide.Add("� Check if the file extension is correct (.db)");
                guide.Add("� Make sure the file hasn't been moved or deleted");
            }
            else if (exception is IOException)
            {
                guide.Add("SOLUTION for I/O Error:");
                guide.Add("� Check available disk space");
                guide.Add("� Verify network connectivity if file is on network drive");
                guide.Add("� Check if the storage device is functioning properly");
                guide.Add("� Try copying the file to local drive first");
            }
            else
            {
                guide.Add("GENERAL TROUBLESHOOTING STEPS:");
                guide.Add("� Verify the file is a valid LiteDB database");
                guide.Add("� Check file permissions and access rights");
                guide.Add("� Try opening in read-only mode");
                guide.Add("� Restart the application");
                guide.Add("� Check Windows Event Viewer for system errors");
            }
            
            guide.Add("");
            guide.Add("ADDITIONAL DEBUGGING STEPS:");
            guide.Add("� Use the 'Diagnose Database' feature for detailed file analysis");
            guide.Add("� Check the Debug Output window for detailed error information");
            guide.Add("� Try creating a new test database to verify LiteDB is working");
            guide.Add("");
            
            // Add version information
            var versionInfo = await GetLiteDbVersionInfoAsync();
            guide.Add("SYSTEM INFORMATION:");
            foreach (var kvp in versionInfo)
            {
                guide.Add($"� {kvp.Key}: {kvp.Value}");
            }
            
            return string.Join("\n", guide);
        }
    }
}