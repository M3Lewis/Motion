using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Ribbon;

namespace Motion.Toolbar
{
    public enum ToolbarPosition
    {
        Top,
        Left,
        Right,
        Bottom,
        OnToolbar,
    }

    public class CustomMotionToolbar : ToolStrip
    {
        private static CustomMotionToolbar _instance;
        private ToolbarPosition _currentPosition = ToolbarPosition.Top;
        public ToolbarPosition CurrentPosition => _currentPosition;
        private Point _floatingLocation = new Point(100, 100);
        private bool _isPositionLocked = false;

        // 单例模式获取工具栏实例
        public static CustomMotionToolbar Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CustomMotionToolbar();
                }
                return _instance;
            }
        }

        private CustomMotionToolbar()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
               ControlStyles.ResizeRedraw |
               ControlStyles.SupportsTransparentBackColor, true);
            this.AutoSize = true;
            this.ImageScalingSize = new Size(24, 24);  // Keep image size consistent
            this.Padding = new Padding(1);  // Small padding
            this.Renderer = new ToolStripProfessionalRenderer(new CustomToolStripColorTable());
            this.GripStyle = ToolStripGripStyle.Hidden;
            this.Dock = DockStyle.Left;
            this.BackColor = Color.FromArgb(128, 240, 240, 240);
            this.ShowItemToolTips = true;
            this.AllowItemReorder = true;
            this.Width = 30;

            // 初始设置
            SetPosition(ToolbarPosition.Left);

            // 添加到Grasshopper界面
            AttachToGrasshopperInterface();
        }


        private void AttachToGrasshopperInterface()
        {
            if (Instances.DocumentEditor != null)
            {
                GH_DocumentEditor editor = Instances.DocumentEditor;
                // 使用更安全的方式添加工具栏
                if (editor.Controls.Count > 0)
                {
                    editor.Controls.Add(this);
                    // 确保工具栏在Z顺序中是最前面的
                    this.BringToFront();
                }
            }
        }

        public void SetPosition(ToolbarPosition position)
        {
            if (_isPositionLocked) return;

            _currentPosition = position;

            switch (position)
            {
                case ToolbarPosition.Top:
                    this.Dock = DockStyle.None; // 取消停靠
                    UpdateTopPosition();
                    _currentPosition = ToolbarPosition.Top;
                    break;
                case ToolbarPosition.Left:
                    this.Dock = DockStyle.Left;
                    _currentPosition = ToolbarPosition.Left;
                    break;
                case ToolbarPosition.Right:
                    this.Dock = DockStyle.Right;
                    _currentPosition = ToolbarPosition.Right;
                    break;
                case ToolbarPosition.Bottom:
                    this.Dock = DockStyle.Bottom;
                    _currentPosition = ToolbarPosition.Bottom;
                    break;
                case ToolbarPosition.OnToolbar:
                    MoveToGrasshopperToolbar();
                    _currentPosition = ToolbarPosition.OnToolbar;
                    break;
            }
        }

        private void UpdateTopPosition()
        {
            if (this.Parent != null)
            {
                var editor = Instances.DocumentEditor;
                Type typeFromHandle = typeof(GH_DocumentEditor);
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
                FieldInfo field = typeFromHandle.GetField("_Ribbon", bindingAttr);
                GH_Ribbon ribbon = (GH_Ribbon)RuntimeHelpers.GetObjectValue(field.GetValue(Instances.DocumentEditor));

                ToolStrip toolStrip = (ToolStrip)GetGrasshopperToolbar();
                int toolbarHeight = ribbon.Bottom+toolStrip.Bounds.Height+10;
                this.Location = new Point(0, toolbarHeight);
                this.Width = this.Parent.Width;
                this.Height = this.PreferredSize.Height;
            }
        }

        private void MoveToGrasshopperToolbar()
        {
            this.Visible = false;

            ToolStrip grasshopperToolbar = GetGrasshopperToolbar();
            if (grasshopperToolbar != null)
            {
                while (this.Items.Count > 0)
                {
                    ToolStripItem item = this.Items[0];
                    this.Items.RemoveAt(0);
                    grasshopperToolbar.Items.Add(item);
                }
            }
        }

        private ToolStrip GetGrasshopperToolbar()
        {
            var editor = Instances.DocumentEditor;
            if (editor == null) return null;

            Type typeFromHandle = typeof(GH_DocumentEditor);
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
            FieldInfo field = typeFromHandle.GetField("_CanvasToolbar", bindingAttr);
            object objectValue = RuntimeHelpers.GetObjectValue(field.GetValue(Instances.DocumentEditor));
            if (objectValue == null) return null;

            return (ToolStrip)objectValue;
        }

        // 添加锁定位置的方法
        public void SetPositionLock(bool isLocked)
        {
            _isPositionLocked = isLocked;
            this.GripStyle = _isPositionLocked ? ToolStripGripStyle.Hidden : ToolStripGripStyle.Visible;
        }
    }

    // 自定义颜色表，使工具栏看起来更像Grasshopper风格
    public class CustomToolStripColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Color.FromArgb(40,0, 240, 240);
        public override Color ToolStripGradientMiddle => Color.FromArgb(40, 230, 230, 230);
        public override Color ToolStripGradientEnd => Color.FromArgb(40, 220, 220, 220);
        public override Color ButtonSelectedHighlight => Color.FromArgb(40, 200, 220, 255);
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(40, 210, 230, 255);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(40, 180, 210, 255);
    }
}
