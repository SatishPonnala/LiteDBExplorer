using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;

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
                var token = JToken.Parse(json);
                var rootNode = CreateTreeNode(token, "root");
                JsonTreeView.RootNodes.Clear();
                JsonTreeView.RootNodes.Add(rootNode);
            }
            catch
            {
                // Handle invalid JSON
                JsonTreeView.RootNodes.Clear();
            }
        }

        private TreeViewNode CreateTreeNode(JToken token, string key)
        {
            var node = new TreeViewNode();
            var nodeData = new JsonTreeNode { Key = key };

            SolidColorBrush blue = new SolidColorBrush(Colors.Blue);
            SolidColorBrush gray = new SolidColorBrush(Colors.Gray);
            SolidColorBrush green = new SolidColorBrush(Colors.Green);
            SolidColorBrush orange = new SolidColorBrush(Colors.Orange);
            SolidColorBrush purple = new SolidColorBrush(Colors.Purple);
            SolidColorBrush darkBlue = new SolidColorBrush(Colors.DarkBlue);
            SolidColorBrush red = new SolidColorBrush(Colors.Red);
            SolidColorBrush black = new SolidColorBrush(Colors.Black);

            switch (token.Type)
            {
                case JTokenType.Object:
                    nodeData.Icon = "\uE8B7"; // Folder icon
                    nodeData.IconColor = blue;
                    nodeData.Value = "{...}";
                    nodeData.ValueColor = gray;
                    
                    var obj = token as JObject;
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
                    nodeData.Icon = "\uE8F1"; // List icon
                    nodeData.IconColor = green;
                    var array = token as JArray;
                    nodeData.Value = $"[{array?.Count ?? 0} items]";
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
                    nodeData.Icon = "\uE8B4"; // Text icon
                    nodeData.IconColor = orange;
                    nodeData.Value = $"\"{token.Value<string>()}\"";
                    nodeData.ValueColor = green;
                    break;

                case JTokenType.Integer:
                case JTokenType.Float:
                    nodeData.Icon = "\uE710"; // Number icon
                    nodeData.IconColor = purple;
                    nodeData.Value = token.ToString();
                    nodeData.ValueColor = darkBlue;
                    break;

                case JTokenType.Boolean:
                    nodeData.Icon = "\uE930"; // Check mark
                    nodeData.IconColor = token.Value<bool>() ? green : red;
                    nodeData.Value = token.Value<bool>().ToString().ToLower();
                    nodeData.ValueColor = token.Value<bool>() ? green : red;
                    break;

                case JTokenType.Null:
                    nodeData.Icon = "\uE9D9"; // Null icon
                    nodeData.IconColor = gray;
                    nodeData.Value = "null";
                    nodeData.ValueColor = gray;
                    break;

                default:
                    nodeData.Icon = "\uE8A5"; // Document icon
                    nodeData.IconColor = black;
                    nodeData.Value = token.ToString();
                    nodeData.ValueColor = black;
                    break;
            }

            node.Content = nodeData;
            return node;
        }
    }

    public class JsonTreeNode
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public SolidColorBrush IconColor { get; set; }
        public SolidColorBrush ValueColor { get; set; }
    }
}