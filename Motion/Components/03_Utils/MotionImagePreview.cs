using GH_IO.Types;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Components;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Microsoft.VisualBasic;
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
            : base("Motion Image Preview", "Motion Image Preview",
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
            pManager.AddGenericParameter("Diffuse Map", "DM", "Diffuse贴图文件的路径或System.Drawing.Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Environment Map", "EM", "环境贴图", GH_ParamAccess.item);
            Param_OGLShader param_OGLShader = new Param_OGLShader();
            param_OGLShader.SetPersistentData(new GH_Material(Color.Plum));
            pManager.AddParameter(param_OGLShader, "Material", "M", "The material override", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Image Index", "I", "如果Diffuse文件路径为文件夹路径，则读取指定序号的图片", GH_ParamAccess.item, 0);
            
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Current Image Path", "P", "当前使用的Diffuse贴图文件的路径", GH_ParamAccess.item);
            pManager.AddGenericParameter("Material", "M", "渲染材质", GH_ParamAccess.item);
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

            IGH_GeometricGoo geometry = null;
            object diffuseMap = null;
            object environmentMap = null;
            object materialObj = null;
            int imageIndex = 0;

            if (!DA.GetData(0, ref geometry)) return;
            DA.GetData(1, ref diffuseMap);
            DA.GetData(2, ref environmentMap);
            DA.GetData(3, ref materialObj);
            DA.GetData(4, ref imageIndex);

            GH_Material ghMaterialObj = materialObj as GH_Material;

            // 处理 DiffuseMap（保持原有的处理逻辑）
            if (diffuseMap != null)
            {
                if (diffuseMap is string diffusePath && File.Exists(diffusePath))
                {
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
                    // 如果是Bitmap，保存到临时文件
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

            // 处理漫反射贴图
            if (diffuseMap != null)
            {
                if (diffuseMap is string diffusePath && File.Exists(diffusePath))
                {
                    // 如果文件路径
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
                ghMaterialObj.Value.SetBitmapTexture(imagePath, true);

                // 添加输出
                DA.SetData(0, imagePath);
            }

            // 处理环境贴图
            if (environmentMap != null)
            {
                if (environmentMap is string envPath && File.Exists(envPath))
                {
                    string ext = Path.GetExtension(envPath).ToLower();
                    if (ext == ".hdr" || ext == ".hdri")
                    {
                        // 对于 HDR 文件使用特殊的处理
                        var fileRef = new FileReference(
                            envPath,
                            envPath,
                            ContentHash.CreateFromFile(envPath),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture
                        {
                            FileReference = fileRef,
                            Enabled = true,
                            TextureType = TextureType.Emap
                        };
                        ghMaterialObj.Value.SetEnvironmentTexture(texture, true);
                    }
                    else
                    {
                        var fileRef = new FileReference(
                            envPath,
                            envPath,
                            ContentHash.CreateFromFile(envPath),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        ghMaterialObj.Value.SetEnvironmentTexture(texture, true);
                    }
                }
                else if (environmentMap is Bitmap envBitmap)
                {
                    var texture = BitmapToTexture(envBitmap, true);
                    if (texture != null)
                    {
                        ghMaterialObj.Value.SetEnvironmentTexture(texture, true);
                    }
                }
                else if (environmentMap is GH_String envGhString)
                {
                    string path = envGhString.Value;
                    if (File.Exists(path))
                    {
                        var fileRef = new FileReference(
                            path,
                            path,
                            ContentHash.CreateFromFile(path),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        ghMaterialObj.Value.SetEnvironmentTexture(texture, true);
                    }
                }
                else if (environmentMap is IGH_Goo)
                {
                    var goo = environmentMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        var texture = BitmapToTexture(bmp, true);
                        if (texture != null)
                        {
                            ghMaterialObj.Value.SetEnvironmentTexture(texture, true);
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


                GH_CustomPreviewItem item = new GH_CustomPreviewItem();
                item.Geometry = (IGH_PreviewData)geometry;
                
                item.Shader = ghMaterialObj.Value;
                item.Colour = ghMaterialObj.Value.Diffuse;
                item.Material = ghMaterialObj;
                m_items.Add(item);
                m_clipbox.Union(geometry.Boundingbox);


                m_items.Add(item);
                m_clipbox.Union(geometry.Boundingbox);
            }
            DA.SetData(1, ghMaterialObj);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (m_items == null || args.Document.IsRenderMeshPipelineViewport(args.Display))
            {
                return;
            }
            if (base.Attributes.Selected)
            {
                GH_PreviewWireArgs args2 = new GH_PreviewWireArgs(args.Viewport, args.Display, args.WireColour_Selected, args.DefaultCurveThickness);
                {
                    foreach (GH_CustomPreviewItem item in m_items)
                    {
                        if (!(item.Geometry is GH_Mesh) || CentralSettings.PreviewMeshEdges)
                        {
                            item.Geometry.DrawViewportWires(args2);
                        }
                    }
                    return;
                }
            }
            foreach (GH_CustomPreviewItem item2 in m_items)
            {
                if (!(item2.Geometry is GH_Mesh) || CentralSettings.PreviewMeshEdges)
                {
                    GH_PreviewWireArgs args3 = new GH_PreviewWireArgs(args.Viewport, args.Display, item2.Colour, args.DefaultCurveThickness);
                    item2.Geometry.DrawViewportWires(args3);
                }
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (m_items == null || args.Document.IsRenderMeshPipelineViewport(args.Display))
            {
                return;
            }
            if (base.Attributes.Selected)
            {
                GH_PreviewMeshArgs args2 = new GH_PreviewMeshArgs(args.Viewport, args.Display, args.ShadeMaterial_Selected, args.MeshingParameters);
                {
                    foreach (GH_CustomPreviewItem item in m_items)
                    {
                        item.Geometry.DrawViewportMeshes(args2);
                    }
                    return;
                }
            }
            foreach (GH_CustomPreviewItem item2 in m_items)
            {
                GH_PreviewMeshArgs args3 = new GH_PreviewMeshArgs(args.Viewport, args.Display, item2.Shader, args.MeshingParameters);
                item2.Geometry.DrawViewportMeshes(args3);
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