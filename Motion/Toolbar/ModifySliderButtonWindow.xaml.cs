using Grasshopper;
using Motion.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Motion.UI
{
    public partial class ModifySliderWindow : Window
    {
        private List<MotionSlider> _selectedSliders;
        public bool HasSelectedSliders => _selectedSliders?.Any() ?? false;
        public bool HasSingleSliderSelected => _selectedSliders?.Count == 1;

        public ModifySliderWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Initialize(List<MotionSlider> selectedSliders)
        {
            _selectedSliders = selectedSliders;

            if (HasSelectedSliders)
            {
                // 填充替换区间的文本框
                var ranges = _selectedSliders.Select(s => new[] { s.Slider.Minimum, s.Slider.Maximum })
                                          .SelectMany(x => x)
                                          .Distinct()
                                          .OrderBy(x => x);
                ReplaceValues.Text = string.Join(",", ranges);
            }

            if (HasSingleSliderSelected)
            {
                var slider = _selectedSliders.First();
                SplitValues.Text = $"{slider.Slider.Minimum},{slider.Slider.Maximum}";
            }
        }

        private void CreateSliders_Click(object sender, RoutedEventArgs e)
        {
            var values = ParseValues(NewSliderValues.Text);
            if (values == null) return;

            var ranges = AllCombinations.IsChecked == true
                ? GenerateAllRanges(values, NoOverlap.IsChecked == true)
                : GenerateSequentialRanges(values, NoOverlap.IsChecked == true);

            CreateMotionSliders(ranges);
        }

        private void ReplaceRanges_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSliders) return;

            var values = ParseValues(ReplaceValues.Text);
            if (values == null) return;

            var oldRanges = _selectedSliders.Select(s => new[] { s.Slider.Minimum, s.Slider.Maximum })
                                          .SelectMany(x => x)
                                          .Distinct()
                                          .OrderBy(x => x)
                                          .ToList();

            if (values.Count != oldRanges.Count)
            {
                MessageBox.Show("新值的数量必须与原始值的数量相同！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 创建新旧值的映射
            var mapping = oldRanges.Zip(values, (old, @new) => new { Old = old, New = @new })
                                  .ToDictionary(x => x.Old, x => x.New);

            // 更新所有选中的slider
            foreach (var slider in _selectedSliders)
            {
                slider.Slider.Minimum = mapping[slider.Slider.Minimum];
                slider.Slider.Maximum = mapping[slider.Slider.Maximum];
                slider.ExpireSolution(true);
            }
        }

        private void MergeSliders_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSliders) return;

            var minValue = _selectedSliders.Min(s => s.Slider.Minimum);
            var maxValue = _selectedSliders.Max(s => s.Slider.Maximum);

            CreateMotionSliders(new[] { (minValue, maxValue) });
        }

        private void SplitSlider_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSingleSliderSelected) return;

            var values = ParseValues(SplitValues.Text);
            if (values == null) return;

            var slider = _selectedSliders.First();
            if (values.First() != slider.Slider.Minimum || values.Last() != slider.Slider.Maximum)
            {
                MessageBox.Show("拆分值必须以当前slider的最小值开始，以最大值结束！", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ranges = SplitAllCombinations.IsChecked == true
                ? GenerateAllRanges(values, SplitNoOverlap.IsChecked == true)
                : GenerateSequentialRanges(values, SplitNoOverlap.IsChecked == true);

            CreateMotionSliders(ranges);
        }

        private List<decimal> ParseValues(string input)
        {
            try
            {
                var values = input.Split(',')
                                .Select(x => decimal.Parse(x.Trim()))
                                .OrderBy(x => x)
                                .ToList();

                if (values.Count < 2)
                    throw new Exception("至少需要输入两个数值！");

                return values;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输入格式错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private IEnumerable<(decimal min, decimal max)> GenerateSequentialRanges(List<decimal> values, bool noOverlap)
        {
            for (int i = 0; i < values.Count - 1; i++)
            {
                decimal maxValue = values[i + 1];
                if (noOverlap && i < values.Count - 2)
                {
                    maxValue -= 1;
                }
                
                // 确保最小值小于最大值
                if (values[i] < maxValue)
                {
                    // 使用yield return可以实现延迟计算,只有在实际需要值的时候才会计算和返回结果
                    // 这样可以提高性能,避免一次性生成所有结果占用大量内存
                    // 特别是当输入数值很多时,yield return的方式更高效
                    yield return (values[i], maxValue);
                }
            }
        }

        private IEnumerable<(decimal min, decimal max)> GenerateAllRanges(List<decimal> values, bool noOverlap)
        {
            for (int i = 0; i < values.Count; i++)
            {
                for (int j = i + 1; j < values.Count; j++)
                {
                    decimal maxValue = values[j];
                    if (noOverlap)
                    {
                        maxValue -= 1;
                    }
                    yield return (values[i], maxValue);
                }
            }
        }

        private void CreateMotionSliders(IEnumerable<(decimal min, decimal max)> ranges)
        {
            var doc = Instances.ActiveCanvas.Document;

            // 获取当前视图中心点
            var bounds = Instances.ActiveCanvas.Viewport.VisibleRegion;
            var centerX = (bounds.Left + bounds.Right) / 2;
            var centerY = (bounds.Top + bounds.Bottom) / 2;

            // 设置起始位置和间距
            double startX = centerX;
            double startY = centerY;
            const double verticalSpacing = 50;

            // 计算总数以确定起始Y坐标
            int totalSliders = ranges.Count();
            startY -= (totalSliders - 1) * verticalSpacing / 2;

            // 查找现有的 MotionUnionSlider
            var existingUnionSlider = doc.Objects.OfType<MotionUnionSlider>().FirstOrDefault();

            int index = 0;
            foreach (var (min, max) in ranges)
            {
                var slider = new Motion.Animation.MotionSlider();
                doc.AddObject(slider, false);

                // 设置slider的位置
                slider.Attributes.Pivot = new System.Drawing.PointF(
                    (float)startX,
                    (float)(startY + index * verticalSpacing)
                );

                // 设置slider的范围
                slider.Slider.Minimum = min;
                slider.Slider.Maximum = max;
                slider.Slider.Value = min;

                // 如果存在 UnionSlider，建立控制关系
                existingUnionSlider?.AddControlledSlider(slider);

                index++;
            }

            doc.NewSolution(true);
        }
    }
}