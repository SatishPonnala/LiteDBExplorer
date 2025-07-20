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
            
            // Safely serialize the document to JSON
            try
            {
                _jsonString = JsonConvert.SerializeObject(document, Formatting.Indented);
            }
            catch (Exception)
            {
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
                    _jsonString = JsonConvert.SerializeObject(value, Formatting.Indented);
                }
                catch (Exception)
                {
                    _jsonString = value?.ToString() ?? "{}";
                }
                OnPropertyChanged(nameof(Document));
                OnPropertyChanged(nameof(JsonString));
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
                        
                        // Handle different ID types safely
                        if (idValue.IsObjectId)
                            return idValue.AsObjectId;
                        else if (idValue.IsString)
                            return idValue.AsString;
                        else if (idValue.IsInt32)
                            return idValue.AsInt32;
                        else if (idValue.IsInt64)
                            return idValue.AsInt64;
                        else if (idValue.IsGuid)
                            return idValue.AsGuid;
                        else
                            return idValue.ToString();
                    }
                    
                    // If no _id field exists, return a placeholder
                    return "No ID";
                }
                catch (Exception)
                {
                    // If any error occurs, return a safe fallback
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
                    if (_document?.ContainsKey("_id") == true && _document["_id"].IsObjectId)
                    {
                        return _document["_id"].AsObjectId;
                    }
                    return null;
                }
                catch (Exception)
                {
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