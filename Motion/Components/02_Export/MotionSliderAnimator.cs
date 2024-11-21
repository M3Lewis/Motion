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
            outputPathList = new List<string>();
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
            double currentValue, maxValue;
            if (UseCustomRange)
            {
                currentValue = CustomRange.Min;
                maxValue = CustomRange.Max;
            }
            else
            {
                currentValue = Convert.ToDouble(m_owner.Slider.Minimum);
                maxValue = Convert.ToDouble(m_owner.Slider.Maximum);
            }

            m_currentValue = currentValue;
            var doc = RhinoDoc.ActiveDoc;

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

                    // ½ʾ
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

                    progressCallback?.Invoke(m_frameIndex, UseCustomRange ? (int)CustomRange.Length + 1 : (int)maxValue + 1);

                    //Stopwatch stopwatch = new Stopwatch();
                    //stopwatch.Start();
                    // 񲢱ͼƬ
                    Bitmap bitmap = MotionCreateFrame(isTransparent, myView, isCycles, realtimeRenderPasses, cycles, oldPasses);
                    if (bitmap != null)
                    {
                        string arg = string.Format(m_fileTemplate, m_frameIndex + (int)CustomRange.Min);
                        string text = string.Format("{0}{2}{1}", m_folder, arg, Path.DirectorySeparatorChar);
                        bitmap.Save(text, ImageFormat.Png);
                        outputPathList.Add(text);
                        RhinoApp.WriteLine($"Frame {m_frameIndex} saved to disk: {text}");
                        m_frameIndex++;
                        bitmap.Dispose();
                        cycles.MaxPasses = oldPasses;
                        cycles.Paused = false;
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Frame {m_frameIndex + (int)CustomRange.Min} failed to render");
                    }
                    //stopwatch.Stop();
                    //RhinoApp.WriteLine ($"{m_currentValue} spend time:{stopwatch.ElapsedMilliseconds} ms");
                    m_currentValue += 1.0;
                }

                // õʼ֡
                m_owner.Slider.Value = UseCustomRange ? (int)CustomRange.Min : 0;
                doc.Views.Redraw();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during animation: {ex.Message}");
                wasAborted = true;
            }

            RhinoApp.WriteLine(wasAborted ?
                "Animation aborted by user." :
                $"Animation saved to disk: {m_folder}\\");
            RhinoApp.CommandPrompt = string.Empty;
        }

        public Bitmap MotionCreateFrame(bool isTransparent, RhinoView myView, bool isCycles, int realtimeRenderPasses, RealtimeDisplayMode cycles, int oldPasses)
        {
            cycles = myView.RealtimeDisplayMode;
            oldPasses = 1;
            if (myView == null)
                return null;

            ViewCapture viewCapture;
            if (isCycles)
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