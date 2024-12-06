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

namespace Motion.Export
{
    public class MotionSliderAnimator : GH_SliderAnimator
    {
        public Interval CustomRange { get; set; }
        public bool UseCustomRange { get; set; }

        public MotionSliderAnimator(GH_NumberSlider nOwner)
            : base(nOwner)
        {
            CustomRange = new Interval(0, 100);
            UseCustomRange = false;
        }

        public void MotionStartAnimation(bool isTransparent, string viewName, bool isCycles, int realtimeRenderPasses, out List<string> outputPathList, out bool wasAborted, Action<int, int> progressCallback = null)
        {
            outputPathList = null;
            wasAborted = false;
            StoreSettingsAsDefault();

            // ֤
            if (m_fileTemplate == null)
                throw new Exception("File name mask is not valid");
            if (m_folder == null)
                throw new Exception("Destination folder path is not valid");
            if (m_owner == null)
                throw new Exception("Number slider reference cannot be resolved");
            if (m_frameCount < 1)
                throw new Exception("Insufficient frames for animation");

            // Ŀ¼
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

            // ÷ֱ
            m_resolution.Width = Math.Max(m_resolution.Width, 24);
            m_resolution.Height = Math.Max(m_resolution.Height, 24);
            m_frameIndex = 0;

            // ȡĵ
            GH_Document gH_Document = m_owner.OnPingDocument();
            if (gH_Document == null)
                return;

            // ʼʱ
            long ticks = DateTime.Now.Ticks;

            // ÷Χ
            double currentValue, maxValue,minValue;
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
                minValue= Convert.ToDouble(m_owner.Slider.Minimum); 
            }

            m_currentValue = currentValue;
            var doc = RhinoDoc.ActiveDoc;
            var activeView = doc.Views.ActiveView;
            var displayMode = activeView.ActiveViewport.DisplayMode;  // 返回 DisplayMode 对象
            string modeName = displayMode.EnglishName;  // 获取显示模式名称，如 "Shaded"、"Wireframe" 等
            bool isRaytracedMode = modeName == "Raytraced" || modeName == "光线跟踪";

            try
            {
                var myView = RhinoDoc.ActiveDoc.Views.Find(viewName, true);
                RealtimeDisplayMode cycles = myView.RealtimeDisplayMode;
                int oldPasses = -1;
                while (m_currentValue <= maxValue)
                {
                    if (GH_Document.IsEscapeKeyDown())
                    {
                        wasAborted = true;
                        break;
                    }

                    if (UseCustomRange)
                    {
                        RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex + (int)CustomRange.Min} of {(int)CustomRange.Min}-{(int)CustomRange.Max}. Total:{(int)CustomRange.Max - (int)CustomRange.Min + 1} Time left: {EstimateTimeLeft(ticks)}s";
                    }
                    else
                    {
                        RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex} of {m_owner.Slider.Minimum}-{m_owner.Slider.Maximum}. Total:{m_owner.Slider.Maximum - m_owner.Slider.Minimum + 1} Time left: {EstimateTimeLeft(ticks)}s";
                    }

                    // sliderֵ
                    m_owner.Slider.Value = Convert.ToDecimal(m_currentValue);

                    // ˢͼ
                    doc.Views.Redraw();
                    Grasshopper.Instances.ActiveCanvas.Refresh();
                    Grasshopper.Instances.RedrawAll();

                    progressCallback?.Invoke(m_frameIndex, UseCustomRange ? (int)CustomRange.Length + 1 : (int)maxValue -(int)minValue+1);

                    //Stopwatch stopwatch = new Stopwatch();
                    //stopwatch.Start();

                    Bitmap bitmap = MotionCreateFrame(isTransparent, myView, isCycles,isRaytracedMode, realtimeRenderPasses, cycles, oldPasses);
                    if (bitmap != null)
                    {
                        string arg = string.Format(m_fileTemplate, m_frameIndex + (int)CustomRange.Min);
                        string text = string.Format("{0}{2}{1}", m_folder, arg, Path.DirectorySeparatorChar);
                        bitmap.Save(text, ImageFormat.Png);
                        RhinoApp.WriteLine($"Frame {m_frameIndex} saved to disk: {text}");
                        m_frameIndex++;
                        bitmap.Dispose();
                        cycles.MaxPasses = oldPasses;
                        cycles.Paused = false;
                    }
                    else if (isCycles&&!isRaytracedMode)
                    {
                        RhinoApp.WriteLine($"Please open the raytraced display mode！");
                        wasAborted = true;
                        break;
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Frame {m_frameIndex + (int)CustomRange.Min} failed to render");
                    }
                    //stopwatch.Stop();
                    //RhinoApp.WriteLine ($"{m_currentValue} spend time:{stopwatch.ElapsedMilliseconds} ms");
                    m_currentValue += 1.0;
                }

                m_owner.Slider.Value = UseCustomRange ? (int)CustomRange.Min : 0;
                doc.Views.Redraw();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during animation: {ex.Message}");
                wasAborted = true;
            }

            RhinoApp.WriteLine(wasAborted ?
                "Animation cancelled by user." :
                $"Animation saved to disk: {m_folder}\\");
            RhinoApp.CommandPrompt = string.Empty;
        }

        public Bitmap MotionCreateFrame(bool isTransparent, RhinoView myView, bool isCycles,
            bool isRaytracedMode, int realtimeRenderPasses, RealtimeDisplayMode cycles, int oldPasses)
        {
            cycles = myView.RealtimeDisplayMode;
            oldPasses = 1;
            if (myView == null)
                return null;

            ViewCapture viewCapture;
            if (isCycles&& isRaytracedMode)
            {
                oldPasses = cycles.MaxPasses;
                cycles.PostEffectsOn = true;
                cycles.MaxPasses = 2;
                cycles.Paused = true;
                viewCapture = new ViewCapture
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
            else if (!isCycles && isRaytracedMode )
            {
                return null;
            }
            else if (isCycles && !isRaytracedMode)
            {
                return null;
            }
            else
            {
                viewCapture = new ViewCapture
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

            return viewCapture.CaptureToBitmap(myView);
        }
    }
}