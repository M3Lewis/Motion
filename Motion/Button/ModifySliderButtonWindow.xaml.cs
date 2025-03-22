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
using System.Drawing;


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
                // 填充替换区间的文本框，确保数字按照正确顺序排序
                var ranges = _selectedSenders
                    .Select(s => new[] { s.NickName.Split('-')[0], s.NickName.Split('-')[1] })
                    .SelectMany(x => x)
                    .Distinct()
                    .Select(x => int.Parse(x))  // 转换为数字进行排序
                    .OrderBy(x => x)            // 按数字大小排序
                    .Select(x => x.ToString()); // 转回字符串
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
            int currentMin = int.Parse(selectedSlider.NickName.Split('-')[0]);
            int currentMax = int.Parse(selectedSlider.NickName.Split('-')[1]);

            // 根据偏移量计算新的区间
            int newMin, newMax;

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


            List<int> values = new List<int> { newMin, newMax };
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
                string newNickname = $"{mapping[selectedSender.NickName.Split('-')[0]]}-{mapping[selectedSender.NickName.Split('-')[1]]}";
                selectedSender.SetNicknameWithUndo(newNickname);
                selectedSender.ExpireSolution(true);
            }

            Close();
        }

        private void InsertRange_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            try
            {
                // 解析输入文本
                var parts = ReplaceValues.Text.Split(',')
                                                .Select(x => x.Trim())
                                                .ToList();

                // 查找所有插入点和长度
                var insertPoints = new List<(int position, int length)>();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].StartsWith("[") && parts[i].EndsWith("]"))
                    {
                        int length = int.Parse(parts[i].Trim('[', ']'));
                        if (length < 0)
                        {
                            MessageBox.Show("插入长度不能为负数！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        // 记录插入位置和长度
                        insertPoints.Add((i, length));
                    }
                }

                if (!insertPoints.Any())
                {
                    MessageBox.Show("请使用方括号指定插入长度，例如：100,[100],101,200,[100],201",
                        "格式错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 获取所有画布上的MotionSender
                var doc = Instances.ActiveCanvas.Document;
                var allSenders = doc.Objects
                    .OfType<MotionSender>()
                    .OrderBy(s => int.Parse(s.NickName.Split('-')[0]))
                    .ToList();

                var newRanges = new List<(int min, int max)>();
                int accumulatedOffset = 0;  // 累计偏移量

                // 先获取所有非插入区间的值和位置
                var normalValues = new List<(int position, int value)>();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (!parts[i].StartsWith("[") && int.TryParse(parts[i], out int value))
                    {
                        normalValues.Add((i, value));
                    }
                }

                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].StartsWith("[") && parts[i].EndsWith("]"))
                    {
                        int length = int.Parse(parts[i].Trim('[', ']'));
                        int insertValue;

                        // 找到当前插入点之前的最近的数值
                        var previousValue = normalValues
                            .Where(x => x.position < i)
                            .OrderByDescending(x => x.position)
                            .FirstOrDefault();

                        // 如果是在末尾插入，需要考虑已经调整过的区间
                        if (i > previousValue.position && i == parts.Count - 1)
                        {
                            // 获取最后一个已存在的区间的结束值
                            var lastSender = allSenders
                                .OrderByDescending(s => int.Parse(s.NickName.Split('-')[1]))
                                .FirstOrDefault();

                            if (lastSender != null)
                            {
                                insertValue = int.Parse(lastSender.NickName.Split('-')[1]);
                            }
                            else
                            {
                                insertValue = previousValue.value + accumulatedOffset;
                            }
                        }
                        else
                        {
                            insertValue = previousValue.value + accumulatedOffset;
                        }

                        // 创建新的插入区间
                        newRanges.Add((insertValue + 1, insertValue + length));

                        // 调整插入点之后的所有Sender的区间
                        foreach (var motionSender in allSenders.Where(s =>
                            int.Parse(s.NickName.Split('-')[0]) > insertValue))
                        {
                            var range = motionSender.NickName.Split('-');
                            int min = int.Parse(range[0]) + length;
                            int max = int.Parse(range[1]) + length;

                            string newNickname = $"{min}-{max}";
                            motionSender.SetNicknameWithUndo(newNickname);
                            motionSender.ExpireSolution(true);
                        }

                        // 更新累计偏移量，同时更新后续的normalValues
                        accumulatedOffset += length;
                        for (int j = 0; j < normalValues.Count; j++)
                        {
                            if (normalValues[j].position > i)
                            {
                                // 更新normalValues[j]的值，考虑之前的插入影响
                                normalValues[j] = (normalValues[j].position, normalValues[j].value + length);
                            }
                        }

                        // 更新插入点之后的所有normalValues的位置
                        for (int j = 0; j < normalValues.Count; j++)
                        {
                            if (normalValues[j].position > i)
                            {
                                normalValues[j] = (normalValues[j].position + 1, normalValues[j].value);
                            }
                        }
                    }
                }

                // 生成所有新的区间
                if (newRanges.Any())
                {
                    GenerateMotionSenders(newRanges);
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理过程中出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteAndAdjust_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            try
            {
                var doc = Instances.ActiveCanvas.Document;

                // 按照起始值排序选中的Sender，确保从小到大删除
                var sortedSelectedSenders = _selectedSenders
                    .OrderBy(s => int.Parse(s.NickName.Split('-')[0]))
                    .ToList();

                // 获取所有Sender并按起始值排序
                var allSenders = doc.Objects
                    .OfType<MotionSender>()
                    .OrderBy(s => int.Parse(s.NickName.Split('-')[0]))
                    .ToList();

                // 累计偏移量
                int totalOffset = 0;

                // 逐个处理每个选中的Sender
                foreach (var selectedSender in sortedSelectedSenders)
                {
                    var range = selectedSender.NickName.Split('-');
                    int selectedMin = int.Parse(range[0]);
                    int selectedMax = int.Parse(range[1]);
                    int intervalLength = selectedMax - selectedMin + 1;

                    // 调整当前Sender之后的所有未删除的Sender的区间
                    foreach (var motionSender in allSenders.Where(s =>
                        !sortedSelectedSenders.Contains(s) && // 排除所有要删除的Sender
                        int.Parse(s.NickName.Split('-')[0]) > selectedMax - totalOffset)) // 使用已调整的位置进行比较
                    {
                        var senderRange = motionSender.NickName.Split('-');
                        int min = int.Parse(senderRange[0]) - intervalLength;
                        int max = int.Parse(senderRange[1]) - intervalLength;

                        string newNickname = $"{min}-{max}";
                        motionSender.SetNicknameWithUndo(newNickname);
                        motionSender.ExpireSolution(true);
                    }

                    // 更新累计偏移量
                    totalOffset += intervalLength;

                    // 删除当前Sender
                    doc.RemoveObject(selectedSender, false);
                }

                doc.ExpireSolution();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理过程中出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AdjustRangesExisting_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            // 解析最小值和最大值的调整量
            decimal minAdjustment = 0;
            decimal maxAdjustment = 0;
            int loopCount = 1;

            try
            {
                if (!string.IsNullOrWhiteSpace(MinValueAdjustment.Text))
                    minAdjustment = decimal.Parse(MinValueAdjustment.Text.Trim());

                if (!string.IsNullOrWhiteSpace(MaxValueAdjustment.Text))
                    maxAdjustment = decimal.Parse(MaxValueAdjustment.Text.Trim());

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

            var doc = Instances.ActiveCanvas.Document;
            if (doc == null) return;

            // 在修改之前记录撤销事件
            doc.UndoUtil.RecordEvent("调整Motion Sender区间");

            // 获取所有Sender并按最小值排序
            var allSenders = doc.Objects
                .OfType<MotionSender>()
                .ToList();

            // 创建字典来存储每个Sender的最终最小值和最大值
            var adjustmentDictionary = new Dictionary<MotionSender, (decimal Min, decimal Max)>();

            // 初始化字典，存储原始值
            foreach (var currentSender in allSenders)
            {
                var parts = currentSender.NickName.Split('-');
                decimal min = decimal.Parse(parts[0]);
                decimal max = decimal.Parse(parts[1]);
                adjustmentDictionary[currentSender] = (min, max);
            }

            // 计算所有调整值

            // 1. 首先计算选中Sender的直接调整
            foreach (var selectedSender in _selectedSenders)
            {
                var (min, max) = adjustmentDictionary[selectedSender];
                decimal finalMin = min;
                decimal finalMax = max;

                for (int i = 0; i < loopCount; i++)
                {
                    finalMin += minAdjustment;
                    finalMax += maxAdjustment;
                }

                adjustmentDictionary[selectedSender] = (finalMin, finalMax);
            }

            // 2. 处理相邻区间调整
            if (AdjustAdjacentSenders.IsChecked == true)
            {
                foreach (var selectedSender in _selectedSenders)
                {
                    var (currentMin, currentMax) = adjustmentDictionary[selectedSender];

                    // 处理最小值调整
                    if (minAdjustment != 0)
                    {
                        // 查找最大值等于当前最小值-1的相邻Sender
                        var lowerAdjacents = allSenders
                            .Where(s => s != selectedSender &&
                                   decimal.Parse(s.NickName.Split('-')[1]) == decimal.Parse(selectedSender.NickName.Split('-')[0]) - 1)
                            .ToList();

                        foreach (var lowerAdjacent in lowerAdjacents)
                        {
                            var (min, max) = adjustmentDictionary[lowerAdjacent];
                            adjustmentDictionary[lowerAdjacent] = (min, max + minAdjustment);
                        }
                    }

                    // 处理最大值调整
                    if (maxAdjustment != 0)
                    {
                        // 查找最小值等于当前最大值+1的相邻Sender
                        var upperAdjacents = allSenders
                            .Where(s => s != selectedSender &&
                                   decimal.Parse(s.NickName.Split('-')[0]) == decimal.Parse(selectedSender.NickName.Split('-')[1]) + 1)
                            .ToList();

                        foreach (var upperAdjacent in upperAdjacents)
                        {
                            var (min, max) = adjustmentDictionary[upperAdjacent];
                            adjustmentDictionary[upperAdjacent] = (min + maxAdjustment, max);
                        }
                    }
                }
            }

            // 3. 处理具有相同最小/最大值的Sender
            if (AdjustSameValueSenders.IsChecked == true)
            {
                foreach (var selectedSender in _selectedSenders)
                {
                    var originalMin = decimal.Parse(selectedSender.NickName.Split('-')[0]);
                    var originalMax = decimal.Parse(selectedSender.NickName.Split('-')[1]);

                    // 处理最小值调整
                    if (minAdjustment != 0)
                    {
                        // 查找具有相同最小值的Sender
                        var sameMinSenders = allSenders
                            .Where(s => s != selectedSender &&
                                   !_selectedSenders.Contains(s) &&
                                   decimal.Parse(s.NickName.Split('-')[0]) == originalMin)
                            .ToList();

                        foreach (var sameSender in sameMinSenders)
                        {
                            var (min, max) = adjustmentDictionary[sameSender];
                            adjustmentDictionary[sameSender] = (min + minAdjustment, max);
                        }
                    }

                    // 处理最大值调整
                    if (maxAdjustment != 0)
                    {
                        // 查找具有相同最大值的Sender
                        var sameMaxSenders = allSenders
                            .Where(s => s != selectedSender &&
                                   !_selectedSenders.Contains(s) &&
                                   decimal.Parse(s.NickName.Split('-')[1]) == originalMax)
                            .ToList();

                        foreach (var sameSender in sameMaxSenders)
                        {
                            var (min, max) = adjustmentDictionary[sameSender];
                            adjustmentDictionary[sameSender] = (min, max + maxAdjustment);
                        }
                    }
                }
            }

            // 4. 处理后续区间调整
            if (AdjustFollowingSenders.IsChecked == true && maxAdjustment != 0)
            {
                // 获取选中sender的最大的max值，确定哪些是后续sender
                var maxSelectedMax = _selectedSenders.Max(s => decimal.Parse(s.NickName.Split('-')[1]));

                // 找到所有后续Sender
                var followingSenders = allSenders
                    .Where(s => !_selectedSenders.Contains(s) &&
                           decimal.Parse(s.NickName.Split('-')[0]) > maxSelectedMax)
                    .ToList();

                foreach (var followingSender in followingSenders)
                {
                    var (min, max) = adjustmentDictionary[followingSender];
                    // 整体偏移相同的调整量
                    adjustmentDictionary[followingSender] = (min + maxAdjustment, max + maxAdjustment);
                }
            }

            // 验证所有调整后的值是否有效
            foreach (var kvp in adjustmentDictionary)
            {
                var (min, max) = kvp.Value;

                if (min < 0)
                {
                    MessageBox.Show($"调整后的最小值不能小于0！(Sender: {kvp.Key.NickName})", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (min >= max)
                {
                    MessageBox.Show($"调整后的最小值不能大于或等于最大值！(Sender: {kvp.Key.NickName} → {min}-{max})",
                        "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // 应用所有调整
            foreach (var kvp in adjustmentDictionary)
            {
                var motionSender = kvp.Key;
                var (finalMin, finalMax) = kvp.Value;

                // 只更新发生了变化的Sender
                if (finalMin != decimal.Parse(motionSender.NickName.Split('-')[0]) ||
                    finalMax != decimal.Parse(motionSender.NickName.Split('-')[1]))
                {
                    string newNickname = $"{finalMin}-{finalMax}";
                    motionSender.SetNicknameWithUndo(newNickname);
                    motionSender.ExpireSolution(true);
                }
            }

            // 确保更新文档
            doc.ExpireSolution();
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

            // 存储生成的新区间
            var newRanges = new List<(int min, int max)>();

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

                    // 添加新的区间到列表中
                    newRanges.Add(((int)currentMin, (int)currentMax));
                }
            }

            // 使用GenerateMotionSenders方法创建新的滑块
            GenerateMotionSenders(newRanges);
            Close();
        }

        private void MergeSenders_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedSenders) return;

            var minValue = _selectedSenders.Min(s => int.Parse(s.NickName.Split('-')[0]));
            var maxValue = _selectedSenders.Max(s => int.Parse(s.NickName.Split('-')[1]));

            GenerateMotionSenders(new[] { (minValue, maxValue) });
            Close();
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

        private List<int> ParseValues(string input)
        {
            try
            {
                var values = input.Split(',')
                                .Select(x => int.Parse(x.Trim()))
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

        // 创建不重叠的区间组合
        private IEnumerable<(int min, int max)> GenerateSequentialRanges(List<int> values, bool noOverlap)
        {
            for (int i = 0; i < values.Count - 1; i++)
            {
                int maxValue = values[i + 1];
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

        //创建所有可能的区间组合
        private IEnumerable<(int min, int max)> GenerateAllRanges(List<int> values, bool noOverlap)
        {
            for (int i = 0; i < values.Count; i++)
            {
                for (int j = i + 1; j < values.Count; j++)
                {
                    int maxValue = values[j];
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

        // 创建MotionSender并添加到文档中
        private void GenerateMotionSenders(IEnumerable<(int min, int max)> ranges)
        {
            // 过滤掉任何包含负值的区间
            var validRanges = ranges.Where(r => r.min >= 0 && r.max >= 0).ToList();

            if (validRanges.Count == 0)
            {
                MessageBox.Show("没有有效的非负区间可以创建！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var doc = Instances.ActiveCanvas.Document;

            RectangleF bounds;
            // 获取选中的Sender中心点
            if (HasSelectedSenders)
            {
                bounds = _selectedSenders.First().Attributes.Bounds;
            }
            else
            {
                bounds = Instances.ActiveCanvas.Viewport.VisibleRegion;
            }

            var centerX = (bounds.Left + bounds.Right) / 2;
            var centerY = (bounds.Top + bounds.Bottom) / 2;

            // 设置起始位置和间距
            double startX = centerX;
            double startY = centerY;
            const double verticalSpacing = 50;

            // 计算总数以确定起始Y坐标
            int totalSenders = validRanges.Count;
            startY += totalSenders * verticalSpacing / 4;

            int index = 0;

            foreach (var (min, max) in validRanges)
            {
                var motionSender = new MotionSender();
                motionSender.CreateSenderWithUndo(doc);

                motionSender.Attributes.Pivot = new System.Drawing.PointF(
                    (float)startX + 100,
                    (float)(startY + index * verticalSpacing)
                );

                // 设置slider的范围
                motionSender.NickName = $"{min.ToString()}-{max.ToString()}";

                // 手动触发NickName的set方法
                motionSender.NickName = $"{min.ToString()}-{max.ToString()}";

                var motionSlider = doc.Objects
                  .Where(o => o.GetType().ToString() == "Motion.Animation.MotionSlider")
                  .Cast<GH_NumberSlider>()
                  .FirstOrDefault();

                motionSender.AddSource(motionSlider);
                motionSender.WireDisplay = GH_ParamWireDisplay.hidden;
                index++;
            }


            // 获取所有MotionSlider
            var sliders = doc.Objects
                .OfType<MotionSlider>()
                .ToList();
            // 更新所有MotionSlider的区间
            foreach (var slider in sliders)
            {
                slider.UpdateRangeBasedOnSenders();
            }
        }
    }
}
