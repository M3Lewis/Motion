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

namespace Motion
{
    public class ZDepthComponent : GH_Component
    {
        private bool currentShowBufferState = false;
        public ZDepthComponent()
          : base("ZDepth", "ZDepth",
            "计算Rhino Viewport 的ZDepth",
            "Motion", "03_Util")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Show Buffer?", "Show?", "Show Z Buffer", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save Buffer?", "Save?", "Save Z Buffer image", GH_ParamAccess.item);
            pManager.AddTextParameter("File path", "Path", "File path", GH_ParamAccess.item, Environment.ExpandEnvironmentVariables("%userprofile%\\Desktop"));
            pManager[1].Optional = true;
            pManager[2].Optional = true;
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

            double oMin = 1000000;
            double oMax = 0;
            double oAverage = 0;

            double distanceAdd = 0;
            int count = 0;

            DA.GetData(0, ref iShowBuffer);
            DA.GetData(1, ref iSaveBuffer);
            DA.GetData(2, ref iFilePath);

            var view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            var cameraPoint = view.CameraLocation;
            var buffer = new Rhino.Display.ZBufferCapture(view);

            for (int i = 0; i < view.Size.Width; i++)
            {
                for (int j = 0; j < view.Size.Height; j++)
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
            Bitmap bufferImage = buffer.GrayscaleDib();

            // 确保路径末尾有分隔符
            if (!iFilePath.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !iFilePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                iFilePath = iFilePath + Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(Path.GetDirectoryName(iFilePath))) // 修改这里
            {
                Directory.CreateDirectory(Path.GetDirectoryName(iFilePath)); // 修改这里
            }
            try // 添加错误处理
            {
                // 确保文件路径包含文件名和扩展名
                string fullPath = iFilePath.EndsWith(".png") ? iFilePath : Path.Combine(iFilePath, "zbuffer.png");
                bufferImage.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                // 处理可能的错误
                Rhino.RhinoApp.WriteLine("保存图片时出错: " + ex.Message);
            }
            finally
            {
                bufferImage.Dispose(); // 确保释放资源
            }
        }
        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("93c51e48-2e19-49cc-a4bb-87d7e44e502d");
    }
}