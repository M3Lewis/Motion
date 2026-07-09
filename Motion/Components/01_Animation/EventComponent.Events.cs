using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.GUI.Base;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Motion.Animation
{
    public partial class EventComponent
    {
        // 添加文档加载完成后的初始化方法
        private void InitializeAfterLoad()
        {
            if (_isInitialized) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找或等待 Timeline Slider
            FindAndConnectTimelineSlider();

            // 添加文档事件监听，以便在后续添加 Timeline Slider 时能够检测到
            doc.ObjectsAdded += Doc_ObjectsAdded;

            _isInitialized = true;
            UpdateGroupVisibilityAndLock();
        }

        private void SafeExecute(string methodName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"[Motion] {methodName} 出错: {ex.Message}");
            }
        }

        private void Doc_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            SafeExecute(nameof(Doc_ObjectsAdded), () => HandleDocObjectsAdded(e));
        }

        private void HandleDocObjectsAdded(GH_DocObjectEventArgs e)
        {
            var doc = OnPingDocument();
            var addedUnionSlider = e.Objects.FirstOrDefault(obj => obj is MotionSlider);
            if (addedUnionSlider == null || doc == null) return;

            doc.ScheduleSolution(5, d =>
            {
                SafeExecute(nameof(HandleDocObjectsAdded) + ".ScheduleSolution", () =>
                {
                    FindAndConnectTimelineSlider();
                    ExpireSolution(true);
                });
            });
        }

        private void FindAndConnectTimelineSlider()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找 Timeline Slider
            _timelineSlider = doc.Objects
                .OfType<GH_NumberSlider>()
                .FirstOrDefault();

            if (_timelineSlider != null)
            {
                var numberSlider = _timelineSlider as GH_NumberSlider;
                if (numberSlider?.Slider != null)
                {
                    numberSlider.Slider.ValueChanged -= OnSliderValueChanged;
                    numberSlider.Slider.ValueChanged += OnSliderValueChanged;
                }
            }
        }

        // 修正事件处理器的参数类型
        private void OnSliderValueChanged(object sender, GH_SliderEventArgs e)
        {
            SafeExecute(nameof(OnSliderValueChanged), UpdateGroupVisibilityAndLock);
        }

        public void LinkToSender(MotionSender sender)
        {
            if (_linkedSender == sender) return;

            if (_linkedSender != null)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
            }

            _linkedSender = sender;
            // 确保 NickName 与 Sender 保持一致
            if (this.NickName != sender.NickName)
            {
                this.NickName = sender.NickName;
            }

            UpdateMessage(); // 更Message

            // 重新订阅事件
            sender.NickNameChanged -= OnSenderNickNameChanged; // 先取消订阅以避免重复
            sender.NickNameChanged += OnSenderNickNameChanged;
        }

        private void OnSenderNickNameChanged(IGH_DocumentObject sender, string newNickName)
        {
            SafeExecute(nameof(OnSenderNickNameChanged), () =>
            {
                if (this.NickName != newNickName)
                {
                    this.NickName = newNickName;
                    UpdateMessage();
                    ExpireSolution(true);
                }
            });
        }

        private void HandleConnectionsOnNicknameChange()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var timeInput = this.Params.Input[0];

            // 如果新的nickname与原linked sender不同，断开连接
            if (_linkedSender != null && _linkedSender.NickName != base.NickName)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
                timeInput.RemoveAllSources();
            }

            // 查找匹配的sender并连接
            doc.ScheduleSolution(5, d =>
            {
                var matchingSenders = d.Objects
                    .OfType<MotionSender>()
                    .Where(s => s.NickName == base.NickName)
                    .ToList();

                foreach (var sender in matchingSenders)
                {
                    if (!timeInput.Sources.Contains(sender))
                    {
                        // 移除现有连接
                        timeInput.RemoveAllSources();

                        // 建立新连接
                        timeInput.AddSource(sender);
                        timeInput.WireDisplay = GH_ParamWireDisplay.hidden;

                        // 更新linked sender
                        LinkToSender(sender);
                        break; // 只连接第一个匹配的sender
                    }
                }
            });
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            // 监听文档的对象添加事件
            document.ObjectsAdded += Document_ObjectsAdded;

            // 延迟执行以确保所有组件都已加载
            document.ScheduleSolution(5, doc =>
            {
                // 监听输入端的变化
                this.Params.Input[0].ObjectChanged += Input_ObjectChanged;

                // 检查现有连接
                var timeInput = this.Params.Input[0];
                if (timeInput.SourceCount > 0)
                {
                    var source = timeInput.Sources[0].Attributes.GetTopLevel.DocObject;
                    if (source is MotionSender remoteSender)
                    {
                        LinkToSender(remoteSender);
                    }
                }
                else
                {
                    // 如果没有连接，尝试查找匹配的 Sender
                    TryConnectToMatchingSender();
                }
            });

            // 添加对象删除事件监听
            document.ObjectsDeleted += Document_ObjectsDeleted;
        }

        private void Document_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            SafeExecute(nameof(Document_ObjectsAdded), () => HandleDocumentObjectsAdded(e));
        }

        private void HandleDocumentObjectsAdded(GH_DocObjectEventArgs e)
        {
            var doc = OnPingDocument();
            // 检查是否添加了匹配 of MotionSender
            var addedSender = e.Objects
                .OfType<MotionSender>()
                .FirstOrDefault(s => s.NickName == this.NickName);

            if (addedSender == null || doc == null) return;

            // 延迟执行以确保 Sender 完全初始化
            doc.ScheduleSolution(5, d =>
            {
                SafeExecute(nameof(HandleDocumentObjectsAdded), () =>
                {
                    var timeInput = this.Params.Input[0];
                    if (timeInput.SourceCount == 0) // 只在没有连接时尝试连接
                    {
                        timeInput.AddSource(addedSender);
                        timeInput.WireDisplay = GH_ParamWireDisplay.hidden;
                        LinkToSender(addedSender);
                    }
                });
            });
        }

        private void TryConnectToMatchingSender()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var matchingSender = doc.Objects
                .OfType<MotionSender>()
                .FirstOrDefault(s => s.NickName == this.NickName);

            if (matchingSender != null)
            {
                var timeInput = this.Params.Input[0];
                timeInput.AddSource(matchingSender);
                timeInput.WireDisplay = GH_ParamWireDisplay.hidden;
                LinkToSender(matchingSender);

                // 确保 NickName 与 Sender 保持一致
                if (this.NickName == matchingSender.NickName) return;

                this.NickName = matchingSender.NickName;
            }
        }


        private void Input_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            SafeExecute(nameof(Input_ObjectChanged), () =>
            {
                var timeInput = this.Params.Input[0];
                if (timeInput.SourceCount > 0)
                {
                    var source = timeInput.Sources[0].Attributes.GetTopLevel.DocObject;
                    if (source is MotionSender remoteSender)
                    {
                        LinkToSender(remoteSender);
                    }
                }
                else if (_linkedSender != null)
                {
                    // 断开连接时清理
                    _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                    _linkedSender = null;
                }
            });
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            // 清理事件订阅
            if (document != null)
            {
                document.ObjectsAdded -= Document_ObjectsAdded;
                document.ObjectsDeleted -= Document_ObjectsDeleted;
            }

            if (this.Params.Input.Count > 0)
            {
                this.Params.Input[0].ObjectChanged -= Input_ObjectChanged;
            }

            if (_linkedSender != null)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
            }

            base.RemovedFromDocument(document);
        }

        // 添加删除事件处理方法
        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            SafeExecute(nameof(Document_ObjectsDeleted), () =>
            {
                bool needUpdate = false;

                // 检查是否有受控制的对象被删除
                foreach (var deletedObj in e.Objects)
                {
                    if (!affectedObjects.Contains(deletedObj)) continue;
                    affectedObjects.Remove(deletedObj);
                    needUpdate = true;
                }

                // 如果没有任何受控制的对象，关闭 HIDE/LOCK 开关
                if (needUpdate && !affectedObjects.Any())
                {
                    HideWhenEmpty = false;
                    LockWhenEmpty = false;

                    // 更新UI
                    ExpireSolution(true);
                }
            });
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            base.DocumentContextChanged(document, context);

            if (context == GH_DocumentContext.Close)
            {
                if (_timelineSlider != null)
                {
                    var numberSlider = _timelineSlider as GH_NumberSlider;
                    if (numberSlider?.Slider != null)
                    {
                        numberSlider.Slider.ValueChanged -= OnSliderValueChanged;
                    }

                    _timelineSlider = null;
                }

                _isInitialized = false;
            }
        }
    }
}