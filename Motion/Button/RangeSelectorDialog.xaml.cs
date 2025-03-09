using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Motion.Views
{
    public partial class RangeSelectorDialog : Window
    {
        public string SelectedTimeIntervalStr { get; private set; }
        public bool IsConfirmed { get; private set; }

        public RangeSelectorDialog(IEnumerable<string> values)
        {
            InitializeComponent();

            // 排序并去重
            var sortedValues = values.Distinct().OrderBy(x => x).ToList();

            // 填充下拉框
            TimeIntervalComboBox.ItemsSource = sortedValues;

            // 默认选择第一个和最后一个值
            if (sortedValues.Any())
            {
                TimeIntervalComboBox.SelectedItem = sortedValues.First();
            }
        }

        private void TimeIntervalComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateSelection();
        }

        private void ValidateSelection()
        {
            if (TimeIntervalComboBox.SelectedItem == null)
                return;

            string timeIntervalStr = (string)TimeIntervalComboBox.SelectedItem;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (TimeIntervalComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid time interval.",
                    "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedTimeIntervalStr = (string)TimeIntervalComboBox.SelectedItem;
            IsConfirmed = true;
            Close();
        }
    }
} 