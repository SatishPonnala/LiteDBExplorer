using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace LiteDBExplorer.Controls
{
    public class JsonTreeNode : INotifyPropertyChanged
    {
        private string _key = string.Empty;
        private string _value = string.Empty;
        private string _icon = string.Empty;
        private SolidColorBrush _iconColor = new SolidColorBrush(Microsoft.UI.Colors.Black);
        private SolidColorBrush _valueColor = new SolidColorBrush(Microsoft.UI.Colors.Black);
        private bool _isEditable;
        private string _originalValue = string.Empty;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                OnPropertyChanged(nameof(Key));
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public SolidColorBrush IconColor
        {
            get => _iconColor;
            set
            {
                _iconColor = value;
                OnPropertyChanged(nameof(IconColor));
            }
        }

        public SolidColorBrush ValueColor
        {
            get => _valueColor;
            set
            {
                _valueColor = value;
                OnPropertyChanged(nameof(ValueColor));
            }
        }

        public bool IsEditable
        {
            get => _isEditable;
            set
            {
                _isEditable = value;
                OnPropertyChanged(nameof(IsEditable));
            }
        }

        public string OriginalValue
        {
            get => _originalValue;
            set
            {
                _originalValue = value;
                OnPropertyChanged(nameof(OriginalValue));
            }
        }

        public ObservableCollection<JsonTreeNode> Children { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}