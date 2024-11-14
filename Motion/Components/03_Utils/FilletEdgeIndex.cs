using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;

namespace Motion
{
    public class FilletEdgeIndexComponent : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.FilletEdgeIndex;
        public override Guid ComponentGuid => new Guid("4736a5be-2ecc-42b0-beaa-1cef9424375a");

        public FilletEdgeIndexComponent()
          : base("FilletEdgeIndex", "FilletEdgeIndex",
            "根据点获取Brep的边序号",
            "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Brep Data", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "用于提取边缘的点", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Valence", "V", "每边点的最小价数(与该点相连的边的数量)", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Tolerance", "T", "公差", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("EdgeIndex", "E", "倒角边序号", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep iBrep = null;
            List<Point3d> iPoints = new List<Point3d>();
            int iValence = 0;
            double iTolerance = 0d;

            if (!DA.GetData(0, ref iBrep) || !DA.GetDataList(1, iPoints) || !DA.GetData(2, ref iValence))
            {
                return;
            }
            if (!DA.GetData(3, ref iTolerance))
            {
                iTolerance = GH_Component.DocumentTolerance();
            }

            if (iBrep == null || iPoints.Count == 0)
            {
                return;
            }

            if (iValence <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "价数必须大于0");
                iValence = 1;
            }

            if (iTolerance < 2.3283064365386963E-10)
            {
                iTolerance = 2.3283064365386963E-10;
            }

            //获取点所在边的序号
            int edgeCount = iBrep.Edges.Count;
            List<int> edgeIndices = new List<int>();

            for (int i = 0; i < edgeCount; i++)
            {
                BrepEdge brepEdge = iBrep.Edges[i];
                List<int> coincidentPointsIndices = new List<int>();

                for (int j = 0; j < iPoints.Count; j++)
                {
                    if (EdgePoint(brepEdge, iPoints[j], iTolerance))
                    {
                        coincidentPointsIndices.Add(j);
                    }
                }

                if (coincidentPointsIndices.Count >= iValence)
                {
                    edgeIndices.Add(brepEdge.EdgeIndex);
                }
            }

            List<Curve> curves = iBrep.DuplicateEdgeCurves().ToList();

            DataTree<int> chainEdgeIndexTree = GetChainEdgeIndex(curves);

            DataTree<int> oIndices = new DataTree<int>();
            for (int i = 0; i < chainEdgeIndexTree.BranchCount; i++)
            {
                List<int> branch = chainEdgeIndexTree.Branch(i);
                foreach (var item in branch)
                {
                    for (int j = 0; j < edgeIndices.Count; j++)
                    {
                        if (item==edgeIndices[j])
                        {
                            GH_Path path = new GH_Path(i);
                            oIndices.AddRange(branch,path);
                        }
                    }
                }
            }
            DA.SetDataTree(0, oIndices);
        }
        private static bool EdgePoint(Curve edge, Point3d point, double tolerance)
        {
            if (!edge.ClosestPoint(point, out var t))
            {
                return false;
            }
            return edge.PointAt(t).DistanceTo(point) <= tolerance;
        }

        private static DataTree<int> GetChainEdgeIndex(List<Curve> curves)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            int numberOfCurves = curves.Count;
            Point3d[] startPoints = new Point3d[numberOfCurves];
            Point3d[] endPoints = new Point3d[numberOfCurves];
            Vector3d[] startTangents = new Vector3d[numberOfCurves];
            Vector3d[] endTangents = new Vector3d[numberOfCurves];
            bool[] usedStartPoints = new bool[numberOfCurves];
            bool[] usedEndPoints = new bool[numberOfCurves];
            int[] startIndices = new int[numberOfCurves];
            int[] endIndices = new int[numberOfCurves];
            bool[] usedCurves = new bool[numberOfCurves];

            for (int i = 0; i < numberOfCurves; i++)
            {
                startPoints[i] = curves[i].PointAtStart;
                endPoints[i] = curves[i].PointAtEnd;
                startTangents[i] = curves[i].TangentAtStart;
                endTangents[i] = curves[i].TangentAtEnd;
                startIndices[i] = -1;
                endIndices[i] = -1;
                if (curves[i].IsPeriodic)
                {
                    startIndices[i] = i;
                    endIndices[i] = i;
                }
            }

