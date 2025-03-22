using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class CameraPointsButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 150; // 设置为工具栏按钮的最后一个

        public CameraPointsButton()
        {
        }

        private void AddCameraPointsButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToToolbars(button);
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor editor = Instances.DocumentEditor;
            if (editor == null) return;
            AddCameraPointsButton();
        }

        private void Instantiate()
        {
            button.Name = "Camera Points";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.CameraPointsButton; // 需要添加相机图标资源
            button.ToolTipText = "在画布上放置两个点参数，分别表示当前Rhino窗口的摄像机起始点和目标点";
            button.Click += CreateCameraPoints;
        }

        private void CreateCameraPoints(object sender, EventArgs e)
        {
            try
            {
                var doc = Instances.ActiveCanvas.Document;
                if (doc == null) return;

                // 获取当前Rhino视图
                var view = RhinoDoc.ActiveDoc.Views.ActiveView;
                if (view == null)
                {
                    MessageBox.Show("无法获取当前Rhino视图。",
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 获取摄像机位置和目标点
                var cameraLocation = view.ActiveViewport.CameraLocation;
                var cameraTarget = view.ActiveViewport.CameraTarget;

                // 创建两个点参数
                var cameraLocationParam = new Param_Point();
                var cameraTargetParam = new Param_Point();

                // 设置参数名称
                cameraLocationParam.NickName = "摄像机位置";
                cameraTargetParam.NickName = "摄像机目标";
                cameraLocationParam.CreateAttributes();
                cameraTargetParam.CreateAttributes();

                // 设置参数值
                cameraLocationParam.PersistentData.Clear();
                cameraLocationParam.PersistentData.Append(new GH_Point(cameraLocation));

                cameraTargetParam.PersistentData.Clear();
                cameraTargetParam.PersistentData.Append(new GH_Point(cameraTarget));

                // 获取当前视图中心
                var canvas = Instances.ActiveCanvas;
                var viewport = canvas.Viewport;
                var centerPoint = viewport.UnprojectPoint(
                    new PointF(canvas.Width / 2, canvas.Height / 2));


                // 设置参数位置
                cameraLocationParam.Attributes.Pivot = new PointF(
                    centerPoint.X, centerPoint.Y - 30);
                cameraTargetParam.Attributes.Pivot = new PointF(
                    centerPoint.X, centerPoint.Y + 30);

                // 添加到文档
                doc.AddObject(cameraLocationParam, false);
                doc.AddObject(cameraTargetParam, false);

                // 显示成功消息
                ShowTemporaryMessage(canvas, "已添加摄像机位置和目标点参数");

                // 刷新文档
                doc.NewSolution(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建摄像机点参数时出错: {ex.Message}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}