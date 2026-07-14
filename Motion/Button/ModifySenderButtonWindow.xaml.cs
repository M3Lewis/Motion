using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Motion.Animation;
using Motion.General;
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
            Motion.General.LanguageManager.LocalizeWindow(this);
            DataContext = this;
        }
        
        /// <summary>
        /// 统一在 Solver 锁定和自动异常处理下执行 UI 操作
        /// </summary>
        private void ExecuteWithSolverLock(Action action)
        {
            SetSolverEnabled(false);
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ShowErrorFormat("Msg.ErrorProcessing", "处理过程中出错：{0}", ex.Message);
            }
            finally
            {
                SetSolverEnabled(true);
            }
        }

        /// <summary>
        /// 获取画布放置新物体的中心坐标
        /// </summary>
        private (float centerX, float centerY) GetCanvasPlacementCenter()
        {
            var bounds = HasSelectedSenders
                ? _selectedSenders[0].Attributes.Bounds
                : (Instances.ActiveCanvas?.Viewport.VisibleRegion ?? RectangleF.Empty);

            return ((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
        }

        /// <summary>
        /// 统一解析并校验最小值/最大值调整量以及循环次数
        /// </summary>
        private bool TryParseAdjustInputs(out decimal minAdj, out decimal maxAdj, out int loops)
        {
            minAdj = 0;
            maxAdj = 0;
            loops = 1;

            if (!string.IsNullOrWhiteSpace(MinValueAdjustment.Text) && !decimal.TryParse(MinValueAdjustment.Text.Trim(), out minAdj))
            {
                ShowError("Msg.MinAdjustmentInvalid", "输入格式错误：最小值调整量必须为有效数字");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(MaxValueAdjustment.Text) && !decimal.TryParse(MaxValueAdjustment.Text.Trim(), out maxAdj))
            {
                ShowError("Msg.MaxAdjustmentInvalid", "输入格式错误：最大值调整量必须为有效数字");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(LoopCount.Text) && !int.TryParse(LoopCount.Text.Trim(), out loops))
            {
                ShowError("Msg.LoopCountInvalid", "输入格式错误：循环次数必须为有效整数");
                return false;
            }
            if (loops < 1)
            {
                ShowError("Msg.CycleMustBePositive", "循环次数必须至少为1！");
                return false;
            }
            return true;
        }

        private void ShowError(string key, string fallback)
        {
            MessageBox.Show(
                Motion.General.LanguageManager.GetString(key, fallback),
                Motion.General.LanguageManager.GetString("Msg.ErrorTitle", "错误"),
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void ShowErrorFormat(string key, string fallback, params object[] args)
        {
            MessageBox.Show(
                string.Format(Motion.General.LanguageManager.GetString(key, fallback), args),
                Motion.General.LanguageManager.GetString("Msg.ErrorTitle", "错误"),
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        public void Initialize(List<MotionSender> selectedSenders)
        {
            _selectedSenders = selectedSenders;

            if (HasSelectedSenders)
            {
                // 填充替换区间的文本框，确保数字按照正确顺序排序
                var ranges = _selectedSenders
                    .Select(s =>
                    {
                        MotilityUtils.TryParseNickNameInterval(s.NickName, out double minVal, out double maxVal);
                        return new[] { minVal.ToString(), maxVal.ToString() };
                    })
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
                MotilityUtils.TryParseNickNameInterval(sender.NickName, out double minVal, out double maxVal);
                SplitValues.Text = $"{minVal},{maxVal}";
            }
        }

        private void SetSolverEnabled(bool enabled)
        {
            GH_Document.EnableSolutions = enabled;
        }

        private void CreateSenders_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                var values = ParseValues(NewSliderValues.Text);
                if (values == null) return;

                var ranges = AllCombinations.IsChecked == true
                    ? GenerateAllRanges(values, NoOverlap.IsChecked == true)
                    : GenerateSequentialRanges(values, NoOverlap.IsChecked == true);

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var (centerX, centerY) = GetCanvasPlacementCenter();
                if (!SenderDocumentService.GenerateMotionSenders(doc, ranges, centerX, centerY, out string errorKey))
                {
                    ShowError(errorKey, "创建失败！");
                    return;
                }

                Close();
            });
        }
        
        private void CreateOffsetSender_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!int.TryParse(OffsetFrames.Text, out int offsetFrames))
                {
                    ShowError("Msg.InvalidOffset", "请输入有效的偏移帧数值");
                    return;
                }

                if (_selectedSenders.Count != 1)
                {
                    ShowError("Msg.SelectMotionSlider", "请选择一个Motion Slider");
                    return;
                }

                MotionSender selectedSlider = _selectedSenders[0];

                MotilityUtils.TryParseNickNameInterval(selectedSlider.NickName, out double minVal, out double maxVal);
                int currentMin = (int)minVal;
                int currentMax = (int)maxVal;

                int newMin, newMax;
                if (offsetFrames >= 0)
                {
                    newMin = currentMax + 1;
                    newMax = newMin + offsetFrames - 1;
                }
                else
                {
                    newMax = currentMin - 1;
                    newMin = newMax + offsetFrames + 1;
                }

                List<int> values = new List<int> { newMin, newMax };
                var ranges = GenerateAllRanges(values, NoOverlap.IsChecked == true);

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var (centerX, centerY) = GetCanvasPlacementCenter();
                if (!SenderDocumentService.GenerateMotionSenders(doc, ranges, centerX, centerY, out string errorKey))
                {
                    ShowError(errorKey, "创建失败！");
                    return;
                }

                Close();
            });
        }


        private void ReplaceRanges_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSelectedSenders) return;

                var values = ParseValues(ReplaceValues.Text);
                if (values == null) return;

                var oldRanges = _selectedSenders
                    .Select(s =>
                    {
                        MotilityUtils.TryParseNickNameInterval(s.NickName, out double minVal, out double maxVal);
                        return new[] { minVal.ToString(), maxVal.ToString() };
                    })
                    .SelectMany(x => x)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (values.Count != oldRanges.Count)
                {
                    ShowError("Msg.CountMustBeSame", "新值的数量必须与原始值的数量相同！");
                    return;
                }

                var mapping = oldRanges.Zip(values, (old, @new) => new { Old = old, New = @new })
                    .ToDictionary(x => x.Old, x => x.New);

                if (mapping.Values.Any(v => v < 0))
                {
                    ShowError("Msg.NoNegativeMin", "不允许设置负数值，最小值必须大于或等于0！");
                    return;
                }

                foreach (var selectedSender in _selectedSenders)
                {
                    MotilityUtils.TryParseNickNameInterval(selectedSender.NickName, out double minVal, out double maxVal);
                    string newNickname = $"{mapping[minVal.ToString()]}-{mapping[maxVal.ToString()]}";
                    selectedSender.SetNicknameWithUndo(newNickname);
                    selectedSender.ExpireSolution(true);
                }

                Close();
            });
        }


        private void InsertRange_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSelectedSenders) return;

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var (centerX, centerY) = GetCanvasPlacementCenter();
                if (!SenderDocumentService.InsertRange(doc, _selectedSenders, ReplaceValues.Text, centerX, centerY, out string errorKey))
                {
                    if (errorKey.StartsWith("Msg."))
                    {
                        if (errorKey.Contains('|'))
                        {
                            var parts = errorKey.Split('|');
                            ShowErrorFormat(parts[0], "插入失败", parts.Skip(1).ToArray());
                        }
                        else
                        {
                            ShowError(errorKey, "插入失败");
                        }
                    }
                    else
                    {
                        ShowErrorFormat("Msg.ErrorProcessing", "处理过程中出错：{0}", errorKey);
                    }
                    return;
                }

                Close();
            });
        }


        private void DeleteAndAdjust_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSelectedSenders) return;

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                if (!SenderDocumentService.DeleteAndAdjust(doc, _selectedSenders, out string errorKey))
                {
                    ShowErrorFormat("Msg.ErrorProcessing", "处理过程中出错：{0}", errorKey);
                    return;
                }

                Close();
            });
        }


        private void AdjustRangesExisting_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSelectedSenders) return;

                if (!TryParseAdjustInputs(out decimal minAdjustment, out decimal maxAdjustment, out int loopCount))
                    return;

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                bool adjustAdjacent = AdjustAdjacentSenders.IsChecked == true;
                bool adjustFollowing = AdjustFollowingSenders.IsChecked == true;

                if (SenderDocumentService.AdjustRangesExisting(
                        doc,
                        _selectedSenders,
                        minAdjustment,
                        maxAdjustment,
                        loopCount,
                        adjustAdjacent,
                        adjustFollowing,
                        out string errorKey,
                        out string errorParam))
                {
                    Close();
                    return;
                }

                switch (errorKey)
                {
                    case "Msg.MinLessThanZero":
                        ShowErrorFormat("Msg.MinLessThanZero", "调整后的最小值不能小于0！(Sender: {0})", errorParam);
                        break;
                    case "Msg.MinGteMax":
                        var errParts = errorParam.Split('|');
                        ShowErrorFormat("Msg.MinGteMax", "调整后的最小值不能大于或等于最大值！(Sender: {0} → {1}-{2})", errParts[0], errParts[1], errParts[2]);
                        break;
                    default:
                        if (!string.IsNullOrEmpty(errorKey))
                        {
                            ShowError(errorKey, "调整失败");
                        }
                        break;
                }
            });
        }



        private void AdjustRangesNew_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSelectedSenders) return;

                if (!TryParseAdjustInputs(out decimal minAdjustment, out decimal maxAdjustment, out int loopCount))
                    return;

                var newRanges = new List<(int min, int max)>();

                foreach (var originalSender in _selectedSenders)
                {
                    MotilityUtils.TryParseNickNameInterval(originalSender.NickName, out double minVal, out double maxVal);
                    decimal currentMin = (decimal)minVal;
                    decimal currentMax = (decimal)maxVal;

                    for (int i = 0; i < loopCount; i++)
                    {
                        currentMin += minAdjustment;
                        currentMax += maxAdjustment;

                        if (currentMin < 0 || currentMax < 0)
                        {
                            ShowError("Msg.NoNegativeIntervals", "不允许设置负数值，最小值和最大值必须大于或等于0！");
                            return;
                        }

                        if (currentMin >= currentMax)
                        {
                            ShowError("Msg.MinGteMaxSimple", "调整后的最小值不能大于或等于最大值！");
                            return;
                        }

                        newRanges.Add(((int)currentMin, (int)currentMax));
                    }
                }

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var (centerX, centerY) = GetCanvasPlacementCenter();
                if (!SenderDocumentService.GenerateMotionSenders(doc, newRanges, centerX, centerY, out string errorKey))
                {
                    ShowError(errorKey, "创建失败！");
                    return;
                }

                Close();
            });
        }

        private void MergeSenders_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSelectedSenders) return;

                var minValue = _selectedSenders.Min(s =>
                {
                    MotilityUtils.TryParseNickNameInterval(s.NickName, out double minVal, out _);
                    return (int)minVal;
                });
                var maxValue = _selectedSenders.Max(s =>
                {
                    MotilityUtils.TryParseNickNameInterval(s.NickName, out _, out double maxVal);
                    return (int)maxVal;
                });

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var (centerX, centerY) = GetCanvasPlacementCenter();
                if (!SenderDocumentService.GenerateMotionSenders(doc, new[] { (minValue, maxValue) }, centerX, centerY, out string errorKey))
                {
                    ShowError(errorKey, "创建失败！");
                    return;
                }

                Close();
            });
        }
        
        private void SplitSender_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithSolverLock(() =>
            {
                if (!HasSingleSenderSelected) return;

                var values = ParseValues(SplitValues.Text);
                if (values == null) return;

                var slider = _selectedSenders[0];
                MotilityUtils.TryParseNickNameInterval(slider.NickName, out double minVal, out double maxVal);
                if (values.First() != (int)minVal || values.Last() != (int)maxVal)
                {
                    ShowError("Msg.SplitRangeInvalid", "拆分值必须以当前slider的最小值开始，以最大值结束！");
                    return;
                }

                var ranges = SplitAllCombinations.IsChecked == true
                    ? GenerateAllRanges(values, SplitNoOverlap.IsChecked == true)
                    : GenerateSequentialRanges(values, SplitNoOverlap.IsChecked == true);

                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var (centerX, centerY) = GetCanvasPlacementCenter();
                if (!SenderDocumentService.GenerateMotionSenders(doc, ranges, centerX, centerY, out string errorKey))
                {
                    ShowError(errorKey, "创建失败！");
                    return;
                }

                Close();
            });
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
                    throw new Exception("Msg.AtLeastTwoValues");

                // 检查是否有负值
                if (values.Any(v => v < 0))
                    throw new Exception("Msg.NoNegativeIntervals");

                return values;
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("Msg."))
                {
                    ShowError(ex.Message, "输入错误");
                }
                else
                {
                    ShowErrorFormat("Msg.InputFormatError", "输入格式错误：{0}", ex.Message);
                }
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
    }
}
