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

        public bool IsDatabaseOpen => _database != null;
        public string? CurrentDatabasePath => _currentDatabasePath;

        public async Task<bool> OpenDatabaseAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                _database?.Dispose();
                _database = new LiteDatabase($"Filename={filePath};ReadOnly=false");
                _currentDatabasePath = filePath;
                return true;
            }
            catch (Exception)
            {
                return false;
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
                return new List<CollectionMetadata>();

            var collections = new List<CollectionMetadata>();
            
            foreach (var collectionName in _database.GetCollectionNames())
            {
                var collection = _database.GetCollection(collectionName);
                var documentCount = collection.Count();
                // LiteDB does not provide per-collection size, so set to 0 or estimate if needed
                var size = 0L;
                
                collections.Add(new CollectionMetadata(collectionName, documentCount, size));
            }

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
                        result.Add(new LiteDbDocument(doc));
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing other documents
                        System.Diagnostics.Debug.WriteLine($"Error processing document: {ex.Message}");
                        // Optionally add a placeholder document or skip
                        continue;
                    }
                }
                
                return result;
            }
            catch (Exception)
            {
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

        public void Dispose()
        {
            CloseDatabase();
        }
    }
}