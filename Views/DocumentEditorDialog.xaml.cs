using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;

namespace LiteDBExplorer.Views
{
    public sealed partial class DocumentEditorDialog : ContentDialog
    {
        public string JsonContent { get; private set; } = string.Empty;

        public DocumentEditorDialog()
        {
            this.InitializeComponent();
            JsonEditor.TextChanged += JsonEditor_TextChanged;
        }

        public DocumentEditorDialog(string initialJson) : this()
        {
            JsonContent = initialJson;
            JsonEditor.Text = FormatJson(initialJson);
        }

        private void JsonEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateJson();
        }

        private void ValidateJson()
        {
            try
            {
                var text = JsonEditor.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    SetValidationState(false, "Empty JSON");
                    return;
                }

                // Try to parse the JSON
                var obj = JsonConvert.DeserializeObject(text);
                SetValidationState(true, "Valid JSON");
            }
            catch (JsonException ex)
            {
                SetValidationState(false, $"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                SetValidationState(false, $"Error: {ex.Message}");
            }
        }

        private void SetValidationState(bool isValid, string message)
        {
            ValidationText.Text = message;
            
            if (isValid)
            {
                ValidationIcon.Glyph = "\uE930"; // Check mark
                ValidationIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessBrush"];
            }
            else
            {
                ValidationIcon.Glyph = "\uE783"; // Error icon
                ValidationIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ErrorBrush"];
            }
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
                var text = JsonEditor.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    JsonConvert.DeserializeObject(text);
                    JsonContent = text;
                }
                else
                {
                    args.Cancel = true;
                }
            }
            catch (JsonException)
            {
                args.Cancel = true;
            }
        }
    }
} 