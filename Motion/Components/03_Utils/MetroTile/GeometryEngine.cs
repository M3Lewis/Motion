using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

namespace Motion.Utils.MetroTile
{
    /// <summary>
    /// Provides interval-based tile surface generation without boolean operations.
    /// </summary>
    public static class GeometryEngine
    {
        /// <summary>
        /// Generates tile faces for one base grid cell based on a rule.
        /// </summary>
        /// <param name="baseSrf">Base surface to trim.</param>
        /// <param name="uDomain">U interval for the current grid cell.</param>
        /// <param name="vDomain">V interval for the current grid cell.</param>
        /// <param name="rule">Tile rule for this cell.</param>
        /// <param name="margin">Inset margin applied before trimming.</param>
        /// <returns>A list of generated surfaces.</returns>
        public static List<Surface> GenerateTileFaces(
            Surface baseSrf,
            Interval uDomain,
            Interval vDomain,
            TileRule rule,
            double margin)
        {
            var faces = new List<Surface>();
            if (baseSrf == null || rule == null || !uDomain.IsValid || !vDomain.IsValid)
            {
                return faces;
            }

            switch (rule.Type)
            {
                case TileType.E:
                    return faces;
                case TileType.L:
                    TryAddTrimmedFace(baseSrf, uDomain, vDomain, margin, faces);
                    return faces;
                case TileType.M:
                    AddPreservedSubTiles(baseSrf, GetMediumSubTiles(uDomain, vDomain), rule.PreservedIndices, margin, faces);
                    return faces;
                case TileType.S:
                    AddPreservedSubTiles(baseSrf, GetSmallSubTiles(uDomain, vDomain), rule.PreservedIndices, margin, faces);
                    return faces;
                default:
                    return faces;
            }
        }

        private static void AddPreservedSubTiles(
            Surface baseSrf,
            IEnumerable<SubTile> subTiles,
            HashSet<int> preservedIndices,
            double margin,
            List<Surface> output)
        {
            foreach (SubTile subTile in subTiles)
            {
                if (preservedIndices == null || !preservedIndices.Contains(subTile.Index))
                {
                    continue;
                }

                TryAddTrimmedFace(baseSrf, subTile.U, subTile.V, margin, output);
            }
        }

        private static IEnumerable<SubTile> GetMediumSubTiles(Interval uDomain, Interval vDomain)
        {
            Interval top = CreateSubInterval(vDomain, 0.5, 1.0);
            Interval bottom = CreateSubInterval(vDomain, 0.0, 0.5);

            // Z-order for M:
            // 0 = upper half, 1 = lower half.
            yield return new SubTile(0, uDomain, top);
            yield return new SubTile(1, uDomain, bottom);
        }

        private static IEnumerable<SubTile> GetSmallSubTiles(Interval uDomain, Interval vDomain)
        {
            Interval left = CreateSubInterval(uDomain, 0.0, 0.5);
            Interval right = CreateSubInterval(uDomain, 0.5, 1.0);
            Interval top = CreateSubInterval(vDomain, 0.5, 1.0);
            Interval bottom = CreateSubInterval(vDomain, 0.0, 0.5);

            // Z-order for S:
            // 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right.
            yield return new SubTile(0, left, top);
            yield return new SubTile(1, right, top);
            yield return new SubTile(2, left, bottom);
            yield return new SubTile(3, right, bottom);
        }

        private static void TryAddTrimmedFace(
            Surface baseSrf,
            Interval originalU,
            Interval originalV,
            double margin,
            List<Surface> output)
        {
            Interval u = ApplySafeMargin(originalU, margin);
            Interval v = ApplySafeMargin(originalV, margin);

            if (Math.Abs(u.Length) <= RhinoMath.ZeroTolerance || Math.Abs(v.Length) <= RhinoMath.ZeroTolerance)
            {
                return;
            }

            Surface trimmed = baseSrf.Trim(u, v);
            if (trimmed != null)
            {
                output.Add(trimmed);
            }
        }

        private static Interval ApplySafeMargin(Interval interval, double margin)
        {
            if (!interval.IsValid)
            {
                return interval;
            }

            double safeMargin = Math.Max(0.0, margin);
            double half = Math.Abs(interval.Length) * 0.5;

            if (half <= RhinoMath.ZeroTolerance)
            {
                return interval;
            }

            if (safeMargin > half)
            {
                // Clamp to 90% of half-width/half-height to avoid interval flip.
                safeMargin = half * 0.9;
            }

            if (safeMargin <= RhinoMath.ZeroTolerance)
            {
                return interval;
            }

            double direction = Math.Sign(interval.T1 - interval.T0);
            if (Math.Abs(direction) <= RhinoMath.ZeroTolerance)
            {
                return interval;
            }

            double start = interval.T0 + (direction * safeMargin);
            double end = interval.T1 - (direction * safeMargin);
            return new Interval(start, end);
        }

        private static Interval CreateSubInterval(Interval interval, double t0, double t1)
        {
            return new Interval(interval.ParameterAt(t0), interval.ParameterAt(t1));
        }

        private readonly struct SubTile
        {
            public SubTile(int index, Interval u, Interval v)
            {
                Index = index;
                U = u;
                V = v;
            }

            public int Index { get; }

            public Interval U { get; }

            public Interval V { get; }
        }
    }
}