            for (int i = 0; i < numberOfCurves; i++)
            {
                for (int j = 0; j < numberOfCurves; j++)
                {
                    if (i != j)
                    {
                        if (!usedStartPoints[i] && !usedStartPoints[j] && (startPoints[i].DistanceTo(startPoints[j]) < tolerance) && (Math.Sin(Rhino.Geometry.Vector3d.VectorAngle(startTangents[i], startTangents[j])) < 0.001))
                        {
                            usedStartPoints[i] = true;
                            usedStartPoints[j] = true;
                            startIndices[i] = j;
                            startIndices[j] = i;
                        }
                        if (!usedStartPoints[i] && !usedEndPoints[j] && (startPoints[i].DistanceTo(endPoints[j]) < tolerance) && (Math.Sin(Rhino.Geometry.Vector3d.VectorAngle(startTangents[i], endTangents[j])) < 0.001))
                        {
                            usedStartPoints[i] = true;
                            usedEndPoints[j] = true;
                            startIndices[i] = j;
                            endIndices[j] = i;
                        }
                        if (!usedEndPoints[i] && !usedStartPoints[j] && (endPoints[i].DistanceTo(startPoints[j]) < tolerance) && (Math.Sin(Rhino.Geometry.Vector3d.VectorAngle(endTangents[i], startTangents[j])) < 0.001))
                        {
                            usedEndPoints[i] = true;
                            usedStartPoints[j] = true;
                            endIndices[i] = j;
                            startIndices[j] = i;
                        }
                        if (!usedEndPoints[i] && !usedEndPoints[j] && (endPoints[i].DistanceTo(endPoints[j]) < tolerance) && (Math.Sin(Rhino.Geometry.Vector3d.VectorAngle(endTangents[i], endTangents[j])) < 0.001))
                        {
                            usedEndPoints[i] = true;
                            usedEndPoints[j] = true;
                            endIndices[i] = j;
                            endIndices[j] = i;
                        }
                    }
                }
            }

            DataTree<int> curveGroups = new DataTree<int>();
            int currentLoop = 0;
            int currentCurveIndex = 0;
            int previousCurveIndex = -1;

            for (int i = 0; i < numberOfCurves; i++)
            {
                currentCurveIndex = i;
                if (!usedCurves[currentCurveIndex])
                {
                    curveGroups.Add(currentCurveIndex, new GH_Path(currentLoop));
                    usedCurves[currentCurveIndex] = true;

                    if (!usedStartPoints[currentCurveIndex] && !usedEndPoints[currentCurveIndex])
                    {
                        currentLoop++;
                        continue;
                    }

                    if (usedEndPoints[currentCurveIndex])
                    {
                        for (int k = 0; k < 100; k++)
                        {
                            if (usedEndPoints[currentCurveIndex] && (endIndices[currentCurveIndex] != previousCurveIndex) && (endIndices[currentCurveIndex] != i) && !usedCurves[endIndices[currentCurveIndex]])
                            {
                                previousCurveIndex = currentCurveIndex;
                                currentCurveIndex = endIndices[currentCurveIndex];
                                curveGroups.Add(currentCurveIndex, new GH_Path(currentLoop));
                                usedCurves[currentCurveIndex] = true;
                            }
                            else if (usedStartPoints[currentCurveIndex] && (startIndices[currentCurveIndex] != previousCurveIndex) && (startIndices[currentCurveIndex] != i) && !usedCurves[startIndices[currentCurveIndex]])
                            {
                                previousCurveIndex = currentCurveIndex;
                                currentCurveIndex = startIndices[currentCurveIndex];
                                curveGroups.Add(currentCurveIndex, new GH_Path(currentLoop));
                                usedCurves[currentCurveIndex] = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    previousCurveIndex = -1;
                    currentCurveIndex = i;

                    if (usedStartPoints[currentCurveIndex])
                    {
                        for (int k = 0; k < 100; k++)
                        {
                            if (usedEndPoints[currentCurveIndex] && (endIndices[currentCurveIndex] != previousCurveIndex) && (endIndices[currentCurveIndex] != i) && !usedCurves[endIndices[currentCurveIndex]])
                            {
                                previousCurveIndex = currentCurveIndex;
                                currentCurveIndex = endIndices[currentCurveIndex];
                                curveGroups.Add(currentCurveIndex, new GH_Path(currentLoop));
                                usedCurves[currentCurveIndex] = true;
                            }
                            else if (usedStartPoints[currentCurveIndex] && (startIndices[currentCurveIndex] != previousCurveIndex) && (startIndices[currentCurveIndex] != i) && !usedCurves[startIndices[currentCurveIndex]])
                            {
                                previousCurveIndex = currentCurveIndex;
                                currentCurveIndex = startIndices[currentCurveIndex];
                                curveGroups.Add(currentCurveIndex, new GH_Path(currentLoop));
                                usedCurves[currentCurveIndex] = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    currentLoop++;
                }
            }

            return curveGroups;
        }
        
    }
}