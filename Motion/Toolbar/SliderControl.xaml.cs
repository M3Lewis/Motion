using Grasshopper.Kernel.Special;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Motion.Toolbar
{
    public partial class SliderControlWPF : Window, INotifyPropertyChanged
    {
        private GH_NumberSlider _ghSlider;
        private double _value;
        private static readonly Regex _numericRegex = new Regex("[^0-9-]+");
        private System.Windows.Threading.DispatcherTimer _timer;
        private bool _updatingFromTimer = false;
        private const int UPDATE_DELAY = 16; // 保持60fps的更新频率
        private DateTime _lastUpdateTime = DateTime.Now;
        private bool _isDragging = false;
        private double _pendingValue = 0;
        private System.Windows.Threading.DispatcherTimer _updateTimer;
        private System.Windows.Threading.DispatcherTimer _buttonRepeatTimer;
        private System.Windows.Controls.Button _currentButton;
        private bool _isAutoIncrementing = false;
        private bool _isAutoDecrementing = false;
        private System.Windows.Threading.DispatcherTimer _autoUpdateTimer;
        private static SliderControlWPF _instance;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Value
        {
            get => _value;
            set
            {
                double roundedValue = Math.Round(value);
                if (Math.Abs(_value - roundedValue) > double.Epsilon)
                {
                    _value = roundedValue;
                    _pendingValue = roundedValue;
                    
                    if (!_isDragging)
                    {
                        var now = DateTime.Now;
                        if ((now - _lastUpdateTime).TotalMilliseconds >= UPDATE_DELAY)
                        {
                            UpdateGHSlider(roundedValue);
                            _lastUpdateTime = now;
                        }
                    }
                    
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public static void ShowWindow(GH_NumberSlider ghSlider)
        {
            if (ghSlider == null) return;

            if (_instance != null)
            {
                try
                {
                    // 恢复窗口状态
                    if (_instance.WindowState == WindowState.Minimized)
                    {
                        _instance.WindowState = WindowState.Normal;
                    }
                    
                    _instance.UpdateSlider(ghSlider);
                    
                    // 显示并激活窗口
                    _instance.Show();
                    _instance.Activate();
                    _instance.Focus();
                    
                    // 确保窗口置顶
                    _instance.Topmost = false;
                    _instance.Topmost = true;
                }
                catch
                {
                    // 如果出现异常，创建新实例
                    _instance = null;
                    _instance = new SliderControlWPF(ghSlider);
                    _instance.Show();
                    _instance.Closed += (s, e) => _instance = null;
                }
            }
            else
            {
                _instance = new SliderControlWPF(ghSlider);
                _instance.Show();
                _instance.Closed += (s, e) => _instance = null;
            }
        }

        private void UpdateSlider(GH_NumberSlider ghSlider)
        {
            if (ghSlider == null) return;
            
            _ghSlider = ghSlider;
            // 使用 slider 控件的属性
            slider.Minimum = Convert.ToDouble(_ghSlider.Slider.Minimum);
            slider.Maximum = Convert.ToDouble(_ghSlider.Slider.Maximum);
            Value = Convert.ToDouble(_ghSlider.Slider.Value);
        }

        internal SliderControlWPF(GH_NumberSlider ghSlider)
        {
            InitializeComponent();
            
            _ghSlider = ghSlider;
            
            // 直接设置滑块的属性
            slider.Minimum = Convert.ToDouble(_ghSlider.Slider.Minimum);
            slider.Maximum = Convert.ToDouble(_ghSlider.Slider.Maximum);
            Value = Convert.ToDouble(_ghSlider.Slider.Value);

            DataContext = this;

            // 设置滑块事件
            slider.PreviewMouseDown += (s, e) => _isDragging = true;
            slider.PreviewMouseUp += (s, e) => 
            {
                _isDragging = false;
                UpdateGHSlider(_pendingValue);
            };
            slider.ValueChanged += Slider_ValueChanged;

            // 初始化更新计时器
            _updateTimer = new System.Windows.Threading.DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(UPDATE_DELAY);
            _updateTimer.Tick += UpdateTimer_Tick;

            // 初始化监控计时器
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 初始化自动更新计时器
            _autoUpdateTimer = new System.Windows.Threading.DispatcherTimer();
            _autoUpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
            _autoUpdateTimer.Tick += AutoUpdateTimer_Tick;

            this.Closed += (s, e) => 
            {
                _timer.Stop();
                _updateTimer.Stop();
                _autoUpdateTimer.Stop();
            };

            this.Topmost = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _updatingFromTimer = true;
            CheckAndUpdateRange();
            _updatingFromTimer = false;
        }

        private void CheckAndUpdateRange()
        {
            if (_ghSlider != null)
            {
                double newMin = Math.Round((double)_ghSlider.Slider.Minimum);
                double newMax = Math.Round((double)_ghSlider.Slider.Maximum);
                double newValue = Math.Round((double)_ghSlider.Slider.Value);

                bool rangeChanged = false;

                // 直接更新滑块的属性
                if (Math.Abs(slider.Minimum - newMin) > double.Epsilon)
                {
                    slider.Minimum = newMin;
                    rangeChanged = true;
                }

                if (Math.Abs(slider.Maximum - newMax) > double.Epsilon)
                {
                    slider.Maximum = newMax;
                    rangeChanged = true;
                }

                if (Math.Abs(Value - newValue) > double.Epsilon || rangeChanged)
                {
                    Value = Math.Max(newMin, Math.Min(newMax, newValue));
                }
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromTimer) return;
            
            _pendingValue = e.NewValue;
            
            // 如果正在拖动，使用计时器延迟更新
            if (_isDragging)
            {
                _updateTimer.Stop();
                _updateTimer.Start();
            }
            else
            {
                // 非拖动状态下，使用节流方式更新
                var now = DateTime.Now;
                if ((now - _lastUpdateTime).TotalMilliseconds >= UPDATE_DELAY)
                {
                    UpdateGHSlider(e.NewValue);
                    _lastUpdateTime = now;
                }
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _numericRegex.IsMatch(e.Text);
        }

        private void ValueTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateValueFromTextBox();
            }
        }

        private void ValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateValueFromTextBox();
        }

        private void UpdateValueFromTextBox()
        {
            if (int.TryParse(valueTextBox.Text, out int newValue))
            {
                // 使用 slider.Maximum 和 slider.Minimum 替代 Max 和 Min
                newValue = (int)Math.Max(slider.Minimum, Math.Min(slider.Maximum, newValue));
                Value = newValue;
            }
            else
            {
                // 如果输入无效，恢复为当前值
                valueTextBox.Text = Value.ToString("F0");
            }
            
            // 移除焦点
            FocusManager.SetFocusedElement(this, null);
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否是鼠标右键
            if (e is MouseButtonEventArgs mouseEvent && mouseEvent.ChangedButton == MouseButton.Right)
            {
                _isAutoIncrementing = !_isAutoIncrementing;
                _isAutoDecrementing = false;

                // 更新按钮视觉状态
                plusButton.Tag = _isAutoIncrementing ? "Active" : null;
                minusButton.Tag = null;

                if (_isAutoIncrementing)
                {
                    _autoUpdateTimer.Start();
                }
                else
                {
                    _autoUpdateTimer.Stop();
                }
            }
            else if (!_isAutoIncrementing && !_isAutoDecrementing)
            {
                // 左键点击时的行为，使用 slider.Maximum
                Value = Math.Min(slider.Maximum, Value + 1);
            }
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否是鼠标右键
            if (e is MouseButtonEventArgs mouseEvent && mouseEvent.ChangedButton == MouseButton.Right)
            {
                _isAutoDecrementing = !_isAutoDecrementing;
                _isAutoIncrementing = false;

                // 更新按钮视觉状态
                minusButton.Tag = _isAutoDecrementing ? "Active" : null;
                plusButton.Tag = null;

                if (_isAutoDecrementing)
                {
                    _autoUpdateTimer.Start();
                }
                else
                {
                    _autoUpdateTimer.Stop();
                }
            }
            else if (!_isAutoIncrementing && !_isAutoDecrementing)
            {
                // 左键点击时的行为，使用 slider.Minimum
                Value = Math.Max(slider.Minimum, Value - 1);
            }
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            Value = slider.Minimum;  // 使用 slider.Minimum
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            Value = slider.Maximum;  // 使用 slider.Maximum
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            _updateTimer.Stop();
            if (_isDragging)
            {
                var now = DateTime.Now;
                if ((now - _lastUpdateTime).TotalMilliseconds >= UPDATE_DELAY)
                {
                    UpdateGHSlider(_pendingValue);
                    _lastUpdateTime = now;
                }
            }
        }

        private void UpdateGHSlider(double value)
        {
            if (_ghSlider == null || _updatingFromTimer) return;

            _ghSlider.Slider.Value = (decimal)value;
            _ghSlider.ExpireSolution(true);
        }

        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isAutoIncrementing)
            {
                Value = Math.Min(slider.Maximum, Value + 1);  // 使用 slider.Maximum
            }
            else if (_isAutoDecrementing)
            {
                Value = Math.Max(slider.Minimum, Value - 1);  // 使用 slider.Minimum
            }
        }
    }
}
