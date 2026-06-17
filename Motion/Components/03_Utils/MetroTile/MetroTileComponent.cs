using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Motion.Utils.MetroTile;
using Rhino.Geometry;

namespace Motion.Utils
{
    /// <summary>
    /// Generates metro-style tiles from a base surface using rule-driven interval trimming.
    /// </summary>
    public class MetroTileComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetroTileComponent"/> class.
        /// </summary>
        public MetroTileComponent()
            : base("Metro Tile", "MetroTile",
                "Generate Metro-style surface tiles from a base surface and rule text.",
                "Motion", "03_Utils")
        {
        }

        /// <inheritdoc />
        public override Guid ComponentGuid => new Guid("e3cb90d8-0b14-4b2f-9ab4-cc66c9af2674");

        /// <inheritdoc />
        protected override Bitmap Icon => null;

        /// <inheritdoc />
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <inheritdoc />
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Base", "B", "Base surface.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Count X", "CX", "Grid count in U direction.", GH_ParamAccess.item, 4);
            pManager.AddIntegerParameter("Count Y", "CY", "Grid count in V direction.", GH_ParamAccess.item, 4);
            pManager.AddNumberParameter("Margin", "M", "Inset margin applied to each generated tile.", GH_ParamAccess.item, 0.0);
            pManager.AddTextParameter("Rules", "R", "Rule text list, e.g. M(0,1) {2;*}", GH_ParamAccess.list);
            pManager[4].Optional = true;
        }

        /// <inheritdoc />
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Tiles", "T", "Generated tile faces.", GH_ParamAccess.tree);
        }

        /// <inheritdoc />
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Surface baseSurface = null;
            int countX = 4;
            int countY = 4;
            double margin = 0.0;
            var inputRules = new List<string>();

            if (!DA.GetData(0, ref baseSurface)) return;
            if (!DA.GetData(1, ref countX)) return;
            if (!DA.GetData(2, ref countY)) return;
            if (!DA.GetData(3, ref margin)) return;
            DA.GetDataList(4, inputRules);

            if (baseSurface == null || !baseSurface.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input 'Base' must be a valid surface.");
                return;
            }

            if (countX <= 0 || countY <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Count X and Count Y must be greater than 0.");
                return;
            }

            if (margin < 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Margin below 0 is clamped to 0.");
                margin = 0.0;
            }

            List<TileRule> parsedRules;
            try
            {
                parsedRules = RuleParser.ParseRules(inputRules);
            }
            catch (RuleParseException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                return;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unexpected rule parser failure: {ex.Message}");
                return;
            }

            Dictionary<(int X, int Y), TileRule> tileRuleMap = InitializeRuleMap(countX, countY);
            ApplyRules(tileRuleMap, parsedRules, countX, countY);

            Interval baseU = baseSurface.Domain(0);
            Interval baseV = baseSurface.Domain(1);
            var outputTree = new GH_Structure<GH_Surface>();

            for (int x = 0; x < countX; x++)
            {
                Interval u = GetCellInterval(baseU, x, countX);
                for (int y = 0; y < countY; y++)
                {
                    GH_Path path = new GH_Path(x, y);
                    outputTree.EnsurePath(path);
                    (int X, int Y) key = CreateCellKey(x, y);
                    if (!tileRuleMap.TryGetValue(key, out TileRule rule))
                    {
                        AddRuntimeMessage(
                            GH_RuntimeMessageLevel.Warning,
                            $"Missing rule for cell {path}. Falling back to large tile.");
                        rule = new TileRule(TileType.L, x, y);
                    }

                    if (rule.Type == TileType.E)
                    {
                        continue;
                    }

                    Interval v = GetCellInterval(baseV, y, countY);
                    List<Surface> faces = GeometryEngine.GenerateTileFaces(baseSurface, u, v, rule, margin);

                    foreach (Surface face in faces)
                    {
                        if (face != null)
                        {
                            outputTree.Append(new GH_Surface(face), path);
                        }
                    }
                }
            }

            DA.SetDataTree(0, outputTree);
        }

        private static Dictionary<(int X, int Y), TileRule> InitializeRuleMap(int countX, int countY)
        {
            var map = new Dictionary<(int X, int Y), TileRule>();
            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    map[CreateCellKey(x, y)] = new TileRule(TileType.L, x, y);
                }
            }

            return map;
        }

        private void ApplyRules(Dictionary<(int X, int Y), TileRule> map, List<TileRule> rules, int countX, int countY)
        {
            if (rules == null || rules.Count == 0)
            {
                return;
            }

            var filtered = new List<TileRule>();
            foreach (TileRule rule in rules)
            {
                if (rule.TargetColumn.HasValue && rule.TargetColumn.Value >= countX)
                {
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Warning,
                        $"Rule target column {rule.TargetColumn.Value} is out of range [0, {countX - 1}] and was ignored.");
                    continue;
                }

                if (rule.TargetRow.HasValue && rule.TargetRow.Value >= countY)
                {
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Warning,
                        $"Rule target row {rule.TargetRow.Value} is out of range [0, {countY - 1}] and was ignored.");
                    continue;
                }

                filtered.Add(rule);
            }

            // Macro rules with wildcard are applied first, then specific rules override them.
            IEnumerable<TileRule> macroRules = filtered.Where(IsMacroRule);
            IEnumerable<TileRule> microRules = filtered.Where(r => !IsMacroRule(r));

            foreach (TileRule rule in macroRules)
            {
                ApplySingleRule(map, rule, countX, countY);
            }

            foreach (TileRule rule in microRules)
            {
                ApplySingleRule(map, rule, countX, countY);
            }
        }

        private static bool IsMacroRule(TileRule rule)
        {
            return !rule.TargetColumn.HasValue || !rule.TargetRow.HasValue;
        }

        private static void ApplySingleRule(Dictionary<(int X, int Y), TileRule> map, TileRule rule, int countX, int countY)
        {
            for (int x = 0; x < countX; x++)
            {
                if (rule.TargetColumn.HasValue && rule.TargetColumn.Value != x)
                {
                    continue;
                }

                for (int y = 0; y < countY; y++)
                {
                    if (rule.TargetRow.HasValue && rule.TargetRow.Value != y)
                    {
                        continue;
                    }

                    map[CreateCellKey(x, y)] = new TileRule(rule.Type, x, y, rule.PreservedIndices);
                }
            }
        }

        private static (int X, int Y) CreateCellKey(int x, int y)
        {
            return (x, y);
        }

        private static Interval GetCellInterval(Interval domain, int index, int count)
        {
            double t0 = index / (double)count;
            double t1 = (index + 1) / (double)count;
            return new Interval(domain.ParameterAt(t0), domain.ParameterAt(t1));
        }
    }
}
