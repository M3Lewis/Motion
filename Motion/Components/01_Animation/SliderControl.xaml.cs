using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Grasshopper.Kernel.Special;
using System.ComponentModel;
using System;

namespace Motion.Components
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
                    if (_ghSlider != null)
                    {
                        if (!_updatingFromTimer)
                        {
                            _ghSlider.Slider.Value = (decimal)roundedValue;
                            _ghSlider.ExpireSolution(true);
                        }
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
            slider.ValueChanged += Slider_ValueChanged;

            // 设置窗口置顶
            this.Topmost = true;

            // 初始化定时器
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100); // 100ms 检查一次
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 窗口关闭时停止定时器
            this.Closed += (s, e) => _timer.Stop();
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
            Value = Math.Min(Max, Value + 1);
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
