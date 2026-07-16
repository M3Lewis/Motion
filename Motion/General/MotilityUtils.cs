using System;
using Grasshopper.GUI.Canvas;
using Grasshopper;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Motion.Animation;

namespace Motion.General
{
    public static class MotilityUtils
    {   
        public static void RemoveSources(IGH_Param target, List<IGH_Param> sources)
        {
            foreach (IGH_Param source in sources)
            {
                if (source == null) continue;
                if (!target.Sources.Contains(source))
                {
                    continue;
                }
                target.Sources.Remove(source);
                source.Recipients.Remove(target);

            }
            if (sources.Count > 0)
            {
                target.OnObjectChanged(GH_ObjectEventType.Sources);
                target.ExpireSolution(false);
            }


        }
        
        internal static string GetLastUsedKey(GH_Document doc)
        {
            return GetAllKeys(doc).Last();
        }
        
        public static List<string> GetAllKeys(GH_Document doc)
        {
            var allKeys = doc.ActiveObjects().Where(o => o is RemoteParam).Select(o => o.NickName).Distinct().ToList();
            return allKeys;
        }
        
        internal static void FindReplace(string find, string replace, bool forceExact)
        {
            var allKeys = Grasshopper.Instances.ActiveCanvas.Document.ActiveObjects().Where(o => o is RemoteParam);
            
            foreach (var key in allKeys)
            {
                if (forceExact ? key.NickName == find : key.NickName.Contains(find))
                {
                    key.NickName = key.NickName.Replace(find, replace);
                    key.Attributes.ExpireLayout();
                }
            }
        }

        internal static void GoComponent(IGH_DocumentObject com)
        {
            PointF view_point = new PointF(com.Attributes.Pivot.X, com.Attributes.Pivot.Y);
            GH_NamedView gH_NamedView = new GH_NamedView("", view_point, 1.5f, GH_NamedViewType.center);
            foreach (IGH_DocumentObject item in com.OnPingDocument().SelectedObjects())
            {
                item.Attributes.Selected = false;
            }
            com.Attributes.Selected = true;
            gH_NamedView.SetToViewport(Instances.ActiveCanvas, 300);
        }

        internal static bool TryParseNickNameInterval(string nickname, out double min, out double max)
        {
            min = 0;
            max = 0;
            if (string.IsNullOrEmpty(nickname)) return false;

            string[] parts = nickname.Split(new char[] { '-' }, 2);
            if (parts.Length != 2) return false;

            return double.TryParse(parts[0], out min)
                   && double.TryParse(parts[1], out max);
        }
        
        public static void ShowTemporaryMessageAtLocation(GH_Canvas canvas, string message, PointF location)
        {
            GH_Canvas.CanvasPostPaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                var originalTransform = g.Transform;
                g.ResetTransform();

                SizeF textSize = GH_FontServer.MeasureString(message, GH_FontServer.Standard);
                RectangleF textBounds = new RectangleF(location, textSize);
                textBounds.Inflate(6, 3);

                GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Pink,
                    message);

                capsule.Render(g, Color.LightSkyBlue);
                capsule.Dispose();

                g.Transform = originalTransform;
            };

