using LiteDBExplorer.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace LiteDBExplorer.Views
{
    public sealed partial class DatabaseStatsView : UserControl
    {
        public ObservableCollection<CollectionMetadata> Collections { get; set; } = new();

        public DatabaseStatsView()
        {
            this.InitializeComponent();
            CollectionsListView.ItemsSource = Collections;
        }

        public void UpdateStats(string filePath, bool isOpen, Dictionary<string, object> stats)
        {
            // Update file information
            FilePathText.Text = filePath ?? "Not specified";
            StatusText.Text = isOpen ? "Open" : "Closed";

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                FileSizeText.Text = FormatFileSize(fileInfo.Length);
                LastModifiedText.Text = fileInfo.LastWriteTime.ToString("g");
            }
            else
            {
                FileSizeText.Text = "0 B";
                LastModifiedText.Text = "Unknown";
            }

            // Update database statistics
            if (stats.ContainsKey("TotalCollections"))
                CollectionsCountText.Text = stats["TotalCollections"].ToString();
            
            if (stats.ContainsKey("TotalDocuments"))
                TotalDocumentsText.Text = stats["TotalDocuments"].ToString();
            
            if (stats.ContainsKey("TotalSize"))
                TotalSizeText.Text = FormatFileSize(Convert.ToInt64(stats["TotalSize"]));
            
            if (stats.ContainsKey("Indexes"))
                IndexesText.Text = stats["Indexes"].ToString();
        }

        public void UpdateCollections(IEnumerable<CollectionMetadata> collections)
        {
            Collections.Clear();
            foreach (var collection in collections)
            {
                Collections.Add(collection);
            }
        }

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
    }
}