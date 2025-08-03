using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Text;

namespace LiteDBExplorer.Views
{
    public sealed partial class DocumentEditorDialog : ContentDialog
    {
        public string JsonContent { get; private set; } = string.Empty;

        public DocumentEditorDialog()
        {
            this.InitializeComponent();
            WireUpEventHandlers();
        }

        public DocumentEditorDialog(string initialJson) : this()
        {
            JsonContent = initialJson;
            JsonTextBox.Text = FormatJson(initialJson);
            UpdateDocumentInfo();
        }

        private void WireUpEventHandlers()
        {
            JsonTextBox.TextChanged += JsonTextBox_TextChanged;
            JsonTextBox.SelectionChanged += JsonTextBox_SelectionChanged;
            
            FormatJsonButton.Click += FormatJsonButton_Click;
            ValidateJsonButton.Click += ValidateJsonButton_Click;
            ClearButton.Click += ClearButton_Click;
            InsertTemplateButton.Click += InsertTemplateButton_Click;
            InsertTimestampButton.Click += InsertTimestampButton_Click;
            
            PrimaryButtonClick += ContentDialog_PrimaryButtonClick;
        }

        private void JsonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDocumentInfo();
            ValidateJson();
        }

        private void JsonTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCursorPosition();
        }

        private void UpdateDocumentInfo()
        {
            var text = JsonTextBox.Text;
            var byteCount = Encoding.UTF8.GetByteCount(text);
            var charCount = text.Length;
            
            DocumentSizeText.Text = $"{byteCount} bytes";
            CharCountText.Text = $"{charCount} chars";
            
            // Update document ID if it's a new document
            if (string.IsNullOrEmpty(JsonContent))
            {
                DocumentIdText.Text = "New Document";
            }
        }

        private void UpdateCursorPosition()
        {
            var text = JsonTextBox.Text;
            var selectionStart = JsonTextBox.SelectionStart;
            
            if (string.IsNullOrEmpty(text))
            {
                LineCountText.Text = "Line 1, Col 1";
                return;
            }
            
            var lines = text.Substring(0, selectionStart).Split('\n');
            var lineNumber = lines.Length;
            var columnNumber = lines[lines.Length - 1].Length + 1;
            
            LineCountText.Text = $"Line {lineNumber}, Col {columnNumber}";
        }

        private void ValidateJson()
        {
            try
            {
                var text = JsonTextBox.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    SetValidationState(false, "Empty JSON", "Ready to edit");
                    return;
                }

                // Try to parse the JSON
                var obj = JsonConvert.DeserializeObject(text);
                SetValidationState(true, "Valid JSON", "Ready to save");
            }
            catch (JsonException ex)
            {
                SetValidationState(false, "Invalid JSON", ex.Message);
            }
            catch (Exception ex)
            {
                SetValidationState(false, "Error", ex.Message);
            }
        }

        private void SetValidationState(bool isValid, string status, string details)
        {
            ValidationStatusText.Text = status;
            SyntaxStatusText.Text = details;
            
            if (isValid)
            {
                ValidationStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessBrush"];
            }
            else
            {
                ValidationStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ErrorBrush"];
            }
        }

        private void FormatJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = JsonTextBox.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var formatted = FormatJson(text);
                    JsonTextBox.Text = formatted;
                    ValidateJson();
                }
            }
            catch (Exception ex)
            {
                SetValidationState(false, "Format Error", ex.Message);
            }
        }

        private void ValidateJsonButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateJson();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            JsonTextBox.Text = string.Empty;
            ValidateJson();
        }

        private void InsertTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var template = @"{
  ""_id"": """",
  ""name"": ""Sample Document"",
  ""description"": ""This is a sample document"",
  ""created"": """ + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + @""",
  ""updated"": """ + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + @""",
  ""active"": true,
  ""tags"": [""sample"", ""template""],
  ""metadata"": {
    ""version"": 1,
    ""category"": ""sample""
  }
}";
            
            JsonTextBox.Text = template;
            ValidateJson();
        }

        private void InsertTimestampButton_Click(object sender, RoutedEventArgs e)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var insertText = $@"""{timestamp}""";
            
            // Insert at cursor position
            var currentText = JsonTextBox.Text;
            var selectionStart = JsonTextBox.SelectionStart;
            var selectionLength = JsonTextBox.SelectionLength;
            
            var newText = currentText.Substring(0, selectionStart) + 
                         insertText + 
                         currentText.Substring(selectionStart + selectionLength);
            
            JsonTextBox.Text = newText;
            JsonTextBox.SelectionStart = selectionStart + insertText.Length;
            ValidateJson();
        }

        private string FormatJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch (JsonSerializationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"DocumentEditor JSON formatting serialization error: {ex.Message}");
                return json; // Return original if formatting fails
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DocumentEditor JSON formatting error: {ex.Message}");
                return json; // Return original if formatting fails
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                // Validate JSON before allowing save
                var text = JsonTextBox.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    JsonConvert.DeserializeObject(text);
                    JsonContent = text;
                }
                else
                {
                    args.Cancel = true;
                    SetValidationState(false, "Cannot save", "Document cannot be empty");
                }
            }
            catch (JsonException ex)
            {
                args.Cancel = true;
                SetValidationState(false, "Cannot save", ex.Message);
            }
        }
    }
} 