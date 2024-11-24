using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;
using System.Drawing;
using System.IO;

namespace Motion.Utils
{
    public class ZDepthComponent : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.ZDepth;
        public override Guid ComponentGuid => new Guid("93c51e48-2e19-49cc-a4bb-87d7e44e502d");

        private bool currentShowBufferState = false;
        public ZDepthComponent()
          : base("ZDepth", "ZDepth",
            "计算Rhino Viewport 的ZDepth",
            "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Show Buffer?", "Show?", "Show Z Buffer", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save Buffer?", "Save?", "Save Z Buffer image", GH_ParamAccess.item);
            pManager.AddTextParameter("File path", "Path", "File path", GH_ParamAccess.item, Environment.ExpandEnvironmentVariables("%userprofile%\\Desktop"));
            pManager.AddNumberParameter("Scale", "S", "输出图片的放大倍数", GH_ParamAccess.item, 2.0);
            pManager.AddTextParameter("View Name", "V", "视图名称，默认使用当前视图", GH_ParamAccess.item, "Perspective");
            pManager.AddIntegerParameter("Index", "I", "导出文件的序号", GH_ParamAccess.item, 0);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Max", "Max", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min", "Min", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Average", "Average", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iShowBuffer = false;
            bool iSaveBuffer = false;
            string iFilePath = "";
            double scale = 2.0;  // 默认2倍
            string viewName = "Perspective";  // 默认视图名称
            int index = 0;  // 添加索引变量

            double oMin = 1000000;
            double oMax = 0;
            double oAverage = 0;

            double distanceAdd = 0;
            int count = 0;

            DA.GetData(0, ref iShowBuffer);
            DA.GetData(1, ref iSaveBuffer);
            DA.GetData(2, ref iFilePath);
            DA.GetData(3, ref scale);
            DA.GetData(4, ref viewName);
            DA.GetData(5, ref index);  // 获取索引值

            // 确保放大倍数至少为1
            scale = Math.Max(1.0, scale);

            // 获取指定视图或当前活动视图
            var doc = Rhino.RhinoDoc.ActiveDoc;
            var view = string.IsNullOrWhiteSpace(viewName) ? 
                doc.Views.ActiveView : 
                doc.Views.Find(viewName, false);

            // 如果找不到指定视图，使用活动视图并发出警告
            if (view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"找不到名为 '{viewName}' 的视图，将使用当前活动视图。");
                view = doc.Views.ActiveView;
            }

            var viewport = view.ActiveViewport;
            var cameraPoint = viewport.CameraLocation;
            var buffer = new Rhino.Display.ZBufferCapture(viewport);

            // 计算深度数据
            for (int i = 0; i < viewport.Size.Width; i++)
            {
                for (int j = 0; j < viewport.Size.Height; j++)
                {
                    var distance = buffer.WorldPointAt(i, j).DistanceTo(cameraPoint);
                    if (distance == 0) return;

                    count++;
                    distanceAdd += distance;
                    if (distance < oMin)
                    {
                        oMin = distance;
                    }
                    if (distance > oMax)
                    {
                        oMax = distance;
                    }
                }
            }

            oAverage = distanceAdd / count;
            DA.SetData(0, oMin);
            DA.SetData(1, oMax);
            DA.SetData(2, oAverage);

            if (iShowBuffer != currentShowBufferState)
            {
                Rhino.RhinoApp.RunScript("ShowZBuffer", false);
                currentShowBufferState = iShowBuffer;
            }

            if (!iSaveBuffer) return;

            try
            {
                // 保存原始视窗大小
                var originalSize = viewport.Size;
                
                // 临时将视窗大小放大指定倍数
                viewport.Size = new System.Drawing.Size(
                    (int)(originalSize.Width * scale),
                    (int)(originalSize.Height * scale)
                );
                
                // 刷新视图以应用新的大小
                view.Redraw();
                
                // 使用新的大小创建缓冲区并捕获图像
                using (var largeBuffer = new Rhino.Display.ZBufferCapture(viewport))
                {
                    Bitmap bufferImage = largeBuffer.GrayscaleDib();

                    // 确保路径末尾有分隔符
                    if (!iFilePath.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                        !iFilePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                    {
                        iFilePath = iFilePath + Path.DirectorySeparatorChar;
                    }

                    if (!Directory.Exists(Path.GetDirectoryName(iFilePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(iFilePath));
                    }

                    // 使用索引构建文件名
                    string fileName = string.Format("zbuffer_{0}.png", index);
                    string fullPath = Path.Combine(iFilePath, fileName);

                    bufferImage.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                    bufferImage.Dispose();
                }

                // 恢复原始视窗大小
                viewport.Size = originalSize;
                view.Redraw();
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "保存图片时出错: " + ex.Message);
            }
        }
        
    }
}