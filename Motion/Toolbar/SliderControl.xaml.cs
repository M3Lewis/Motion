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
        private const int UPDATE_DELAY = 16; // 约60fps
        private DateTime _lastUpdateTime = DateTime.Now;
        private bool _isDragging = false;
        private double _pendingValue = 0;
        private System.Windows.Threading.DispatcherTimer _updateTimer;
        private System.Windows.Threading.DispatcherTimer _buttonRepeatTimer;
        private System.Windows.Controls.Button _currentButton;

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
                    
                    // 如果不是正在拖动，立即更新
                    if (!_isDragging)
                    {
                        UpdateGHSlider(roundedValue);
                    }
                    
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public SliderControlWPF(GH_NumberSlider ghSlider)
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

            this.Closed += (s, e) => 
            {
                _timer.Stop();
                _updateTimer.Stop();
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
            Value = e.NewValue;
            
            // 如果正在拖动，启动更新计时器
            if (_isDragging)
            {
                _updateTimer.Stop();
                _updateTimer.Start();
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
            if ((DateTime.Now - _lastUpdateTime).TotalMilliseconds < UPDATE_DELAY) return;
            Value = Math.Min(Max, Value + 1);
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if ((DateTime.Now - _lastUpdateTime).TotalMilliseconds < UPDATE_DELAY) return;
            Value = Math.Max(Min, Value - 1);
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
                UpdateGHSlider(_pendingValue);
            }
        }

        private void UpdateGHSlider(double value)
        {
            if (_ghSlider == null || _updatingFromTimer) return;

            var now = DateTime.Now;
            if ((now - _lastUpdateTime).TotalMilliseconds < UPDATE_DELAY) return;

            _ghSlider.Slider.Value = (decimal)value;
            _ghSlider.ExpireSolution(true);
            _lastUpdateTime = now;
        }

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _currentButton = sender as System.Windows.Controls.Button;
            
            if (_buttonRepeatTimer == null)
            {
                _buttonRepeatTimer = new System.Windows.Threading.DispatcherTimer();
                _buttonRepeatTimer.Tick += ButtonRepeatTimer_Tick;
            }
            
            // 每次按下时都重新设置为初始延迟
            _buttonRepeatTimer.Interval = TimeSpan.FromMilliseconds(750);
            _buttonRepeatTimer.Start();
        }

        private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _buttonRepeatTimer?.Stop();
            _currentButton = null;
        }

        private void ButtonRepeatTimer_Tick(object sender, EventArgs e)
        {
            // 第一次触发后改为快速重复
            _buttonRepeatTimer.Interval = TimeSpan.FromMilliseconds(50);
            
            if (_currentButton == plusButton)
            {
                Value = Math.Min(Max, Value + 1);
            }
            else if (_currentButton == minusButton)
            {
                Value = Math.Max(Min, Value - 1);
            }
        }
    }
}
