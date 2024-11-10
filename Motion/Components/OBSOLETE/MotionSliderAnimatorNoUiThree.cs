using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Render;

public class MotionSliderAnimatorNoUiThree : GH_SliderAnimator
{
    public Interval CustomRange { get; set; }
    public bool UseCustomRange { get; set; }
    private readonly object _lockObject = new object();

    public MotionSliderAnimatorNoUiThree(GH_NumberSlider nOwner)
        : base(nOwner)
    {
        CustomRange = new Interval(0, 100);
        UseCustomRange = false;
    }

    public async Task<int> MotionStartAnimation(bool isTransparent, string viewName, bool isCycles, int realtimeRenderPasses, List<string> outputPathList)
    {
        outputPathList = new List<string>();
        StoreSettingsAsDefault();

        // 基础验证
        if (m_fileTemplate == null)
            throw new Exception("File name mask is not valid");
        if (m_folder == null)
            throw new Exception("Destination folder path is not valid");
        if (m_owner == null)
            throw new Exception("Number slider reference cannot be resolved");
        if (m_frameCount < 1)
            throw new Exception("Insufficient frames for animation");

        // 创建输出目录
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

        // 设置分辨率
        m_resolution.Width = Math.Max(m_resolution.Width, 24);
        m_resolution.Height = Math.Max(m_resolution.Height, 24);
        m_frameIndex = 0;

        // 获取文档对象
        GH_Document gH_Document = m_owner.OnPingDocument();
        if (gH_Document == null)
            return 0;

        // 初始化计时器
        long ticks = DateTime.Now.Ticks;

        // 设置范围
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

            // 预分配内存
            int totalFrames = (int)(maxValue - currentValue + 1);
            outputPathList = new List<string>(totalFrames);

            // 暂停布局更新
            Grasshopper.Instances.ActiveCanvas.SuspendLayout();
            doc.Views.RedrawEnabled = false;

            while (m_currentValue <= maxValue)
            {
                if (GH_Document.IsEscapeKeyDown())
                    break;

                // 更新进度显示
                if (m_frameIndex % 5 == 0)  // 每5帧更新一次状态
                {
                    if (UseCustomRange)
                    {
                        RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex + (int)CustomRange.Min} of {(int)CustomRange.Min}-{(int)CustomRange.Max}. Total:{(int)CustomRange.Max - (int)CustomRange.Min + 1} Time left: {EstimateTimeLeft(ticks)}s";
                    }
                    else
                    {
                        RhinoApp.CommandPrompt = $"Generating frame {m_frameIndex} of {m_owner.Slider.Minimum}-{m_owner.Slider.Maximum}. Total:{m_owner.Slider.Maximum - m_owner.Slider.Minimum + 1} Time left: {EstimateTimeLeft(ticks)}s";
                    }
                }

                // 更新slider值
                m_owner.Slider.Value = Convert.ToDecimal(m_currentValue);

                // 批量处理UI更新
                if (m_frameIndex % 5 == 0)
                {
                    Application.DoEvents();
                    doc.Views.Redraw();
                    Grasshopper.Instances.ActiveCanvas.Refresh();
                }

                m_owner.ExpireSolution(true);

                // 创建并保存图片
                using (Bitmap bitmap = MotionCreateFrame(isTransparent, myView, isCycles, realtimeRenderPasses, cycles, oldPasses))
                {
                    if (bitmap != null)
                    {
                        await Task.Run(() =>
                        {
                            lock (_lockObject)
                            {
                                string arg = string.Format(m_fileTemplate, m_frameIndex + (int)CustomRange.Min);
                                string text = string.Format("{0}{2}{1}", m_folder, arg, Path.DirectorySeparatorChar);
                                bitmap.Save(text, ImageFormat.Png);
                                outputPathList.Add(text);
                            }
                        });

                        RhinoApp.WriteLine($"Frame {m_frameIndex} saved to disk");
                        m_frameIndex++;
                        cycles.MaxPasses = oldPasses;
                        cycles.Paused = false;
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Frame {m_frameIndex + (int)CustomRange.Min} failed to render");
                    }
                }

                m_currentValue += 1.0;
            }

            // 恢复初始状态
            m_owner.Slider.Value = UseCustomRange ? (int)CustomRange.Min : 0;
            doc.Views.RedrawEnabled = true;
            doc.Views.Redraw();
            Grasshopper.Instances.ActiveCanvas.ResumeLayout();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error during animation: {ex.Message}");
        }
        finally
        {
            doc.Views.RedrawEnabled = true;
            Grasshopper.Instances.ActiveCanvas.ResumeLayout();
        }

        RhinoApp.WriteLine($"Animation saved to disk: {m_folder}\\");
        RhinoApp.CommandPrompt = string.Empty;
        return m_frameIndex;
    }

    public Bitmap MotionCreateFrame(bool isTransparent, RhinoView myView, bool isCycles, int realtimeRenderPasses, RealtimeDisplayMode cycles, int oldPasses)
    {
        if (myView == null)
            return null;

        cycles = myView.RealtimeDisplayMode;
        oldPasses = 1;

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