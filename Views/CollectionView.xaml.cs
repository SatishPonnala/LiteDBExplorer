using LiteDBExplorer.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;

namespace LiteDBExplorer.Views
{
    public sealed partial class CollectionView : UserControl
    {
        public ObservableCollection<LiteDbDocument> Documents { get; set; } = new();
        public string CollectionName { get; set; } = string.Empty;
        public event EventHandler<LiteDbDocument>? DocumentSelected;
        public event EventHandler<LiteDbDocument>? DocumentEditRequested;
        public event EventHandler<LiteDbDocument>? DocumentDeleteRequested;
        public event EventHandler? ExportRequested;
        public event EventHandler? ImportRequested;

        public CollectionView()
        {
            this.InitializeComponent();
            DocumentsListView.ItemsSource = Documents;
        }

        public void SetCollection(string collectionName, IEnumerable<LiteDbDocument> documents)
        {
            CollectionName = collectionName;
            CollectionNameText.Text = collectionName;
            
            Documents.Clear();
            foreach (var doc in documents)
            {
                Documents.Add(doc);
            }
            
            DocumentCountText.Text = $"{Documents.Count} documents";
        }

        public void AddDocument(LiteDbDocument document)
        {
            Documents.Add(document);
            DocumentCountText.Text = $"{Documents.Count} documents";
        }

        public void RemoveDocument(LiteDbDocument document)
        {
            Documents.Remove(document);
            DocumentCountText.Text = $"{Documents.Count} documents";
        }

        public void UpdateDocument(LiteDbDocument document)
        {
            var index = Documents.IndexOf(document);
            if (index >= 0)
            {
                Documents[index] = document;
            }
        }

        private void DocumentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentsListView.SelectedItem is LiteDbDocument document)
            {
                DocumentSelected?.Invoke(this, document);
            }
        }

        private void EditDocument_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is LiteDbDocument document)
            {
                DocumentEditRequested?.Invoke(this, document);
            }
        }

        private void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is LiteDbDocument document)
            {
                DocumentDeleteRequested?.Invoke(this, document);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}