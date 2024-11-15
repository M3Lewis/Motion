using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Motion.Animation;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Motility
{
    public class Param_RemoteData : Param_GenericObject, ICloneable
    {
        private Param_RemoteReceiver _linkedReceiver;
        private Guid _linkedReceiverGuid = Guid.Empty;

        public Param_RemoteData() : base()
        {
            Name = "Motion Data";
            NickName = "MData";
            Description = "Motion Data Parameter";
            Category = "Motion";
            SubCategory = "04_Motility";

            this.Hidden = true;
        }

        public override Guid ComponentGuid => new Guid("A45D8759-6C07-4C65-8E99-D2E6E2E678D9");
        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override void CreateAttributes()
        {
            m_attributes = new RemoteDataParamAttributes(this);
        }
        
        #region Component Info
        public override string TypeName => "Motion Data";

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
            get => "Motion Data";
            set => base.Name = value;
        }
        #endregion

        internal void ShowConnectionMenu(GH_Canvas canvas, PointF canvasLocation)
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem locationItem = new ToolStripMenuItem("Location");
            locationItem.Click += (sender, e) => ConnectToMergeComponent("MotionLocation");
            menu.Items.Add(locationItem);

            ToolStripMenuItem targetItem = new ToolStripMenuItem("Target");
            targetItem.Click += (sender, e) => ConnectToMergeComponent("MotionTarget");
            menu.Items.Add(targetItem);

            // 添加分隔线
            menu.Items.Add(new ToolStripSeparator());

            // 添加 Disconnect 按钮
            ToolStripMenuItem disconnectItem = new ToolStripMenuItem("Disconnect");
            disconnectItem.Click += (sender, e) => DisconnectOutputs();
            menu.Items.Add(disconnectItem);

            // 将画布坐标转换为屏幕坐标
            Point screenPoint = canvas.PointToScreen(new Point((int)canvasLocation.X, (int)canvasLocation.Y));

            // 获取菜单的预期大小
            Size menuSize = menu.PreferredSize;

            // 确保菜单不会超出屏幕边界
            Rectangle screenBounds = Screen.FromPoint(screenPoint).WorkingArea;
            
            int x = screenPoint.X;
            int y = screenPoint.Y;

            // 如果菜单会超出屏幕右边界，则向左偏移
            if (x + menuSize.Width > screenBounds.Right)
            {
                x = screenBounds.Right - menuSize.Width;
            }

            // 如果菜单会超出屏幕下边界，则向上偏移
            if (y + menuSize.Height > screenBounds.Bottom)
            {
                y = screenPoint.Y - menuSize.Height;
            }

            // 显示菜单
            menu.Show(x, y);
        }

        private void ConnectToMergeComponent(string mergeNickName)
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找所有 MergeMotionData 组件
            var mergeComps = doc.Objects
                .OfType<MergeMotionData>()
                .Where(m => m.NickName == mergeNickName)
                .ToList();

            foreach (var mergeComp in mergeComps)
            {
                // 查找匹配的输入端
                foreach (var param in mergeComp.Params.Input)
                {
                    if (param.NickName == this.NickName && !param.Sources.Contains(this))
                    {
                        param.AddSource(this);
                        break;
                    }
                }
            }

            ExpireSolution(true);
        }

        private void DisconnectOutputs()
        {
            // 创建一个临时列表来存储需要断开的连接
            var recipientsToDisconnect = this.Recipients.ToList();

            // 断开所有输出端的连线
            foreach (var recipient in recipientsToDisconnect)
            {
                recipient.RemoveSource(this);
            }

            // 强制更新
            this.ExpireSolution(true);
            this.OnDisplayExpired(true);
        }

        public void LinkToReceiver(Param_RemoteReceiver receiver)
        {
            if (_linkedReceiver != null)
            {
                _linkedReceiver.NickNameChanged -= OnReceiverNickNameChanged;
            }

            _linkedReceiver = receiver;
            _linkedReceiverGuid = receiver.InstanceGuid;
            
            // 立即更新昵称
            this.NickName = receiver.NickName;
            
            // 确保事件订阅
            receiver.NickNameChanged += OnReceiverNickNameChanged;
            
            // 强制更新
            this.ExpireSolution(true);
            this.OnDisplayExpired(true);
        }

        private void OnReceiverNickNameChanged(IGH_DocumentObject sender, string newNickName)
        {
            // 确保在主线程中执行
            Grasshopper.Instances.ActiveCanvas.BeginInvoke((Action)(() =>
            {
                if (this.NickName != newNickName)
                {
                    this.NickName = newNickName;
                    this.ExpireSolution(true);
                    this.OnDisplayExpired(true);
                }
            }));
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (_linkedReceiver != null)
            {
                _linkedReceiver.NickNameChanged -= OnReceiverNickNameChanged;
                _linkedReceiver = null;
            }
            document.SolutionEnd -= Document_SolutionEnd;
            base.RemovedFromDocument(document);
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;
            
            try
            {
                writer.SetGuid("LinkedReceiver", _linkedReceiverGuid);
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
                if (reader.ItemExists("LinkedReceiver"))
                {
                    _linkedReceiverGuid = reader.GetGuid("LinkedReceiver");
                }
            }
            catch
            {
                return false;
            }
            
            return true;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            
            // 立即尝试重新连接
            if (_linkedReceiver == null && _linkedReceiverGuid != Guid.Empty)
            {
                var receiver = document.FindObject(_linkedReceiverGuid, false) as Param_RemoteReceiver;
                if (receiver != null)
                {
                    LinkToReceiver(receiver);
                }
            }
        }

        private void Document_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            var doc = OnPingDocument();
            if (doc != null)
            {
                doc.SolutionEnd -= Document_SolutionEnd;
                
                if (_linkedReceiver == null && _linkedReceiverGuid != Guid.Empty)
                {
                    var receiver = doc.FindObject(_linkedReceiverGuid, false) as Param_RemoteReceiver;
                    if (receiver != null)
                    {
                        LinkToReceiver(receiver);
                    }
                }
            }
        }

        public object Clone()
        {
            // 创建新的 Param_RemoteData 实例
            var dup = new Param_RemoteData();

            // 复制基本属性
            dup.Name = this.Name;
            dup.NickName = this.NickName;
            dup.Description = this.Description;
            dup.Category = this.Category;
            dup.SubCategory = this.SubCategory;
            dup._linkedReceiverGuid = this._linkedReceiverGuid;

            // 获取文档
            var doc = OnPingDocument();
            if (doc != null)
            {
                // 查找并链接到对应的 Receiver
                var receiver = doc.FindObject(_linkedReceiverGuid, false) as Param_RemoteReceiver;
                if (receiver != null)
                {
                    // 使用已有的 LinkToReceiver 方法建立连接
                    dup.LinkToReceiver(receiver);
                }
            }

            return dup;
        }
    }

    public class RemoteDataParamAttributes : RemoteParamAttributes
    {
        public RemoteDataParamAttributes(Param_RemoteData owner) : base(owner) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner is Param_RemoteData dataParam)
            {
                dataParam.ShowConnectionMenu(sender, e.ControlLocation);
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }
    }
} 