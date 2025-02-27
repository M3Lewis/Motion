using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Motion.Export
{
    public class MotionSliderAnimator : GH_SliderAnimator
    {
        public Interval CustomRange { get; set; }
        public bool UseCustomRange { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public MotionSliderAnimator(GH_NumberSlider nOwner)
            : base(nOwner)
        {
            CustomRange = new Interval(0, 100);
            UseCustomRange = false;
            CancellationToken = CancellationToken.None;
        }

        public void MotionStartAnimation(bool isTransparent, string viewName, bool isCycles, 
            int realtimeRenderPasses, out List<string> outputPathList, out bool wasAborted, 
            Action<int, int> progressCallback = null)
        {
            outputPathList = null;
            wasAborted = false;
            StoreSettingsAsDefault();

            // 验证参数
            ValidateParameters();
            
            // 确保目标目录存在
            EnsureDirectoryExists();

            // 设置分辨率
            m_resolution.Width = Math.Max(m_resolution.Width, 24);
            m_resolution.Height = Math.Max(m_resolution.Height, 24);
            m_frameIndex = 0;

            // 获取文档
            GH_Document gH_Document = m_owner.OnPingDocument();
            if (gH_Document == null)
                return;

            // 开始时间
            long startTicks = DateTime.Now.Ticks;

            // 设置范围
            double currentValue, maxValue, minValue;
            SetRangeValues(out currentValue, out maxValue, out minValue);
            m_currentValue = currentValue;
            
            var doc = RhinoDoc.ActiveDoc;
            var myView = doc.Views.Find(viewName, true);
            if (myView == null)
            {
                RhinoApp.WriteLine("Could not find specified view: " + viewName);
                wasAborted = true;
                return;
            }
            
            var activeView = doc.Views.ActiveView;
            var displayMode = activeView.ActiveViewport.DisplayMode;
            string modeName = displayMode.EnglishName;
            bool isRaytracedMode = modeName == "Raytraced" || modeName == "光线跟踪";

            // 检查渲染模式兼容性
            if (!CheckRenderModeCompatibility(isCycles, isRaytracedMode))
            {
                wasAborted = true;
                return;
            }

            try
            {
                RealtimeDisplayMode cycles = myView.RealtimeDisplayMode;
                int oldPasses = -1;
                
                while (m_currentValue <= maxValue)
                {
                    // 检查取消条件
                    if (CancellationToken.IsCancellationRequested || GH_Document.IsEscapeKeyDown())
                    {
                        wasAborted = true;
                        break;
                    }

                    // 更新命令行状态
                    UpdateCommandPrompt(startTicks, currentValue, maxValue, minValue);

                    // 更新slider值并刷新
                    m_owner.Slider.Value = Convert.ToDecimal(m_currentValue);
                    RefreshViews(doc);

                    // 更新进度
                    int totalFrames = UseCustomRange ? (int)CustomRange.Length + 1 : (int)(maxValue - minValue) + 1;
                    progressCallback?.Invoke(m_frameIndex, totalFrames);

                    // 创建并保存帧
                    Bitmap bitmap = MotionCreateFrame(isTransparent, myView, isCycles, isRaytracedMode, realtimeRenderPasses, cycles, oldPasses);
                    if (bitmap != null)
                    {
                        SaveFrameToDisk(bitmap);
                        bitmap.Dispose();
                        
                        // 重置Cycles设置
                        if (cycles != null)
                        {
                            cycles.MaxPasses = oldPasses;
                            cycles.Paused = false;
                        }
                    }
                    else
                    {
                        // 处理创建帧失败的情况
                        HandleFrameCreationFailure(isCycles, isRaytracedMode);
                        wasAborted = true;
                        break;
                    }

                    m_currentValue += 1.0;
                    Application.DoEvents();
                }

                // 重置Slider到初始位置
                m_owner.Slider.Value = UseCustomRange ? (int)CustomRange.Min : 0;
                doc.Views.Redraw();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during animation: {ex.Message}");
                wasAborted = true;
            }

            // 输出完成消息
            RhinoApp.WriteLine(wasAborted ?
                "Animation cancelled by user." :
                $"Animation saved to disk: {m_folder}\\");
            RhinoApp.CommandPrompt = string.Empty;
        }

        private bool CheckRenderModeCompatibility(bool isCycles, bool isRaytracedMode)
        {
            // 如果需要Cycles渲染但不是光线追踪模式
            if (isCycles && !isRaytracedMode)
            {
                RhinoApp.WriteLine("Please open the raytraced display mode！");
                return false;
            }
            
            // 如果是光线追踪模式但没有启用Cycles
            if (!isCycles && isRaytracedMode)
            {
                RhinoApp.WriteLine("Please enable isCycles to render！");
                return false;
            }
            
            return true;
        }

        private void ValidateParameters()
        {
            if (m_fileTemplate == null)
                throw new Exception("File name mask is not valid");
            if (m_folder == null)
                throw new Exception("Destination folder path is not valid");
            if (m_owner == null)
                throw new Exception("Number slider reference cannot be resolved");
            if (m_frameCount < 1)
                throw new Exception("Insufficient frames for animation");
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(m_folder))
            {
                try
                {
                    Directory.CreateDirectory(m_folder);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to create destination folder: " + ex.Message);
                }
            }
        }

        private void SetRangeValues(out double currentValue, out double maxValue, out double minValue)
        {
            if (UseCustomRange)
            {
                currentValue = CustomRange.Min;
                maxValue = CustomRange.Max;
                minValue = Convert.ToDouble(m_owner.Slider.Minimum);
            }
            else
            {
                currentValue = Convert.ToDouble(m_owner.Slider.Minimum);
                maxValue = Convert.ToDouble(m_owner.Slider.Maximum);
                minValue = Convert.ToDouble(m_owner.Slider.Minimum);
            }
        }

        private void UpdateCommandPrompt(long startTicks, double currentValue, double maxValue, double minValue)
        {
            if (UseCustomRange)
            {
                RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex + (int)CustomRange.Min} of {(int)CustomRange.Min}-{(int)CustomRange.Max}. | Total:{(int)CustomRange.Max - (int)CustomRange.Min + 1} | Time left: {EstimateTimeLeftFormatted(startTicks)}";
            }
            else
            {
                RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex} of {m_owner.Slider.Minimum}-{m_owner.Slider.Maximum}. | Total:{m_owner.Slider.Maximum - m_owner.Slider.Minimum + 1} | Time left: {EstimateTimeLeftFormatted(startTicks)}";
            }
        }

        private void RefreshViews(RhinoDoc doc)
        {
            doc.Views.Redraw();
            Grasshopper.Instances.ActiveCanvas.Refresh();
            Grasshopper.Instances.RedrawAll();
        }

        private void SaveFrameToDisk(Bitmap bitmap)
        {
            string arg = string.Format(m_fileTemplate, m_frameIndex + (UseCustomRange ? (int)CustomRange.Min : 0));
            string filePath = string.Format("{0}{2}{1}", m_folder, arg, Path.DirectorySeparatorChar);
            bitmap.Save(filePath, ImageFormat.Png);
            RhinoApp.WriteLine($"Frame {m_frameIndex} saved to disk: {filePath}");
            m_frameIndex++;
        }

        private void HandleFrameCreationFailure(bool isCycles, bool isRaytracedMode)
        {
            // 使用switch语句替代嵌套的if-else
            switch (true)
            {
                case true when isCycles && !isRaytracedMode:
                    RhinoApp.WriteLine("Please open the raytraced display mode！");
                    break;
                case true when !isCycles && isRaytracedMode:
                    RhinoApp.WriteLine("Please enable isCycles to render！");
                    break;
                default:
                    RhinoApp.WriteLine($"Frame {m_frameIndex + (int)CustomRange.Min} failed to render");
                    break;
            }
        }

        public Bitmap MotionCreateFrame(bool isTransparent, RhinoView myView, bool isCycles,
            bool isRaytracedMode, int realtimeRenderPasses, RealtimeDisplayMode cycles, int oldPasses)
        {
            if (myView == null)
                return null;

            ViewCapture viewCapture;
            cycles = myView.RealtimeDisplayMode;
            
            // 使用switch语句替代嵌套if-else结构
            switch (true)
            {
                case true when isCycles && isRaytracedMode:
                    oldPasses = cycles.MaxPasses;
                    cycles.PostEffectsOn = true;
                    cycles.MaxPasses = 2;
                    cycles.Paused = true;
                    viewCapture = CreateCyclesViewCapture(isTransparent, realtimeRenderPasses);
                    break;
                    
                case true when !isCycles && isRaytracedMode:
                case true when isCycles && !isRaytracedMode:
                    return null;
                    
                default:
                    viewCapture = CreateStandardViewCapture(isTransparent);
                    break;
            }

            return viewCapture.CaptureToBitmap(myView);
        }
        
        private ViewCapture CreateCyclesViewCapture(bool isTransparent, int realtimeRenderPasses)
        {
            return new ViewCapture
            {
                Width = Width,
                Height = Height,
                ScaleScreenItems = false,
                DrawAxes = false,
                DrawGrid = false,
                DrawGridAxes = false,
                TransparentBackground = isTransparent,
                RealtimeRenderPasses = realtimeRenderPasses,
            };
        }
        
        private ViewCapture CreateStandardViewCapture(bool isTransparent)
        {
            return new ViewCapture
            {
                Width = Width,
                Height = Height,
                ScaleScreenItems = false,
                DrawAxes = false,
                DrawGrid = false,
                DrawGridAxes = false,
                TransparentBackground = isTransparent
            };
        }

        private string EstimateTimeLeftFormatted(long startTicks)
        {
            checked
            {
                // 使用缓存变量减少重复计算
                long elapsedTicks = DateTime.Now.Ticks - startTicks;
                long avgTicksPerFrame = (m_frameIndex + 1) > 0 ? elapsedTicks / (m_frameIndex + 1) : 0;
                long remainingTicks = (m_frameCount - m_frameIndex) * avgTicksPerFrame;
                
                // 使用TimeSpan代替手动计算
                TimeSpan remainingTime = new TimeSpan(remainingTicks);
                double totalSeconds = Math.Round(remainingTime.TotalSeconds * 0.2, 0) * 5.0;
                
                int minutes = (int)(totalSeconds / 60);
                int seconds = (int)(totalSeconds % 60);
                return $"{minutes}m{seconds}s";
            }
        }
    }
}
