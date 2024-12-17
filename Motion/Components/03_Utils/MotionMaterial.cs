using Grasshopper.Kernel;
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
    public class MotionMaterial : GH_Component
    {
        private readonly string _transparencyTempFile;
        private readonly string _bumpTempFile;
        private readonly string _environmentTempFile;
        private readonly string _diffuseTempFile;
        private static int _instanceCounter = 0;
        private readonly int _instanceId;
        private readonly Dictionary<string, Texture> _textureCache;

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
                "创建渲染材质,贴图支持输入路径和System.Drawing.Bitmap，可配合Javid/Bitmap+插件使用",
                "Motion", "03_Utils")
        {
            _instanceId = System.Threading.Interlocked.Increment(ref _instanceCounter);
            _transparencyTempFile = Path.Combine(Path.GetTempPath(), $"MotionTransparencyTex_{_instanceId}.png");
            _bumpTempFile = Path.Combine(Path.GetTempPath(), $"MotionBumpTex_{_instanceId}.png");
            _environmentTempFile = Path.Combine(Path.GetTempPath(), $"MotionEnvironmentTex_{_instanceId}.png");
            _diffuseTempFile = Path.Combine(Path.GetTempPath(), $"MotionDiffuseTex_{_instanceId}.png");
            _textureCache = new Dictionary<string, Texture>();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            base.RemovedFromDocument(document);
            _textureCache.Clear();

            // 清理临时文件
            DeleteTempFile(_transparencyTempFile);
            DeleteTempFile(_bumpTempFile);
            DeleteTempFile(_environmentTempFile);
            DeleteTempFile(_diffuseTempFile);
        }

        private void DeleteTempFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                // 忽略删除失败的错误
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // 基础颜色参数
            pManager.AddColourParameter("Diffuse Color", "DC", "反射颜色", GH_ParamAccess.item, Color.White);
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
            pManager.AddTransformParameter("Transform Settings", "TS", "贴图变换设置", GH_ParamAccess.list);

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

        private Texture GetTextureFromObject(object obj, TextureType textureType)
        {
            if (obj == null) return null;

            try
            {
                // 直接使用文件路径
                if (obj is string path && File.Exists(path))
                    return GetTextureFromCache(path);

                if (obj is GH_String ghString && File.Exists(ghString.Value))
                    return GetTextureFromCache(ghString.Value);

                // 处理Bitmap
                if (obj is Bitmap bitmap)
                    return BitmapToTexture(bitmap, textureType);

                if (obj is IGH_Goo goo && goo.CastTo<Bitmap>(out Bitmap bmp))
                    return BitmapToTexture(bmp, textureType);

                return null;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"处理贴图时出错: {ex.Message}");
                return null;
            }
        }

        private Texture GetTextureFromCache(string path)
        {
            if (_textureCache.ContainsKey(path))
                return _textureCache[path];

            var fileRef = new FileReference(
                path,
                path,
                ContentHash.CreateFromFile(path),
                FileReferenceStatus.FullPathValid
            );
            var texture = new Texture { FileReference = fileRef };
            _textureCache[path] = texture;
            return texture;
        }

        private Texture BitmapToTexture(Bitmap bitmap, TextureType textureType)
        {
            if (bitmap == null) return null;

            string tempPath = textureType switch
            {
                TextureType.Transparency => _transparencyTempFile,
                TextureType.Bump => _bumpTempFile,
                TextureType.Emap => _environmentTempFile,
                TextureType.Diffuse => _diffuseTempFile,
                _ => _diffuseTempFile
            };

            try
            {
                bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                return GetTextureFromCache(tempPath);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"保存位图时出错: {ex.Message}");
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
            if (environmentMap == null) return;

            var texture = GetTextureFromObject(environmentMap, TextureType.Emap);
            if (texture != null)
            {
                if (environmentMap is string path && 
                    Path.GetExtension(path).ToLower() is string ext && 
                    (ext == ".hdr" || ext == ".hdri"))
                {
                    texture.TextureType = TextureType.Emap;
                }
                ghMaterialObj.Value.SetEnvironmentTexture(texture, true);
            }
        }

        private void HandleBumpMap(object bumpMap, GH_Material ghMaterialObj)
        {
            if (bumpMap == null) return;

            var texture = GetTextureFromObject(bumpMap, TextureType.Bump);
            if (texture != null)
            {
                ghMaterialObj.Value.SetBumpTexture(texture, true);
            }
        }

        private void HandleDiffuseMap(object diffuseMap, Material material)
        {
            if (diffuseMap == null) return;

            var texture = GetTextureFromObject(diffuseMap, TextureType.Diffuse);
            if (texture != null)
            {
                material.SetTexture(texture, TextureType.Diffuse);
            }
        }

        private void HandleTransparencyMap(object transparencyMap, Material material)
        {
            if (transparencyMap == null) return;

            var texture = GetTextureFromObject(transparencyMap, TextureType.Transparency);
            if (texture != null)
            {
                material.SetTransparencyTexture(texture);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.MotionMaterial;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("096EFA10-33CE-462A-B0D1-C4BBF52DCAE7");
    }
}