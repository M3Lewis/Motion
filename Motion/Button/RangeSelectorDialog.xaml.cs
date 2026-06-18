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
            Motion.General.LanguageManager.LocalizeWindow(this);

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

            SelectedTimeIntervalStr = (string)TimeIntervalComboBox.SelectedItem;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (TimeIntervalComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    Motion.General.LanguageManager.GetString("Msg.SelectValidInterval", "请选择一个有效的区间数值。"),
                    Motion.General.LanguageManager.GetString("Msg.SelectionError", "选择错误"), 
                    MessageBoxButton.OK, MessageBoxImage.Warning
                );
                return;
            }

            IsConfirmed = true;
            Close();
        }
    }
} 