using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Undo;
using Motion.General;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace Motion.Animation
{
    public class MotionSender : RemoteParam
    {
        private Interval _senderRange;
        public delegate void NickNameChangedEventHandler(IGH_DocumentObject sender, string newNickName);
        public event NickNameChangedEventHandler NickNameChanged;
        private double _previousValue = double.NaN;

        public MotionSender()
            : base()
        {
            nicknameKey = "";
            base.NickName = nicknameKey;
            base.Hidden = true;

            UpdateRangeFromNickname();
        }

        protected string nicknameKey = "";
        private Guid _connectedSliderGuid = Guid.Empty;


        private void UpdateRangeFromNickname()
        {
            try
            {
                // 如果 NickName 为空，使用默认范围
                if (string.IsNullOrWhiteSpace(NickName))
                {
                    _senderRange = new Interval(0, 100);
                    return;
                }

                // 尝试解析类似 "0-100" 的 NickName
                var parts = NickName.Split('-');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double min) &&
                    double.TryParse(parts[1], out double max))
                {
                    _senderRange = new Interval(min, max);
                }
                else
                {
                    // 如果解析失败，使用默认范围
                    _senderRange = new Interval(0, 100);
                }
            }
            catch
            {
                // 发生任何异常，使用默认范围
                _senderRange = new Interval(0, 100);
            }
        }


        public override bool AppendMenuItems(ToolStripDropDown menu)
        {
            
            Menu_AppendObjectNameEx(menu);
            Menu_AppendSeparator(menu);

            // 创建"跳转到Event"的子菜单
            ToolStripMenuItem jumpToEventMenu = Menu_AppendItem(menu, "跳转到Event");

            // 获取所有关联的Event组件及其所在的组
            var eventGroups = new Dictionary<string, IGH_DocumentObject>();
            foreach (var recipient in this.Recipients)
            {
                var eventComponent = recipient.Attributes.GetTopLevel.DocObject;
                if (eventComponent == null) continue;

                // 获取组件所在的组
                var group = Instances.ActiveCanvas.Document.Objects
                    .OfType<GH_Group>()
                    .FirstOrDefault(g => g.ObjectIDs.Contains(eventComponent.InstanceGuid));

                string groupName = group != null ? group.NickName : "未分组";

                // 使用组名作为键，确保不重复
                if (!eventGroups.ContainsKey(groupName))
                {
                    eventGroups.Add(groupName, eventComponent);
                }
            }

            // 按组名排序并添加到菜单
            foreach (var pair in eventGroups.OrderBy(x => x.Key))
            {
                var menuItem = new ToolStripMenuItem(pair.Key);
                var eventComponent = pair.Value;
                menuItem.Click += (sender, e) => MotilityUtils.GoComponent(eventComponent);
                jumpToEventMenu.DropDownItems.Add(menuItem);
            }

            return true;
        }
        

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            // 如果已经有源组件，直接返回
            if (this.Sources.Count > 0) return;

            // 如果之前有保存的滑块 GUID，尝试重新连接
            if (_connectedSliderGuid != Guid.Empty)
            {
                var savedSlider = document.FindObject(_connectedSliderGuid, true) as GH_NumberSlider;
                if (savedSlider != null)
                {
                    this.AddSource(savedSlider);
                    return;
                }
            }

            // 原有的自动查找和连接最近滑块的逻辑
            var doc = OnPingDocument();
            if (doc == null) return;

            var sliders = doc.Objects
                .Where(o => o.GetType().ToString() == "Motion.Animation.MotionSlider")
                .Cast<GH_NumberSlider>()
                .ToList();

            if (sliders.Any())
            {
                var closestSlider = FindClosestSlider(sliders);
                if (closestSlider != null)
                {
                    this.AddSource(closestSlider);
                    _connectedSliderGuid = closestSlider.InstanceGuid;  // 保存连接的滑块 GUID
                }
            }
            UpdateConnectedSliderRange();

            // 查找并通知所有使用相同 NickName 的 EventComponent
            doc.ScheduleSolution(10, d => {
                var relatedEvents = d.Objects
                    .OfType<EventComponent>()
                    .Where(e => e.NickName == this.NickName);

                foreach (var eventComp in relatedEvents)
                {
                    if (eventComp.Params.Input.Count > 0 &&
                        eventComp.Params.Input[0].SourceCount == 0)
                    {
                        eventComp.Params.Input[0].AddSource(this);
                        eventComp.Params.Input[0].WireDisplay = Grasshopper.Kernel.GH_ParamWireDisplay.hidden;
                        eventComp.LinkToSender(this);
                    }
                }
            });
        }

        private GH_NumberSlider FindClosestSlider(List<GH_NumberSlider> sliders)
        {
            var myPivot = this.Attributes.Pivot;
            var closestDist = double.MaxValue;
            GH_NumberSlider closestSlider = null;

            foreach (var slider in sliders)
            {
                var sliderPivot = slider.Attributes.Pivot;
                var dist = Math.Abs(myPivot.Y - sliderPivot.Y);
                if (dist < closestDist && dist < 100)
                {
                    closestDist = dist;
                    closestSlider = slider;
                }
            }

            return closestSlider;
        }

        private void CheckSameNicknameSender(GH_Document doc)
        {
            if (doc == null) return;
            // 检查是否存在同名且非自身的 Sender
            var existingSender = doc.Objects
                .OfType<MotionSender>()
                .FirstOrDefault(s => s != this && s.NickName == NickName);

            if (existingSender == null) return;

            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas == null) return;

            ShowTemporaryMessage(canvas, $"已存在相同标识({NickName})的 Sender!");
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
            var doc = OnPingDocument();
            if (doc == null) return;

            if (Sources.Count == 0) return;

            var savedSlider = doc.FindObject(_connectedSliderGuid, true) as GH_NumberSlider;

            if (savedSlider != null)
            {
                this.AddSource(savedSlider);
            }

            if (Sources.Count == 0)
            {
                m_data.Clear();
                return;
            }

            var source = Sources[0];
            var allSourceData = source.VolatileData.AllData(true);
            if (!allSourceData.Any())
            {
                m_data.Clear();
                return;
            }

            var sourceValue = allSourceData.FirstOrDefault() as GH_Number;
            if (sourceValue == null)
            {
                m_data.Clear();
                return;
            }

            double value = sourceValue.Value;

            if (_senderRange == Interval.Unset || _senderRange.IsValid == false)
            {
                UpdateRangeFromNickname();
                _senderRange = new Interval(0, 100);
            }

            double outputValue;
            if (value < _senderRange.Min)
                outputValue = _senderRange.Min;
            else if (value > _senderRange.Max)
                outputValue = _senderRange.Max;
            else
                outputValue = value;

            bool valueChanged = double.IsNaN(_previousValue) || outputValue != _previousValue;
            if (valueChanged)
            {
                CheckSameNicknameSender(doc);
                _previousValue = outputValue;
            }

            m_data.Clear();
            m_data.Append(new GH_Number(outputValue));
        }


        public override void RemovedFromDocument(GH_Document document)
        {
            try
            {
                document.UndoUtil.RecordEvent("Remove Motion Sender");

                // 在文档中查找所有的MotionSlider
                var sliders = document.Objects
                    .OfType<MotionSlider>()
                    .ToList();

                // 如果找到了滑块，尝试更新其范围
                if (sliders.Any())
                {
                    foreach (var slider in sliders)
                    {
                        slider.UpdateRangeBasedOnSenders();
                    }
                }

                if (_connectedSliderGuid != Guid.Empty)
                {
                    var slider = document.FindObject(_connectedSliderGuid, true) as GH_NumberSlider;
                    if (slider != null)
                    {
                        document.UndoUtil.RecordEvent("Reset Slider Name");
                        slider.NickName = "Slider";
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error handling MotionSender removal: {ex.Message}");
            }
            finally
            {
                base.RemovedFromDocument(document);
            }
        }

        public void SetNicknameWithUndo(string newNickname)
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 使用专门的昵称更改撤销记录
            GH_UndoRecord record = doc.UndoUtil.CreateNickNameEvent("修改 Motion Sender 区间", this);
            doc.UndoServer.PushUndoRecord(record);
            // 记录会自动保存当前昵称状态，我们只需要修改昵称即可
            this.NickName = newNickname;

            // 确保变更生效
            doc.ScheduleSolution(5);
        }

        public void CreateSenderWithUndo(GH_Document doc)
        {
            if (doc == null) return;

            // 初始化Attributes
            if (this.Attributes == null)
                this.CreateAttributes();

            // 记录操作前的状态
            GH_UndoRecord record = doc.UndoUtil.CreateAddObjectEvent("新建 Motion Sender", this);

            // 确保记录被添加到撤销栈中
            doc.UndoServer.PushUndoRecord(record);

            doc.AddObject(this, false);
            doc.Modified();
            doc.ScheduleSolution(5);
        }

        private void ShowTemporaryMessage(GH_Canvas canvas, string message)
        {
            // 创建一个自定义事件处理器来绘制消息
            GH_Canvas.CanvasPrePaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                // 设置消息位置（在组件下方）
                PointF location = new PointF(
                    this.Attributes.Bounds.Right+20,
                    this.Attributes.Bounds.Top+4
                );

                // 计算文本小
                SizeF textSize = GH_FontServer.MeasureString(message, GH_FontServer.Standard);
                RectangleF textBounds = new RectangleF(location, textSize);
                textBounds.Inflate(6, 3);  // 添加一些内边距

                // 绘制消息
                GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Pink,
                    message);

                capsule.Render(g, Color.LightSkyBlue);
                capsule.Dispose();
            };

            // 添加临时事件处理器
            canvas.CanvasPrePaintObjects += canvasRepaint;

            // 设置定时器移除事件处理器
            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (sender, e) =>
            {
                canvas.CanvasPrePaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        public override string NickName
        {
            get
            {
                nicknameKey = base.NickName;
                return nicknameKey;
            }
            set
            {
                if (nicknameKey == value) return;

                nicknameKey = value;
                base.NickName = nicknameKey;
                UpdateRangeFromNickname();
                UpdateConnectedSliderRange();

                var doc = this.OnPingDocument();
                if (doc == null) return;

                    doc.UndoUtil.RecordEvent("修改 Motion Sender 区间");

                var existingSender = doc.Objects
                    .OfType<MotionSender>()
                    .FirstOrDefault(s => s != this && s.NickName == nicknameKey);

                if (existingSender != null)
                {
                    ShowDuplicateNicknameMessage();
                    return;
                }

                // 通过 Recipients 查找所有关联的 EventComponent
                var relatedEvents = new HashSet<EventComponent>();
                foreach (var recipient in this.Recipients)
                {
                    var eventComp = recipient.Attributes.GetTopLevel.DocObject as EventComponent;
                    if (eventComp != null)
                    {
                        relatedEvents.Add(eventComp);
                    }
                }

                // 更新所有关联的 EventComponent 的昵称
                foreach (var eventComp in relatedEvents)
                {
                    eventComp.NickName = nicknameKey;
                }

                NickNameChanged?.Invoke(this, nicknameKey);
            }
        }

        private void ShowDuplicateNicknameMessage()
        {
            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas != null)
            {
                ShowTemporaryMessage(canvas, $"已存在相同标识({nicknameKey})的 Sender!");
            }
        }

        internal void UpdateConnectedSliderRange()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 获取所有源组件中的 MotionSlider
            foreach (var source in Sources)
            {
                if (source is MotionSlider slider)
                {
                    slider.UpdateRangeBasedOnSenders();
                }
            }
        }


        

        #region Overriding Name and Description
        public override string TypeName => "Motion Sender";

        public override string Category
        {
            get => "Motion";
            set => base.Category = value;
        }
        public override string SubCategory
        {
            get => "01_Animation";
            set => base.SubCategory = value;
        }

        public override string Name
        {
            get => "Motion Sender";
            set => base.Name = value;
        }
        #endregion

        public override Guid ComponentGuid => new Guid("{28fb5992-ed75-4c89-ae8a-3cb4bb3c5227}");
        protected override Bitmap Icon => Properties.Resources.Sender;

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            try
            {
                writer.SetString("NicknameKey", nicknameKey);
                writer.SetGuid("ConnectedSliderGuid", _connectedSliderGuid);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;

            try
            {
                if (reader.ItemExists("NicknameKey"))
                    nicknameKey = reader.GetString("NicknameKey");
                if (reader.ItemExists("ConnectedSliderGuid"))
                    _connectedSliderGuid = reader.GetGuid("ConnectedSliderGuid");
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
