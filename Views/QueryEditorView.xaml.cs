using LiteDBExplorer.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LiteDBExplorer.Views
{
    public sealed partial class QueryEditorView : UserControl
    {
        public ObservableCollection<LiteDbDocument> QueryResults { get; set; } = new();
        public event EventHandler<string>? QueryExecuted;

        public QueryEditorView()
        {
            this.InitializeComponent();
            ResultsListView.ItemsSource = QueryResults;
        }

        public void SetCollections(IEnumerable<string> collections)
        {
            CollectionComboBox.Items.Clear();
            foreach (var collection in collections)
            {
                CollectionComboBox.Items.Add(collection);
            }
        }

        public void SetQuery(string query)
        {
            QueryTextBox.Text = query;
        }

        public void SetResults(IEnumerable<LiteDbDocument> results)
        {
            QueryResults.Clear();
            foreach (var result in results)
            {
                QueryResults.Add(result);
            }
            
            ResultsCountText.Text = $"{QueryResults.Count} documents";
        }

        private void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            var collection = CollectionComboBox.SelectedItem?.ToString();
            var query = QueryTextBox.Text;

            if (string.IsNullOrEmpty(collection))
            {
                ShowError("Please select a collection.");
                return;
            }

            if (string.IsNullOrEmpty(query))
            {
                ShowError("Please enter a query.");
                return;
            }

            QueryExecuted?.Invoke(this, query);
        }

        private void ShowError(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
        }

        public void ClearResults()
        {
            QueryResults.Clear();
            ResultsCountText.Text = "0 documents";
        }
    }
}