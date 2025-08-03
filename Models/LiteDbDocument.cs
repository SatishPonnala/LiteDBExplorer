using LiteDB;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace LiteDBExplorer.Models
{
    public class LiteDbDocument : INotifyPropertyChanged
    {
        private BsonDocument _document;
        private string _jsonString;
        private bool _isSelected;

        public LiteDbDocument(BsonDocument document)
        {
            _document = document;
            
            // Safely serialize the document to JSON using LiteDB's built-in serialization
            try
            {
                // Convert BsonDocument to a regular .NET object first, then serialize
                var jsonObject = BsonDocumentToObject(document);
                _jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON serialization cast error: {ex.Message}");
                // Fallback to LiteDB's ToString which is safer
                _jsonString = document?.ToString() ?? "{}";
            }
            catch (JsonSerializationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON serialization error: {ex.Message}");
                // Fallback to LiteDB's ToString which is safer
                _jsonString = document?.ToString() ?? "{}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON serialization error: {ex.Message}");
                // If JSON serialization fails, create a safe fallback
                _jsonString = document?.ToString() ?? "{}";
            }
        }

        public BsonDocument Document
        {
            get => _document;
            set
            {
                _document = value;
                try
                {
                    // Convert BsonDocument to a regular .NET object first, then serialize
                    var jsonObject = BsonDocumentToObject(value);
                    _jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);
                }
                catch (InvalidCastException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Document property JSON cast error: {ex.Message}");
                    _jsonString = value?.ToString() ?? "{}";
                }
                catch (JsonSerializationException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Document property JSON serialization error: {ex.Message}");
                    _jsonString = value?.ToString() ?? "{}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Document property JSON error: {ex.Message}");
                    _jsonString = value?.ToString() ?? "{}";
                }
                OnPropertyChanged(nameof(Document));
                OnPropertyChanged(nameof(JsonString));
            }
        }

        // Helper method to convert BsonDocument to a regular .NET object
        private static object BsonDocumentToObject(BsonDocument bsonDoc)
        {
            if (bsonDoc == null) return new { };

            var result = new System.Collections.Generic.Dictionary<string, object>();
            
            foreach (var kvp in bsonDoc)
            {
                result[kvp.Key] = BsonValueToObject(kvp.Value);
            }
            
            return result;
        }

        // Helper method to convert BsonValue to a regular .NET object
        private static object? BsonValueToObject(BsonValue bsonValue)
        {
            if (bsonValue == null || bsonValue.IsNull)
                return null;

            try
            {
                if (bsonValue.IsString)
                    return bsonValue.AsString;
                else if (bsonValue.IsInt32)
                    return bsonValue.AsInt32;
                else if (bsonValue.IsInt64)
                    return bsonValue.AsInt64;
                else if (bsonValue.IsDouble)
                    return bsonValue.AsDouble;
                else if (bsonValue.IsDecimal)
                    return bsonValue.AsDecimal;
                else if (bsonValue.IsBoolean)
                    return bsonValue.AsBoolean;
                else if (bsonValue.IsDateTime)
                    return bsonValue.AsDateTime;
                else if (bsonValue.IsObjectId)
                    return bsonValue.AsObjectId.ToString();
                else if (bsonValue.IsGuid)
                    return bsonValue.AsGuid;
                else if (bsonValue.IsBinary)
                    return Convert.ToBase64String(bsonValue.AsBinary);
                else if (bsonValue.IsArray)
                {
                    var array = bsonValue.AsArray;
                    var list = new System.Collections.Generic.List<object?>();
                    foreach (var item in array)
                    {
                        list.Add(BsonValueToObject(item));
                    }
                    return list;
                }
                else if (bsonValue.IsDocument)
                {
                    return BsonDocumentToObject(bsonValue.AsDocument);
                }
                else
                {
                    // For any other type, return the string representation
                    return bsonValue.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting BsonValue: {ex.Message}");
                // Return string representation as fallback
                return bsonValue.ToString();
            }
        }

        public string JsonString
        {
            get => _jsonString;
            set
            {
                _jsonString = value;
                OnPropertyChanged(nameof(JsonString));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public object Id 
        { 
            get 
            {
                try
                {
                    if (_document?.ContainsKey("_id") == true)
                    {
                        var idValue = _document["_id"];
                        if (idValue == null)
                            return "Null ID";
                        if (idValue.IsObjectId)
                            return idValue.AsObjectId.ToString(); // Always return string
                        else if (idValue.IsString)
                            return idValue.AsString;
                        else if (idValue.IsInt32)
                            return idValue.AsInt32.ToString();
                        else if (idValue.IsInt64)
                            return idValue.AsInt64.ToString();
                        else if (idValue.IsGuid)
                            return idValue.AsGuid.ToString();
                        else if (idValue.IsDouble)
                            return idValue.AsDouble.ToString();
                        else if (idValue.IsDecimal)
                            return idValue.AsDecimal.ToString();
                        else if (idValue.IsBoolean)
                            return idValue.AsBoolean.ToString();
                        else if (idValue.IsDateTime)
                            return idValue.AsDateTime.ToString("o");
                        else
                            return idValue.ToString();
                    }
                    return "No ID";
                }
                catch (Exception)
                {
                    return "Invalid ID";
                }
            }
        }

        public ObjectId? ObjectId
        {
            get
            {
                try
                {
                    if (_document?.ContainsKey("_id") == true)
                    {
                        var idValue = _document["_id"];
                        if (idValue != null && idValue.IsObjectId)
                        {
                            return idValue.AsObjectId;
                        }
                    }
                    return null;
                }
                catch (InvalidCastException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ObjectId cast error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ObjectId error: {ex.Message}");
                    return null;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return _jsonString;
        }
    }
}