using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Motion.Views
{
    public partial class RangeSelectorDialog : Window
    {
        public double SelectedMin { get; private set; }
        public double SelectedMax { get; private set; }
        public bool IsConfirmed { get; private set; }

        public RangeSelectorDialog(IEnumerable<double> values)
        {
            InitializeComponent();

            // 排序并去重
            var sortedValues = values.Distinct().OrderBy(x => x).ToList();

            // 填充下拉框
            MinValueComboBox.ItemsSource = sortedValues;
            MaxValueComboBox.ItemsSource = sortedValues;

            // 默认选择第一个和最后一个值
            if (sortedValues.Any())
            {
                MinValueComboBox.SelectedItem = sortedValues.First();
                MaxValueComboBox.SelectedItem = sortedValues.Last();
            }
        }

        private void MinValueComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateSelection();
        }

        private void MaxValueComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateSelection();
        }

        private void ValidateSelection()
        {
            if (MinValueComboBox.SelectedItem == null || MaxValueComboBox.SelectedItem == null)
                return;

            double min = (double)MinValueComboBox.SelectedItem;
            double max = (double)MaxValueComboBox.SelectedItem;

            if (min > max)
            {
                MessageBox.Show("Minimum value cannot be greater than maximum value!", 
                    "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                MinValueComboBox.SelectedItem = max;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (MinValueComboBox.SelectedItem == null || MaxValueComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select both minimum and maximum values.", 
                    "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedMin = (double)MinValueComboBox.SelectedItem;
            SelectedMax = (double)MaxValueComboBox.SelectedItem;
            IsConfirmed = true;
            Close();
        }
    }
} 