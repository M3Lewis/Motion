using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using Grasshopper;

namespace Motion.Settings
{
    public class MotionSettings : INotifyPropertyChanged
    {
        private static MotionSettings _Instance;

        private bool _TimelineWidgetToggle;

        public static MotionSettings Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new MotionSettings();
                }
                return _Instance;
            }
        }

        public bool TimelineWidgetToggle
        {
            get
            {
                return _TimelineWidgetToggle;
            }
            set
            {
                _TimelineWidgetToggle = value;
                OnPropertyChanged("TimelineWidgetToggle");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MotionSettings()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            _TimelineWidgetToggle = Instances.Settings.GetValue("Motion:TimelineWidgetToggle", @default: true);
            NotifyAll();
        }

        public void ResetSettings()
        {
            Instances.Settings.DeleteValue("Motion:TimelineWidgetToggle");
            LoadSettings();
            SaveSettings();
        }

        private void NotifyAll()
        {
            OnPropertyChanged("TimelineWidgetToggle");
        }

        public void SaveSettings()
        {
            Instances.Settings.SetValue("Motion:TimelineWidgetToggle", TimelineWidgetToggle);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}