using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.Motility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class MergeCameraData : GH_Component, IGH_VariableParameterComponent
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.MergeCameraData;
        public override Guid ComponentGuid => new Guid("ECA244CD-0484-45D8-B516-833938CDDFE6");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        private List<IGH_DocumentObject> _currentSliders = new List<IGH_DocumentObject>();
        private Dictionary<IGH_DocumentObject, (decimal min, decimal max)> _previousValues = new Dictionary<IGH_DocumentObject, (decimal min, decimal max)>();
        private Dictionary<IGH_DocumentObject, string> _previousNicknames = new Dictionary<IGH_DocumentObject, string>();

        private bool _isUpdatingParameters = false;

        public MergeCameraData()
            : base(
                "MergeCameraData",
                "MergeCameraData",
                "根据slider数量生成输入端，合并Camera数据",
                "Motion",
                "01_Animation"
            )
        {
            _previousNicknames = new Dictionary<IGH_DocumentObject, string>();
            _previousValues = new Dictionary<IGH_DocumentObject, (decimal min, decimal max)>();
            _currentSliders = new List<IGH_DocumentObject>();
            
            // 添加参数源变化事件监听
            Params.ParameterSourcesChanged += ParamSourcesChanged;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsAdded += Document_TimelineSliderChanged;
            document.ObjectsDeleted += Document_TimelineSliderChanged;
            document.SolutionEnd += Document_SolutionEnd;

            // 初始化时立即更新一次参数
            UpdateInputParameters();

            // 初始化现有的Slider值
            foreach (var slider in GetTimelineSliders())
            {
                if (!_previousValues.ContainsKey(slider))
                {
                    var value = GetSliderValues(slider);
                    if (value.HasValue)
                    {
                        _previousValues[slider] = value.Value;
                    }
                }
            }
            this.Hidden = true;
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            document.ObjectsAdded -= Document_TimelineSliderChanged;
            document.ObjectsDeleted -= Document_TimelineSliderChanged;
            document.SolutionEnd -= Document_SolutionEnd;
            _previousValues.Clear();
            _previousNicknames.Clear();
            _timelineSliderTypeName = null;
            base.RemovedFromDocument(document);
        }

        private void Document_TimelineSliderChanged(object sender, GH_DocObjectEventArgs e)
        {
            // 检查是否有Timeline Slider被添加或删除
            bool hasTimelineSliderChange = e.Objects.Any(obj =>
            {
                if (_timelineSliderTypeName == null)
                {
                    // 如果类型名称未缓存，则进行初始检查
                    return obj.GetType().ToString().Contains("pOd_TimeLineSlider")
                           && obj.NickName != "TimeLine(Union)";
                }
                // 使用缓存的类型名称进行检查
                return obj.GetType().ToString() == _timelineSliderTypeName
                       && obj.NickName != "TimeLine(Union)";
            });

            if (hasTimelineSliderChange)
            {
                UpdateInputParameters();
            }
        }

        private void Document_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            bool valueChanged = false;
            bool nicknameChanged = false;
            var currentSliders = GetTimelineSliders();

            foreach (var slider in currentSliders)
            {
                if (slider == null) continue;

                // 检查昵称变化
                string currentNickname = slider.NickName;
                if (!_previousNicknames.ContainsKey(slider) || _previousNicknames[slider] != currentNickname)
                {
                    _previousNicknames[slider] = currentNickname;
                    nicknameChanged = true;
                }

                // 检查最大最小值变化
                var currentValue = GetSliderValues(slider);
                if (!currentValue.HasValue) continue;

                var (currentMin, currentMax) = currentValue.Value;

                if (!_previousValues.ContainsKey(slider))
                {
                    _previousValues[slider] = (currentMin, currentMax);
                    valueChanged = true;
                }
                else
                {
                    var (prevMin, prevMax) = _previousValues[slider];
                    if (prevMin != currentMin || prevMax != currentMax)
                    {
                        _previousValues[slider] = (currentMin, currentMax);
                        valueChanged = true;
                    }
                }
            }

            // 如果昵称或值发生变化，则更新参数
            if (nicknameChanged || valueChanged)
            {
                _isUpdatingParameters = false; // 重置标志位以确保可以更新
                UpdateInputParameters();
            }
        }

        private (decimal min, decimal max)? GetSliderValues(IGH_DocumentObject sliderObject)
        {
            try
            {
                GH_NumberSlider slider = sliderObject as GH_NumberSlider;
                if (slider != null)
                {
                    return (slider.Slider.Minimum, slider.Slider.Maximum);
                }
            }
            catch { }
            return null;
        }

        private void UpdateInputParameters()
        {
            if (_isUpdatingParameters) return;
            _isUpdatingParameters = true;

            GH_Document doc = OnPingDocument();
            if (doc != null)
            {
                doc.ScheduleSolution(0, CallBack);
            }
        }

        private string _timelineSliderTypeName = null;
        private List<IGH_DocumentObject> GetTimelineSliders()
        {
            // 缓存类型名称，避免重复字符串操作
            if (_timelineSliderTypeName == null)
            {
                var doc = OnPingDocument();
                if (doc?.Objects != null)
                {
                    var firstSlider = doc.Objects.FirstOrDefault(o => o.GetType().ToString().Contains("pOd_TimeLineSlider"));
                    if (firstSlider != null)
                    {
                        _timelineSliderTypeName = firstSlider.GetType().ToString();
                    }
                }
            }

            return OnPingDocument().Objects
                .Where(o => o.GetType().ToString() == _timelineSliderTypeName
                            && o.NickName != "TimeLine(Union)")
                .OrderBy(s => s.Attributes.Bounds.Y)
                .ToList();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "R", "Result of merge", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 确保 Message 始终显示当前的 NickName
            this.Message = this.NickName;


            GH_Structure<IGH_Goo> result = new GH_Structure<IGH_Goo>();

            try
            {
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    if (DA.GetDataTree(i, out GH_Structure<IGH_Goo> tree) && tree != null)
                    {
                        result.MergeStructure(tree);
                    }
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }

            DA.SetDataTree(0, result);
        }

        public void CallBack(GH_Document gdoc)
        {
            try 
            {
                var currentSliders = GetTimelineSliders();
                _currentSliders = new List<IGH_DocumentObject>(currentSliders);

                // 保存现有的正确连接信息，包括Y坐标
                var existingConnections = new List<(string nickname, Param_RemoteData remoteData, float sliderY)>();
                
                // 创建 slider 位置映射，处理重复的 nickname
                var sliderPositions = new Dictionary<(string nickname, float y), IGH_DocumentObject>();
                foreach (var slider in currentSliders)
                {
                    var y = slider.Attributes.Bounds.Y;
                    sliderPositions.Add((slider.NickName, y), slider);
                }

                // 保存现有的正确连接
                foreach (var param in Params.Input)
                {
                    if (param.SourceCount > 0 && param.Sources[0] is Param_RemoteData remoteData)
                    {
                        // 找到最近的匹配 slider
                        var matchingSlider = sliderPositions
                            .Where(kvp => kvp.Key.nickname == param.NickName)
                            .OrderBy(kvp => Math.Abs(kvp.Key.y - param.Attributes.Bounds.Y))
                            .FirstOrDefault();

                        // 只保存完全匹配的连接
                        if (param.NickName == remoteData.NickName && 
                            !matchingSlider.Equals(default(KeyValuePair<(string, float), IGH_DocumentObject>)))
                        {
                            existingConnections.Add((
                                param.NickName,
                                remoteData,
                                matchingSlider.Key.y
                            ));
                        }
                    }
                }

                // 按Y坐标排序连接信息
                existingConnections = existingConnections
                    .OrderBy(c => c.sliderY)
                    .ToList();

                // 断开所有现有连接
                foreach (var param in Params.Input)
                {
                    param.RemoveAllSources();
                }

                // 更新参数数量
                while (Params.Input.Count > currentSliders.Count)
                {
                    Params.UnregisterInputParameter(Params.Input[Params.Input.Count - 1]);
                }

                while (Params.Input.Count < currentSliders.Count)
                {
                    var param = new Param_GenericObject
                    {
                        Access = GH_ParamAccess.tree,
                        Optional = true
                    };
                    Params.RegisterInputParam(param);
                }

                // 更新参数名称
                for (int i = 0; i < currentSliders.Count; i++)
                {
                    if (i < Params.Input.Count)
                    {
                        var slider = currentSliders[i];
                        var param = Params.Input[i];
                        
                        param.Name = slider.Name;
                        param.NickName = slider.NickName;
                    }
                }

                // 恢复连接，按照Y坐标顺序
                foreach (var connection in existingConnections)
                {
                    // 找到对应的slider
                    var slider = currentSliders.FirstOrDefault(s => 
                        Math.Abs(s.Attributes.Bounds.Y - connection.sliderY) < 0.1);

                    if (slider != null)
                    {
                        // 找到对应的参数
                        var param = Params.Input.FirstOrDefault(p => p.NickName == slider.NickName);
                        if (param != null && param.NickName == connection.nickname)
                        {
                            // 确保这个RemoteData没有被其他参数使用
                            bool isRemoteDataInUse = Params.Input
                                .Any(p => p != param && 
                                        p.SourceCount > 0 && 
                                        p.Sources[0] == connection.remoteData);

                            if (!isRemoteDataInUse)
                            {
                                param.AddSource(connection.remoteData);
                            }
                        }
                    }
                }

                Params.OnParametersChanged();
                ExpireSolution(true);
            }
            finally
            {
                _isUpdatingParameters = false;
            }
        }

        private void ParamSourcesChanged(object sender, GH_ParamServerEventArgs e)
        {
            if (e.ParameterSide == GH_ParameterSide.Input)
            {
                UpdateInputParameters();
            }
        }

        public void VariableParameterMaintenance()
        {
            for (int i = 0; i < Params.Input.Count; i++)
            {
                var param = Params.Input[i];
                param.Optional = true;
                param.MutableNickName = false;
                param.Access = GH_ParamAccess.tree;
            }
        }

        // IGH_VariableParameterComponent 接口实现
        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Param_GenericObject();
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);

            ToolStripMenuItem locationItem = Menu_AppendItem(menu, "Location", OnLocationClicked);
            locationItem.ToolTipText = "Set as Motion Location";

            ToolStripMenuItem targetItem = Menu_AppendItem(menu, "Target", OnTargetClicked);
            targetItem.ToolTipText = "Set as Motion Target";
        }

        private void OnLocationClicked(object sender, EventArgs e)
        {
            // 直接设置 NickName
            this.NickName = "MotionLocation";
            // 设置 Message
            this.Message = this.NickName;
            // 使组件失效并更新显示
            this.ExpireSolution(true);
            this.Attributes.ExpireLayout();
        }

        private void OnTargetClicked(object sender, EventArgs e)
        {
            // 直接设置 NickName
            this.NickName = "MotionTarget";
            // 设置 Message
            this.Message = this.NickName;
            // 使组件失效并更新显示
            this.ExpireSolution(true);
            this.Attributes.ExpireLayout();
        }
    }
}