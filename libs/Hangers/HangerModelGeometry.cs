using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Loader;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Tools;
using JPMorrow.Revit.Worksets;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Hangers
{

    /// <summary>
    /// Holds Model Geometry Data for placing of hanger family
    /// </summary>
    public class SingleHangerModelGeometry
    {
        public ElementId ConduitId { get; set; }
        public XYZ OriginPt { get; set; }
        public string RodDiameter { get; set; }
        public double RodLength { get; set; }
        public string AttachmentType { get; set; }

        public string ConduitDiameter(ModelInfo info)
        {
            var conduit = info.DOC.GetElement(ConduitId);
            return conduit.LookupParameter("Diameter(Trade Size)").AsValueString();
        }
    }

    /// <summary>
    /// Holds Model Geometry Data for placing of hanger family
    /// </summary>
    public class StrutHangerModelGeometry
    {
        public Line StrutLine { get; set; }
        public int ConduitId { get; set; }
        public XYZ Direction { get => StrutLine.Direction; }
        public XYZ OriginPt { get; set; }
        public XYZ SecondStrutPt { get => StrutLine.GetEndPoint(1); }
        public string RodDiameter { get; set; }
        public string StrutSize { get; set; }
        public double Rod1Length { get; set; }
        public double Rod2Length { get; set; }
        public double InsideGap { get; set; }
        public double OutsideExtraLength { get; set; }
        public double YOriginOffset { get; set; }

        public override string ToString()
        {
            return string.Format("Strut Line Length: {0}\nRod Lengths: {1} | {2}\n",
            StrutLine.Length.ToString(), Rod1Length, Rod2Length);
        }
    }




    // Draw Hanger Geometry in the model
    public static class HangerDrawing
    {
        // Debug Point Event
        public static string DebugPointFamilyName { get; } = "Hanger_Anchor_Pt_Generic_Base_Family.rfa";
        public static string DebugPointFamilyNameNoExt { get; } = DebugPointFamilyName.Split('.').First();
        private static InsertHangerDebugPoint handler_create_hanger_debug_points = null;
        private static ExternalEvent exEvent_create_hanger_debug_points = null;

        ///
        /// Debug Points
        ///

        public static void HangerDebugPointCreationSignUp()
        {
            handler_create_hanger_debug_points = new InsertHangerDebugPoint();
            exEvent_create_hanger_debug_points = ExternalEvent.Create(handler_create_hanger_debug_points);
        }

        public static void CreateHangerDebugPoint(ModelInfo info, IEnumerable<XYZ> anchor_pts, ElementId lvl_id, WorksetId workset_id)
        {
            if (anchor_pts == null || !anchor_pts.Any())
                throw new Exception("No points were provided for drawing.");

            Level lvl = info.DOC.GetElement(lvl_id) as Level;

            handler_create_hanger_debug_points.Info = info;
            handler_create_hanger_debug_points.Points = anchor_pts.ToArray();
            handler_create_hanger_debug_points.Level = lvl;
            handler_create_hanger_debug_points.Workset_Id = workset_id;

            exEvent_create_hanger_debug_points.Raise();
        }

        public class InsertHangerDebugPoint : IExternalEventHandler
        {
            public ModelInfo Info { get; set; }
            public XYZ[] Points { get; set; }
            public Level Level { get; set; }
            public WorksetId Workset_Id { get; set; }

            public void Execute(UIApplication app)
            {
                FilteredElementCollector el_coll = new FilteredElementCollector(Info.DOC);
                FamilySymbol sym = el_coll
                    .OfClass(typeof(FamilySymbol)).Where(x =>
                    (x as FamilySymbol).FamilyName.Equals(HangerDrawing.DebugPointFamilyNameNoExt))
                    .First() as FamilySymbol;

                using var tx = new Transaction(Info.DOC, "placing anchor pt");

                foreach (var pt in Points)
                {
                    _ = tx.Start();
                    var ha = MakeDebugPoint(Info, pt, sym, Level, Workset_Id);
                    Parameter workset_param = ha.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                    workset_param.Set(Workset_Id.IntegerValue);
                    tx.Commit();

                    Info.SEL.SetElementIds(new[] { ha.Id });
                    debugger.show(header: "Debug Point", err: "point placed");
                }

            }

            public string GetName()
            {
                return "Insert Hanger Anchor Widget";
            }
        }

        private static FamilyInstance MakeDebugPoint(
            ModelInfo info, XYZ pt, FamilySymbol sym,
            Level level, WorksetId workset_id)
        {

            var ha = info.DOC.Create.NewFamilyInstance(pt, sym, level, StructuralType.NonStructural);
            Parameter workset_param = ha.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
            workset_param.Set(workset_id.IntegerValue);
            return ha;
        }

        ///
        /// Single Hanger
        ///



        /// <summary>
        /// Revit Event for placing a single hanger
        /// </summary>
        public class SingleHangerModelCreation : IExternalEventHandler, ICloneable
        {
            public ModelInfo Info { get; set; }
            public View View { get; set; }
            public SingleHangerModelGeometry Geometry { get; set; }
            public FamilyInstance ReturnHanger { get; set; } = null;

            public object Clone()
            {
                return this;
            }

            public void Execute(UIApplication app)
            {
                try
                {
                    FamilySymbol sym = null;

                    // start transactions
                    using var transGroup = new TransactionGroup(Info.DOC, "Model Single Hangers");
                    transGroup.Start();
                    using var tx = new Transaction(Info.DOC, "Place Single Hanger");
                    tx.Start();

                    //create sketch plane
                    Plane plane = Plane.CreateByNormalAndOrigin(
                        Info.DOC.ActiveView.ViewDirection, Info.UIDOC.Document.ActiveView.Origin);
                    SketchPlane sp = SketchPlane.Create(Info.DOC, plane);
                    Info.DOC.ActiveView.SketchPlane = sp;

                    var g = Geometry;

                    // get symbol for current geometry
                    FilteredElementCollector el_coll = new FilteredElementCollector(Info.DOC);
                    if (g.AttachmentType.Equals("Mineralac"))
                    {
                        sym = el_coll
                            .OfClass(typeof(FamilySymbol)).Where(x =>
                            (x as FamilySymbol).FamilyName.Equals(SingleHanger.MineralacFamilyNameNoExt) &&
                            (x as FamilySymbol).Name.Replace("-", " ").Equals(g.ConduitDiameter(Info)))
                            .First() as FamilySymbol;
                    }
                    else if (g.AttachmentType.Equals("Batwing"))
                    {
                        sym = el_coll
                            .OfClass(typeof(FamilySymbol)).Where(x =>
                            (x as FamilySymbol).FamilyName.Equals(SingleHanger.K16BatwingFamilyNameNoExt) &&
                            (x as FamilySymbol).Name.Replace("-", " ").Equals(g.ConduitDiameter(Info)))
                            .First() as FamilySymbol;
                    }

                    if (sym == null)
                    {
                        debugger.show(err: "Single Hanger Family Symbol is null.");
                        return;
                    }

                    if (!sym.IsActive)
                        FamilyLoader.ActivateSymbol(sym);

                    // place hanger
                    FamilyInstance fam = Info.DOC.Create.NewFamilyInstance(
                        g.OriginPt, sym, Internal.HangerUtil.GetLevel(Info, g.ConduitId), StructuralType.NonStructural);

                    // rotate hanger
                    var conduit = Info.DOC.GetElement(g.ConduitId);
                    var conduit_curve = (conduit.Location as LocationCurve).Curve;
                    var foward = fam.GetTransform().BasisY;
                    var up_sec_pt = new XYZ(g.OriginPt.X, g.OriginPt.Y, g.OriginPt.Z + 5.0);
                    var axis = Line.CreateBound(g.OriginPt, up_sec_pt);
                    var angle = RMeasure.AngleDbl(Info.DOC, "90");

                    if ((conduit_curve as Line).Direction.DotProduct(foward) != 1 &&
                        (conduit_curve as Line).Direction.DotProduct(foward) != -1)
                        fam.Location.Rotate(axis, -angle);

                    // set workset of hanger
                    var workset = conduit.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).AsInteger();
                    Parameter workset_param = fam.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                    workset_param.Set(workset);

                    // dimension the hanger
                    fam.LookupParameter("Rod Length").Set(g.RodLength);

                    var offset = conduit.LookupParameter("Offset");
                    var o_param = fam.LookupParameter("Offset");
                    if (offset == null || !offset.HasValue)
                    {
                        offset = conduit.LookupParameter("Middle Elevation");
                        o_param = fam.LookupParameter("Elevation from Level");
                    }
                    o_param.Set(offset.AsDouble());

                    ReturnHanger = fam;

                    tx.Commit();
                    transGroup.Assimilate();
                }
                catch (Exception ex)
                {
                    debugger.show(err: ex.ToString());
                }
            }

            public string GetName()
            {
                return "Insert Single Hanger Models";
            }
        }
    }
}
