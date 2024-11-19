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
    public abstract class MergeCameraBase : GH_Component, IGH_VariableParameterComponent
    {
        protected List<IGH_DocumentObject> _currentSliders = new List<IGH_DocumentObject>();
        protected Dictionary<IGH_DocumentObject, (decimal min, decimal max)> _previousValues = new Dictionary<IGH_DocumentObject, (decimal min, decimal max)>();
        protected Dictionary<IGH_DocumentObject, string> _previousNicknames = new Dictionary<IGH_DocumentObject, string>();
        protected bool _isUpdatingParameters = false;

        // 添加一个枚举来标识组件类型
        protected enum MergeCameraType
        {
            Location,
            Target
        }

        // 在基类中添加一个抽象属性
        protected abstract MergeCameraType ComponentType { get; }

        protected MergeCameraBase(string name, string nickname, string description)
            : base(name, nickname, description, "Motion", "01_Animation")
        {
            _previousNicknames = new Dictionary<IGH_DocumentObject, string>();
            _previousValues = new Dictionary<IGH_DocumentObject, (decimal min, decimal max)>();
            _currentSliders = new List<IGH_DocumentObject>();
            
            Params.ParameterSourcesChanged += ParamSourcesChanged;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsAdded += Document_TimelineSliderChanged;
            document.ObjectsDeleted += Document_TimelineSliderChanged;
            document.SolutionEnd += Document_SolutionEnd;

            document.ScheduleSolution(5, doc =>
            {
                UpdateInputParameters();

                doc.ScheduleSolution(10, innerDoc =>
                {
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

                    ConnectMatchingRemoteParams();
                    SetAllInputWiresDisplay();
                });
            });

            this.Hidden = true;
        }

        private void ConnectMatchingRemoteParams()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 根据组件类型选择对应的远程参数类型
            var remoteParams = doc.Objects
                .Where(obj => ComponentType == MergeCameraType.Location ? 
                    obj is Param_RemoteLocation : 
                    obj is Param_RemoteTarget)
                .Cast<IGH_Param>()
                .ToList();

            foreach (var input in Params.Input)
            {
                var matchingParam = remoteParams.FirstOrDefault(p => p.NickName == input.NickName);
                
                if (matchingParam != null)
                {
                    bool isRemoteParamInUse = Params.Input
                        .Any(p => p != input && 
                                 p.SourceCount > 0 && 
                                 p.Sources[0] == matchingParam);

                    if (!isRemoteParamInUse)
                    {
                        input.AddSource(matchingParam);
                    }
                }
            }

            Params.OnParametersChanged();
            SetAllInputWiresDisplay();
            ExpireSolution(true);
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
            bool hasTimelineSliderChange = e.Objects.Any(obj =>
            {
                if (_timelineSliderTypeName == null)
                {
                    return obj.GetType().ToString().Contains("pOd_TimeLineSlider")
                           && obj.NickName != "TimeLine(Union)";
                }
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

                string currentNickname = slider.NickName;
                if (!_previousNicknames.ContainsKey(slider) || _previousNicknames[slider] != currentNickname)
                {
                    _previousNicknames[slider] = currentNickname;
                    nicknameChanged = true;
                }

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

            if (nicknameChanged || valueChanged)
            {
                _isUpdatingParameters = false;
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
                doc.ScheduleSolution(15, CallBack);
            }
        }

        private string _timelineSliderTypeName = null;
        private List<IGH_DocumentObject> GetTimelineSliders()
        {
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
                .OrderBy(s => 
                {
                    // 从 NickName 中提取 "-" 前面的数字
                    var parts = s.NickName.Split('-');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int number))
                    {
                        return number;
                    }
                    return int.MaxValue; // 如果无法解析数字，放到最后
                })
                .ToList();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "R", "Result of merge", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            this.Message = this.NickName;
            IGH_Goo result = null;

            try
            {
                // 获取 TimeLine(Union) slider
                var doc = OnPingDocument();
                var unionSlider = doc?.Objects
                    .FirstOrDefault(o => o.GetType().ToString().Contains("pOd_TimeLineSlider") && 
                                        o.NickName == "TimeLine(Union)") as GH_NumberSlider;

                if (unionSlider != null)
                {
                    double unionValue = (double)unionSlider.CurrentValue;
                    double maxStartValue = double.MinValue;
                    int selectedInputIndex = -1;

                    // 遍历所有有连线的输入端
                    for (int i = 0; i < Params.Input.Count; i++)
                    {
                        var param = Params.Input[i];
                        if (param.SourceCount > 0)
                        {
                            string[] parts = param.NickName.Split('-');
                            if (parts.Length == 2 && 
                                double.TryParse(parts[0], out double start) && 
                                double.TryParse(parts[1], out double end))
                            {
                                if (unionValue >= start && unionValue <= end && start > maxStartValue)
                                {
                                    maxStartValue = start;
                                    selectedInputIndex = i;
                                }
                            }
                        }
                    }

                    // 如果找到符合条件的输入端，获取其数据
                    if (selectedInputIndex != -1)
                    {
                        IGH_Goo item = null;
                        if (DA.GetData(selectedInputIndex, ref item) && item != null)
                        {
                            result = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }

            DA.SetData(0, result);
        }

        public void CallBack(GH_Document gdoc)
        {
            try 
            {
                var currentSliders = GetTimelineSliders();
                _currentSliders = new List<IGH_DocumentObject>(currentSliders);

                var existingConnections = new List<(string nickname, IGH_Param remoteParam, float sliderY)>();
                
                var sliderPositions = new Dictionary<(string nickname, float y), IGH_DocumentObject>();
                foreach (var slider in currentSliders)
                {
                    var y = slider.Attributes.Bounds.Y;
                    sliderPositions.Add((slider.NickName, y), slider);
                }

                foreach (var param in Params.Input)
                {
                    if (param.SourceCount > 0)
                    {
                        var source = param.Sources[0];
                        if (source is Param_RemoteLocation locationParam || source is Param_RemoteTarget targetParam)
                        {
                            var remoteParam = source as IGH_Param;
                            var matchingSlider = sliderPositions
                                .Where(kvp => kvp.Key.nickname == param.NickName)
                                .OrderBy(kvp => Math.Abs(kvp.Key.y - param.Attributes.Bounds.Y))
                                .FirstOrDefault();

                            if (param.NickName == remoteParam.NickName && 
                                !matchingSlider.Equals(default(KeyValuePair<(string, float), IGH_DocumentObject>)))
                            {
                                existingConnections.Add((
                                    param.NickName,
                                    remoteParam,
                                    matchingSlider.Key.y
                                ));
                            }
                        }
                    }
                }

                existingConnections = existingConnections
                    .OrderBy(c => c.sliderY)
                    .ToList();

                foreach (var param in Params.Input)
                {
                    param.RemoveAllSources();
                }

                while (Params.Input.Count > currentSliders.Count)
                {
                    Params.UnregisterInputParameter(Params.Input[Params.Input.Count - 1]);
                }

                while (Params.Input.Count < currentSliders.Count)
                {
                    var param = new Param_GenericObject
                    {
                        Access = GH_ParamAccess.item,
                        Optional = true
                    };
                    Params.RegisterInputParam(param);
                }

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

                foreach (var connection in existingConnections)
                {
                    var slider = currentSliders.FirstOrDefault(s => 
                        Math.Abs(s.Attributes.Bounds.Y - connection.sliderY) < 0.1);

                    if (slider != null)
                    {
                        var param = Params.Input.FirstOrDefault(p => p.NickName == slider.NickName);
                        if (param != null && param.NickName == connection.nickname)
                        {
                            bool isRemoteParamInUse = Params.Input
                                .Any(p => p != param && 
                                        p.SourceCount > 0 && 
                                        p.Sources[0] == connection.remoteParam);

                            if (!isRemoteParamInUse)
                            {
                                param.AddSource(connection.remoteParam);
                            }
                        }
                    }
                }

                Params.OnParametersChanged();
                SetAllInputWiresDisplay();
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
                param.Access = GH_ParamAccess.item;
            }
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject
            {
                Optional = true,
                Access = GH_ParamAccess.item
            };
        }
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        private void SetAllInputWiresDisplay()
        {
            foreach (var param in Params.Input)
            {
                if (param.WireDisplay != GH_ParamWireDisplay.hidden)
                {
                    param.WireDisplay = GH_ParamWireDisplay.faint;
                }
            }
        }
    }
} 