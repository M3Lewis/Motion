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
using Grasshopper.Kernel.Special;


namespace Motion.UI
{
    public partial class ModifySliderWindow : Window
    {
        private List<MotionSender> _selectedSenders;
        public bool HasSelectedSenders => _selectedSenders?.Any() ?? false;
        public bool HasSingleSenderSelected => _selectedSenders?.Count == 1;


        public ModifySliderWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Initialize(List<MotionSender> selectedSenders)
        {
            _selectedSenders = selectedSenders;

            if (HasSelectedSenders)
            {
                // 填充替换区间的文本框
                var ranges = _selectedSenders.
                    Select(s => new[] { s.NickName.Split('-')[0], s.NickName.Split('-')[1] })
                                          .SelectMany(x => x)
                                          .Distinct()
                                          .OrderBy(x => x);
                ReplaceValues.Text = string.Join(",", ranges);

                // 初始化调整值的文本框
                MinValueAdjustment.Text = "0";
                MaxValueAdjustment.Text = "0";
            }

            if (HasSingleSenderSelected)
            {
                var sender = _selectedSenders.First();
                SplitValues.Text = $"{sender.NickName.Split('-')[0]},{sender.NickName.Split('-')[1]}";
            }
        }

        private void CreateSenders_Click(object sender, RoutedEventArgs e)
        {
            var values = ParseValues(NewSliderValues.Text);
            if (values == null) return;

            var ranges = AllCombinations.IsChecked == true
                ? GenerateAllRanges(values, NoOverlap.IsChecked == true)
                : GenerateSequentialRanges(values, NoOverlap.IsChecked == true);

            GenerateMotionSenders(ranges);
            Close();
        }

