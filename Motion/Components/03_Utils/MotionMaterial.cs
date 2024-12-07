using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Motion.Utils
{
    public class MotionMaterial : GH_Component
    {
        private readonly string _transparencyTempFile;
        private readonly string _bumpTempFile;

        private string _ImageFolder;
        private string[] _ImageFiles;
        public string ImageFolder
        {
            get { return _ImageFolder; }
            set
            {
                _ImageFolder = value;
            }
        }
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

            pManager.AddGenericParameter("Diffuse Map", "DM", "Diffuse贴图文件的路径或System.Drawing.Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Transparency Map", "TM", "透明度贴图文件的路径或System.Drawing.Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Environment Map", "EM", "环境贴图文件的路径或System.Drawing.Bitmap", GH_ParamAccess.item);
            pManager.AddGenericParameter("Bump Map", "BM", "凹凸贴图文件的路径或System.Drawing.Bitmap", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transform Settings", "TS", "贴图变换设置列表", GH_ParamAccess.list);

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

        private Texture BitmapToTexture(Bitmap bitmap, bool isTransparency)
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
            object environmentMap = null;
            object diffuseMap = null;
            object bumpMap = null;

            List<Transform> transformSettings = new List<Transform>();

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

            DA.GetData("Diffuse Map", ref diffuseMap);
            DA.GetData("Transparency Map", ref transparencyMap);
            DA.GetData("Environment Map", ref environmentMap);
            DA.GetData("Bump Map", ref bumpMap);
            DA.GetDataList("Transform Settings", transformSettings);

            if (transformSettings.Count == 0)
            {
                // 如果没有输入,使用默认的单位变换
                transformSettings = new List<Transform>
                {
                    Transform.Identity,
                    Transform.Identity,
                    Transform.Identity
                };
            }

            // 确保列表长度为3
            while (transformSettings.Count < 3)
                transformSettings.Add(Transform.Identity);

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

            HandleDiffuseMap(diffuseMap, material);
            HandleTransparencyMap(transparencyMap, material);
            

            HandleTransformSettings(material, transformSettings);
            // 创建渲染材质
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            var renderMaterial = Rhino.Render.RenderMaterial.CreateBasicMaterial(material, activeDoc);

            GH_Material ghMaterialObj = new GH_Material(renderMaterial);
            HandleEnviromentMap(environmentMap, ghMaterialObj);
            HandleBumpMap(transparencyMap, ghMaterialObj);
            // 输出
            DA.SetData(0, ghMaterialObj);
        }

        private void HandleEnviromentMap(object environmentMap, GH_Material ghMaterialObj)
        {
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
        }
        private void HandleBumpMap(object bumpMap, GH_Material ghMaterialObj)
        {
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
                    //material.SetTexture(texture, TextureType.Bump);
                    ghMaterialObj.Value.SetBumpTexture(texture, true);
                }
                else if (bumpMap is Bitmap bumpBitmap)
                {
                    var texture = BitmapToTexture(bumpBitmap, false);
                    if (texture != null)
                    {
                        //material.SetTexture(texture, TextureType.Bump);
                        ghMaterialObj.Value.SetBumpTexture(texture, true);
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
                        //material.SetTexture(texture, TextureType.Bump);
                        ghMaterialObj.Value.SetBumpTexture(texture, false);
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
                            //material.SetTexture(texture, TextureType.Bump);
                            ghMaterialObj.Value.SetBumpTexture(texture, true);
                        }
                    }
                }
            }
        }
        private void HandleDiffuseMap(object diffuseMap, Material material)
        {
            if (diffuseMap != null)
            {
                if (diffuseMap is string diffusePath && File.Exists(diffusePath))
                {
                    var fileRef = new FileReference(
                        diffusePath,
                        diffusePath,
                        ContentHash.CreateFromFile(diffusePath),
                        FileReferenceStatus.FullPathValid
                    );
                    var texture = new Texture { FileReference = fileRef };
                    material.SetTexture(texture, TextureType.Diffuse);
                }
                else if (diffuseMap is Bitmap transBitmap)
                {
                    var texture = BitmapToTexture(transBitmap, true);
                    if (texture != null)
                    {
                        material.SetTexture(texture, TextureType.Diffuse);
                    }
                }
                else if (diffuseMap is GH_String transGhString)
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
                        material.SetTexture(texture, TextureType.Diffuse);
                    }
                }
                else if (diffuseMap is IGH_Goo)
                {
                    var goo = diffuseMap as IGH_Goo;
                    if (goo.CastTo<Bitmap>(out Bitmap bmp))
                    {
                        var texture = BitmapToTexture(bmp, true);
                        if (texture != null)
                        {
                            material.SetTexture(texture, TextureType.Diffuse);
                        }
                    }
                }
            }
        }
        private void HandleTransparencyMap(object transparencyMap, Material material)
        {
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
                    var texture = BitmapToTexture(transBitmap, true);
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
        }
        
        private void HandleTransformSettings(Material material, List<Transform> transformSettings)
        {
            if (material != null)
            {
                // 透明贴图变换
                if (material.GetTexture(TextureType.Transparency) is Texture transparencyTex)
                {
                    transparencyTex.ApplyUvwTransform = true;
                    transparencyTex.UvwTransform = transformSettings[0];
                    material.SetTexture(transparencyTex, TextureType.Transparency);
                }

                // 漫反射贴图变换
                if (material.GetTexture(TextureType.Diffuse) is Texture diffuseTex)
                {
                    diffuseTex.ApplyUvwTransform = true;
                    diffuseTex.UvwTransform = transformSettings[1];
                    material.SetTexture(diffuseTex, TextureType.Diffuse);
                }

                // 凹凸贴图变换
                if (material.GetTexture(TextureType.Bump) is Texture bumpTex)
                {
                    bumpTex.ApplyUvwTransform = true;
                    bumpTex.UvwTransform = transformSettings[2];
                    material.SetTexture(bumpTex, TextureType.Bump);
                }
            }
        }
        protected override System.Drawing.Bitmap Icon => null; //Properties.Resources.MotionMaterial;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("096EFA10-33CE-462A-B0D1-C4BBF52DCAE7");
    }
}