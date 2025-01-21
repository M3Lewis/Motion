using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//时间轴播放设置

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                _currentFrame++;
                if (_currentFrame > _endFrame)
                {
                    _currentFrame = _startFrame;  // 循环播放
                }
                UpdateCurrentValue();
                Invalidate();  // 请求重绘
            }
        }

        private void HandleStartFrameJump()
        {
            _currentFrame = _startFrame;
            UpdateCurrentValue();
            Invalidate();
        }

        private void HandlePrevKeyframe()
        {
            if (!_keyframeGroups.ContainsKey(_activeGroup) || _keyframeGroups[_activeGroup].Count == 0) 
                return;

            var activeKeyframes = _keyframeGroups[_activeGroup];

            // 找到当前帧之前的最近关键帧
            var prevKeyframe = activeKeyframes
                .Where(k => k.Frame < _currentFrame)
                .OrderByDescending(k => k.Frame)
                .FirstOrDefault();

            // 如果没有找到之前的关键帧，跳转到最后一个关键帧（循环）
            if (prevKeyframe == null)
            {
                prevKeyframe = activeKeyframes.OrderByDescending(k => k.Frame).First();
            }

            _currentFrame = prevKeyframe.Frame;
            UpdateCurrentValue();
            Invalidate();
        }

        private void HandleNextKeyframe()
        {
            if (!_keyframeGroups.ContainsKey(_activeGroup) || _keyframeGroups[_activeGroup].Count == 0) 
                return;

            var activeKeyframes = _keyframeGroups[_activeGroup];

            // 找到当前帧之后的最近关键帧
            var nextKeyframe = activeKeyframes
                .Where(k => k.Frame > _currentFrame)
                .OrderBy(k => k.Frame)
                .FirstOrDefault();

            // 如果没有找到之后的关键帧，跳转到第一个关键帧（循环）
            if (nextKeyframe == null)
            {
                nextKeyframe = activeKeyframes.OrderBy(k => k.Frame).First();
            }

            _currentFrame = nextKeyframe.Frame;
            UpdateCurrentValue();
            Invalidate();
        }

        private void HandleEndFrameJump()
        {
            _currentFrame = _endFrame;
            UpdateCurrentValue();
            Invalidate();
        }
    }
}
