using System.Windows;

namespace Motion.Toolbar
{
    public partial class MotionSliderSettingsWindow : Window
    {
        public delegate void FPSChangedHandler(int fps);
        public event FPSChangedHandler FPSChanged;

        public int CurrentFPS
        {
            get => int.TryParse(FPSTextBox.Text, out int fps) ? fps : 60;
            set => FPSTextBox.Text = value.ToString();
        }

        public MotionSliderSettingsWindow()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FPSTextBox.Text, out int fps) && fps > 0)
            {
                FPSChanged?.Invoke(fps);
                Close();
            }
            else
            {
                MessageBox.Show("请输入有效的正整数！", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
} 