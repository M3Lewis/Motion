using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System.Linq;
using Grasshopper.GUI.Canvas;
using System.Drawing;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using Motion.General;
using Rhino;

namespace Motion.Toolbar
{
    public enum ToolbarType
    {
        GrasshopperDefault,
        CustomMotion,
    }

    public abstract class MotionToolbarButton : GH_AssemblyPriority
    {
        public virtual int ToolbarOrder => 0;

        // 默认使用悬浮工具栏
        protected virtual ToolbarType PreferredToolbarType => ToolbarType.CustomMotion;

        protected void InitializeToolbarGroup()
        {
            MotionToolbarManager.InitializeToolbarGroup(PreferredToolbarType);
        }

        // 使用委托来保存原始按钮的点击事件处理程序
        private void Button_Click(object sender, EventArgs e)
        {
            // 当克隆按钮被点击时，我们将调用OnButtonClicked方法
            OnButtonClicked(sender, e);
        }

        // 子类可以重写此方法来处理按钮点击
        protected virtual void OnButtonClicked(object sender, EventArgs e) { }

        public virtual void UpdateLanguage() { }

        protected ToolStripItem MyButton { get; set; }

        protected void AddButtonToToolbars(ToolStripItem originalButton)
        {
            MyButton = originalButton;
            MotionToolbarManager.AddButtonToToolbars(originalButton, PreferredToolbarType, ToolbarOrder, this);
        }

        protected void ShowTemporaryMessage(GH_Canvas canvas, string message)
        {
            MotilityUtils.ShowTemporaryMessageAtTop(canvas, message);
        }
    }
}
