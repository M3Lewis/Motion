using System;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        private void CreateEditableTextBox(Rectangle bounds, int currentValue, Action<int> onValueChanged)
        {
            try
            {
                // 如果已经有活动的文本框，先移除它
                if (_activeTextBox != null)
                {
                    Controls.Remove(_activeTextBox);
                    _activeTextBox.Dispose();
                    _activeTextBox = null;
                }

                // 创建新的文本框
                _activeTextBox = new TextBox
                {
                    Text = currentValue.ToString(),
                    Size = bounds.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = HorizontalAlignment.Center
                };

                // 直接使用bounds的位置，因为这些坐标已经是相对于TimelineWidget的了
                _activeTextBox.Location = new Point(bounds.X, bounds.Y);

                // 添加事件处理
                _activeTextBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true; // 阻止提示音
                        if (int.TryParse(_activeTextBox.Text, out int value))
                        {
                            onValueChanged(value);
                        }
                        RemoveActiveTextBox();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        RemoveActiveTextBox();
                    }
                };

                _activeTextBox.LostFocus += (s, e) =>
                {
                    if (_activeTextBox != null)
                    {
                        if (int.TryParse(_activeTextBox.Text, out int value))
                        {
                            onValueChanged(value);
                        }
                        RemoveActiveTextBox();
                    }
                };

                // 添加到控件并设置焦点
                Controls.Add(_activeTextBox);
                _activeTextBox.BringToFront();
                _activeTextBox.Focus();
                _activeTextBox.SelectAll();
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error creating text box: {ex.Message}");
                RemoveActiveTextBox(); // 确保清理任何可能部分创建的文本框
            }
        }

        private void RemoveActiveTextBox()
        {
            if (_activeTextBox == null) return;

            try
            {
                var textBox = _activeTextBox;
                _activeTextBox = null; // 立即设置为 null 以防止重复调用

                if (!textBox.IsDisposed)
                {
                    Controls.Remove(textBox);
                    textBox.Dispose();
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error removing text box: {ex.Message}");
            }
            finally
            {
                _activeTextBox = null;
                Invalidate(); // 重绘控件
            }
        }

        // 在TimelineWidget类中添加处理起始帧和结束帧文本框的点击事件
        private void HandleStartFrameClick()
        {
            CreateEditableTextBox(
                _startFrameBounds,
                _startFrame,
                newValue =>
                {
                    if (newValue >= 1 && newValue < _endFrame)
                    {
                        _startFrame = newValue;
                        Invalidate();
                    }
                }
            );
        }

        private void HandleEndFrameClick()
        {
            CreateEditableTextBox(
                _endFrameBounds,
                _endFrame,
                newValue =>
                {
                    if (newValue > _startFrame)
                    {
                        _endFrame = newValue;
                        Invalidate();
                    }
                }
            );
        }
    }
}
