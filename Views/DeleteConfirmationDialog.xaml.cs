using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LiteDBExplorer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Text;

namespace LiteDBExplorer.Views
{
    public sealed partial class DeleteConfirmationDialog : ContentDialog
    {
        public bool CreateBackup { get; private set; }
        public DeleteType DeleteType { get; private set; }
        public IEnumerable<LiteDbDocument> DocumentsToDelete { get; private set; } = new List<LiteDbDocument>();
        public string CollectionName { get; private set; } = string.Empty;

        public DeleteConfirmationDialog()
        {
            this.InitializeComponent();
            IsPrimaryButtonEnabled = false;
        }

        public static DeleteConfirmationDialog CreateForSingleDocument(LiteDbDocument document, string collectionName)
        {
            var dialog = new DeleteConfirmationDialog();
            dialog.DeleteType = DeleteType.SingleDocument;
            dialog.DocumentsToDelete = new[] { document };
            dialog.CollectionName = collectionName;
            
            dialog.MainMessage.Text = "Are you sure you want to delete this document?";
            
            // Add document details
            var idBlock = new TextBlock 
            { 
                Text = $"Document ID: {document.Id}",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12
            };
            dialog.DetailsPanel.Children.Add(idBlock);
            
            var previewBlock = new TextBlock 
            { 
                Text = $"Content Preview: {(document.JsonString.Length > 100 ? document.JsonString.Substring(0, 100) + "..." : document.JsonString)}",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 11,
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
                TextWrapping = TextWrapping.Wrap
            };
            dialog.DetailsPanel.Children.Add(previewBlock);
            
            return dialog;
        }

        public static DeleteConfirmationDialog CreateForMultipleDocuments(IEnumerable<LiteDbDocument> documents, string collectionName)
        {
            var documentList = documents.ToList();
            var dialog = new DeleteConfirmationDialog();
            dialog.DeleteType = DeleteType.MultipleDocuments;
            dialog.DocumentsToDelete = documentList;
            dialog.CollectionName = collectionName;
            
            dialog.MainMessage.Text = $"Are you sure you want to delete {documentList.Count} documents?";
            
            // Add summary
            var summaryBlock = new TextBlock 
            { 
                Text = $"Collection: {collectionName}",
                FontWeight = FontWeights.SemiBold
            };
            dialog.DetailsPanel.Children.Add(summaryBlock);
            
            var countBlock = new TextBlock 
            { 
                Text = $"Documents to delete: {documentList.Count}",
                FontSize = 14
            };
            dialog.DetailsPanel.Children.Add(countBlock);
            
            // Show first few document IDs
            var maxShow = Math.Min(5, documentList.Count);
            for (int i = 0; i < maxShow; i++)
            {
                var idBlock = new TextBlock 
                { 
                    Text = $"• {documentList[i].Id}",
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 11,
                    Margin = new Thickness(16, 2, 0, 2)
                };
                dialog.DetailsPanel.Children.Add(idBlock);
            }
            
            if (documentList.Count > maxShow)
            {
                var moreBlock = new TextBlock 
                { 
                    Text = $"... and {documentList.Count - maxShow} more",
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    Margin = new Thickness(16, 2, 0, 2)
                };
                dialog.DetailsPanel.Children.Add(moreBlock);
            }
            
            dialog.OptionsPanel.Visibility = Visibility.Visible;
            return dialog;
        }

        public static DeleteConfirmationDialog CreateForAllDocuments(string collectionName, int documentCount)
        {
            var dialog = new DeleteConfirmationDialog();
            dialog.DeleteType = DeleteType.AllDocuments;
            dialog.CollectionName = collectionName;
            
            dialog.MainMessage.Text = $"Are you sure you want to delete ALL documents in this collection?";
            
            var warningBlock = new TextBlock 
            { 
                Text = "?? This will delete EVERY document in the collection!",
                FontWeight = FontWeights.Bold,
                Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as Microsoft.UI.Xaml.Media.Brush,
                FontSize = 14
            };
            dialog.DetailsPanel.Children.Add(warningBlock);
            
            var collectionBlock = new TextBlock 
            { 
                Text = $"Collection: {collectionName}",
                FontWeight = FontWeights.SemiBold
            };
            dialog.DetailsPanel.Children.Add(collectionBlock);
            
            var countBlock = new TextBlock 
            { 
                Text = $"Total documents: {documentCount:N0}",
                FontSize = 14
            };
            dialog.DetailsPanel.Children.Add(countBlock);
            
            dialog.OptionsPanel.Visibility = Visibility.Visible;
            return dialog;
        }

        public static DeleteConfirmationDialog CreateForCollection(string collectionName, int documentCount)
        {
            var dialog = new DeleteConfirmationDialog();
            dialog.DeleteType = DeleteType.Collection;
            dialog.CollectionName = collectionName;
            
            dialog.MainMessage.Text = $"Are you sure you want to delete this entire collection?";
            
            var warningBlock = new TextBlock 
            { 
                Text = "?? This will delete the collection AND all its documents!",
                FontWeight = FontWeights.Bold,
                Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as Microsoft.UI.Xaml.Media.Brush,
                FontSize = 14
            };
            dialog.DetailsPanel.Children.Add(warningBlock);
            
            var collectionBlock = new TextBlock 
            { 
                Text = $"Collection: {collectionName}",
                FontWeight = FontWeights.SemiBold
            };
            dialog.DetailsPanel.Children.Add(collectionBlock);
            
            var countBlock = new TextBlock 
            { 
                Text = $"Documents that will be deleted: {documentCount:N0}",
                FontSize = 14
            };
            dialog.DetailsPanel.Children.Add(countBlock);
            
            dialog.OptionsPanel.Visibility = Visibility.Visible;
            return dialog;
        }

        private void ConfirmationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsPrimaryButtonEnabled = ConfirmationCheckBox.IsChecked == true;
        }

        private void ConfirmationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsPrimaryButtonEnabled = false;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            CreateBackup = CreateBackupCheckBox.IsChecked == true;
        }
    }

    public enum DeleteType
    {
        SingleDocument,
        MultipleDocuments,
        AllDocuments,
        Collection
    }
}