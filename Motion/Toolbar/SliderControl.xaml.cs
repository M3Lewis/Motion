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
        private double _min;
        private double _max;
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

        public double Min
        {
            get => _min;
            private set
            {
                if (Math.Abs(_min - value) > double.Epsilon)
                {
                    _min = value;
                    OnPropertyChanged(nameof(Min));
                }
            }
        }

        public double Max
        {
            get => _max;
            private set
            {
                if (Math.Abs(_max - value) > double.Epsilon)
                {
                    _max = value;
                    OnPropertyChanged(nameof(Max));
                }
            }
        }

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
                    
                    // 如果不是正在拖动，使用节流方式更新
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
            if (_instance != null)
            {
                // 恢复窗口状态
                if (_instance.WindowState == WindowState.Minimized)
                {
                    _instance.WindowState = WindowState.Normal;
                }
                
                // 显示并激活窗口
                _instance.Show();
                _instance.Activate();
                _instance.Focus();
                
                // 确保窗口置顶
                _instance.Topmost = false;
                _instance.Topmost = true;
                
                _instance.UpdateSlider(ghSlider);
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
            _ghSlider = ghSlider;
            Min = Math.Round((double)_ghSlider.Slider.Minimum);
            Max = Math.Round((double)_ghSlider.Slider.Maximum);
            Value = Math.Round((double)_ghSlider.Slider.Value);
        }

        internal SliderControlWPF(GH_NumberSlider ghSlider)
        {
            InitializeComponent();

            _ghSlider = ghSlider;
            
            // 初始化范围和值
            _min = Math.Round((double)_ghSlider.Slider.Minimum);
            _max = Math.Round((double)_ghSlider.Slider.Maximum);
            _value = Math.Round((double)_ghSlider.Slider.Value);

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
            _autoUpdateTimer.Interval = TimeSpan.FromMilliseconds(50); // 50ms 的更新间隔
            _autoUpdateTimer.Tick += AutoUpdateTimer_Tick;

            this.Closed += (s, e) => 
            {
                _timer.Stop();
                _updateTimer.Stop();
                _autoUpdateTimer.Stop(); // 添加新计时器的停止
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

                // 检查最小值是否改变
                if (Math.Abs(_min - newMin) > double.Epsilon)
                {
                    Min = newMin;
                    rangeChanged = true;
                }

                // 检查最大值是否改变
                if (Math.Abs(_max - newMax) > double.Epsilon)
                {
                    Max = newMax;
                    rangeChanged = true;
                }

                // 检查值是否改变
                if (Math.Abs(_value - newValue) > double.Epsilon)
                {
                    Value = newValue;
                }
                // 如果范围改变了，确保值在新范围内
                else if (rangeChanged)
                {
                    Value = Math.Max(newMin, Math.Min(newMax, _value));
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
                // 确保值在范围内
                newValue = (int)Math.Max(Min, Math.Min(Max, newValue));
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
                // 左键点击时的行为
                Value = Math.Min(Max, Value + 1);
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
                // 左键点击时的行为
                Value = Math.Max(Min, Value - 1);
            }
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Min;
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Max;
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
                Value = Math.Min(Max, Value + 1);
            }
            else if (_isAutoDecrementing)
            {
                Value = Math.Max(Min, Value - 1);
            }
        }
    }
}
