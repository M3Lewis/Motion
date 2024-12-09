using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Components;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Motion.Utils
{
    public class MotionImagePreview : GH_Component
    {
        private List<GH_CustomPreviewItem> m_items;
        private BoundingBox m_clipbox;
        
        private readonly string _transparencyTempFile;
        private readonly string _environmentTempFile;


        public MotionImagePreview()
            : base("Motion Image Preview", "Motion Image Preview",
                "图片预览(输入Motion Material)",
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
            Param_OGLShader param_OGLShader = new Param_OGLShader();
            param_OGLShader.SetPersistentData(new GH_Material(Color.Plum));
            pManager.AddParameter(param_OGLShader, "Material", "M", "Motion Material", GH_ParamAccess.item);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0)
            {
                m_items = new List<GH_CustomPreviewItem>();
                m_clipbox = BoundingBox.Empty;
            }

            IGH_GeometricGoo geometry = null;
            object materialObj = null;

            if (!DA.GetData(0, ref geometry)) return;
            DA.GetData(1, ref materialObj);

            GH_Material ghMaterialObj = materialObj as GH_Material;

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
            base.RemovedFromDocument(document);
        }

        public override BoundingBox ClippingBox => m_clipbox;
        protected override Bitmap Icon => Properties.Resources.MotionImagePreview;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("63210FC2-D925-4BCB-B0E6-3E8D0C0A21E7");
    }
} 