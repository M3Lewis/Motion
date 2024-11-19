using Grasshopper.Kernel;
using System;
using System.Drawing;
using GH_IO.Serialization;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using System.Linq;

namespace Motion.Motility
{
    public class Param_RemoteReceiver : RemoteParam
    {
        public delegate void NickNameChangedEventHandler(IGH_DocumentObject sender, string newNickName);
        public event NickNameChangedEventHandler NickNameChanged;

        public Param_RemoteReceiver()
            : base()
        {
            nicknameKey = "";
            base.NickName = nicknameKey;
            base.WireDisplay = GH_ParamWireDisplay.hidden;
            base.Hidden = true;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ScheduleSolution(10, doc =>
            {
                UpdateGroupVisibilityAndLock();
            });
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
            UpdateGroupVisibilityAndLock();
        }

        public override void ExpireSolution(bool recompute)
        {
            base.ExpireSolution(recompute);
            if (recompute)
            {
                UpdateGroupVisibilityAndLock();
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            try
            {
                writer.SetString("NicknameKey", nicknameKey);
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
            }
            catch
            {
                return false;
            }

            return true;
        }

        protected string nicknameKey = "";

        public override string NickName
        {
            get => nicknameKey;
            set
            {
                if (nicknameKey != value)
                {
                    nicknameKey = value;
                    base.NickName = nicknameKey;
                    NickNameChanged?.Invoke(this, nicknameKey);
                    ExpireSolution(true);
                }
            }
        }

        #region Overriding Name and Description
        public override string TypeName => "Motion Receiver";

        public override string Category
        {
            get => "Motion";
            set => base.Category = value;
        }
        
        public override string SubCategory
        {
            get => "04_Motility";
            set => base.SubCategory = value;
        }

        public override string Name
        {
            get => "Motion Receiver";
            set => base.Name = value;
        }
        #endregion

        public override Guid ComponentGuid => new Guid("{3f65d28a-8f48-4b85-9bc4-7ce36260d062}");

        protected override Bitmap Icon => Properties.Resources.Receiver;

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

                // 计算文本大小
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
            timer.Interval = 2000;
            timer.Tick += (sender, e) =>
            {
                canvas.CanvasPrePaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private bool HasDuplicateReceiversAndData()
        {
            var doc = OnPingDocument();
            if (doc == null) return false;

            // 检查是否存在重复的Receiver
            var receivers = doc.Objects
                .OfType<Param_RemoteReceiver>()
                .Where(r => r.NickName == this.NickName && r != this)  // 排除自身
                .ToList();

            if (receivers.Count == 0) return false;  // 如果没有重复的Receiver，直接返回false

            // 检查是否存在相同nickname的RemoteTarget或RemoteLocation
            var existingTargets = doc.Objects
                .OfType<Param_RemoteTarget>()
                .Any(t => t.NickName == this.NickName);

            var existingLocations = doc.Objects
                .OfType<Param_RemoteLocation>()
                .Any(l => l.NickName == this.NickName);

            // 只有当同时存在重复的Receiver和相关的数据时才返回true
            return existingTargets || existingLocations;
        }

        internal void CreateRemoteData()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 检查是否同时存在重复的Receiver和相关数据
            if (HasDuplicateReceiversAndData())
            {
                var canvas = Grasshopper.Instances.ActiveCanvas;
                if (canvas != null)
                {
                    ShowTemporaryMessage(canvas, 
                        $"存在多个相同标识({this.NickName})的 Receiver 且已创建Camera数据，无法重复创建!");
                }
                return;
            }

            try
            {
                // 创建 Location 参数
                var locationParam = new Param_RemoteLocation();
                locationParam.NickName = this.NickName;
                locationParam.LinkToReceiver(this);
                // 先设置位置
                locationParam.CreateAttributes();
                locationParam.Attributes.Pivot = new PointF(
                    this.Attributes.Pivot.X + 600,
                    this.Attributes.Pivot.Y
                );
                // 再添加到文档
                doc.AddObject(locationParam, false);

                // 创建 Target 参数
                var targetParam = new Param_RemoteTarget();
                targetParam.NickName = this.NickName;
                targetParam.LinkToReceiver(this);
                // 先设置位置
                targetParam.CreateAttributes();
                targetParam.Attributes.Pivot = new PointF(
                    this.Attributes.Pivot.X + 600,
                    this.Attributes.Pivot.Y + 100
                );
                // 再添加到文档
                doc.AddObject(targetParam, false);

                // 强制更新文档
                doc.DestroyAttributeCache();
                doc.ScheduleSolution(5);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to create remote data: {ex.Message}");
            }
        }
    }
}
