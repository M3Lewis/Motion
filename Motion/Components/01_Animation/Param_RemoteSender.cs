using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class Param_RemoteSender : RemoteParam
    {
        private readonly bool _autoRename = true;
        
        public delegate void NickNameChangedEventHandler(IGH_DocumentObject sender, string newNickName);
        public event NickNameChangedEventHandler NickNameChanged;

        public Param_RemoteSender()
            : base()
        {
            nicknameKey = "";
            base.NickName = nicknameKey;
            base.Hidden = true;
        }

        protected string nicknameKey = "";
        private Guid _connectedSliderGuid = Guid.Empty;

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            
            if (this.Sources.Count > 0) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            var sliders = doc.Objects
                .Where(o => o.GetType().ToString() == "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider"|| o.GetType().ToString() == "Motion.Animation.MotionSlider")
                .Cast<GH_NumberSlider>()
                .ToList();

            if (sliders.Any())
            {
                var closestSlider = FindClosestSlider(sliders);
                if (closestSlider != null)
                {
                    this.AddSource(closestSlider);
                    
                    if (_autoRename)
                    {
                        UpdateNicknameFromSlider(closestSlider);
                        closestSlider.NickName = this.NickName;

                        // 延迟执行重名检查
                        doc.ScheduleSolution(10, (doc) =>
                        {
                            var existingSender = doc.Objects
                                .OfType<Param_RemoteSender>()
                                .FirstOrDefault(s => s != this && s.NickName == this.NickName);

                            if (existingSender != null)
                            {
                                var canvas = Grasshopper.Instances.ActiveCanvas;
                                if (canvas != null)
                                {
                                    ShowTemporaryMessage(canvas, 
                                        $"已存在相同标识({this.NickName})的 Sender!");
                                }
                            }
                        });
                    }
                }
            }
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

        private void UpdateNicknameFromSlider(GH_NumberSlider slider)
        {
            var range = new Interval((double)slider.Slider.Minimum, (double)slider.Slider.Maximum);
            var rangeStr = range.ToString();
            var splitStr = rangeStr.Split(',');
            var newNickname = string.Join("-", splitStr);

            this.NickName = newNickname;
            slider.NickName = newNickname;

            //var doc = OnPingDocument();
            //if (doc != null)
            //{
            //    foreach (var recipient in this.Recipients)
            //    {
            //        if (recipient is Param_RemoteReceiver receiver)
            //        {
            //            receiver.NickName = newNickname;

            //            var locations = doc.Objects
            //                .OfType<Param_RemoteLocation>()
            //                .Where(loc => loc._linkedReceiverGuid == receiver.InstanceGuid);

            //            var targets = doc.Objects
            //                .OfType<Param_RemoteTarget>()
            //                .Where(tar => tar._linkedReceiverGuid == receiver.InstanceGuid);

            //            foreach (var location in locations)
            //            {
            //                location.NickName = newNickname;
            //                location.ReconnectToMergeComponents();
            //            }

            //            foreach (var target in targets)
            //            {
            //                target.NickName = newNickname;
            //                target.ReconnectToMergeComponents();
            //            }
            //        }
            //    }
            //}
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();

            if (_autoRename && this.Sources.Count == 1)
            {
                var source = this.Sources[0];
                if (source is GH_NumberSlider slider)
                {
                    _connectedSliderGuid = slider.InstanceGuid;
                    UpdateNicknameFromSlider(slider);
                    slider.NickName = this.NickName;
                }
            }

            var doc = OnPingDocument();
            if (doc != null)
            {
                // 检查是否存在同名的 Sender
                var existingSender = doc.Objects
                    .OfType<Param_RemoteSender>()
                    .FirstOrDefault(s => s != this && s.NickName == this.NickName);

                if (existingSender != null)
                {
                    var canvas = Grasshopper.Instances.ActiveCanvas;
                    if (canvas != null)
                    {
                        ShowTemporaryMessage(canvas,
                            $"已存在相同标识({this.NickName})的 Sender!");
                    }
                }
            }
        }


        public override void RemovedFromDocument(GH_Document document)
        {
            try
            {
                if (_connectedSliderGuid != Guid.Empty)
                {
                    var slider = document.FindObject(_connectedSliderGuid,true) as GH_NumberSlider;
                    if (slider != null)
                    {
                        slider.NickName = "Slider";
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error resetting slider nickname: {ex.Message}");
            }
            finally
            {
                base.RemovedFromDocument(document);
            }
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            // 完全移除右键菜单选项
            base.AppendAdditionalMenuItems(menu);
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
                    this.Attributes.Bounds.Left,
                    this.Attributes.Bounds.Bottom + 25
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

        //public override string NickName
        //{
        //    get => nicknameKey;
        //    set
        //    {
        //        if (nicknameKey != value)
        //        {
        //            nicknameKey = value;
        //            base.NickName = nicknameKey;
        //            NickNameChanged?.Invoke(this, nicknameKey);  // 触发事件
        //            ExpireSolution(true);
        //        }
        //    }
        //}
        public override string NickName
        {
            get
            {
                nicknameKey = base.NickName;
                return nicknameKey;
            }
            set
            {
                if (nicknameKey != value)
                {
                    nicknameKey = value;
                    base.NickName = nicknameKey;

                    GH_Document doc = this.OnPingDocument();
                    if (doc != null) 
                    {
                        var existingSender = doc.Objects
                            .OfType<Param_RemoteSender>()
                            .FirstOrDefault(s => s != this && s.NickName == nicknameKey);

                        if (existingSender != null)
                        {
                            var canvas = Grasshopper.Instances.ActiveCanvas;
                            if (canvas != null)
                            {
                                ShowTemporaryMessage(canvas, 
                                    $"已存在相同标识({nicknameKey})的 Sender!");
                            }
                            
                            return;
                        }

                        // 触发 NickNameChanged 事件
                        NickNameChanged?.Invoke(this, nicknameKey);
                        
                        //doc.ScheduleSolution(10, MotilityUtils.connectMatchingParams);
                    }
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
                writer.SetString("ConnectedSliderGuid", _connectedSliderGuid.ToString());
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
                    _connectedSliderGuid = new Guid(reader.GetString("ConnectedSliderGuid"));
            }
            catch
            {
                return false;
            }
            
            return true;
        }
    }
}
