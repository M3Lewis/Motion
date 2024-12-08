using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Motion.Utils
{
    public class MotionImageSelector : GH_Component
    {
        private string[] _imageFiles;

        public MotionImageSelector()
            : base("Motion Image Selector", "ImgSelect",
                "从文件夹或文件路径中选择图片",
                "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_FilePath(), "Path", "P", "图片文件路径或文件夹路径", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Index", "I", "如果输入为文件夹路径，则选择指定序号的图片", GH_ParamAccess.item, 0);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Image Path", "P", "选中的图片路径", GH_ParamAccess.item);
            pManager.AddTextParameter("All Paths", "AP", "文件夹中所有图片的路径", GH_ParamAccess.list);
        }

        private bool IsImageFile(string file)
        {
            if (string.IsNullOrEmpty(file)) return false;
            
            // 检查文件扩展名
            string ext = Path.GetExtension(file).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                   ext == ".bmp" || ext == ".tif" || ext == ".tiff" ||
                   ext == ".gif" || ext == ".hdr" || ext == ".hdri";
        }

        private void UpdateImageFiles(string path)
        {
            if (Directory.Exists(path))
            {
                // 获取所有图片文件并按文件名中的数字排序
                _imageFiles = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => IsImageFile(file))
                    .OrderBy(file =>
                    {
                        // 从文件名中提取数字
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string numbers = new string(fileName.Where(c => char.IsDigit(c)).ToArray());

                        // 如果成功解析为数字则使用该数字，否则使用最大值（确保非数字文件名排在最后）
                        return int.TryParse(numbers, out int num) ? num : int.MaxValue;
                    })
                    .ToArray();
            }
            else if (File.Exists(path) && IsImageFile(path))
            {
                // 如果是单个图片文件，创建只包含这个文件的数组
                _imageFiles = new string[] { path };
            }
            else
            {
                _imageFiles = new string[0];
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = string.Empty;
            int index = 0;

            if (!DA.GetData(0, ref path)) return;
            DA.GetData(1, ref index);

            // 更新图片文件列表
            UpdateImageFiles(path);

            // 检查是否找到任何图片
            if (_imageFiles.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "未找到有效的图片文件");
                return;
            }

            // 处理索引
            if (index < 0)
            {
                index = _imageFiles.Length - (-index % _imageFiles.Length);
            }
            int actualIndex = index % _imageFiles.Length;

            // 输出选中的图片路径
            DA.SetData(0, _imageFiles[actualIndex]);
            // 输出所有图片路径
            DA.SetDataList(1, _imageFiles);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("B6C3D245-8E6F-4A47-9F9A-123456789ABC");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
    }
} 