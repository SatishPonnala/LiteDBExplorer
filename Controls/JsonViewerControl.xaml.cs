using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using System;

namespace LiteDBExplorer.Controls
{
    public sealed partial class JsonViewerControl : UserControl
    {
        public JsonViewerControl()
        {
            this.InitializeComponent();
        }

        public void LoadJson(string json)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl: Loading JSON of length {json?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(json))
                {
                    JsonTreeView.RootNodes.Clear();
                    return;
                }
                
                var token = JToken.Parse(json);
                var rootNode = CreateTreeNode(token, "Document");
                JsonTreeView.RootNodes.Clear();
                JsonTreeView.RootNodes.Add(rootNode);
                
                // Expand the root node by default
                rootNode.IsExpanded = true;
                System.Diagnostics.Debug.WriteLine("JsonViewerControl: JSON loaded successfully");
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl JSON parse error: {ex.Message}");
                // Handle invalid JSON
                JsonTreeView.RootNodes.Clear();
                var errorNode = new TreeViewNode();
                var errorData = new JsonTreeNode 
                { 
                    Key = "JSON Parse Error", 
                    Value = $"Invalid JSON: {ex.Message}",
                    Icon = "\uE783",
                    IconColor = new SolidColorBrush(Colors.Red),
                    ValueColor = new SolidColorBrush(Colors.Red)
                };
                errorNode.Content = errorData;
                JsonTreeView.RootNodes.Add(errorNode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JsonViewerControl general error: {ex.Message}");
                // Handle other errors
                JsonTreeView.RootNodes.Clear();
                var errorNode = new TreeViewNode();
                var errorData = new JsonTreeNode 
                { 
                    Key = "Error", 
                    Value = $"Error loading JSON: {ex.Message}",
                    Icon = "\uE783",
                    IconColor = new SolidColorBrush(Colors.Red),
                    ValueColor = new SolidColorBrush(Colors.Red)
                };
                errorNode.Content = errorData;
                JsonTreeView.RootNodes.Add(errorNode);
            }
        }

        private TreeViewNode CreateTreeNode(JToken token, string key)
        {
            var node = new TreeViewNode();
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
                    nodeData.Icon = "\uE8B7"; // Folder icon
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
                            node.Children.Add(childNode);
                        }
                    }
                    break;

                case JTokenType.Array:
                    nodeData.Icon = "\uE8FD"; // List icon
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
                            node.Children.Add(childNode);
                        }
                    }
                    break;

                case JTokenType.String:
                    nodeData.Icon = "\uE8C8"; // Text icon
                    nodeData.IconColor = orange;
                    var stringValue = token.Value<string>() ?? "";
                    nodeData.Value = $"\"{stringValue}\"";
                    nodeData.ValueColor = green;
                    nodeData.IsEditable = true;
                    break;

                case JTokenType.Integer:
                    nodeData.Icon = "\uE8EF"; // Number icon
                    nodeData.IconColor = purple;
                    nodeData.Value = token.Value<long>().ToString();
                    nodeData.ValueColor = purple;
                    nodeData.IsEditable = true;
                    break;

                case JTokenType.Float:
                    nodeData.Icon = "\uE8EF"; // Number icon
                    nodeData.IconColor = purple;
                    nodeData.Value = token.Value<double>().ToString("F");
                    nodeData.ValueColor = purple;
                    nodeData.IsEditable = true;
                    break;

                case JTokenType.Boolean:
                    nodeData.Icon = "\uE8B3"; // Boolean icon
                    nodeData.IconColor = darkBlue;
                    nodeData.Value = token.Value<bool>().ToString().ToLower();
                    nodeData.ValueColor = darkBlue;
                    nodeData.IsEditable = true;
                    break;

                case JTokenType.Null:
                    nodeData.Icon = "\uE894"; // Null icon
                    nodeData.IconColor = gray;
                    nodeData.Value = "null";
                    nodeData.ValueColor = gray;
                    nodeData.IsEditable = true;
                    break;

                case JTokenType.Date:
                    nodeData.Icon = "\uE8BF"; // Date icon
                    nodeData.IconColor = brown;
                    nodeData.Value = token.Value<DateTime>().ToString("yyyy-MM-dd HH:mm:ss");
                    nodeData.ValueColor = brown;
                    nodeData.IsEditable = true;
                    break;

                default:
                    nodeData.Icon = "\uE946"; // Unknown icon
                    nodeData.IconColor = red;
                    nodeData.Value = token.ToString();
                    nodeData.ValueColor = red;
                    break;
            }

            nodeData.OriginalValue = nodeData.Value;
            node.Content = nodeData;
            return node;
        }

        public void Clear()
        {
            JsonTreeView.RootNodes.Clear();
        }

        // Helper to safely get JsonTreeNode from a TreeViewNode
        public static JsonTreeNode? GetJsonTreeNode(TreeViewNode node)
        {
            return node?.Content as JsonTreeNode;
        }

        private void JsonTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count > 0 && args.AddedItems[0] is TreeViewNode node)
            {
                var nodeData = GetJsonTreeNode(node);
                if (nodeData != null)
                {
                    // Safe to use nodeData here
                    System.Diagnostics.Debug.WriteLine($"Selected node: {nodeData.Key} = {nodeData.Value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Selected node is not a JsonTreeNode.");
                }
            }
        }

        private void JsonTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is JsonTreeNode nodeData)
            {
                System.Diagnostics.Debug.WriteLine($"ItemInvoked: {nodeData.Key} = {nodeData.Value}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ItemInvoked: Not a JsonTreeNode.");
            }
        }

        private void JsonTreeView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (sender is TreeView treeView && treeView.SelectedNode is TreeViewNode node)
            {
                var nodeData = GetJsonTreeNode(node);
                if (nodeData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"DoubleTapped: {nodeData.Key} = {nodeData.Value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DoubleTapped: Not a JsonTreeNode.");
                }
            }
        }
    }
}