        // Add this method to the ModifySliderWindow class
        private void CreateOffsetSender_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(OffsetFrames.Text, out int offsetFrames))
            {
                MessageBox.Show("请输入有效的偏移帧数值", "输入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedSenders.Count != 1)
            {
                MessageBox.Show("请选择一个Motion Slider", "选择错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MotionSender selectedSlider = _selectedSenders[0];

            // 获取所选Slider的当前区间
            double currentMin = double.Parse(selectedSlider.NickName.Split('-')[0]);
            double currentMax = double.Parse(selectedSlider.NickName.Split('-')[1]);

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

            GenerateMotionSenders(ranges);
            // 关闭窗口
            Close();
        }

        private void ReplaceRanges_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            var values = ParseValues(ReplaceValues.Text);
            if (values == null) return;

            var oldRanges = _selectedSenders.
                Select(s => new[] { s.NickName.Split('-')[0], s.NickName.Split('-')[1] })
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

            // 更新所有选中的sender
            foreach (var selectedSender in _selectedSenders)
            {
                selectedSender.NickName = $"{mapping[selectedSender.NickName.Split('-')[0]]}-{mapping[selectedSender.NickName.Split('-')[1]]}";
                selectedSender.ExpireSolution(true);
            }

            Close();
        }

        private void AdjustRangesExisting_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            // 解析最小值和最大值的调整量
            decimal minAdjustment = 0;
            decimal maxAdjustment = 0;
            int loopCount = 1; // 默认不循环

            try
            {
                if (!string.IsNullOrWhiteSpace(MinValueAdjustment.Text))
                    minAdjustment = decimal.Parse(MinValueAdjustment.Text.Trim());

                if (!string.IsNullOrWhiteSpace(MaxValueAdjustment.Text))
                    maxAdjustment = decimal.Parse(MaxValueAdjustment.Text.Trim());

                // 尝试解析循环次数，默认为1（不循环）
                if (!string.IsNullOrWhiteSpace(LoopCount.Text))
                    loopCount = int.Parse(LoopCount.Text.Trim());

                if (loopCount < 1)
                {
                    MessageBox.Show("循环次数必须至少为1！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输入格式错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            decimal finalMin = 0;
            decimal finalMax = 0;
            // 检查调整是否会使任何slider的最小值变为负数
            foreach (var selectedSender in _selectedSenders)
            {
                finalMin = decimal.Parse(selectedSender.NickName.Split('-')[0]);
                finalMax = decimal.Parse(selectedSender.NickName.Split('-')[1]);
                for (int i = 0; i < loopCount; i++)
                {
                    finalMin += minAdjustment;
                    finalMax += maxAdjustment;

                    if (finalMin < 0)
                    {
                        MessageBox.Show("调整后的最小值不能小于0！", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (finalMin >= finalMax)
                    {
                        MessageBox.Show("调整后的最小值不能大于或等于最大值！", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            // 更新所有选中的slider
            foreach (var selectedSender in _selectedSenders)
            {
                decimal currentMin = finalMin;
                decimal currentMax = finalMax;

                for (int i = 0; i < loopCount; i++)
                {
                    currentMin += minAdjustment;
                    currentMax += maxAdjustment;
                }
                selectedSender.NickName = $"{currentMin.ToString()}-{currentMax.ToString()}";

                selectedSender.ExpireSolution(true);
            }

            Close();
        }

        private void AdjustRangesNew_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            // 解析最小值和最大值的调整量
            decimal minAdjustment = 0;
            decimal maxAdjustment = 0;
            int loopCount = 1; // 默认不循环

            try
            {
                if (!string.IsNullOrWhiteSpace(MinValueAdjustment.Text))
                    minAdjustment = decimal.Parse(MinValueAdjustment.Text.Trim());

                if (!string.IsNullOrWhiteSpace(MaxValueAdjustment.Text))
                    maxAdjustment = decimal.Parse(MaxValueAdjustment.Text.Trim());

                // 尝试解析循环次数，默认为1（不循环）
                if (!string.IsNullOrWhiteSpace(LoopCount.Text))
                    loopCount = int.Parse(LoopCount.Text.Trim());

                if (loopCount < 1)
                {
                    MessageBox.Show("循环次数必须至少为1！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输入格式错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 为每个选定的滑块创建新的滑块
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
            int totalSliders = _selectedSenders.Count * loopCount;
            startY -= (totalSliders - 1) * verticalSpacing / 2;

            int index = 0;
            foreach (var originalSender in _selectedSenders)
            {
                decimal currentMin = decimal.Parse(originalSender.NickName.Split('-')[0]);
                decimal currentMax = decimal.Parse(originalSender.NickName.Split('-')[1]);

                for (int i = 0; i < loopCount; i++)
                {
                    currentMin += minAdjustment;
                    currentMax += maxAdjustment;

                    // 检查是否有负值
                    if (currentMin < 0 || currentMax < 0)
                    {
                        MessageBox.Show("不允许设置负数值，最小值和最大值必须大于或等于0！", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 确保最小值小于最大值
                    if (currentMin >= currentMax)
                    {
                        MessageBox.Show("调整后的最小值不能大于或等于最大值！", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var range = GenerateAllRanges(new List<decimal> { currentMin, currentMax }, NoOverlap.IsChecked == true);
                    GenerateMotionSenders(range);

                    index++;
                }
            }

            Close();
        }

        private void MergeSenders_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            var minValue = _selectedSenders.Min(s => int.Parse(s.NickName.Split('-')[0]));
            var maxValue = _selectedSenders.Max(s => int.Parse(s.NickName.Split('-')[1]));

            CreateMotionSenders(new[] { (minValue, maxValue) });
            Close();
        }

        private void CreateMotionSenders(IEnumerable<(int min, int max)> ranges)
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


            int index = 0;
            foreach (var (min, max) in validRanges)
            {
                var motionSender = new Motion.Animation.MotionSender();
                doc.AddObject(motionSender, false);

                // 设置slider的位置
                motionSender.Attributes.Pivot = new System.Drawing.PointF(
                    (float)startX,
                    (float)(startY + index * verticalSpacing)
                );

                // 设置slider的位置
                motionSender.Attributes.Pivot = new System.Drawing.PointF(
                    (float)startX,
                    (float)(startY + index * verticalSpacing)
                );

                // 设置slider的范围
                motionSender.NickName = $"{min.ToString()}-{max.ToString()}";

                index++;
            }

            doc.NewSolution(true);
        }
        private void SplitSender_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSingleSenderSelected) return;

            var values = ParseValues(SplitValues.Text);
            if (values == null) return;

            var slider = _selectedSenders.First();
            if (values.First() != decimal.Parse(slider.NickName.Split('-')[0])
                || values.Last() != decimal.Parse(slider.NickName.Split('-')[1]))
            {
                MessageBox.Show("拆分值必须以当前slider的最小值开始，以最大值结束！", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ranges = SplitAllCombinations.IsChecked == true
                ? GenerateAllRanges(values, SplitNoOverlap.IsChecked == true)
                : GenerateSequentialRanges(values, SplitNoOverlap.IsChecked == true);

            GenerateMotionSenders(ranges);
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

        private void GenerateMotionSenders(IEnumerable<(decimal min, decimal max)> ranges)
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
            int totalSenders = validRanges.Count;
            startY -= (totalSenders - 1) * verticalSpacing / 2;

            int index = 0;
            foreach (var (min, max) in validRanges)
            {
                var motionSender = new MotionSender();
                doc.AddObject(motionSender, false);

                // 设置slider的位置
                motionSender.Attributes.Pivot = new System.Drawing.PointF(
                    (float)startX,
                    (float)(startY + index * verticalSpacing)
                );

                // 设置slider的范围
                motionSender.NickName = $"{min.ToString()}-{max.ToString()}";

                var motionSlider = doc.Objects
                  .Where(o => o.GetType().ToString() == "Motion.Animation.MotionSlider")
                  .Cast<GH_NumberSlider>()
                  .FirstOrDefault();

                motionSender.AddSource(motionSlider);

                index++;
            }
        }
    }
}
