using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Motion.Utils
{
    /// <summary>
    /// 动态输出组件：根据输入数据结构自动创建对应的输出端
    /// </summary>
    public class DynamicOutput : GH_Component, IGH_VariableParameterComponent
    {
        // 记录上一次的状态
        private int lastOutputCount = 0;
        private int lastPathCount = 0;

        public DynamicOutput() : base(
            "Dynamic Output", 
            "DynOut",
            "自动为每个输入数据创建输出端。单个路径时每个数据分配一个输出端；多个路径时每个路径分配一个输出端",
            "Motion", 
            "03_Utils")
        { }

        /// <summary>
        /// 注册输入参数：一个树形数据输入端
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "输入数据", GH_ParamAccess.tree);
        }

        /// <summary>
        /// 注册输出参数：动态创建，初始为空
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// 在求解实例之前检查并更新输出端结构
        /// </summary>
        protected override void BeforeSolveInstance()
        {
            // 获取输入数据结构
            var structure = Params.Input[0].VolatileData as GH_Structure<IGH_Goo>;
            if (structure == null) return;

            // 确定所需的输出端数量
            int requiredOutputs;
            if (structure.PathCount == 1)
            {
                // 单路径模式：每个数据一个输出端
                requiredOutputs = structure.get_Branch(0).Count;
            }
            else
            {
                // 多路径模式：每个路径一个输出端
                requiredOutputs = structure.PathCount;
            }

            // 检查是否需要更新输出端
            bool needsUpdate = false;

            // 检查路径数量是否发生变化（从单路径变多路径，或从多路径变单路径）
            if ((lastPathCount == 1 && structure.PathCount > 1) || 
                (lastPathCount > 1 && structure.PathCount == 1))
            {
                needsUpdate = true;
            }
            // 检查输出端数量是否需要更新
            else if (requiredOutputs != lastOutputCount)
            {
                needsUpdate = true;
            }

            // 如果需要更新，则更新输出端
            if (needsUpdate)
            {
                lastOutputCount = requiredOutputs;
                lastPathCount = structure.PathCount;
                UpdateOutputParams(structure);
            }
        }

        /// <summary>
        /// 处理实际的数据分配
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 获取输入的数据树
            GH_Structure<IGH_Goo> tree = new GH_Structure<IGH_Goo>();
            if (!DA.GetDataTree(0, out tree)) return;

            if (tree.PathCount == 1)
            {
                // 单路径模式：处理单一路径下的每个数据
                ProcessSinglePath(DA, tree);
            }
            else
            {
                // 多路径模式：处理多个路径
                ProcessMultiplePaths(DA, tree);
            }
        }

        /// <summary>
        /// 处理单一路径的数据分配
        /// </summary>
        private void ProcessSinglePath(IGH_DataAccess DA, GH_Structure<IGH_Goo> tree)
        {
            var branch = tree.get_Branch(0);
            for (int i = 0; i < Math.Min(branch.Count, Params.Output.Count); i++)
            {
                var data = branch[i];
                if (data is IGH_Goo goo)
                {
                    goo.CastTo(out data);
                }

                // 如果数据是集合（但不是字符串），则作为列表输出
                if (data is IEnumerable enumerable && !(data is string))
                {
                    DA.SetDataList(i, enumerable.Cast<object>());
                }
                else
                {
                    DA.SetData(i, data);
                }
            }
        }

        /// <summary>
        /// 处理多路径的数据分配
        /// </summary>
        private void ProcessMultiplePaths(IGH_DataAccess DA, GH_Structure<IGH_Goo> tree)
        {
            for (int i = 0; i < Math.Min(tree.PathCount, Params.Output.Count); i++)
            {
                var path = tree.Paths[i];
                var branch = tree[path];

                if (branch == null || branch.Count == 0) continue;

                // 处理单个数据的特殊情况
                if (branch.Count == 1)
                {
                    var data = branch[0];
                    if (data is IGH_Goo goo)
                    {
                        goo.CastTo(out data);
                    }

                    if (!(data is IEnumerable))
                    {
                        DA.SetData(i, data);
                        continue;
                    }
                }

                // 作为列表输出
                DA.SetDataList(i, branch);
            }
        }

        /// <summary>
        /// 更新输出参数结构
        /// </summary>
        private void UpdateOutputParams(GH_Structure<IGH_Goo> structure)
        {
            // 移除所有现有输出端
            while (Params.Output.Count > 0)
            {
                Params.Output.RemoveAt(Params.Output.Count - 1);
            }

            if (structure.PathCount == 1)
            {
                CreateSinglePathOutputs(structure);
            }
            else
            {
                CreateMultiplePathOutputs(structure);
            }

            // 确保更新UI
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        /// <summary>
        /// 为单一路径创建输出端
        /// </summary>
        private void CreateSinglePathOutputs(GH_Structure<IGH_Goo> structure)
        {
            var branch = structure.get_Branch(0);
            for (int i = 0; i < branch.Count; i++)
            {
                var param = new Param_GenericObject
                {
                    Name = $"Output {i}",
                    NickName = $"Out{i}",
                    Description = $"Output data {i}"
                };

                var data = branch[i];
                if (data is IGH_Goo goo)
                {
                    goo.CastTo(out data);
                }
                param.Access = (data is IEnumerable)
                    ? GH_ParamAccess.list
                    : GH_ParamAccess.item;

                Params.RegisterOutputParam(param);
            }
        }

        /// <summary>
        /// 为多路径创建输出端
        /// </summary>
        private void CreateMultiplePathOutputs(GH_Structure<IGH_Goo> structure)
        {
            for (int i = 0; i < structure.PathCount; i++)
            {
                var path = structure.Paths[i];
                var branch = structure[path];
                
                var param = new Param_GenericObject();
                SetupParameter(param, path, branch);
                Params.RegisterOutputParam(param);
            }
        }

        /// <summary>
        /// 设置参数的属性（名称、描述和访问类型）
        /// </summary>
        private void SetupParameter(IGH_Param param, GH_Path path, List<IGH_Goo> branch)
        {
            string pathName = path.ToString();
            param.Name = pathName;
            param.NickName = pathName;
            param.Description = $"Data from path {pathName}";

            if (branch != null && branch.Count == 1)
            {
                var data = branch[0];
                if (data is IGH_Goo goo)
                {
                    goo.CastTo(out data);
                }

                param.Access = (data is IEnumerable)
                    ? GH_ParamAccess.list
                    : GH_ParamAccess.item;
            }
            else
            {
                param.Access = GH_ParamAccess.list;
            }
        }

        // IGH_VariableParameterComponent 接口实现
        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Param_GenericObject();
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance() { }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.DynamicOutput;
        public override Guid ComponentGuid => new Guid("058F4109-93F9-4FBB-94D9-B4FF6C5B33CA");
        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}