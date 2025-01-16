using System;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        private void CreateEditableTextBox(Rectangle bounds, int currentValue, Action<int> onValueChanged)
        {
            // 确保先清理已存在的文本框
            RemoveActiveTextBox();

            try
            {
                // 创建新的文本框
                _activeTextBox = new TextBox();
                _activeTextBox.Location = bounds.Location;
                _activeTextBox.Size = bounds.Size;
                _activeTextBox.Text = currentValue.ToString();
                _activeTextBox.BorderStyle = BorderStyle.FixedSingle;
                _activeTextBox.TextAlign = HorizontalAlignment.Center;

                // 处理按键事件
                _activeTextBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        if (int.TryParse(_activeTextBox.Text, out int newValue))
                        {
                            onValueChanged(newValue);
                        }
                        RemoveActiveTextBox();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        RemoveActiveTextBox();
                    }
                };

                // 处理失去焦点事件
                _activeTextBox.LostFocus += (s, e) =>
                {
                    if (_activeTextBox != null && !_activeTextBox.IsDisposed)
                    {
                        if (int.TryParse(_activeTextBox.Text, out int newValue))
                        {
                            onValueChanged(newValue);
                        }
                        RemoveActiveTextBox();
                    }
                };

                // 添加到画布
                if (Owner != null && !Owner.IsDisposed)
                {
                    Owner.Controls.Add(_activeTextBox);
                    _activeTextBox.Focus();
                    _activeTextBox.SelectAll();
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error creating text box: {ex.Message}");
                RemoveActiveTextBox();
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
                    if (Owner != null && !Owner.IsDisposed && Owner.Controls.Contains(textBox))
                    {
                        Owner.Controls.Remove(textBox);
                    }
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
                if (Owner != null && !Owner.IsDisposed)
                {
                    Owner.Refresh();
                }
            }
        }
    }
}
