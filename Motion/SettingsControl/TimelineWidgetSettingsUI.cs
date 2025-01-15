using Grasshopper.GUI;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Motion.SettingsControls
{
    public class TimelineWidgetSettingsUI : IGH_SettingFrontend
    {
        public string Category => "Motion";

        public IEnumerable<string> Keywords => new string[2] { "Widget", "Timeline" };

        public string Name => "Timeline";

        public Control SettingsUI()
        {
            return new TimelineWidgetSettingFrontEnd();
        }

        Control IGH_SettingFrontend.SettingsUI()
        {
            return this.SettingsUI();
        }
    }
}