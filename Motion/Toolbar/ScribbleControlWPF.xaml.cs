using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Motion.Windows
{
    public partial class ScribbleControlWPF : Window
    {
        private GH_Document _document;
        private string _savedTextContent = "Hello!";
        
        public ScribbleControlWPF(GH_Document document)
        {
            InitializeComponent();
            _document = document;
            
            // 初始化字体列表
            foreach (var font in System.Drawing.FontFamily.Families)
            {
                FontComboBox.Items.Add(font.Name);
            }
            FontComboBox.SelectedItem = "Arial";
            
            // 初始化字体样式和Scribble类型
            FontStyleComboBox.SelectedIndex = 0;
            ScribbleTypeComboBox.SelectedIndex = 0;
            
            // 绑定预览更新事件
            TextContentBox.TextChanged += UpdatePreview;
            MaxCharsBox.TextChanged += UpdatePreview;
            ScribbleTypeComboBox.SelectionChanged += UpdatePreview;
        }

        private void ScribbleTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ScribbleTypeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                bool isTextMode = selectedItem.Tag.ToString() == "text";
                TextContentBox.IsEnabled = isTextMode;
                
                if (!isTextMode)
                {
                    _savedTextContent = TextContentBox.Text;
                    TextContentBox.Text = DateTime.Now.ToString();
                }
                else
                {
                    TextContentBox.Text = _savedTextContent;
                }
            }
            UpdatePreview(null, null);
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            var selectedItem = ScribbleTypeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string text = selectedItem.Tag.ToString() == "text" 
                ? TextContentBox.Text 
                : DateTime.Now.ToString();

            if (int.TryParse(MaxCharsBox.Text, out int maxChars) && maxChars > 0)
            {
                PreviewText.Text = FormatText(text, maxChars);
            }
            else
            {
                PreviewText.Text = text;
            }
        }

        private string FormatText(string text, int maxCharsPerLine)
        {
            // 使用与原组件相同的文本格式化逻辑
            string[] words = text.Split(new[] { "\r\n", "\r", "\n", " " }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            List<List<string>> formattedLines = new List<List<string>>();
            formattedLines.Add(new List<string>());
            int currentLineIndex = 0;
            int currentLineLength = 0;

            foreach (string word in words)
            {
                string wordWithSpace = word + " ";
                if (currentLineLength + wordWithSpace.Length <= maxCharsPerLine && 
                    wordWithSpace.Length < maxCharsPerLine)
                {
                    formattedLines[currentLineIndex].Add(wordWithSpace);
                    currentLineLength += wordWithSpace.Length;
                }
                else
                {
                    formattedLines[currentLineIndex].Add(Environment.NewLine);
                    currentLineIndex++;
                    currentLineLength = wordWithSpace.Length;
                    formattedLines.Add(new List<string>());
                    formattedLines[currentLineIndex].Add(wordWithSpace);
                }
            }

            string result = "";
            foreach (var line in formattedLines)
            {
                foreach (string segment in line)
                {
                    result += segment;
                }
            }
            return result;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var selectedGroups = _document?.SelectedObjects().OfType<GH_Group>().ToList();
                
                if (selectedGroups.Any())
                {
                    foreach (var group in selectedGroups)
                    {
                        CreateAndAddScribble(group);
                    }
                }
                else
                {
                    CreateAndAddScribble(null);
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please create a new grasshopper file!");
            }
        }

        private void CreateAndAddScribble(GH_Group group)
        {
            GH_Scribble scribble = new GH_Scribble();
            
            var selectedItem = ScribbleTypeComboBox.SelectedItem as ComboBoxItem;
            string text = selectedItem?.Tag.ToString() == "text" 
                ? TextContentBox.Text 
                : DateTime.Now.ToString();

            if (int.TryParse(MaxCharsBox.Text, out int maxChars) && maxChars > 0)
            {
                scribble.Text = FormatText(text, maxChars);
            }
            else
            {
                scribble.Text = text;
            }

            // 设置字体
            if (int.TryParse(TextSizeBox.Text, out int fontSize))
            {
                var selectedFontStyle = FontStyleComboBox.SelectedItem as ComboBoxItem;
                int fontStyle = selectedFontStyle != null ? 
                    int.Parse(selectedFontStyle.Tag.ToString()) : 0;

                string fontName = FontComboBox.SelectedItem?.ToString() ?? "Arial";
                
                scribble.Font = new System.Drawing.Font(fontName, fontSize, (System.Drawing.FontStyle)fontStyle);
            }

            _document.AddObject(scribble, false);

            if (group != null)
            {
                var groupBounds = group.Attributes.Bounds;
                scribble.Attributes.Pivot = new PointF(
                    groupBounds.Left + 10,
                    groupBounds.Top + 10
                );
                group.AddObject(scribble.InstanceGuid);
            }
            else
            {
                // 如果没有组，则添加到当前视图中心
                var canvas = Instances.ActiveCanvas;
                if (canvas != null)
                {
                    scribble.Attributes.Pivot = new PointF(
                        (float)canvas.Viewport.MidPoint.X,
                        (float)canvas.Viewport.MidPoint.Y
                    );
                }
                else
                {
                    // 如果无法获取画布，则使用默认位置
                    scribble.Attributes.Pivot = new PointF(0, 0);
                }
            }

            scribble.Attributes.ExpireLayout();
            _document.ScheduleSolution(5);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // 可以选择性地清空或重置输入框
            if (ScribbleTypeComboBox.SelectedItem is ComboBoxItem selectedItem
                && selectedItem.Tag.ToString() == "text")
            {
                TextContentBox.Text = "";
                _savedTextContent = "";
            }
        }
    }
} 