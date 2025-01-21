using Grasshopper;
using Motion.Parameters;
using System;
using System.Drawing;
using System.Linq;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        // 添加方法来查找现有的 Motion Value 参数
        private MotionTimelineValueParam FindExistingMotionValue()
        {
            if (Owner?.Document == null) return null;

            // 查找文档中所有的 MotionValueParameter 实例
            return Owner.Document.Objects
                .OfType<MotionTimelineValueParam>()
                .FirstOrDefault();
        }

        // 修改 Menu_AddMotionValue 方法
        private void Menu_AddMotionValue(object sender, EventArgs e)
        {
            if (Owner?.Document == null) return;

            try
            {
                // 先查找是否已存在 Motion Value 参数
                _valueParam = FindExistingMotionValue();

                // 如果不存在，则创建新的
                if (_valueParam == null)
                {
                    _valueParam = new MotionTimelineValueParam();
                    _valueParam.CreateAttributes();
                    _valueParam.NickName = "Motion Value";

                    // 获取当前视图中心
                    var canvas = Instances.ActiveCanvas;
                    var viewport = canvas.Viewport;

                    // 设置参数位置（在widget右侧）
                    _valueParam.Attributes.Pivot = viewport.UnprojectPoint(new PointF(canvas.Width / 2, canvas.Height / 2));

                    // 添加参数到文档
                    Owner.Document.AddObject(_valueParam, false);

                    // 立即更新值
                    UpdateCurrentValue();

                    // 强制更新解决方案
                    Owner.Document.NewSolution(true);

                    // 调试输出
                    Rhino.RhinoApp.WriteLine("Created new Motion Value parameter");
                }
                else
                {
                    Rhino.RhinoApp.WriteLine("Found existing Motion Value parameter");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error creating Motion Value parameter: {ex.Message}");
            }
        }
    }
}
