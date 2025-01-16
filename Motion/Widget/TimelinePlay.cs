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
                if (_currentFrame >= _endFrame)
                {
                    _currentFrame = _startFrame;
                    _isPlaying = false;
                    _animationTimer.Stop();
                }
                else
                {
                    _currentFrame++;
                }

                // 更新当前值
                UpdateCurrentValue();
                Owner?.Refresh();
            }
        }
    }
}
