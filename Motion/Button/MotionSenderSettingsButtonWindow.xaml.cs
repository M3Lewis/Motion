using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace Motion.Toolbar
{
    public partial class MotionSenderSettingsWindow : Window
    {
        public delegate void FPSChangedHandler(int fps);
        public event FPSChangedHandler FPSChanged;
        public delegate void GraphTypeChangedHandler(string graphType);
        public event GraphTypeChangedHandler GraphTypeChanged;
        
        public string CurrentGraphType
        {
            get => (string)GraphSelectComboBox.SelectedItem;
            set
            {
                if (GraphSelectComboBox.Items.Contains(value))
                {
                    GraphSelectComboBox.SelectedItem = value;
                }
            }
        }
        public string SelectedGraphStr { get; private set; }
        public bool IsConfirmed { get; private set; }
        public int CurrentFPS
        {
            get => int.TryParse(FPSTextBox.Text, out int fps) ? fps : 60;
            set => FPSTextBox.Text = value.ToString();
        }

        public MotionSenderSettingsWindow(List<string> loadedGraph)
        {
            InitializeComponent();
            GraphSelectComboBox.ItemsSource = loadedGraph;
            if (loadedGraph.Any())
            {
                GraphSelectComboBox.SelectedItem = loadedGraph.First();
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FPSTextBox.Text, out int fps) && fps > 0)
            {
                FPSChanged?.Invoke(fps);
                // 在确认时同时触发图表类型更新
                if (GraphSelectComboBox.SelectedItem != null)
                {
                    GraphTypeChanged?.Invoke((string)GraphSelectComboBox.SelectedItem);
                }
                Close();
                IsConfirmed = true;
            }
            else
            {
                MessageBox.Show("请输入有效的正整数！", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Graph_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateGraphSelection();
        }

        private void ValidateGraphSelection()
        {
            if (GraphSelectComboBox.SelectedItem == null)
                return;

            SelectedGraphStr = (string)GraphSelectComboBox.SelectedItem;
        }
    }
}