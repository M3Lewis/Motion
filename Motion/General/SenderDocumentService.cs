using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Motion.Animation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Motion.General
{
    public static class SenderDocumentService
    {
        public static List<MotionSender> GetAllSendersSorted(GH_Document doc)
        {
            if (doc == null) return new List<MotionSender>();

            return doc.Objects
                .OfType<MotionSender>()
                .OrderBy(s =>
                {
                    return MotilityUtils.TryParseNickNameInterval(s.NickName, out double min, out _) ? (int)min : 0;
                })
                .ToList();
        }

        private static bool TryParseInsertInputs(
            string replaceValuesText,
            out List<string> parts,
            out List<(int position, int value)> normalValues,
            out string errorMessage)
        {
            parts = null;
            normalValues = null;
            errorMessage = null;

            parts = replaceValuesText.Split(',')
                                     .Select(x => x.Trim())
                                     .ToList();

            var tempInsertPoints = new List<(int position, int length)>();
            var tempNormalValues = new List<(int position, int value)>();

            for (int i = 0; i < parts.Count; i++)
            {
                string part = parts[i];
                if (part.StartsWith("[") && part.EndsWith("]"))
                {
                    if (!int.TryParse(part.Trim('[', ']'), out int length))
                    {
                        errorMessage = "Msg.InputFormatError|" + part;
                        return false;
                    }
                    if (length < 0)
                    {
                        errorMessage = "Msg.NoNegativeInsert";
                        return false;
                    }
                    tempInsertPoints.Add((i, length));
                }
                else if (int.TryParse(part, out int value))
                {
                    tempNormalValues.Add((i, value));
                }
            }

            if (!tempInsertPoints.Any())
            {
                errorMessage = "Msg.UseBracketsForInsert";
                return false;
            }

            normalValues = tempNormalValues;
            return true;
        }

        private static void ShiftFollowingSenders(List<MotionSender> senders, int boundaryValue, int offsetAmount)
        {
            var targetSenders = senders.Where(s => MotilityUtils.TryParseNickNameInterval(s.NickName, out double val, out _) && (int)val > boundaryValue);
            foreach (var motionSender in targetSenders)
            {
                MotilityUtils.TryParseNickNameInterval(motionSender.NickName, out double dMin, out double dMax);
                int min = (int)dMin + offsetAmount;
                int max = (int)dMax + offsetAmount;

                string newNickname = $"{min}-{max}";
                motionSender.SetNicknameWithUndo(newNickname);
                motionSender.ExpireSolution(true);
            }
        }

        public static bool InsertRange(
            GH_Document doc,
            List<MotionSender> selectedSenders,
            string replaceValuesText,
            float centerX,
            float centerY,
            out string errorMessage)
        {
            errorMessage = null;
            if (doc == null)
            {
                errorMessage = Motion.General.LanguageManager.GetString("Msg.DocumentIsNull", "Document is null.");
                return false;
            }

            try
            {
                if (!TryParseInsertInputs(replaceValuesText, out var parts, out var normalValues, out errorMessage))
                {
                    return false;
                }

                // 获取所有画布上的MotionSender
                var allSenders = GetAllSendersSorted(doc);

                var newRanges = new List<(int min, int max)>();
                int accumulatedOffset = 0;  // 累计偏移量

                for (int i = 0; i < parts.Count; i++)
                {
                    string part = parts[i];
                    if (!part.StartsWith("[") || !part.EndsWith("]"))
                        continue;

                    int length = int.Parse(part.Trim('[', ']'));

                    // 找到当前插入点之前的最近的数值
                    var previousValue = normalValues
                        .Where(x => x.position < i)
                        .OrderByDescending(x => x.position)
                        .FirstOrDefault();

                    // 如果是在末尾插入，需要考虑已经调整过的区间
                    bool isEndInsert = i > previousValue.position && i == parts.Count - 1;
                    var lastSender = isEndInsert
                        ? allSenders.OrderByDescending(s => MotilityUtils.TryParseNickNameInterval(s.NickName, out _, out double val) ? (int)val : 0).FirstOrDefault()
                        : null;

                    int insertValue = (isEndInsert && lastSender != null && MotilityUtils.TryParseNickNameInterval(lastSender.NickName, out _, out double val))
                        ? (int)val
                        : previousValue.value + accumulatedOffset;

                    // 创建新的插入区间
                    newRanges.Add((insertValue + 1, insertValue + length));

                    // 调整插入点之后的所有Sender的区间
                    ShiftFollowingSenders(allSenders, insertValue, length);

                    // 更新累计偏移量，并同时更新后续 normalValues 的值与位置（考虑之前的插入影响）
                    accumulatedOffset += length;
                    for (int j = 0; j < normalValues.Count; j++)
                    {
                        if (normalValues[j].position > i)
                        {
                            normalValues[j] = (normalValues[j].position + 1, normalValues[j].value + length);
                        }
                    }
                }

                // 生成所有新的区间
                if (newRanges.Any())
                {
                    GenerateMotionSenders(doc, newRanges, centerX, centerY, out _);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static bool DeleteAndAdjust(GH_Document doc, List<MotionSender> selectedSenders, out string errorMessage)
        {
            errorMessage = null;
            if (doc == null)
            {
                errorMessage = Motion.General.LanguageManager.GetString("Msg.DocumentIsNull", "Document is null.");
                return false;
            }

            try
            {
                // 按照起始值排序选中的Sender，确保从小到大删除
                var sortedSelectedSenders = selectedSenders
                    .OrderBy(s =>
                    {
                        return MotilityUtils.TryParseNickNameInterval(s.NickName, out double val, out _) ? (int)val : 0;
                    })
                    .ToList();

                // 获取所有Sender并按起始值排序
                var allSenders = GetAllSendersSorted(doc);

                // 累计偏移量
                int totalOffset = 0;

                // 逐个处理每个选中的Sender
                foreach (var selectedSender in sortedSelectedSenders)
                {
                    MotilityUtils.TryParseNickNameInterval(selectedSender.NickName, out double dMin, out double dMax);
                    int selectedMin = (int)dMin;
                    int selectedMax = (int)dMax;
                    int intervalLength = selectedMax - selectedMin + 1;

                    // 调整当前Sender之后的所有未删除的Sender的区间
                    foreach (var motionSender in allSenders.Where(s =>
                        !sortedSelectedSenders.Contains(s) && // 排除所有要删除的Sender
                        (MotilityUtils.TryParseNickNameInterval(s.NickName, out double val, out _) && (int)val > selectedMax - totalOffset))) // 使用已调整的位置进行比较
                    {
                        MotilityUtils.TryParseNickNameInterval(motionSender.NickName, out double innerMin, out double innerMax);
                        int min = (int)innerMin - intervalLength;
                        int max = (int)innerMax - intervalLength;

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
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static Dictionary<MotionSender, (decimal Min, decimal Max)> InitializeAdjustmentMap(List<MotionSender> senders)
        {
            var map = new Dictionary<MotionSender, (decimal Min, decimal Max)>();
            foreach (var currentSender in senders)
            {
                MotilityUtils.TryParseNickNameInterval(currentSender.NickName, out double dMin, out double dMax);
                map[currentSender] = ((decimal)dMin, (decimal)dMax);
            }
            return map;
        }

        private static void ApplyBaseAdjustments(
            Dictionary<MotionSender, (decimal Min, decimal Max)> map,
            List<MotionSender> selectedSenders,
            decimal minAdjustment,
            decimal maxAdjustment,
            int loopCount)
        {
            foreach (var selectedSender in selectedSenders)
            {
                if (!map.TryGetValue(selectedSender, out var current))
                    continue;

                decimal finalMin = current.Min;
                decimal finalMax = current.Max;

                for (int i = 0; i < loopCount; i++)
                {
                    finalMin += minAdjustment;
                    finalMax += maxAdjustment;
                }

                map[selectedSender] = (finalMin, finalMax);
            }
        }

        private static void ApplyAdjacentAdjustments(
            Dictionary<MotionSender, (decimal Min, decimal Max)> map,
            List<MotionSender> allSenders,
            List<MotionSender> selectedSenders,
            decimal minAdjustment,
            decimal maxAdjustment)
        {
            if (minAdjustment != 0)
            {
                AdjustLowerAdjacents(map, allSenders, selectedSenders, minAdjustment);
            }

            if (maxAdjustment != 0)
            {
                AdjustUpperAdjacents(map, allSenders, selectedSenders, maxAdjustment);
            }
        }

        private static void AdjustLowerAdjacents(
            Dictionary<MotionSender, (decimal Min, decimal Max)> map,
            List<MotionSender> allSenders,
            List<MotionSender> selectedSenders,
            decimal minAdjustment)
        {
            foreach (var selectedSender in selectedSenders)
            {
                var lowerAdjacents = allSenders.Where(s =>
                    s != selectedSender &&
                    MotilityUtils.TryParseNickNameInterval(s.NickName, out _, out double dMax) &&
                    MotilityUtils.TryParseNickNameInterval(selectedSender.NickName, out double dMin, out _) &&
                    (decimal)dMax == (decimal)dMin - 1);

                foreach (var lowerAdjacent in lowerAdjacents)
                {
                    var val = map[lowerAdjacent];
                    map[lowerAdjacent] = (val.Min, val.Max + minAdjustment);
                }
            }
        }

        private static void AdjustUpperAdjacents(
            Dictionary<MotionSender, (decimal Min, decimal Max)> map,
            List<MotionSender> allSenders,
            List<MotionSender> selectedSenders,
            decimal maxAdjustment)
        {
            foreach (var selectedSender in selectedSenders)
            {
                var upperAdjacents = allSenders.Where(s =>
                    s != selectedSender &&
                    MotilityUtils.TryParseNickNameInterval(s.NickName, out double dMin, out _) &&
                    MotilityUtils.TryParseNickNameInterval(selectedSender.NickName, out _, out double dMax) &&
                    (decimal)dMin == (decimal)dMax + 1);

                foreach (var upperAdjacent in upperAdjacents)
                {
                    var val = map[upperAdjacent];
                    map[upperAdjacent] = (val.Min + maxAdjustment, val.Max);
                }
            }
        }

        private static void ApplyFollowingAdjustments(
            Dictionary<MotionSender, (decimal Min, decimal Max)> map,
            List<MotionSender> allSenders,
            List<MotionSender> selectedSenders,
            decimal maxAdjustment)
        {
            if (maxAdjustment == 0) return;

            var maxSelectedMax = selectedSenders.Max(s =>
            {
                MotilityUtils.TryParseNickNameInterval(s.NickName, out _, out double dMax);
                return (decimal)dMax;
            });

            var followingSenders = allSenders
                .Where(s => !selectedSenders.Contains(s) &&
                       (MotilityUtils.TryParseNickNameInterval(s.NickName, out double dMin, out _) && (decimal)dMin > maxSelectedMax))
                .ToList();

            foreach (var followingSender in followingSenders)
            {
                if (map.TryGetValue(followingSender, out var val))
                {
                    map[followingSender] = (val.Min + maxAdjustment, val.Max + maxAdjustment);
                }
            }
        }

        private static bool ValidateAdjustments(
            Dictionary<MotionSender, (decimal Min, decimal Max)> map,
            out string errorMessage,
            out string errorParam)
        {
            errorMessage = null;
            errorParam = null;

            foreach (var kvp in map)
            {
                var (min, max) = kvp.Value;

                if (min < 0)
                {
                    errorMessage = "Msg.MinLessThanZero";
                    errorParam = kvp.Key.NickName;
                    return false;
                }

                if (min >= max)
                {
                    errorMessage = "Msg.MinGteMax";
                    errorParam = $"{kvp.Key.NickName}|{min}|{max}";
                    return false;
                }
            }

            return true;
        }

        private static void ApplyChangesToDocument(Dictionary<MotionSender, (decimal Min, decimal Max)> map)
        {
            foreach (var kvp in map)
            {
                var motionSender = kvp.Key;
                var (finalMin, finalMax) = kvp.Value;

                MotilityUtils.TryParseNickNameInterval(motionSender.NickName, out double dMin, out double dMax);
                if (finalMin != (decimal)dMin || finalMax != (decimal)dMax)
                {
                    string newNickname = $"{finalMin}-{finalMax}";
                    motionSender.SetNicknameWithUndo(newNickname);
                    motionSender.ExpireSolution(true);
                }
            }
        }

        public static bool AdjustRangesExisting(
            GH_Document doc,
            List<MotionSender> selectedSenders,
            decimal minAdjustment,
            decimal maxAdjustment,
            int loopCount,
            bool adjustAdjacent,
            bool adjustFollowing,
            out string errorMessage,
            out string errorParam)
        {
            errorMessage = null;
            errorParam = null;

            if (doc == null)
            {
                errorMessage = Motion.General.LanguageManager.GetString("Msg.DocumentIsNull", "Document is null.");
                return false;
            }

            try
            {
                // 在修改之前记录撤销事件
                doc.UndoUtil.RecordEvent("调整Motion Sender区间");

                // 获取所有Sender并按最小值排序
                var allSenders = doc.Objects
                    .OfType<MotionSender>()
                    .ToList();

                var adjustmentDictionary = InitializeAdjustmentMap(allSenders);

                // 1. 首先计算选中Sender的直接调整
                ApplyBaseAdjustments(adjustmentDictionary, selectedSenders, minAdjustment, maxAdjustment, loopCount);

                // 2. 处理相邻区间调整
                if (adjustAdjacent)
                {
                    ApplyAdjacentAdjustments(adjustmentDictionary, allSenders, selectedSenders, minAdjustment, maxAdjustment);
                }

                // 3. 处理后续区间调整
                if (adjustFollowing)
                {
                    ApplyFollowingAdjustments(adjustmentDictionary, allSenders, selectedSenders, maxAdjustment);
                }

                // 验证结果
                if (!ValidateAdjustments(adjustmentDictionary, out errorMessage, out errorParam))
                {
                    return false;
                }

                // 应用所有调整
                ApplyChangesToDocument(adjustmentDictionary);

                // 确保更新文档
                doc.ExpireSolution();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                errorParam = null;
                return false;
            }
        }

        public static bool GenerateMotionSenders(
            GH_Document doc,
            IEnumerable<(int min, int max)> ranges,
            float centerX,
            float centerY,
            out string errorMessage)
        {
            errorMessage = null;
            if (doc == null)
            {
                errorMessage = Motion.General.LanguageManager.GetString("Msg.DocumentIsNull", "Document is null.");
                return false;
            }

            // 过滤掉任何包含负值的区间
            var validRanges = ranges.Where(r => r.min >= 0 && r.max >= 0).ToList();

            if (validRanges.Count == 0)
            {
                errorMessage = "Msg.NoValidIntervals";
                return false;
            }

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

                // 设置slider的范围并触发昵称修改
                motionSender.NickName = $"{min}-{max}";

                var motionSlider = doc.Objects
                  .OfType<MotionSlider>()
                  .FirstOrDefault();

                if (motionSlider != null)
                {
                    motionSender.AddSource(motionSlider);
                }
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
                slider.UpdateRangeFromConnectedSenders();
            }

            return true;
        }
    }
}
