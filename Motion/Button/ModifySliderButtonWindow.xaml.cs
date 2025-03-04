using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Motion.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Window = System.Windows.Window;
using MessageBox = System.Windows.MessageBox; 


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
                
                // 初始化调整值的文本框
                MinValueAdjustment.Text = "0";
                MaxValueAdjustment.Text = "0";
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
            Close();
        }

        // Add this method to the ModifySliderWindow class
        private void CreateOffsetSlider_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(OffsetFrames.Text, out int offsetFrames))
            {
                MessageBox.Show("请输入有效的偏移帧数值", "输入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedSliders.Count != 1)
            {
                MessageBox.Show("请选择一个Motion Slider", "选择错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MotionSlider selectedSlider = _selectedSliders[0];

            // 获取所选Slider的当前区间
            double currentMin = (double)selectedSlider.Slider.Minimum;
            double currentMax = (double)selectedSlider.Slider.Maximum;

            // 根据偏移量计算新的区间
            double newMin, newMax;

            if (offsetFrames >= 0)
            {
                // 正偏移：新区间在原区间之后
                newMin = currentMax + 1;
                newMax = newMin + offsetFrames - 1;
            }
            else
            {
                // 负偏移：新区间在原区间之前
                newMax = currentMin - 1;
                newMin = newMax + offsetFrames + 1; // +1 是因为负偏移量
            }


            List<decimal> values = new List<decimal> { (decimal)newMin, (decimal)newMax };
            var ranges = GenerateAllRanges(values, NoOverlap.IsChecked == true);

            CreateMotionSliders(ranges);
            // 关闭窗口
            Close();
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

            // 检查是否有负值
            if (mapping.Values.Any(v => v < 0))
            {
                MessageBox.Show("不允许设置负数值，最小值必须大于或等于0！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 更新所有选中的slider
            foreach (var slider in _selectedSliders)
            {
                slider.Slider.Minimum = mapping[slider.Slider.Minimum];
                slider.Slider.Maximum = mapping[slider.Slider.Maximum];
                slider.ExpireSolution(true);
            }

            Close();
        }

        private void AdjustRanges_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSliders) return;

            // 解析最小值和最大值的调整量
            decimal minAdjustment = 0;
            decimal maxAdjustment = 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(MinValueAdjustment.Text))
                    minAdjustment = decimal.Parse(MinValueAdjustment.Text.Trim());

                if (!string.IsNullOrWhiteSpace(MaxValueAdjustment.Text))
                    maxAdjustment = decimal.Parse(MaxValueAdjustment.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输入格式错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 检查调整是否会使任何slider的最小值变为负数
            foreach (var slider in _selectedSliders)
            {
                if (slider.Slider.Minimum + minAdjustment < 0)
                {
                    MessageBox.Show("调整后的最小值不能小于0！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // 更新所有选中的slider
            foreach (var slider in _selectedSliders)
            {
                slider.Slider.Minimum += minAdjustment;
                slider.Slider.Maximum += maxAdjustment;
                
                // 确保最小值始终小于最大值
                if (slider.Slider.Minimum >= slider.Slider.Maximum)
                {
                    MessageBox.Show("调整后的最小值不能大于或等于最大值！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                slider.ExpireSolution(true);
            }

            Close();
        }

        private void MergeSliders_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSliders) return;

            var minValue = _selectedSliders.Min(s => s.Slider.Minimum);
            var maxValue = _selectedSliders.Max(s => s.Slider.Maximum);

            CreateMotionSliders(new[] { (minValue, maxValue) });
            Close();
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
            Close();
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

                // 检查是否有负值
                if (values.Any(v => v < 0))
                    throw new Exception("不允许使用负数值，所有值必须大于或等于0！");

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
                    
                    // 确保最小值小于最大值
                    if (values[i] < maxValue)
                    {
                        yield return (values[i], maxValue);
                    }
                }
            }
        }

        private void CreateMotionSliders(IEnumerable<(decimal min, decimal max)> ranges)
        {
            // 过滤掉任何包含负值的区间
            var validRanges = ranges.Where(r => r.min >= 0 && r.max >= 0).ToList();
            
            if (validRanges.Count == 0)
            {
                MessageBox.Show("没有有效的非负区间可以创建！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
            int totalSliders = validRanges.Count;
            startY -= (totalSliders - 1) * verticalSpacing / 2;

            // 查找现有的 MotionUnionSlider
            var existingUnionSlider = doc.Objects.OfType<MotionUnionSlider>().FirstOrDefault();

            int index = 0;
            foreach (var (min, max) in validRanges)
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
