using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class TimeScrubRegion
{
    private const int SCRUB_HEIGHT = 35; // 增加高度到35像素
    private const int MARKER_INTERVAL = 10; // 主刻度间隔
    private const int SUB_MARKER_INTERVAL = 2; // 子刻度间隔
    private const int BUTTON_SIZE = 25; // 按钮大小
    private const int BUTTON_MARGIN = 5; // 按钮间距
    private const float DEFAULT_FPS = 30.0f; // 默认帧率
    
    // 自定义颜色
    private static readonly Color BackgroundColor = Color.FromArgb(50, 50, 50);
    private static readonly Color BorderColor = Color.FromArgb(100, 100, 100);
    private static readonly Color MarkerColor = Color.FromArgb(150, 150, 150);
    private static readonly Color TextColor = Color.FromArgb(220, 220, 220);
    private static readonly Color IndicatorColor = Color.FromArgb(0, 170, 230);

    private Rectangle bounds; // 时间轴区域边界
    private float currentFrame = 0; // 当前帧
    private float startFrame = 0; // 开始帧
    private float endFrame = 100; // 结束帧
    
    // 播放控制相关字段
    private bool isPlaying = false;
    private System.Windows.Forms.Timer playbackTimer;
    private List<float> keyframes = new List<float>(); // 关键帧列表

    // 按钮区域
    private Rectangle playButtonBounds;
    private Rectangle stopButtonBounds;
    private Rectangle nextFrameButtonBounds;
    private Rectangle prevFrameButtonBounds;

    // 添加新的缩放相关字段
    private float zoomFactor = 1.0f;
    private float panOffset = 0.0f;
    private bool isDragging = false;
    private Point lastMousePos;
    private const float MIN_ZOOM = 0.1f;
    private const float MAX_ZOOM = 10.0f;
    
    // 修改时间轴高度计算方式
    private int GetScaledHeight(GH_Canvas canvas)
    {
        return (int)(SCRUB_HEIGHT * canvas.Viewport.Zoom);
    }

    // 构造函数
    public TimeScrubRegion()
    {
        bounds = new Rectangle();
        
        playbackTimer = new System.Windows.Forms.Timer();
        playbackTimer.Interval = (int)(1000 / DEFAULT_FPS); // 30fps
        playbackTimer.Tick += PlaybackTimer_Tick;
    }

    // Timer事件处理
    private void PlaybackTimer_Tick(object sender, EventArgs e)
    {
        currentFrame++;
        if (currentFrame >= endFrame)
        {
            currentFrame = startFrame;
            isPlaying = false;
            playbackTimer.Stop();
        }
    }

    // 更新时间轴区域位置和大小
    public void UpdateRegion(GH_Canvas canvas)
    {
        int scaledHeight = GetScaledHeight(canvas);
        bounds = new Rectangle(
            0,
            canvas.Height - scaledHeight,
            canvas.Width,
            scaledHeight
        );

        // 更新按钮位置，使用 canvas.Viewport.Zoom
        int scaledButtonSize = (int)(BUTTON_SIZE * canvas.Viewport.Zoom);
        int scaledMargin = (int)(BUTTON_MARGIN * canvas.Viewport.Zoom);
        
        int buttonY = bounds.Top + (scaledHeight - scaledButtonSize) / 2;
        int buttonX = scaledMargin;

        prevFrameButtonBounds = new Rectangle(buttonX, buttonY, scaledButtonSize, scaledButtonSize);
        buttonX += scaledButtonSize + scaledMargin;
        playButtonBounds = new Rectangle(buttonX, buttonY, scaledButtonSize, scaledButtonSize);
        buttonX += scaledButtonSize + scaledMargin;
        stopButtonBounds = new Rectangle(buttonX, buttonY, scaledButtonSize, scaledButtonSize);
        buttonX += scaledButtonSize + scaledMargin;
        nextFrameButtonBounds = new Rectangle(buttonX, buttonY, scaledButtonSize, scaledButtonSize);
    }

    // 绘制时间轴
    public void Render(Graphics graphics)
    {
        // 绘制背景
        using (SolidBrush backgroundBrush = new SolidBrush(BackgroundColor))
        {
            graphics.FillRectangle(backgroundBrush, bounds);
        }

        // 绘制边框
        using (Pen borderPen = new Pen(BorderColor))
        {
            graphics.DrawRectangle(borderPen, bounds);
        }

        // 绘制刻度
        DrawTimeMarkers(graphics);

        // 绘制当前帧指示器
        DrawCurrentFrame(graphics);

        // 绘制控制按钮
        DrawControlButtons(graphics);
        
        // 绘制关键帧标记
        DrawKeyframes(graphics);
    }

    // 绘制时间刻度
    private void DrawTimeMarkers(Graphics graphics)
    {
        float visibleTimeRange = (endFrame - startFrame) / zoomFactor;
        float pixelsPerFrame = (bounds.Width - GetButtonsWidth()) / visibleTimeRange;
        float visibleStart = startFrame + panOffset;
        float visibleEnd = visibleStart + visibleTimeRange;

        using (Pen markerPen = new Pen(MarkerColor))
        using (Font font = new Font("Segoe UI", 9, FontStyle.Regular)) // 更改字体
        using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
        {
            // 绘制子刻度
            for (int frame = (int)startFrame; frame <= endFrame; frame += SUB_MARKER_INTERVAL)
            {
                float x = bounds.Left + (frame - startFrame) * pixelsPerFrame;
                
                // 绘制小刻度线
                graphics.DrawLine(markerPen, 
                    x, bounds.Bottom - 3, 
                    x, bounds.Bottom);
            }

            // 绘制主刻度和数字
            for (int frame = (int)startFrame; frame <= endFrame; frame += MARKER_INTERVAL)
            {
                float x = bounds.Left + (frame - startFrame) * pixelsPerFrame;
                
                // 绘制大刻度线
                graphics.DrawLine(markerPen, 
                    x, bounds.Bottom - 8, 
                    x, bounds.Bottom);

                // 绘制帧数，使用时间格式 (00:00)
                TimeSpan timeSpan = TimeSpan.FromSeconds(frame / 30.0); // 假设30帧每秒
                string timeText = $"{(int)timeSpan.TotalMinutes:00}:{timeSpan.Seconds:00}";
                
                graphics.DrawString(
                    timeText, 
                    font, 
                    new SolidBrush(TextColor), 
                    x,
                    bounds.Top + 4,
                    format);
            }
        }
    }

    // 绘制当前帧指示器
    private void DrawCurrentFrame(Graphics graphics)
    {
        float pixelsPerFrame = bounds.Width / (endFrame - startFrame);
        float x = bounds.Left + (currentFrame - startFrame) * pixelsPerFrame;

        // 绘制当前帧指示线
        using (Pen currentFramePen = new Pen(IndicatorColor, 2))
        {
            graphics.DrawLine(currentFramePen,
                x, bounds.Top,
                x, bounds.Bottom);
        }

        // 绘制当前帧数值框，显示时间格式
        TimeSpan currentTime = TimeSpan.FromSeconds(currentFrame / 30.0);
        string timeText = $"{(int)currentTime.TotalMinutes:00}:{currentTime.Seconds:00}.{(currentTime.Milliseconds / 10):00}";
        
        using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
        {
            SizeF textSize = graphics.MeasureString(timeText, font);
            RectangleF textBox = new RectangleF(
                x - textSize.Width/2,
                bounds.Top + 2,
                textSize.Width + 8,
                textSize.Height + 4
            );

            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddRectangle(textBox);
                graphics.FillPath(new SolidBrush(IndicatorColor), path);
            }

            graphics.DrawString(timeText, 
                font, 
                Brushes.White, 
                textBox.X + 4,
                textBox.Y + 2);
        }
    }

    // 绘制控制按钮
    private void DrawControlButtons(Graphics graphics)
    {
        using (var buttonBrush = new SolidBrush(Color.FromArgb(70, 70, 70)))
        using (var iconPen = new Pen(Color.White, 2))
        {
            // 绘制上一帧按钮
            graphics.FillRectangle(buttonBrush, prevFrameButtonBounds);
            DrawPrevFrameIcon(graphics, prevFrameButtonBounds, iconPen);

            // 绘制播放/暂停按钮
            graphics.FillRectangle(buttonBrush, playButtonBounds);
            if (!isPlaying)
                DrawPlayIcon(graphics, playButtonBounds, iconPen);
            else
                DrawPauseIcon(graphics, playButtonBounds, iconPen);

            // 绘制停止按钮
            graphics.FillRectangle(buttonBrush, stopButtonBounds);
            DrawStopIcon(graphics, stopButtonBounds, iconPen);

            // 绘制下一帧按钮
            graphics.FillRectangle(buttonBrush, nextFrameButtonBounds);
            DrawNextFrameIcon(graphics, nextFrameButtonBounds, iconPen);
        }
    }

    // 绘制关键帧标记
    private void DrawKeyframes(Graphics graphics)
    {
        float pixelsPerFrame = (bounds.Width - (4 * BUTTON_SIZE + 5 * BUTTON_MARGIN)) / (endFrame - startFrame);
        
        using (var keyframeBrush = new SolidBrush(Color.Yellow))
        {
            foreach (float keyframe in keyframes)
            {
                if (keyframe >= startFrame && keyframe <= endFrame)
                {
                    float x = bounds.Left + (4 * BUTTON_SIZE + 5 * BUTTON_MARGIN) + (keyframe - startFrame) * pixelsPerFrame;
                    
                    // 绘制三角形标记
                    PointF[] trianglePoints = new PointF[]
                    {
                        new PointF(x, bounds.Top),
                        new PointF(x - 5, bounds.Top + 5),
                        new PointF(x + 5, bounds.Top + 5)
                    };
                    graphics.FillPolygon(keyframeBrush, trianglePoints);
                }
            }
        }
    }

    // 按钮图标绘制方法
    private void DrawPlayIcon(Graphics g, Rectangle bounds, Pen pen)
    {
        Point[] points = new Point[]
        {
            new Point(bounds.Left + 8, bounds.Top + 6),
            new Point(bounds.Left + 8, bounds.Bottom - 6),
            new Point(bounds.Right - 8, bounds.Top + bounds.Height/2)
        };
        g.FillPolygon(Brushes.White, points);
    }

    private void DrawPauseIcon(Graphics g, Rectangle bounds, Pen pen)
    {
        g.DrawLine(pen, bounds.Left + 8, bounds.Top + 6, bounds.Left + 8, bounds.Bottom - 6);
        g.DrawLine(pen, bounds.Right - 8, bounds.Top + 6, bounds.Right - 8, bounds.Bottom - 6);
    }

    private void DrawStopIcon(Graphics g, Rectangle bounds, Pen pen)
    {
        g.FillRectangle(Brushes.White, bounds.Left + 7, bounds.Top + 7, BUTTON_SIZE - 14, BUTTON_SIZE - 14);
    }

    private void DrawNextFrameIcon(Graphics g, Rectangle bounds, Pen pen)
    {
        DrawPlayIcon(g, new Rectangle(bounds.Left, bounds.Top, bounds.Width - 6, bounds.Height), pen);
        g.DrawLine(pen, bounds.Right - 6, bounds.Top + 6, bounds.Right - 6, bounds.Bottom - 6);
    }

    private void DrawPrevFrameIcon(Graphics g, Rectangle bounds, Pen pen)
    {
        var playBounds = new Rectangle(bounds.Left + 6, bounds.Top, bounds.Width - 6, bounds.Height);
        Point[] points = new Point[]
        {
            new Point(playBounds.Right - 8, playBounds.Top + 6),
            new Point(playBounds.Right - 8, playBounds.Bottom - 6),
            new Point(playBounds.Left + 8, playBounds.Top + playBounds.Height/2)
        };
        g.FillPolygon(Brushes.White, points);
        g.DrawLine(pen, bounds.Left + 6, bounds.Top + 6, bounds.Left + 6, bounds.Bottom - 6);
    }

    // 添加关键帧方法
    public void AddKeyframe(float frame)
    {
        if (!keyframes.Contains(frame))
        {
            keyframes.Add(frame);
            keyframes.Sort();
        }
    }

    public void RemoveKeyframe(float frame)
    {
        keyframes.Remove(frame);
    }

    // 处理鼠标事件
    public bool HandleMouseDown(Point location)
    {
        if (bounds.Contains(location))
        {
            // 检查是否点击了控制按钮
            if (playButtonBounds.Contains(location))
            {
                TogglePlayback();
                return true;
            }
            
            // 开始拖动时间轴
            isDragging = true;
            lastMousePos = location;
            return true;
        }
        return false;
    }

    public bool HandleMouseMove(Point location)
    {
        if (isDragging)
        {
            float dx = location.X - lastMousePos.X;
            panOffset -= dx / ((bounds.Width - GetButtonsWidth()) / ((endFrame - startFrame) / zoomFactor));
            lastMousePos = location;
            return true;
        }
        return false;
    }

    public bool HandleMouseUp(Point location)
    {
        isDragging = false;
        return bounds.Contains(location);
    }

    public void HandleMouseWheel(Point location, int delta)
    {
        if (bounds.Contains(location))
        {
            float oldZoom = zoomFactor;
            zoomFactor *= (delta > 0) ? 1.1f : 0.9f;
            zoomFactor = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, zoomFactor));

            // 调整平移偏移以保持鼠标位置下的时间点不变
            float mouseX = location.X - bounds.Left - GetButtonsWidth();
            float timeAtMouse = startFrame + panOffset + (mouseX / (bounds.Width - GetButtonsWidth())) * ((endFrame - startFrame) / oldZoom);
            float newTimeAtMouse = startFrame + panOffset + (mouseX / (bounds.Width - GetButtonsWidth())) * ((endFrame - startFrame) / zoomFactor);
            panOffset += (timeAtMouse - newTimeAtMouse);
        }
    }

    // 辅助方法
    private float GetButtonsWidth()
    {
        return 4 * BUTTON_SIZE + 5 * BUTTON_MARGIN;
    }

    // 播放控制方法
    public void TogglePlayback()
    {
        isPlaying = !isPlaying;
        if (isPlaying)
            playbackTimer.Start();
        else
            playbackTimer.Stop();
    }

    public void Stop()
    {
        isPlaying = false;
        playbackTimer.Stop();
        currentFrame = startFrame;
    }

    public void NextFrame()
    {
        currentFrame = Math.Min(endFrame, currentFrame + 1);
    }

    public void PreviousFrame()
    {
        currentFrame = Math.Max(startFrame, currentFrame - 1);
    }

    // 设置当前帧
    public void SetCurrentFrame(float frame)
    {
        currentFrame = Math.Max(startFrame, Math.Min(endFrame, frame));
    }

    // 设置帧范围
    public void SetFrameRange(float start, float end)
    {
        startFrame = start;
        endFrame = end;
        currentFrame = Math.Max(startFrame, Math.Min(endFrame, currentFrame));
    }
} 