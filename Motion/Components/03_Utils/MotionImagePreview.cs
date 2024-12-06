using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Motion.Utils
{
    public class MotionImagePreview : GH_Component
    {
        private List<GH_CustomPreviewItem> m_items;
        private BoundingBox m_clipbox;
        private string _ImageFolder;
        private string[] _ImageFiles;
        private readonly string _transparencyTempFile;
        private readonly string _environmentTempFile;

        private struct GH_CustomPreviewItem
        {
            public IGH_PreviewData m_obj;
            public DisplayMaterial m_mat;
            public int lineWeight;
        }

        public string ImageFolder
        {
            get { return _ImageFolder; }
            set 
            { 
                _ImageFolder = value;
                UpdateImageFiles();
            }
        }

        public MotionImagePreview()
            : base("MotionImagePreview", "MotionImagePreview",
                "图片预览(支持输入路径和System.Drawing.Bitmap，可配合Javid/Bitmap+插件使用)",
                "Motion", "03_Utils")
        {
            // 在构造函数中初始化临时文件路径
            _transparencyTempFile = Path.Combine(Path.GetTempPath(), "MotionTransparencyTex.png");
            _environmentTempFile = Path.Combine(Path.GetTempPath(), "MotionEnvironmentTex.png");
        }

        private Texture BitmapToTexture(Bitmap bitmap, bool isTransparency)
        {
            try
            {
                string tempPath = isTransparency ? _transparencyTempFile : _environmentTempFile;
                bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                
                var fileRef = new FileReference(
                    tempPath,           // absolutePath
                    tempPath,           // relativePath
                    ContentHash.CreateFromFile(tempPath),  // 直接使用构造函数
                    FileReferenceStatus.FullPathValid
                );

                var texture = new Texture
                {
                    FileReference = fileRef,
                    Enabled = true,
                };

                return texture;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert bitmap to texture: " + ex.Message);
                return null;
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "几何体", GH_ParamAccess.item);
            pManager.AddGenericParameter("Diffuse Map", "DM", "Diffuse贴图文件的路径或Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Transparency Map", "TM", "Transparency贴图文件的路径或Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Environment Map", "EM", "Environment贴图文件的路径或Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Bump Map", "BM", "Bump贴图文件的路径或Bitmap", GH_ParamAccess.item);
            pManager.AddColourParameter("Diffuse Color", "DC", "贴图基础色", GH_ParamAccess.item, Color.White);
            pManager.AddNumberParameter("Transparency", "T", "透明度(0-1)", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Image Index", "I", "如果Diffuse文件路径为文件夹路径，则读取指定序号的图片", GH_ParamAccess.item, 0);

            // 设置可选参数
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Current Image Path", "P", "当前使用的Diffuse贴图文件的路径", GH_ParamAccess.item);
        }

        private void UpdateImageFiles()
        {
            if (Directory.Exists(_ImageFolder))
            {
                // 获取所有图片文件并按文件名中的数字排序
                _ImageFiles = Directory.GetFiles(_ImageFolder, "*.*", SearchOption.TopDirectoryOnly)
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
            else if (File.Exists(_ImageFolder) && IsImageFile(_ImageFolder))
            {
                // 如果是单个图片文件，创建只包含这个文件的数组
                _ImageFiles = new string[] { _ImageFolder };
            }
            else
            {
                _ImageFiles = new string[0];
            }
        }

        private bool IsImageFile(string file)
        {
            // 检查文件扩展名
            string ext = Path.GetExtension(file).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || 
                   ext == ".bmp" || ext == ".tif" || ext == ".tiff" || 
                   ext == ".gif" || ext == ".hdr" || ext == ".hdri";
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0)
            {
                m_items = new List<GH_CustomPreviewItem>();
                m_clipbox = BoundingBox.Empty;
            }

            // 声明变量
            IGH_GeometricGoo geometry = null;
            object diffuseMap = null;
            object transparencyMap = null;
            object environmentMap = null;
            object bumpMap = null;
            Color diffuseColor = Color.White;
            double transparency = 0.0;
            int imageIndex = 0;  // 移到最后

            // 获取输入数据
            if (!DA.GetData(0, ref geometry)) return;
            if (!DA.GetData(1, ref diffuseMap)) return;
            DA.GetData(2, ref transparencyMap);
            DA.GetData(3, ref environmentMap);
            DA.GetData(4, ref bumpMap);
            DA.GetData(5, ref diffuseColor);
            DA.GetData(6, ref transparency);
            DA.GetData(7, ref imageIndex);  // 更新索引位置

            // 处理漫反射贴图
            if (diffuseMap != null)
            {
                if (diffuseMap is string diffusePath && File.Exists(diffusePath))
                {
                    // 如果��文件路径
                    if (IsImageFile(diffusePath))
                    {
                        _ImageFolder = Path.GetDirectoryName(diffusePath);
                        _ImageFiles = new string[] { diffusePath };
                    }
                    else if (Directory.Exists(diffusePath))
                    {
                        _ImageFolder = diffusePath;
                        UpdateImageFiles();
                    }
                }
                else if (diffuseMap is Bitmap diffuseBitmap)
                {
                    // 如果是Bitmap，保存到临文件
                    string tempPath = Path.Combine(Path.GetTempPath(), "MotionDiffuseTex.png");
                    diffuseBitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                    _ImageFolder = Path.GetDirectoryName(tempPath);
                    _ImageFiles = new string[] { tempPath };
                }
                else if (diffuseMap is GH_String diffuseGhString)
                {
                    string path = diffuseGhString.Value;
                    if (File.Exists(path) && IsImageFile(path))
                    {
                        _ImageFolder = Path.GetDirectoryName(path);
                        _ImageFiles = new string[] { path };
                    }
                    else if (Directory.Exists(path))
                    {
                        _ImageFolder = path;
                        UpdateImageFiles();
                    }
                }
                else if (diffuseMap is IGH_Goo)
                {
                    var goo = diffuseMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        string tempPath = Path.Combine(Path.GetTempPath(), "MotionDiffuseTex.png");
                        bmp.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                        _ImageFolder = Path.GetDirectoryName(tempPath);
                        _ImageFiles = new string[] { tempPath };
                    }
                }
            }

            DisplayMaterial material = new DisplayMaterial();

            // 设置基本材质属性
            material.Diffuse = diffuseColor;
            material.Transparency = transparency;

            // 设置漫反射贴图
            if (_ImageFiles != null && _ImageFiles.Length > 0)
            {
                if (imageIndex < 0)
                {
                    // 处理负数索引
                    imageIndex = _ImageFiles.Length - (-imageIndex % _ImageFiles.Length);
                }
                // 使用取模运算确保索引在有效范围内
                int actualIndex = imageIndex % _ImageFiles.Length;
                string imagePath = _ImageFiles[actualIndex];
                material.SetBitmapTexture(imagePath, true);
                
                // 添加输出
                DA.SetData(0, imagePath);
            }

            // 设置透明度贴图
            if (transparencyMap != null)
            {
                if (transparencyMap is string transPath && File.Exists(transPath))
                {
                    var fileRef = new FileReference(
                        transPath,          // absolutePath
                        transPath,          // relativePath
                        ContentHash.CreateFromFile(transPath),  // 直接使用构造函数
                        FileReferenceStatus.FullPathValid
                    );
                    var texture = new Texture { FileReference = fileRef };
                    material.SetTransparencyTexture(texture, true);
                }
                else if (transparencyMap is Bitmap transBitmap)
                {
                    var texture = BitmapToTexture(transBitmap, true);
                    if (texture != null)
                    {
                        material.SetTransparencyTexture(texture, true);
                    }
                }
                else if (transparencyMap is GH_String transGhString)
                {
                    string path = transGhString.Value;
                    if (File.Exists(path))
                    {
                        var fileRef = new FileReference(
                            path,            // absolutePath
                            path,            // relativePath
                            ContentHash.CreateFromFile(path),  // 直接使用构造函数
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        material.SetTransparencyTexture(texture, true);
                    }
                }
                else if (transparencyMap is IGH_Goo)
                {
                    var goo = transparencyMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        var texture = BitmapToTexture(bmp, true);
                        if (texture != null)
                        {
                            material.SetTransparencyTexture(texture, true);
                        }
                    }
                }
            }

            // 设置环境贴图
            if (environmentMap != null)
            {
                if (environmentMap is string envPath && File.Exists(envPath))
                {
                    string ext = Path.GetExtension(envPath).ToLower();
                    if (ext == ".hdr" || ext == ".hdri")
                    {
                        // 对于 HDR 文件使用特殊的处理
                        var fileRef = new FileReference(
                            envPath,            // absolutePath
                            envPath,            // relativePath
                            ContentHash.CreateFromFile(envPath),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture 
                        { 
                            FileReference = fileRef,
                            Enabled = true,
                            TextureType = TextureType.Emap  // 确保正确处理 HDR 格式
                        };
                        material.SetEnvironmentTexture(texture, true);
                    }
                    else
                    {
                        // 对于普通图片文件的处理保持不变
                        var fileRef = new FileReference(
                            envPath,
                            envPath,
                            ContentHash.CreateFromFile(envPath),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        material.SetEnvironmentTexture(texture, true);
                    }
                }
                else if (environmentMap is Bitmap envBitmap)
                {
                    var texture = BitmapToTexture(envBitmap, false);
                    if (texture != null)
                    {
                        material.SetEnvironmentTexture(texture, true);
                    }
                }
                else if (environmentMap is GH_String envGhString)
                {
                    string path = envGhString.Value;
                    if (File.Exists(path))
                    {
                        var fileRef = new FileReference(
                            path,            // absolutePath
                            path,            // relativePath
                            ContentHash.CreateFromFile(path),  // 直接使用构造函数
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        material.SetEnvironmentTexture(texture, true);
                    }
                }
                else if (environmentMap is IGH_Goo)
                {
                    var goo = environmentMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        var texture = BitmapToTexture(bmp, false);
                        if (texture != null)
                        {
                            material.SetEnvironmentTexture(texture, true);
                        }
                    }
                }
            }

            // 设置凹凸贴图
            if (bumpMap != null)
            {
                if (bumpMap is string bumpPath && File.Exists(bumpPath))
                {
                    var fileRef = new FileReference(
                        bumpPath,            // absolutePath
                        bumpPath,            // relativePath
                        ContentHash.CreateFromFile(bumpPath),  // 直接使用构造函数
                        FileReferenceStatus.FullPathValid
                    );
                    var texture = new Texture { FileReference = fileRef };
                    material.SetBumpTexture(texture, true);
                }
                else if (bumpMap is Bitmap bumpBitmap)
                {
                    var texture = BitmapToTexture(bumpBitmap, false);
                    if (texture != null)
                    {
                        material.SetBumpTexture(texture, true);
                    }
                }
                else if (bumpMap is GH_String bumpGhString)
                {
                    string path = bumpGhString.Value;
                    var fileRef = new FileReference(
                        path,            // absolutePath
                        path,            // relativePath
                        ContentHash.CreateFromFile(path),  // 直接使用构造函数
                        FileReferenceStatus.FullPathValid
                    );
                    var texture = new Texture { FileReference = fileRef };
                    material.SetBumpTexture(texture, true);
                }
                else if (bumpMap is IGH_Goo)
                {
                    var goo = bumpMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        var texture = BitmapToTexture(bmp, false);
                        if (texture != null)
                        {
                            material.SetBumpTexture(texture, true);
                        }
                    }
                }
            }

            if (geometry.IsValid)
            {
                if (!(geometry is IGH_PreviewData))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, 
                        geometry.TypeName + " does not support previews");
                    return;
                }

                GH_CustomPreviewItem item = new GH_CustomPreviewItem
                {
                    m_obj = (IGH_PreviewData)geometry,
                    m_mat = material,
                    lineWeight = -1
                };

                m_items.Add(item);
                m_clipbox.Union(geometry.Boundingbox);
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (Attributes.Selected)
            {
                foreach (var item in m_items)
                {
                    var meshArgs = new GH_PreviewMeshArgs(args.Viewport, args.Display,
                        args.ShadeMaterial_Selected, args.MeshingParameters);
                    item.m_obj.DrawViewportMeshes(meshArgs);
                }
            }
            else
            {
                foreach (var item in m_items)
                {
                    var meshArgs = new GH_PreviewMeshArgs(args.Viewport, args.Display,
                        item.m_mat, args.MeshingParameters);
                    item.m_obj.DrawViewportMeshes(meshArgs);
                }
            }
        }

        public override void ClearData()
        {
            base.ClearData();
            m_items = null;
            m_clipbox = BoundingBox.Empty;
        }

        

        public override void RemovedFromDocument(GH_Document document)
        {
            try
            {
                // 删除定的临时文件
                if (File.Exists(_transparencyTempFile))
                {
                    File.Delete(_transparencyTempFile);
                }
                if (File.Exists(_environmentTempFile))
                {
                    File.Delete(_environmentTempFile);
                }
            }
            catch { }

            base.RemovedFromDocument(document);
        }

        public override BoundingBox ClippingBox => m_clipbox;
        protected override Bitmap Icon => Properties.Resources.MotionImagePreview;
        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("63210FC2-D925-4BCB-B0E6-3E8D0C0A21E7");
    }
} 