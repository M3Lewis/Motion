using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Motility
{
    public abstract class Param_RemoteCameraPointBase : Param_GenericObject
    {
        protected Param_RemoteReceiver _linkedReceiver;
        internal Guid _linkedReceiverGuid = Guid.Empty;

        protected Param_RemoteCameraPointBase() : base()
        {
            Category = "Motion";
            SubCategory = "04_Motility";
            this.Hidden = true;
        }

        protected override Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override void CreateAttributes()
        {
            m_attributes = new RemoteDataParamAttributes(this);
        }

        internal void ShowConnectionMenu(GH_Canvas canvas, PointF canvasLocation)
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            // 添加 Connect 按钮
            ToolStripMenuItem connectItem = new ToolStripMenuItem("Connect");
            connectItem.Click += (sender, e) => ReconnectToMergeComponents();
            menu.Items.Add(connectItem);

            // 添加分隔符
            menu.Items.Add(new ToolStripSeparator());

            // 添加 Disconnect 按钮
            ToolStripMenuItem disconnectItem = new ToolStripMenuItem("Disconnect");
            disconnectItem.Click += (sender, e) => DisconnectOutputs();
            menu.Items.Add(disconnectItem);

            Point screenPoint = canvas.PointToScreen(new Point((int)canvasLocation.X, (int)canvasLocation.Y));
            Size menuSize = menu.PreferredSize;
            Rectangle screenBounds = Screen.FromPoint(screenPoint).WorkingArea;
            
            int x = screenPoint.X;
            int y = screenPoint.Y;

            if (x + menuSize.Width > screenBounds.Right)
            {
                x = screenBounds.Right - menuSize.Width;
            }

            if (y + menuSize.Height > screenBounds.Bottom)
            {
                y = screenPoint.Y - menuSize.Height;
            }

            menu.Show(x, y);
        }

        protected abstract void ConnectToMergeComponent();

        private void DisconnectOutputs()
        {
            var recipientsToDisconnect = this.Recipients.ToList();
            foreach (var recipient in recipientsToDisconnect)
            {
                recipient.RemoveSource(this);
            }
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
            this.NickName = receiver.NickName;
            receiver.NickNameChanged += OnReceiverNickNameChanged;
            this.ExpireSolution(true);
            this.OnDisplayExpired(true);
        }

        private void OnReceiverNickNameChanged(IGH_DocumentObject sender, string newNickName)
        {
            Grasshopper.Instances.ActiveCanvas.BeginInvoke((Action)(() =>
            {
                if (this.NickName != newNickName)
                {
                    // 更新 NickName
                    this.NickName = newNickName;
                    
                    // 调用子类特定的重连方法
                    ReconnectToMergeComponents();
                    
                    this.ExpireSolution(true);
                    this.OnDisplayExpired(true);
                }
            }));
        }

        // 改为抽象方法，让子类必须实现
        public abstract void ReconnectToMergeComponents();

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
            if (_linkedReceiver == null && _linkedReceiverGuid != Guid.Empty)
            {
                var receiver = document.FindObject(_linkedReceiverGuid, false) as Param_RemoteReceiver;
                if (receiver != null)
                {
                    LinkToReceiver(receiver);
                }
            }
        }

        protected void Document_SolutionEnd(object sender, GH_SolutionEventArgs e)
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
        protected abstract Param_RemoteCameraPointBase CreateInstance();
    }

    public class RemoteDataParamAttributes : RemoteParamAttributes
    {
        public RemoteDataParamAttributes(IGH_Param owner) : base(owner) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner is Param_RemoteCameraPointBase dataParam)
            {
                dataParam.ShowConnectionMenu(sender, e.ControlLocation);
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }
    }
} 