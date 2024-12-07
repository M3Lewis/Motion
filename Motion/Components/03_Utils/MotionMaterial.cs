using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Motion.Utils
{
    public class MotionMaterial : GH_Component
    {
        private readonly string _transparencyTempFile;
        private readonly string _bumpTempFile;
        

        public MotionMaterial()
            : base("Motion Material", "Motion Material",
                "创建渲染材质",
                "Motion", "03_Utils")
        {
            _transparencyTempFile = Path.Combine(Path.GetTempPath(), "MotionTransparencyTex.png");
            _bumpTempFile = Path.Combine(Path.GetTempPath(), "MotionBumpTex.png");
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // 基础颜色参数
            pManager.AddColourParameter("Diffuse Color", "DC", "漫反射颜色", GH_ParamAccess.item, Color.White);
            pManager.AddColourParameter("Ambient Color", "AC", "环境色", GH_ParamAccess.item, Color.Black);
            pManager.AddColourParameter("Emission Color", "EC", "自发光颜色", GH_ParamAccess.item, Color.Black);
            pManager.AddColourParameter("Specular Color", "SC", "高光颜色", GH_ParamAccess.item, Color.White);
            pManager.AddColourParameter("Reflection Color", "RC", "反射颜色", GH_ParamAccess.item, Color.White);
            pManager.AddColourParameter("Transparent Color", "TC", "透明颜色", GH_ParamAccess.item, Color.White);

            // 数值参数
            pManager.AddNumberParameter("Transparency", "T", "透明度(0-1)", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Reflectivity", "R", "反射率(0-1)", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Reflection Glossiness", "RG", "反射光泽度(0-1)", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Refraction Glossiness", "FG", "折射光泽度(0-1)", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Shine", "S", "光泽度(0-1)", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Fresnel Reflections", "FR", "菲涅尔反射", GH_ParamAccess.item, false);


            pManager.AddGenericParameter("Transparency Map", "TM", "透明度贴图", GH_ParamAccess.item);
            pManager.AddGenericParameter("Bump Map", "BM", "凹凸贴图", GH_ParamAccess.item);
            pManager.AddTransformParameter("Texture Transform", "TT", "纹理变换", GH_ParamAccess.item);

            // 设置可选参数
            for (int i = 1; i < pManager.ParamCount; i++)
            {
                pManager[i].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Material", "M", "渲染材质", GH_ParamAccess.item);
        }

        private Texture BitmapToTexture(Bitmap bitmap ,bool isTransparency)
        {
            try
            {
                string tempPath = isTransparency ? _transparencyTempFile : _bumpTempFile;
                bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                
                var fileRef = new FileReference(
                    tempPath,
                    tempPath,
                    ContentHash.CreateFromFile(tempPath),
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

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 获取基础颜色参数
            Color diffuseColor = Color.White;
            Color ambientColor = Color.Black;
            Color emissionColor = Color.Black;
            Color specularColor = Color.White;
            Color reflectionColor = Color.White;
            Color transparentColor = Color.White;

            // 获取数值参数
            double transparency = 0.0;
            double reflectivity = 0.0;
            double reflectionGlossiness = 0.0;
            double refractionGlossiness = 0.0;
            double shine = 0.0;
            bool fresnelReflections = false;

            // 获取纹理参数
            object transparencyMap = null;
            object bumpMap = null;

            Transform textureTransform = Transform.Identity;

            // 获取所有输入数据
            DA.GetData("Diffuse Color", ref diffuseColor);
            DA.GetData("Ambient Color", ref ambientColor);
            DA.GetData("Emission Color", ref emissionColor);
            DA.GetData("Specular Color", ref specularColor);
            DA.GetData("Reflection Color", ref reflectionColor);
            DA.GetData("Transparent Color", ref transparentColor);

            DA.GetData("Transparency", ref transparency);
            DA.GetData("Reflectivity", ref reflectivity);
            DA.GetData("Reflection Glossiness", ref reflectionGlossiness);
            DA.GetData("Refraction Glossiness", ref refractionGlossiness);
            DA.GetData("Shine", ref shine);
            DA.GetData("Fresnel Reflections", ref fresnelReflections);

            DA.GetData("Transparency Map", ref transparencyMap);
            DA.GetData("Bump Map", ref bumpMap);
            DA.GetData("Texture Transform", ref textureTransform);

            // 创建材质
            Material material = new Material
            {
                DiffuseColor = diffuseColor,
                AmbientColor = ambientColor,
                EmissionColor = emissionColor,
                SpecularColor = specularColor,
                ReflectionColor = reflectionColor,
                TransparentColor = transparentColor,
                
                Transparency = transparency,
                Reflectivity = reflectivity,
                ReflectionGlossiness = reflectionGlossiness,
                RefractionGlossiness = refractionGlossiness,
                Shine = shine,
                FresnelReflections = fresnelReflections
            };

            // 处理透明度贴图
            if (transparencyMap != null)
            {
                if (transparencyMap is string transPath && File.Exists(transPath))
                {
                    var fileRef = new FileReference(
                        transPath,          
                        transPath,          
                        ContentHash.CreateFromFile(transPath),
                        FileReferenceStatus.FullPathValid
                    );
                    var texture = new Texture { FileReference = fileRef };
                    material.SetTransparencyTexture(texture);
                }
                else if (transparencyMap is Bitmap transBitmap)
                {
                    var texture = BitmapToTexture(transBitmap,true);
                    if (texture != null)
                    {
                        material.SetTransparencyTexture(texture);
                    }
                }
                else if (transparencyMap is GH_String transGhString)
                {
                    string path = transGhString.Value;
                    if (File.Exists(path))
                    {
                        var fileRef = new FileReference(
                            path,            
                            path,            
                            ContentHash.CreateFromFile(path),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        material.SetTransparencyTexture(texture);
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
                            material.SetTransparencyTexture(texture);
                        }
                    }
                }
            }

            // 处理凹凸贴图
            if (bumpMap != null)
            {
                if (bumpMap is string bumpPath && File.Exists(bumpPath))
                {
                    var fileRef = new FileReference(
                        bumpPath,            
                        bumpPath,            
                        ContentHash.CreateFromFile(bumpPath),
                        FileReferenceStatus.FullPathValid
                    );
                    var texture = new Texture { FileReference = fileRef };
                    material.SetBumpTexture(texture);
                }
                else if (bumpMap is Bitmap bumpBitmap)
                {
                    var texture = BitmapToTexture(bumpBitmap,false);
                    if (texture != null)
                    {
                        material.SetBumpTexture(texture);
                    }
                }
                else if (bumpMap is GH_String bumpGhString)
                {
                    string path = bumpGhString.Value;
                    if (File.Exists(path))
                    {
                        var fileRef = new FileReference(
                            path,            
                            path,            
                            ContentHash.CreateFromFile(path),
                            FileReferenceStatus.FullPathValid
                        );
                        var texture = new Texture { FileReference = fileRef };
                        material.SetBumpTexture(texture);
                    }
                }
                else if (bumpMap is IGH_Goo)
                {
                    var goo = bumpMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        var texture = BitmapToTexture(bmp, false);
                        if (texture != null)
                        {
                            material.SetBumpTexture(texture);
                        }
                    }
                }
            }

            // 设置纹理变换
            if (material.GetTexture(TextureType.Transparency) is Texture tex)
            {
                tex.ApplyUvwTransform = true;
                tex.UvwTransform = textureTransform;
                material.SetTexture(tex, TextureType.Transparency);
            }

            // 创建渲染材质
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            var renderMaterial = Rhino.Render.RenderMaterial.CreateBasicMaterial(material, activeDoc);

            // 输出
            DA.SetData(0, new GH_Material(renderMaterial));
        }

        protected override System.Drawing.Bitmap Icon => null; //Properties.Resources.MotionMaterial;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("096EFA10-33CE-462A-B0D1-C4BBF52DCAE7");
    }
} 