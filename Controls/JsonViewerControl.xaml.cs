using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI;
using System;

namespace LiteDBExplorer.Controls
{
    public sealed partial class JsonViewerControl : UserControl
    {
        public ObservableCollection<JsonTreeNode> JsonTreeNodes { get; set; } = new();

        public JsonViewerControl()
        {
            this.InitializeComponent();
            JsonTreeView.ItemsSource = JsonTreeNodes;
        }

        public void LoadJson(string json)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl: Loading JSON of length {json?.Length ?? 0}");
                JsonTreeNodes.Clear();
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                var token = JToken.Parse(json);
                var rootNode = CreateTreeNode(token, "Document");
                JsonTreeNodes.Add(rootNode);
                System.Diagnostics.Debug.WriteLine("JsonViewerControl: JSON loaded successfully");
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl JSON parse error: {ex.Message}");
                JsonTreeNodes.Clear();
                var errorNode = new JsonTreeNode
                {
                    Key = "JSON Parse Error",
                    Value = $"Invalid JSON: {ex.Message}",
                    Icon = "\uE783",
                    IconColor = new SolidColorBrush(Colors.Red),
                    ValueColor = new SolidColorBrush(Colors.Red)
                };
                JsonTreeNodes.Add(errorNode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl general error: {ex.Message}");
                JsonTreeNodes.Clear();
                var errorNode = new JsonTreeNode
                {
                    Key = "Error",
                    Value = $"Error loading JSON: {ex.Message}",
                    Icon = "\uE783",
                    IconColor = new SolidColorBrush(Colors.Red),
                    ValueColor = new SolidColorBrush(Colors.Red)
                };
                JsonTreeNodes.Add(errorNode);
            }
        }

        private JsonTreeNode CreateTreeNode(JToken token, string key)
        {
            var nodeData = new JsonTreeNode { Key = key };
            SolidColorBrush blue = new SolidColorBrush(Colors.DodgerBlue);
            SolidColorBrush gray = new SolidColorBrush(Colors.Gray);
            SolidColorBrush green = new SolidColorBrush(Colors.ForestGreen);
            SolidColorBrush orange = new SolidColorBrush(Colors.Orange);
            SolidColorBrush purple = new SolidColorBrush(Colors.Purple);
            SolidColorBrush darkBlue = new SolidColorBrush(Colors.DarkBlue);
            SolidColorBrush red = new SolidColorBrush(Colors.Crimson);
            SolidColorBrush brown = new SolidColorBrush(Colors.SaddleBrown);

            switch (token.Type)
            {
                case JTokenType.Object:
                    nodeData.Icon = "\uE8B7";
                    nodeData.IconColor = blue;
                    var obj = token as JObject;
                    var objCount = obj?.Properties().Count() ?? 0;
                    nodeData.Value = $"{{ {objCount} field{(objCount != 1 ? "s" : "")} }}";
                    nodeData.ValueColor = gray;
                    if (obj != null)
                    {
                        foreach (var prop in obj.Properties())
                        {
                            var childNode = CreateTreeNode(prop.Value, prop.Name);
                            nodeData.Children.Add(childNode);
                        }
                    }
                    break;
                case JTokenType.Array:
                    nodeData.Icon = "\uE8FD";
                    nodeData.IconColor = green;
                    var array = token as JArray;
                    var arrayCount = array?.Count ?? 0;
                    nodeData.Value = $"[ {arrayCount} item{(arrayCount != 1 ? "s" : "")} ]";
                    nodeData.ValueColor = gray;
                    if (array != null)
                    {
                        for (int i = 0; i < array.Count; i++)
                        {
                            var childNode = CreateTreeNode(array[i], $"[{i}]");
                            nodeData.Children.Add(childNode);
                        }
                    }
                    break;
                case JTokenType.String:
                    nodeData.Icon = "\uE8C8";
                    nodeData.IconColor = orange;
                    var stringValue = token.Value<string>() ?? "";
                    nodeData.Value = $"\"{stringValue}\"";
                    nodeData.ValueColor = green;
                    nodeData.IsEditable = true;
                    break;
                case JTokenType.Integer:
                    nodeData.Icon = "\uE8EF";
                    nodeData.IconColor = purple;
                    nodeData.Value = token.Value<long>().ToString();
                    nodeData.ValueColor = purple;
                    nodeData.IsEditable = true;
                    break;
                case JTokenType.Float:
                    nodeData.Icon = "\uE8EF";
                    nodeData.IconColor = purple;
                    nodeData.Value = token.Value<double>().ToString("F");
                    nodeData.ValueColor = purple;
                    nodeData.IsEditable = true;
                    break;
                case JTokenType.Boolean:
                    nodeData.Icon = "\uE8B3";
                    nodeData.IconColor = darkBlue;
                    nodeData.Value = token.Value<bool>().ToString().ToLower();
                    nodeData.ValueColor = darkBlue;
                    nodeData.IsEditable = true;
                    break;
                case JTokenType.Null:
                    nodeData.Icon = "\uE894";
                    nodeData.IconColor = gray;
                    nodeData.Value = "null";
                    nodeData.ValueColor = gray;
                    nodeData.IsEditable = true;
                    break;
                case JTokenType.Date:
                    nodeData.Icon = "\uE8BF";
                    nodeData.IconColor = brown;
                    nodeData.Value = token.Value<DateTime>().ToString("yyyy-MM-dd HH:mm:ss");
                    nodeData.ValueColor = brown;
                    nodeData.IsEditable = true;
                    break;
                default:
                    nodeData.Icon = "\uE946";
                    nodeData.IconColor = red;
                    nodeData.Value = token.ToString();
                    nodeData.ValueColor = red;
                    break;
            }
            nodeData.OriginalValue = nodeData.Value;
            return nodeData;
        }

        public void Clear()
        {
            try
            {
                JsonTreeNodes.Clear();
                System.Diagnostics.Debug.WriteLine("JsonViewerControl: Cleared tree nodes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl Clear error: {ex.Message}");
            }
        }

        // Helper to safely get JsonTreeNode from a TreeViewNode
        public static JsonTreeNode? GetJsonTreeNode(TreeViewNode node)
        {
            return node?.Content as JsonTreeNode;
        }

        private void JsonTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count > 0 && args.AddedItems[0] is JsonTreeNode nodeData)
            {
                System.Diagnostics.Debug.WriteLine($"Selected node: {nodeData.Key} = {nodeData.Value}");
            }
        }

        private void JsonTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is JsonTreeNode nodeData)
            {
                System.Diagnostics.Debug.WriteLine($"ItemInvoked: {nodeData.Key} = {nodeData.Value}");
            }
        }

        private void JsonTreeView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (JsonTreeView.SelectedItem is JsonTreeNode nodeData)
            {
                System.Diagnostics.Debug.WriteLine($"DoubleTapped: {nodeData.Key} = {nodeData.Value}");
            }
        }
    }
}