            canvas.CanvasPostPaintObjects += canvasRepaint;
            canvas.Refresh();

            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (s, args) =>
            {
                canvas.CanvasPostPaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        public static void ShowTemporaryMessageAtTop(GH_Canvas canvas, string message)
        {
            // 固定位置：画布顶部居中，距离左边 330，顶部 50
            var location = new PointF(330, 50);
            ShowTemporaryMessageAtLocation(canvas, message, location);
        }
        
        public static void SafeExecute(string methodName, Action action)
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

        public static void UpdateObjectsVisibilityAndLock(GH_Document doc, IEnumerable<IGH_DocumentObject> objectsToUpdate)
        {
            if (doc == null || objectsToUpdate == null) return;

            var targets = objectsToUpdate.Where(o => o != null).Distinct().ToList();
            if (!targets.Any()) return;

            doc.ScheduleSolution(5, d =>
            {
                // Find all active controllers in the document
                var eventComponents = d.Objects.OfType<EventComponent>().ToList();
                var remoteParams = d.Objects.OfType<RemoteParam>().ToList();

                // Cache the "should hide/lock" state for each controller
                var eventStates = eventComponents.ToDictionary(ev => ev, ev => EvaluateEventComponentShouldHideOrLock(ev, d));
                var paramStates = remoteParams.ToDictionary(rp => rp, rp => EvaluateRemoteParamShouldHideOrLock(rp, d));

                bool anyChanged = false;
                foreach (var obj in targets)
                {
                    if (obj == null) continue;

                    bool targetHidden = ShouldHide(obj, eventComponents, remoteParams, eventStates, paramStates);
                    bool targetLocked = ShouldLock(obj, eventComponents, remoteParams, eventStates, paramStates);

                    if (ApplyVisibilityAndLock(obj, targetHidden, targetLocked))
                    {
                        anyChanged = true;
                    }
                }

                if (anyChanged)
                {
                    d.ExpireSolution();
                }
            });
        }

        private static bool ShouldHide(
            IGH_DocumentObject obj,
            List<EventComponent> eventComponents,
            List<RemoteParam> remoteParams,
            Dictionary<EventComponent, bool> eventStates,
            Dictionary<RemoteParam, bool> paramStates)
        {
            var affectingControllers = eventComponents
                .Where(ev => ev.HideWhenEmpty && ev.affectedObjects.Any(x => x != null && x.InstanceGuid == obj.InstanceGuid))
                .Select(ev => eventStates[ev])
                .Concat(remoteParams
                    .Where(rp => rp.HideWhenEmpty && rp.affectedObjects.Any(x => x != null && x.InstanceGuid == obj.InstanceGuid))
                    .Select(rp => paramStates[rp]))
                .ToList();

            return affectingControllers.Any() && affectingControllers.All(state => state);
        }

        private static bool ShouldLock(
            IGH_DocumentObject obj,
            List<EventComponent> eventComponents,
            List<RemoteParam> remoteParams,
            Dictionary<EventComponent, bool> eventStates,
            Dictionary<RemoteParam, bool> paramStates)
        {
            var affectingControllers = eventComponents
                .Where(ev => ev.LockWhenEmpty && ev.affectedObjects.Any(x => x != null && x.InstanceGuid == obj.InstanceGuid))
                .Select(ev => eventStates[ev])
                .Concat(remoteParams
                    .Where(rp => rp.LockWhenEmpty && rp.affectedObjects.Any(x => x != null && x.InstanceGuid == obj.InstanceGuid))
                    .Select(rp => paramStates[rp]))
                .ToList();

            return affectingControllers.Any() && affectingControllers.All(state => state);
        }

        private static bool ApplyVisibilityAndLock(IGH_DocumentObject obj, bool targetHidden, bool targetLocked)
        {
            bool changed = false;
            try
            {
                if (obj is IGH_PreviewObject previewObj && previewObj.Hidden != targetHidden)
                {
                    previewObj.Hidden = targetHidden;
                    changed = true;
                }

                if (obj is IGH_ActiveObject activeObj && activeObj.Locked != targetLocked)
                {
                    activeObj.Locked = targetLocked;
                    changed = true;
                    if (targetLocked)
                    {
                        activeObj.Phase = GH_SolutionPhase.Blank;
                        activeObj.ClearData();
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"[Motion] ApplyVisibilityAndLock 出错: {ex.Message}");
            }
            return changed;
        }

        private static bool EvaluateEventComponentShouldHideOrLock(EventComponent ev, GH_Document doc)
        {
            var timelineSlider = doc.Objects.OfType<Grasshopper.Kernel.Special.GH_NumberSlider>()
                .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase))
                ?? doc.Objects.OfType<Grasshopper.Kernel.Special.GH_NumberSlider>().FirstOrDefault();

            if (timelineSlider == null) return false;

            double currentValue = (double)timelineSlider.CurrentValue;
            if (!TryParseNickNameInterval(ev.NickName, out double min, out double max)) return false;

            bool outsideInterval = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);
            return ev.InvertHideAndLock ? !outsideInterval : outsideInterval;
        }

        private static bool EvaluateRemoteParamShouldHideOrLock(RemoteParam rp, GH_Document doc)
        {
            var timelineSlider = doc.Objects.OfType<Grasshopper.Kernel.Special.GH_NumberSlider>()
                .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase))
                ?? doc.Objects.OfType<Grasshopper.Kernel.Special.GH_NumberSlider>().FirstOrDefault();

            if (timelineSlider == null) return false;

            double currentValue = (double)timelineSlider.Slider.Value;
            if (!TryParseNickNameInterval(rp.NickName, out double min, out double max)) return false;

            return currentValue < min || currentValue > max;
        }
    }
}
