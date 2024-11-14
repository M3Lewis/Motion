using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using static System.Net.Mime.MediaTypeNames;

namespace Motion
{
    public static class ViewSwitchButton : 
    {
        private readonly ToolStrip toolStrip;
        private readonly ToolStripButton button;
        private List<string> namedViews = new List<string>();
        private int currentViewIndex = 0;
        private bool isActive = false;

        public ViewSwitchButton()
        {
            button = new System.Windows.Forms.ToolStripButton();
            Instantiate();
            LoadNamedViews();
        }
    }
}