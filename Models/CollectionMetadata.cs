using LiteDB;
using System.ComponentModel;

namespace LiteDBExplorer.Models
{
    public class CollectionMetadata : INotifyPropertyChanged
    {
        private string _name;
        private long _documentCount;
        private long _size;
        private bool _isSelected;

        public CollectionMetadata(string name, long documentCount = 0, long size = 0)
        {
            _name = name;
            _documentCount = documentCount;
            _size = size;
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public long DocumentCount
        {
            get => _documentCount;
            set
            {
                _documentCount = value;
                OnPropertyChanged(nameof(DocumentCount));
            }
        }

        public long Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged(nameof(Size));
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

        public string FormattedSize => FormatFileSize(_size);
        public string FormattedDocumentCount => _documentCount.ToString("N0");

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Name} ({DocumentCount:N0} documents)";
        }
    }
} 