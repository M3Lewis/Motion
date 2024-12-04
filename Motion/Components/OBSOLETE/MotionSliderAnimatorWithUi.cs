using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Microsoft.VisualBasic.CompilerServices;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Render;

/// <exclude />
public class MotionSliderAnimatorWithUi : GH_SliderAnimator
{
    public Interval CustomRange { get; set; }
    public bool UseCustomRange { get; set; }

    public MotionSliderAnimatorWithUi(GH_NumberSlider nOwner)
        : base(nOwner)
    {
        CustomRange = new Interval(0, 100);
        UseCustomRange = false;
    }

    public int MotionStartAnimation(bool isTransparent, string viewName, bool isCycles, int realtimeRenderPasses, out List<string> outputPathList)
    {
        outputPathList = new List<string>();
        StoreSettingsAsDefault();
        if (m_fileTemplate == null)
        {
            throw new Exception("File name mask is not valid");
        }

        if (m_folder == null)
        {
            throw new Exception("Destination folder path is not valid");
        }

        if (!Directory.Exists(m_folder))
        {
            try
            {
                Directory.CreateDirectory(m_folder);
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw new Exception("Unable to create destination folder");
            }
        }

        if (m_owner == null)
        {
            throw new Exception("Number slider reference cannot be resolved");
        }

        if (m_frameCount < 1)
        {
            throw new Exception("Insufficient frames for animation");
        }

        m_resolution.Width = Math.Max(m_resolution.Width, 24);
        m_resolution.Height = Math.Max(m_resolution.Height, 24);
        m_frameIndex = 0;

        GH_Document gH_Document = m_owner.OnPingDocument();
        if (gH_Document == null)
        {
            return 0;
        }

        long ticks = DateTime.Now.Ticks;

        // 根据是否使用自定义范围来设置起始值和结束值
        double currentValue,
            maxValue;
        if (UseCustomRange)
        {
            currentValue = CustomRange.Min;
            maxValue = CustomRange.Max;
        }
        else
        {
            double num = Convert.ToDouble(
                decimal.Divide(
                    decimal.Subtract(m_owner.Slider.Maximum, m_owner.Slider.Minimum),
                    new decimal(m_frameCount)
                )
            );
            currentValue = Convert.ToDouble(m_owner.Slider.Minimum);
            maxValue = Convert.ToDouble(m_owner.Slider.Maximum) + 0.5 * num;
        }

        m_currentValue = currentValue;

        // 获取并关闭动画设置对话框
        System.Windows.Forms.Timer dialogCheckTimer = new System.Windows.Forms.Timer();
        dialogCheckTimer.Interval = 100;
        dialogCheckTimer.Tick += (sender, e) =>
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form is GH_SliderAnimationSetup setupForm)
                {
                    form.Invoke(new Action(() => setupForm.Close()));
                    break;
                }
            }
        };
        dialogCheckTimer.Start();

        try
        {
            var myView = RhinoDoc.ActiveDoc.Views.Find(viewName, true);
            RealtimeDisplayMode cycles = myView.RealtimeDisplayMode ;
            int oldPasses = -1;
            while (m_currentValue <= maxValue)
            {
                if (UseCustomRange)
                {
                    RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex + (int)CustomRange.Min} of {(int)CustomRange.Min}-{(int)CustomRange.Max}.    Total:{(int)CustomRange.Max - (int)CustomRange.Min + 1}    Time left: {EstimateTimeLeft(ticks)}s";
                }
                else
                {
                    RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex} of {m_owner.Slider.Minimum}-{m_owner.Slider.Maximum}.    Total:{m_owner.Slider.Maximum - m_owner.Slider.Minimum + 1}    Time left: {EstimateTimeLeft(ticks)}s";
                }
                if (GH_Document.IsEscapeKeyDown())
                {
                    break;
                }

                m_owner.Slider.Value = Convert.ToDecimal(m_currentValue);
                if (gH_Document.SolutionState == GH_ProcessStep.Aborted)
                {
                    break;
                }

                RhinoApp.Wait();
                Application.DoEvents();

                if (gH_Document != null)
                {
                    gH_Document.NewSolution(false);
                }

                
                Bitmap bitmap = MotionCreateFrame(isTransparent, myView, isCycles, realtimeRenderPasses,ref cycles,ref oldPasses);
                if (bitmap == null)
                {
                    RhinoApp.WriteLine(
                        $"Frame {m_frameIndex + (int)CustomRange.Min} failed to render to DIB"
                    );
                }
                else
                {
                    string arg = string.Format(m_fileTemplate, m_frameIndex + (int)CustomRange.Min);
                    string text = string.Format(
                        "{0}{2}{1}",
                        m_folder,
                        arg,
                        Path.DirectorySeparatorChar
                    );
                    ImageFormat format = ImageFormat.Png;
                    bitmap.Save(text, format);
                    outputPathList.Add(text);
                    RhinoApp.WriteLine($"Frame {m_frameIndex} saved to disk: {text}");
                    m_frameIndex++;
                    bitmap.Dispose();
                }
                cycles.MaxPasses = oldPasses;
                cycles.Paused = false;
                m_currentValue += 1.0;
            }

            //重新生成第一帧，防止BUG
            m_owner.Slider.Value = UseCustomRange ? (int)CustomRange.Min : m_owner.Slider.Value = 0;

            gH_Document?.NewSolution(false);
            RhinoApp.Wait();
            Application.DoEvents();

            Bitmap bitmapFirst = MotionCreateFrame(isTransparent, myView, isCycles, realtimeRenderPasses,ref cycles,ref oldPasses);
            if (bitmapFirst != null)
            {
                string text = string.Format(
                    "{0}{2}{1}",
                    m_folder,
                    string.Format(m_fileTemplate, m_owner.Slider.Value),
                    Path.DirectorySeparatorChar
                );
                bitmapFirst.Save(text, ImageFormat.Png);
                outputPathList.Add(text);
                outputPathList.RemoveAt(outputPathList.Count - 1);
                bitmapFirst.Dispose();
            }
        }
        finally
        {
            dialogCheckTimer.Stop();
            dialogCheckTimer.Dispose();
        }

        RhinoApp.WriteLine($"Animation saved to disk: {m_folder}\\");
        RhinoApp.CommandPrompt = string.Empty;
        return m_frameIndex;
    }

    public Bitmap MotionCreateFrame(bool isTransparent, RhinoView myView, bool isCycles, int realtimeRenderPasses, ref RealtimeDisplayMode cycles,ref int oldPasses)
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
