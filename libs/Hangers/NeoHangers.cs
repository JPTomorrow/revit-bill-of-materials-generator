using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using JPMorrow.Revit.Documents;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Tools;
using JPMorrow.BetterFasterStrongerLinq;
using System;
using JPMorrow.Revit.Measurements;
using Autodesk.Revit.UI.Selection;
using JPMorrow.Revit.RevitPicker;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.BICategories;
using JPMorrow.Revit.Loader;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using MoreLinq;

namespace JPMorrow.Revit.Hangers
{
    public static class HangerConstants
    {
        public static string[] SingleAndFixtureHardware = new string[] { "Washer", "Washer", "Hex Nut", "Spring Nut" };
        public static string[] StrutHardware = new string[] { "Washer", "Washer", "Washer", "Washer", "Hex Nut", "Hex Nut", "Spring Nut", "Spring Nut" };

        public static string[] RodDiameterMeasurments { get; } = new string[] { "1/4\"", "3/8\"", "1/2\"" };
        public static string[] SingleHangerAttachmentTypes { get; } = new string[] { "Batwing", "Mineralac" };
    }

    [DataContract]
    public class SingleHanger
    {
        public SingleHanger() { }
        public SingleHanger(
        double rod_length, double rod_dia, int cc,
        string att_type, string att_size, string anchor_type,
        string[] hardware, int fam_id)
        {

            RodLength = rod_length;
            RodDiameter = rod_dia;
            RodCouplingCount = cc;
            AttachmentType = att_type;
            AttachmentSize = att_size;
            AnchorType = anchor_type;
            Hardware = hardware;
            HangerFamilyInstanceId = fam_id;
        }

        [DataMember]
        public double RodLength { get; private set; } = 0;
        [DataMember]
        public double RodDiameter { get; private set; } = 0;
        [DataMember]
        public int RodCouplingCount { get; private set; } = 0;
        [DataMember]
        public string AttachmentType { get; private set; } = "Batwing";
        [DataMember]
        public string AttachmentSize { get; private set; }
        [DataMember]
        public string AnchorType { get; private set; }
        [DataMember]
        public string[] Hardware { get; private set; } = HangerConstants.SingleAndFixtureHardware;
        [DataMember]
        public int HostConduitId { get; private set; } = -1;
        [DataMember]
        public int HangerFamilyInstanceId { get; private set; } = -1;
        [DataMember]
        public XYZSerializable OriginPt { get; private set; } = null;

        public static string K16BatwingFamilyName { get => "Batwing Hanger.rfa"; }
        public static string MineralacFamilyName { get => "Mineralac Hanger.rfa"; }

        public static string K16BatwingFamilyNameNoExt { get => K16BatwingFamilyName.Split('.').First(); }
        public static string MineralacFamilyNameNoExt { get => MineralacFamilyName.Split('.').First(); }

        public string HostDiameter(ModelInfo info)
        {
            if (HostConduitId == -1)
                throw new Exception("Invalid Host Diameter for this SingleHanger.");

            var el = info.DOC.GetElement(new ElementId(HostConduitId));
            return el.LookupParameter("Diameter(Trade Size)").AsValueString();
        }

        // set the id of the paired hanger family instance in Revit
        public void SetFamilyInstanceId(ElementId id)
        {
            if (HangerFamilyInstanceId == -1)
                HangerFamilyInstanceId = id.IntegerValue;
        }

        public string ToString(ModelInfo info)
        {
            var rl = RMeasure.LengthFromDbl(info.DOC, this.RodLength);
            var rd = RMeasure.LengthFromDbl(info.DOC, this.RodDiameter);

            return string.Format(
                "Rod Length: {0}\n" +
                "Rod Diameter: {1}\n" +
                "Anchor Type: {2}\n" +
                "Attachment Type: {3}\n" +
                "Rod Coupling Count: {4}\n" +
                "---------------\n",
                rl, rd, AnchorType, AttachmentType, RodCouplingCount);
        }

        // Create single hangers in revit and link them to this program
        public static IEnumerable<SingleHanger> CreateSingleHangers(
            ModelInfo info, View3D view, IEnumerable<ElementId> ids, HangerOptions opts)
        {
            List<SingleHanger> hangers = new List<SingleHanger>();

            var placement_data = SingleHangerConduitPlacementData
                .GenerateConduitPlacementData(info, ids, opts);

            foreach (var data in placement_data)
            {
                var add_hangers = PlaceConduitSingleHangers(info, view, opts, data);

                // draw hangers in model
                /*
                if (opts.DrawSingleHangerModelGeometry)
                {
                    foreach (var hanger in add_hangers)
                    {
                        var fam = await GenerateConduitSingleHangerGeometry(info, view, hanger, data, opts);
                        hanger.SetFamilyInstanceId(fam.Id);
                    }
                }
                */
                hangers.AddRange(add_hangers);
            }

            return hangers;
        }

        // place single hangers for conduit using placement data
        private static IEnumerable<SingleHanger> PlaceConduitSingleHangers(
            ModelInfo info, View3D view, HangerOptions opts,
            SingleHangerConduitPlacementData placement_data)
        {

            var hangers = new List<SingleHanger>();

            foreach (var pt in placement_data.Pts)
            {
                SingleHanger hanger = new SingleHanger();
                hanger.HostConduitId = placement_data.Conduit.Id.IntegerValue;
                hanger.OriginPt = new XYZSerializable(pt);

                //shoot a ray and get the furthest collision
                RRay ray = RevitRaycast.Cast(
                    info, view, BICategoryCollection.NormalHangerClash.ToList(),
                    pt, RGeo.PrimitiveDirection.Up);

                // fix attachment to mineralac if diameter is above  1 1/4"
                string dia_str = placement_data.Conduit.LookupParameter("Diameter(Trade Size)").AsValueString();
                double compare_dia = RMeasure.LengthDbl(info.DOC, "1 1/4\"");
                double conduit_dia = RMeasure.LengthDbl(info.DOC, dia_str);

                var attachment_type = opts.SingleAttType;
                if (conduit_dia > compare_dia) attachment_type = "Mineralac";

                if (!ray.collisions.Any())
                {

                    hanger.RodCouplingCount = 0;
                    hanger.RodLength = 0;
                    hanger.AttachmentType = attachment_type;
                    hanger.AttachmentSize = dia_str;
                    hanger.AnchorType = "Hilti KH-EZ I - Concrete Anchor";
                    hanger.RodDiameter = opts.RodDiameter;

                    hangers.Add(hanger);
                    continue;
                }

                //aquire the furthest collision
                RRayCollision closest_collision = new RRayCollision();
                closest_collision.distance = 1000;

                foreach (var collision in ray.collisions)
                    if (collision.distance < closest_collision.distance)
                        closest_collision = collision;

                if (closest_collision.distance >= 1000)
                    closest_collision.distance = -1;

                // get the category of the other element and the type of anchor to throw on the end based on what material it has hit
                string category = Internal.HangerUtil.FixLinkedCategory(info, closest_collision.other_id);
                string AnchorType = Internal.HangerUtil.GetAnchortype(category);

                // correct for close beam clamp
                var close_clamp_length = RMeasure.LengthDbl(info.DOC, "3\"");
                if (AnchorType == "Steel City Beam Clamp" && closest_collision.distance < close_clamp_length)
                    AnchorType = "Close Beam Clamp";

                // Get Coupling Count
                int coupling_count = (int)Math.Floor(closest_collision.distance / 10.0);
                hanger.RodCouplingCount = coupling_count;

                // prune rods that are below the min rod length
                double final_rod_length = 0;
                if (closest_collision.distance > opts.MinRodLength)
                    final_rod_length = closest_collision.distance;

                hanger.RodLength = final_rod_length;
                hanger.AttachmentType = attachment_type;
                hanger.AttachmentSize = dia_str;
                hanger.AnchorType = AnchorType;
                hanger.RodDiameter = opts.RodDiameter;

                hangers.Add(hanger);
            }

            return hangers;
        }

        internal class SingleHangerConduitPlacementData
        {
            public XYZ[] Pts { get; set; }
            public Element Conduit { get; set; }
            public Curve Curve { get; set; }

            public SingleHangerConduitPlacementData(
                IEnumerable<XYZ> pts, Element conduit, Curve curve)
            {

                Pts = pts.ToArray();
                Conduit = conduit;
                Curve = curve;
            }

            public static IEnumerable<SingleHangerConduitPlacementData> GenerateConduitPlacementData(
                ModelInfo info, IEnumerable<ElementId> ids, HangerOptions opts)
            {

                var placement_data = new List<SingleHangerConduitPlacementData>();

                ids.ForEach(x =>
                {

                    Element conduit = info.DOC.GetElement(x);
                    Curve conduit_curve = (conduit.Location as LocationCurve).Curve;

                    //points to place hangers and return hanger list
                    XYZ[] current_pts = Internal.HangerUtil
                        .GetPlacementPoints(opts.NominalSpacing, opts.BendSpacing, conduit_curve);

                    placement_data.Add(new SingleHangerConduitPlacementData(current_pts, conduit, conduit_curve));
                });

                return placement_data;
            }
        }

        ///
        /// Single Hanger Geometry Events
        ///

        // Place Single Hanger
        private static SingleHangerModelCreation handler_create_single_hanger = null;
        private static ExternalEvent exEvent_create_single_hanger = null;

        public static void SingleHangerCreationSignUp()
        {
            handler_create_single_hanger = new SingleHangerModelCreation();
            exEvent_create_single_hanger = ExternalEvent.Create(handler_create_single_hanger.Clone() as IExternalEventHandler);
        }

        private static async Task<FamilyInstance> GenerateConduitSingleHangerGeometry(
            ModelInfo info, View view, SingleHanger hanger,
            SingleHangerConduitPlacementData placement_data,
            HangerOptions opts)
        {

            handler_create_single_hanger.Info = info;
            handler_create_single_hanger.View = view;
            handler_create_single_hanger.PlacementData = placement_data;
            handler_create_single_hanger.Options = opts;
            handler_create_single_hanger.Hanger = hanger;

            exEvent_create_single_hanger.Raise();

            while (exEvent_create_single_hanger.IsPending)
            {
                await Task.Delay(100);
            }

            return handler_create_single_hanger.ReturnHanger;
        }

        internal class SingleHangerModelCreation : IExternalEventHandler, ICloneable
        {
            public ModelInfo Info { get; set; }
            public View View { get; set; }
            public SingleHanger Hanger { get; set; }
            public SingleHangerConduitPlacementData PlacementData { get; set; }
            public HangerOptions Options { get; set; }
            public FamilyInstance ReturnHanger { get; set; } = null;

            public object Clone() => this;

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

                    // @REFACTOR need to make batwing handle K20 properly and make a family size for half inch conduit

                    // get symbol for current geometry
                    FilteredElementCollector el_coll = new FilteredElementCollector(Info.DOC);
                    if (Hanger.AttachmentType.Equals("Mineralac"))
                    {
                        var syms = el_coll
                            .OfClass(typeof(FamilySymbol))
                            .Where(x =>
                                   (x as FamilySymbol).FamilyName.Equals(SingleHanger.MineralacFamilyNameNoExt) &&
                                   (x as FamilySymbol).Name.Replace("-", " ").Equals(Hanger.HostDiameter(Info)));

                        if (syms.Any())
                            sym = syms.First() as FamilySymbol;
                        else
                        {
                            el_coll = new FilteredElementCollector(Info.DOC);
                            syms = el_coll
                            .OfClass(typeof(FamilySymbol))
                            .Where(x =>
                                   (x as FamilySymbol).FamilyName.Equals(SingleHanger.MineralacFamilyNameNoExt) &&
                                   (x as FamilySymbol).Name.Replace("-", " ").Equals("3/4\""));
                            sym = syms.First() as FamilySymbol;
                        }

                    }
                    else if (Hanger.AttachmentType.Equals("Batwing"))
                    {
                        var syms = el_coll
                            .OfClass(typeof(FamilySymbol))
                            .Where(x =>
                                   (x as FamilySymbol).FamilyName.Equals(SingleHanger.K16BatwingFamilyNameNoExt) &&
                                   (x as FamilySymbol).Name.Replace("-", " ").Equals(Hanger.HostDiameter(Info)));

                        if (syms.Any())
                            sym = syms.First() as FamilySymbol;
                        else
                        {
                            el_coll = new FilteredElementCollector(Info.DOC);
                            syms = el_coll
                            .OfClass(typeof(FamilySymbol))
                            .Where(x =>
                                   (x as FamilySymbol).FamilyName.Equals(SingleHanger.K16BatwingFamilyNameNoExt) &&
                                   (x as FamilySymbol).Name.Replace("-", " ").Equals("3/4\""));

                            sym = syms.First() as FamilySymbol;
                        }
                    }

                    if (sym == null)
                    {
                        debugger.show(err: "The single hanger family is either not loaded into the project, or corupted.");
                        return;
                    }

                    if (!sym.IsActive)
                        FamilyLoader.ActivateSymbol(sym);

                    // place hanger
                    FamilyInstance fam = Info.DOC.Create.NewFamilyInstance(
                        Hanger.OriginPt.RevitPoint(), sym, Internal.HangerUtil.GetLevel(Info, new ElementId(Hanger.HostConduitId)), StructuralType.NonStructural);

                    // rotate hanger
                    var conduit = Info.DOC.GetElement(new ElementId(Hanger.HostConduitId));
                    var conduit_curve = (conduit.Location as LocationCurve).Curve;
                    var foward = fam.GetTransform().BasisY;
                    var up_sec_pt = new XYZ(Hanger.OriginPt.X, Hanger.OriginPt.Y, Hanger.OriginPt.Z + 5.0);
                    var axis = Line.CreateBound(Hanger.OriginPt.RevitPoint(), up_sec_pt);
                    var angle = RMeasure.AngleDbl(Info.DOC, "90");

                    if ((conduit_curve as Line).Direction.DotProduct(foward) != 1 &&
                        (conduit_curve as Line).Direction.DotProduct(foward) != -1)
                        fam.Location.Rotate(axis, -angle);

                    // set workset of hanger
                    var workset = conduit.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).AsInteger();
                    Parameter workset_param = fam.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                    workset_param.Set(workset);

                    // dimension the hanger
                    var inch = RMeasure.LengthDbl(Info.DOC, "1\"");
                    var rl = inch;
                    if (Hanger.RodLength != 0) rl = Hanger.RodLength;

                    fam.LookupParameter("Rod Length").Set(rl);
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

    [DataContract]
    public class FixtureHanger
    {
        public FixtureHanger() { }

        [DataMember]
        public double RodLength { get; private set; } = 0;
        [DataMember]
        public double RodDiameter { get; private set; } = 0;
        [DataMember]
        public int RodCouplingCount { get; private set; } = 0;
        [DataMember]
        public string AnchorType { get; private set; }
        [DataMember]
        public string[] Hardware { get; private set; } = HangerConstants.SingleAndFixtureHardware;
        [DataMember]
        public int HostJboxId { get; private set; } = -1;

        public static IEnumerable<FixtureHanger> CreateFixtureHangers(
            ModelInfo info, View3D view, IEnumerable<ElementId> jbox_ids,
            HangerOptions opts)
        {

            List<FixtureHanger> hangers = new List<FixtureHanger>();

            foreach (var id in jbox_ids)
            {
                var hanger = CreateFixtureHanger(info, view, id, opts);
                hangers.Add(hanger);
            }

            return hangers;
        }

        public static FixtureHanger CreateFixtureHanger(
            ModelInfo info, View3D view, ElementId jbox_id,
            HangerOptions opts)
        {

            //get conduit and associated curve
            Element jbox = info.DOC.GetElement(jbox_id);

            //local functions
            double parse_len(string len_str) => RMeasure.LengthDbl(info.DOC, len_str);

            var hanger = new FixtureHanger();

            //shoot a ray and get the furthest collision
            var pt = (jbox.Location as LocationPoint).Point;
            RRay ray = RevitRaycast
                .Cast(info, view, BICategoryCollection.NormalHangerClash.ToList(), pt, RGeo.PrimitiveDirection.Up);

            if (!ray.collisions.Any())
            {
                var ret_hanger = new FixtureHanger();
                ret_hanger.HostJboxId = -1;
                return ret_hanger;
            }

            // aquire the furthest collision
            RRayCollision closest_collision = new RRayCollision();
            closest_collision.distance = 1000;
            foreach (var collision in ray.collisions)
            {
                if (collision.distance < closest_collision.distance)
                    closest_collision = collision;
            }

            if (closest_collision.distance >= 1000)
                closest_collision.distance = -1;

            // get the category of the other element and the type of
            // anchor to throw on the end based on what material it has hit
            string category = Internal.HangerUtil.FixLinkedCategory(info, closest_collision.other_id);
            string anchor_type = Internal.HangerUtil.GetAnchortype(category);

            //correct for close beam clamp
            var extra_rod_length = parse_len("3\"");
            if (anchor_type == "Steel City Beam Clamp" && closest_collision.distance < extra_rod_length)
                anchor_type = "Close Beam Clamp";

            //Get Coupling Count
            int coupling_count = (int)Math.Floor(closest_collision.distance / 10.0);
            hanger.RodCouplingCount = coupling_count;

            //prune rods that are below the min rod length
            double final_rod_length = 0;
            if (closest_collision.distance > opts.MinRodLength)
                final_rod_length = closest_collision.distance + extra_rod_length;

            hanger.RodLength = final_rod_length;
            hanger.AnchorType = anchor_type;
            hanger.RodDiameter = opts.RodDiameter;
            hanger.HostJboxId = jbox_id.IntegerValue;
            return hanger;
        }
    }

    [DataContract]
    public class StrutHanger
    {
        public StrutHanger() { }
        public StrutHanger(
            double rod_one_length, double rod_two_length, double rod_diameter,
            int coupling_count, double strut_length, string strut_size,
            string anchor_one_type, string anchor_two_type, string[] hardware,
            IEnumerable<ConduitStrap> straps, int fam_instance_id)
        {

            RodOneLength = rod_one_length;
            RodTwoLength = rod_two_length;
            RodDiameter = rod_diameter;
            RodCouplingCount = coupling_count;
            StrutSize = strut_size;
            StrutLinePoints = null;
            AnchorOneType = anchor_one_type;
            AnchorTwoType = anchor_two_type;
            Hardware = hardware;
            Straps = straps.ToArray();
            HangerFamilyInstanceId = fam_instance_id;
            OriginPt = null;
        }

        [DataMember]
        public double RodOneLength { get; private set; } = 0;
        [DataMember]
        public double RodTwoLength { get; private set; } = 0;
        [DataMember]
        public double RodDiameter { get; private set; } = 0;
        [DataMember]
        public int RodCouplingCount { get; private set; } = 0;
        [DataMember]
        public XYZSerializable[] StrutLinePoints { get; private set; }
        [DataMember]
        public string StrutSize { get; private set; } = DefaultStrutSize;
        [DataMember]
        private TierSpacing[] TierSpacings { get; set; } = new TierSpacing[0];
        [DataMember]
        public string AnchorOneType { get; private set; }
        [DataMember]
        public string AnchorTwoType { get; private set; }
        [DataMember]
        public string[] Hardware { get; private set; } = HangerConstants.StrutHardware;
        [DataMember]
        public ConduitStrap[] Straps { get; private set; } = new ConduitStrap[0];
        [DataMember]
        public int HangerFamilyInstanceId { get; private set; } = -1;
        [DataMember]
        public XYZSerializable OriginPt { get; private set; } = null;

        public Line StrutLine
        {
            get
            {
                return Line.CreateBound(StrutLinePoints[0].RevitPoint(), StrutLinePoints[1].RevitPoint());
            }
        }

        public int TierCount { get => TierSpacings.Count() + 1; }
        public double StrutLength { get => StrutLine.Length * TierCount; }

        public static string StrutHangerFamilyName { get => "Strut Hanger.rfa"; }
        public static string StrutCeilingHangerFamilyName { get => "Ceiling Strut Hanger.rfa"; }

        public static string StrutHangerFamilyNameNoExt { get => StrutHangerFamilyName.Split('.').First(); }
        public static string StrutCeilingHangerFamilyNameNoExt { get => StrutCeilingHangerFamilyName.Split('.').First(); }

        public static string DefaultStrutSize { get; } = "1 5/8\"";

        public string ToString(ModelInfo info)
        {
            var rl1 = RMeasure.LengthFromDbl(info.DOC, this.RodOneLength);
            var rl2 = RMeasure.LengthFromDbl(info.DOC, this.RodTwoLength);
            var rd = RMeasure.LengthFromDbl(info.DOC, this.RodDiameter);
            var sl = RMeasure.LengthFromDbl(info.DOC, this.StrutLength);

            return string.Format(
                "Rod Length 1: {0}\n" +
                "Rod Length 2: {1}\n" +
                "Rod Diameter: {2}\n" +
                "Strut Length: {3}\n" +
                "Strut Size: {4}\n" +
                "Strap Count: {5}\n" +
                "Tier Count: {6}\n" +
                "Anchor 1: {7}\n" +
                "Anchor 2: {8}\n" +
                "Rod Coupling Count: {9}\n" +
                "---------------\n",
                rl1, rl2, rd, sl, StrutSize, Straps.Count(), TierCount,
                AnchorOneType, AnchorTwoType, RodCouplingCount);
        }

        // set the id of the paired hanger family instance in Revit
        public void SetFamilyInstanceId(ElementId id)
        {
            if (HangerFamilyInstanceId == -1)
                HangerFamilyInstanceId = id.IntegerValue;
        }

        // Main Strut Hanger Creation Method
        public static async Task<IEnumerable<StrutHanger>> CreateStrutHangers(
            ModelInfo info, View3D view, IEnumerable<ElementId> ids,
            HangerOptions opts, StrutHangerRackType rack_type,
            bool auto_resolve_placement_pts)
        {
            List<StrutHanger> hangers = new List<StrutHanger>();

            // create and get placement data for hangers
            try
            {
                if (rack_type == StrutHangerRackType.Conduit)
                {

                    var placement_data = GetConduitPlacementData(info, ids, auto_resolve_placement_pts);
                    var add_hangers = PlaceConduitHangers(info, view, ids, placement_data, opts);

                    // draw hangers in model
                    if (opts.DrawStrutHangerModelGeometry)
                    {
                        foreach (var hanger in add_hangers)
                        {
                            var fam = await GenerateConduitStrutHangerGeometry(info, view, hanger, opts, placement_data);
                            hanger.SetFamilyInstanceId(fam.Id);
                        }
                    }

                    hangers.AddRange(add_hangers);
                }
                else if (rack_type == StrutHangerRackType.CableTray)
                {
                    var placement_data = new List<CableTrayHangerPlacementData>();
                    ids.ToList().ForEach(x => placement_data.Add(GetCableTrayPlacementData(info, x, view, opts)));

                    foreach (var d in placement_data)
                    {
                        var add_hangers = PlaceCableTrayHangers(info, view, d, opts);

                        if (opts.DrawStrutHangerModelGeometry)
                        {
                            foreach (var hanger in add_hangers)
                            {
                                var fam = await GenerateCableTrayStrutHangerGeometry(info, view, hanger, opts, d);
                                hanger.SetFamilyInstanceId(fam.Id);
                            }
                        }

                        // draw hangers in model
                        hangers.AddRange(add_hangers);
                    }
                }
                else
                    throw new Exception("Invalid StrutHangerRacktype");
            }
            catch
            {
                return new List<StrutHanger>();
            }

            return hangers;
        }

        private static IEnumerable<StrutHanger> PlaceConduitHangers(
            ModelInfo info, View3D view, IEnumerable<ElementId> conduit_ids,
            ConduitHangerPlacementData placement_data, HangerOptions opts)
        {

            var hangers = new List<StrutHanger>();

            foreach (var pt in placement_data.PlacementPts)
            {

                StrutHanger hanger = new StrutHanger();

                // handle general settings for hanger
                hanger.RodDiameter = opts.RodDiameter;
                hanger.OriginPt = new XYZSerializable(pt);

                // evaluate conduit straps
                hanger.Straps = ConduitStrap.GenerateConduitStraps(info, conduit_ids).ToArray();

                // resolve tier spacing
                hanger.TierSpacings = TierSpacing
                    .TierSpacingFromPlacementData(info, placement_data, conduit_ids).ToArray();

                // resolve strut line
                Line strut_line = GetStrutBaseLine(info, pt, placement_data);
                if (strut_line == null) continue;

                var sl_pt1 = strut_line.GetEndPoint(0);
                var sl_pt2 = strut_line.GetEndPoint(1);

                double near_radius = placement_data.LeftMostDiameter(info) / 2.0;
                double far_radius = placement_data.RightMostDiameter(info) / 2.0;

                var max_rod_diameter = RMeasure.LengthDbl(info.DOC, "1/2\"");

                strut_line = RGeo.ExtendLineEndPoint(
                    strut_line, (max_rod_diameter * 2) + (opts.InsideRodGap * 2) + far_radius + near_radius);

                if (strut_line.Length < info.DOC.Application.ShortCurveTolerance)
                {
                    debugger.show(
                        header: "Strut Hangers",
                        err: "The following strut line is too short: " +
                        strut_line.Length.ToString() + " < " +
                        info.DOC.Application.ShortCurveTolerance.ToString());

                    return new List<StrutHanger>();
                }

                hanger.StrutLinePoints = new[] {
                    new XYZSerializable(strut_line.GetEndPoint(0)),
                    new XYZSerializable(strut_line.GetEndPoint(1))};

                // @REVISION: standardize strut line

                // get anchor/rod info
                var anchors = GetStrutAnchorDetail(
                    info, view, new[] { sl_pt1, sl_pt2 },
                    opts.MinRodLength, opts.CeilingHangers);

                hanger.AnchorOneType = anchors.Anchor1Type ?? "Hilti KH-EZ I - Concrete Anchor";
                hanger.AnchorTwoType = anchors.Anchor2Type ?? "Hilti KH-EZ I - Concrete Anchor";
                hanger.RodOneLength = anchors.Rod1Length;
                hanger.RodTwoLength = anchors.Rod2Length;
                hanger.RodCouplingCount += (int)Math.Floor((anchors.Rod1Length + anchors.Rod2Length) / 10.0);

                hangers.Add(hanger);
            }

            return hangers;
        }

        // Place cable tray hangers at a specific set of pts
        private static IEnumerable<StrutHanger> PlaceCableTrayHangers(
            ModelInfo info, View3D view,
            CableTrayHangerPlacementData placement_data, HangerOptions opts)
        {

            var hangers = new List<StrutHanger>();

            foreach (var pt in placement_data.Pts)
            {
                StrutHanger hanger = new StrutHanger();
                hanger.RodDiameter = opts.RodDiameter;
                hanger.StrutSize = "1 5/8\"";
                hanger.Straps = new ConduitStrap[0];

                hanger.StrutLinePoints = new[] {
                    new XYZSerializable(
                        placement_data.MaxBBLine.Project(pt).XYZPoint),
                    new XYZSerializable(pt)};

                var max_rod_dia = RMeasure.LengthDbl(info.DOC, "1/2\"");
                var resized_strut_line = RGeo.ExtendLineEndPoint(hanger.StrutLine, (max_rod_dia * 2) + (opts.InsideRodGap * 2));

                hanger.StrutLinePoints = new[] { new XYZSerializable(
                        resized_strut_line.GetEndPoint(0)),
                    new XYZSerializable(resized_strut_line.GetEndPoint(1))};

                var sl_pt1 = hanger.StrutLine.GetEndPoint(0);
                var sl_pt2 = hanger.StrutLine.GetEndPoint(1);

                // shoot anchor points
                StrutAnchorDetail anchors = GetStrutAnchorDetail(info, view, new[] { sl_pt1, sl_pt2 }, opts.MinRodLength, opts.CeilingHangers);

                hanger.AnchorOneType = anchors.Anchor1Type;
                hanger.AnchorTwoType = anchors.Anchor2Type;

                hanger.RodOneLength = anchors.Rod1Length;
                hanger.RodTwoLength = anchors.Rod2Length;

                hanger.RodCouplingCount += (int)Math.Floor((anchors.Rod1Length + anchors.Rod2Length) / 10.0);

                hangers.Add(hanger);
            }

            return hangers;
        }

        // Get hanger placement data for conduit 
        private static ConduitHangerPlacementData GetConduitPlacementData(
            ModelInfo info, IEnumerable<ElementId> ids, bool auto_resolve_placement_pts)
        {

            debugger.show(
                header: "Hanger Conduit Rack Information",
                err: "Pick leftmost conduit in the rack");

            var left_id = RvtPicker.PickObjectsSafe(info,
                ObjectType.Element, new ConduitPlacementDataSelectionFilter(ids),
                "Pick leftmost conduit in the rack", itr: 1).First();

            debugger.show(
                header: "Hanger Conduit Rack Information",
                err: "Pick rightmost conduit in the rack");

            var right_id = RvtPicker.PickObjectsSafe(info,
                ObjectType.Element, new ConduitPlacementDataSelectionFilter(ids),
                "Pick rightmost conduit in the rack", itr: 1).First();

            debugger.show(
                header: "Hanger Conduit Rack Information",
                err: "Pick a conduit from each tier of the rack.");

            var tier_ids = RvtPicker.PickObjectsSafe(info,
                ObjectType.Element, new ConduitPlacementDataSelectionFilter(ids),
                "Pick a conduit from each tier of the rack");

            if (tier_ids.Count() > 4)
                tier_ids = tier_ids.Take(4);

            List<XYZ> placement_pts = new List<XYZ>();

            if (auto_resolve_placement_pts)
            {
                var longest = ids.MaxBy(x =>
                    info.DOC.GetElement(x)
                    .LookupParameter("Length")
                    .AsDouble()).First();

                // (length of longest run / hanger_spacing) + 2
                var el = info.DOC.GetElement(longest);
                var length = (int)Math.Ceiling(el.LookupParameter("Length").AsDouble());
                var line = (el.Location as LocationCurve).Curve as Line;
                var segments = length <= 8 ? 3 : ((length - 6) / 8) + 2;
                var dummy_pt = RGeo.DerivePointsOnLine(line, 1.0).ToList().First();

                for (var i = 0; i < segments; i++)
                    placement_pts.Add(dummy_pt);

                debugger.show(
                    header: "Quick Strut Hanger Creation",
                    err: "The length of the longest pipe in rack: " +
                        RMeasure.LengthFromDbl(info.DOC, length) + "\n" +
                        "Number of hangers to be generated: " +
                        placement_pts.Count().ToString());
            }
            else
            {

                debugger.show(
                    header: "Hanger Conduit Rack Information",
                    err: "Pick points along the conduit rack that will" +
                    " be the placement locations for the hangers.");

                // Need to switch to a floorplan view to pick conduit points
                // check open views first
                FilteredElementCollector coll;
                bool active_view_has_elements = false;
                View view = null;
                View prev_view = info.UIDOC.ActiveView;

                foreach (var v in info.UIDOC.GetOpenUIViews())
                {

                    if (v.ViewId == info.UIDOC.ActiveView.Id) continue;

                    coll = new FilteredElementCollector(info.DOC, v.ViewId);
                    var element_ids = coll.ToElementIds();
                    active_view_has_elements = !ids.Except(element_ids).Any();

                    if (active_view_has_elements)
                    {
                        view = info.DOC.GetElement(v.ViewId) as View;
                        break;
                    }
                }

                if (view == null)
                {

                    coll = new FilteredElementCollector(info.DOC);
                    var filter = new ElementMulticlassFilter(new List<Type> { typeof(ViewPlan) });
                    var views = coll
                        .WherePasses(filter)
                        .Where(x => (x as View).ViewType == ViewType.FloorPlan)
                        .ToList();

                    active_view_has_elements = false;
                    foreach (var v in views)
                    {

                        coll = new FilteredElementCollector(info.DOC, (v as View).Id);
                        var view_element_ids = coll.ToElementIds();
                        active_view_has_elements = !ids.Except(view_element_ids).Any();
                        if (active_view_has_elements)
                        {
                            view = v as View;
                            break;
                        }
                    }
                }

                if (view == null)
                {

                    debugger.show(
                        header: "Hanger Conduit Rack Information",
                        err: "A floor plan view with the rack visible is required " +
                        "in order to pick placement points for the hangers.");

                    throw new Exception("Exiting Hanger Placement");
                }

                info.UIDOC.ActiveView = view;
                info.UIDOC.RefreshActiveView();

                placement_pts = RvtPicker.PickPoints(info, ObjectSnapTypes.Nearest).ToList();

                info.UIDOC.ActiveView = prev_view;
                info.UIDOC.RefreshActiveView();
            }

            return new ConduitHangerPlacementData(
                info, placement_pts.ToArray(), left_id, right_id, tier_ids.ToArray());
        }

        // Get Hanger Placement Data for Cable Tray
        private static CableTrayHangerPlacementData GetCableTrayPlacementData(
            ModelInfo info, ElementId tray_id, View3D view, HangerOptions opts)
        {

            var tray = info.DOC.GetElement(tray_id);

            var tray_line = (tray.Location as LocationCurve).Curve as Line;
            var bb = tray.get_BoundingBox(view);

            var lines = new Queue<Line>();
            Options opt = new Options();
            opt.View = view;
            opt.ComputeReferences = true;
            var geo_el = tray.get_Geometry(opt);

            foreach (var obj in geo_el)
            {
                var solid = obj as Solid;
                foreach (Edge edge in solid.Edges)
                    lines.Enqueue(edge.AsCurve() as Line);
            }

            Line min_bb_line = null;
            Line max_bb_line = null;

            while (lines.Any())
            {

                var line = lines.Dequeue();
                var ep1 = line.GetEndPoint(0);
                var ep2 = line.GetEndPoint(1);
                var cmp_line = tray_line.GetEndPoint(0).Z;

                if (RGeo.IsLeft(tray_line, ep1) && RGeo.IsLeft(tray_line, ep2) &&
                    ep1.Z < cmp_line && ep2.Z < cmp_line && line.Length.IsAlmostEqual(tray_line.Length, 3.0))
                {
                    min_bb_line = line;
                }

                if (!RGeo.IsLeft(tray_line, ep1) && !RGeo.IsLeft(tray_line, ep2) &&
                    ep1.Z < cmp_line && ep2.Z < cmp_line && line.Length.IsAlmostEqual(tray_line.Length, 3.0))
                {
                    max_bb_line = line;
                }
            }

            if (min_bb_line == null)
                throw new Exception("No Bounding Box Min Lines Selected.");
            else if (max_bb_line == null)
                throw new Exception("No Bounding Box Max Lines Selected.");

            XYZ[] placement_pts = Internal.HangerUtil.GetPlacementPoints(opts.NominalSpacing, opts.BendSpacing, min_bb_line);
            return new CableTrayHangerPlacementData(tray_id, placement_pts.ToArray(), min_bb_line, max_bb_line);
        }

        // Data generated to place a strut hanger on cable tray
        internal class CableTrayHangerPlacementData
        {

            public CableTrayHangerPlacementData(
                ElementId tray_id, XYZ[] pts,
                Line min_bb_line, Line max_bb_line)
            {

                TrayId = tray_id;
                Pts = pts;
                MinBBLine = min_bb_line;
                MaxBBLine = max_bb_line;
            }

            public ElementId TrayId { get; set; }
            public XYZ[] Pts { get; set; }
            public Line MinBBLine { get; set; }
            public Line MaxBBLine { get; set; }
        }

        // Data generated to place a strut hanger on a conduit rack
        internal class ConduitHangerPlacementData
        {

            public ConduitHangerPlacementData(
                ModelInfo info, XYZ[] placement_pts, ElementId left,
                ElementId right, ElementId[] raw_tiers)
            {

                PlacementPts = placement_pts;
                LeftmostConduitId = left;
                RightmostConduitId = right;
                RawTierConduitIds = raw_tiers;
                OrderTierConduit(info);
                AlignHangerPlacementPoints(info);
            }

            public XYZ[] PlacementPts { get; set; }
            public ElementId LeftmostConduitId { get; set; }
            public ElementId RightmostConduitId { get; set; }
            public ElementId[] RawTierConduitIds { get; set; }

            public double TierZHeight(ModelInfo info, int tier_idx)
            {
                if (tier_idx > RawTierConduitIds.Count() - 1) return -1;
                var curve = (info.DOC.GetElement(RawTierConduitIds[tier_idx]).Location as LocationCurve).Curve;
                return curve.GetEndPoint(0).Z;
            }

            public double LeftMostDiameter(ModelInfo info)
            {

                var el = info.DOC.GetElement(LeftmostConduitId);
                return el.LookupParameter("Diameter(Trade Size)").AsDouble();
            }

            public double RightMostDiameter(ModelInfo info)
            {

                var el = info.DOC.GetElement(RightmostConduitId);
                return el.LookupParameter("Diameter(Trade Size)").AsDouble();
            }

            // get the diameter for a specific tier
            public double GetTierDiameter(ModelInfo info, int tier)
            {

                if (tier > RawTierConduitIds.Count() - 1) return -1;
                var el = info.DOC.GetElement(RightmostConduitId);
                var diameter = el?.LookupParameter("Diameter(Trade Size)")?.AsDouble();
                if (diameter == null) return -1;
                return diameter.Value;
            }

            // Align the raw placement points that the user selected
            // with the leftmost conduit and bottomost z hieght
            private void AlignHangerPlacementPoints(ModelInfo info)
            {

                var left_conduit = info.DOC.GetElement(LeftmostConduitId);
                var left_conduit_line = (left_conduit.Location as LocationCurve).Curve;

                var new_pts = new List<XYZ>();
                foreach (var pt in PlacementPts)
                {
                    var new_pt = left_conduit_line.Project(pt).XYZPoint;
                    new_pts.Add(new_pt);
                }

                PlacementPts = new_pts.ToArray();
            }

            // Order the tier conduit
            // provided by the user from
            // lowest to highest
            private void OrderTierConduit(ModelInfo info)
            {

                var ordered = RawTierConduitIds.OrderBy(x =>
                {
                    var curve = (info.DOC.GetElement(x).Location as LocationCurve).Curve;
                    return curve.GetEndPoint(0).Z;
                }).ToArray();

                var distinct = ordered.DistinctBy(x =>
                {
                    var curve = (info.DOC.GetElement(x).Location as LocationCurve).Curve;
                    return curve.GetEndPoint(0).Z;
                }).ToArray();

                RawTierConduitIds = distinct;
            }

            public override string ToString()
            {
                return string.Format(
                    "Placement Points Count: {0}\nLeftmost Conduit Id: {1}\n" +
                    "Rightmost Conduit Id: {2}\nTier Conduit Id Count: {3}",
                    PlacementPts.Count(), LeftmostConduitId.IntegerValue.ToString(),
                    RightmostConduitId.IntegerValue.ToString(), RawTierConduitIds.Count());
            }
        }

        // represents a spacing between the
        // different strut tiers in the conduit
        [DataContract]
        internal class TierSpacing
        {

            [DataMember]
            public double Spacing { get; private set; }
            [DataMember]
            public double RadiusOffset { get; private set; }

            public TierSpacing(double spacing, double radius)
            {
                Spacing = spacing;
                RadiusOffset = radius;
            }

            public string ToString(ModelInfo info)
            {
                var spacing = RMeasure.LengthFromDbl(info.DOC, Spacing);
                var offset = RMeasure.LengthFromDbl(info.DOC, RadiusOffset);
                return "Spacing: " + spacing + " - Offset: " + offset;
            }

            // take placement data about a conduit rack and
            // get the tier spacing infomation for it
            public static IEnumerable<TierSpacing> TierSpacingFromPlacementData(
                ModelInfo info, ConduitHangerPlacementData data, IEnumerable<ElementId> conduit_ids)
            {

                var tier_spacings = new List<TierSpacing>();
                var tier_cnt = data.RawTierConduitIds.Count();

                var tolerance = RMeasure.LengthDbl(info.DOC, "1/2\"");
                bool in_tolerance(double expected_val, double val) =>
                    val <= expected_val + tolerance && val >= expected_val - tolerance;

                double prev_z = -1;

                for (var i = 0; i < tier_cnt; i++)
                {

                    if (i == 0) continue;

                    prev_z = data.TierZHeight(info, i - 1);
                    var z = data.TierZHeight(info, i);

                    // gather ids with z hieght in tolerance
                    var largest_diameter = conduit_ids
                        .Where(x =>
                        {
                            var c = (info.DOC.GetElement(x).Location as LocationCurve).Curve;
                            var chk_z = c.GetEndPoint(0).Z;
                            return in_tolerance(z, chk_z);
                        })
                        .Select(x =>
                        {
                            var el = info.DOC.GetElement(x);
                            var diameter = el.LookupParameter("Diameter(Trade Size)").AsDouble();
                            return diameter;
                        })
                        .OrderByDescending(x => x)
                        .First();

                    tier_spacings.Add(new TierSpacing(z - prev_z, largest_diameter));
                }

                return tier_spacings;
            }
        }

        // Draw a strut baseline from the leftmost conduit
        // to the rightmost conduit in the rack
        private static Line GetStrutBaseLine(
            ModelInfo info, XYZ origin_pt,
            ConduitHangerPlacementData placement_data)
        {

            var leftcon = info.DOC.GetElement(placement_data.LeftmostConduitId);
            var rightcon = info.DOC.GetElement(placement_data.RightmostConduitId);

            var left_curve = (leftcon.Location as LocationCurve).Curve;
            var right_curve = (rightcon.Location as LocationCurve).Curve;

            var pt1 = left_curve.Project(origin_pt).XYZPoint;
            var pt2 = right_curve.Project(origin_pt).XYZPoint;

            pt1 = new XYZ(pt1.X, pt1.Y, placement_data.TierZHeight(info, 0));
            pt2 = new XYZ(pt2.X, pt2.Y, placement_data.TierZHeight(info, 0));



            try
            {
                return Line.CreateBound(pt1, pt2);
            }
            catch
            {
                return null;
            }
        }

        // Information about the rods and anchors for a strut hanger
        internal class StrutAnchorDetail
        {
            public string Anchor1Type { get; set; }
            public string Anchor2Type { get; set; }
            public double Rod1Length { get; set; }
            public double Rod2Length { get; set; }
        }

        // Get anchor information for strut hangers
        private static StrutAnchorDetail GetStrutAnchorDetail(
            ModelInfo info, View3D view, IEnumerable<XYZ> pts,
            double min_rod_length, bool ceiling_hanger)
        {

            StrutAnchorDetail anchors = new StrutAnchorDetail();

            var anchor_pts = pts.ToArray();
            if (anchor_pts.Count() != 2)
                throw new Exception("2 anchor points should be provided.");

            RRay anchor_one_ray;
            RRay anchor_two_ray;

            if (ceiling_hanger)
            {
                anchor_one_ray = RevitRaycast.Cast(
                    info, view, BICategoryCollection.CeilingHangerClash.ToList(),
                    anchor_pts[0], RGeo.PrimitiveDirection.Down);

                anchor_two_ray = RevitRaycast.Cast(
                    info, view, BICategoryCollection.CeilingHangerClash.ToList(),
                    anchor_pts[1], RGeo.PrimitiveDirection.Down);
            }
            else
            {
                anchor_one_ray = RevitRaycast.Cast(
                    info, view, BICategoryCollection.NormalHangerClash.ToList(),
                    anchor_pts[0], RGeo.PrimitiveDirection.Up);

                anchor_two_ray = RevitRaycast.Cast(
                    info, view, BICategoryCollection.NormalHangerClash.ToList(),
                    anchor_pts[1], RGeo.PrimitiveDirection.Up);
            }

            //handle first anchor
            if (anchor_one_ray.collisions.Any())
            {
                RRayCollision nearest_collision = anchor_one_ray.collisions
                    .Where(x => x.distance == anchor_one_ray.collisions.Min(y => y.distance)).First();

                string category = Internal.HangerUtil.FixLinkedCategory(info, nearest_collision.other_id);
                string anchor = Internal.HangerUtil.GetAnchortype(category);

                double base_rod_length = anchor_pts[0].DistanceTo(nearest_collision.point);

                if (base_rod_length < min_rod_length)
                    anchors.Rod1Length = 0;
                else
                {
                    anchors.Rod1Length = base_rod_length;
                }

                anchors.Anchor1Type = anchor;
            }

            //handle second anchor
            if (anchor_two_ray.collisions.Any())
            {

                RRayCollision nearest_collision = anchor_two_ray.collisions
                    .Where(x => x.distance == anchor_two_ray.collisions.Min(y => y.distance)).First();

                string category = Internal.HangerUtil.FixLinkedCategory(info, nearest_collision.other_id);
                string anchor = Internal.HangerUtil.GetAnchortype(category);

                double base_rod_length = anchor_pts[1].DistanceTo(nearest_collision.point);

                if (base_rod_length < min_rod_length)
                    anchors.Rod2Length = 0;
                else
                {
                    anchors.Rod2Length = base_rod_length;
                }

                anchors.Anchor2Type = anchor;
            }

            return anchors;

        }

        // Selection Filter for user selection processes in strut hanger
        private class ConduitPlacementDataSelectionFilter : ISelectionFilter
        {

            public List<ElementId> Ids { get; private set; }

            public ConduitPlacementDataSelectionFilter(IEnumerable<ElementId> ids)
            {
                Ids = ids.ToList();
            }

            public bool AllowElement(Element element)
            {
                if (element.Category.Name == "Conduits" &&
                    Ids.Any(x => x.IntegerValue == element.Id.IntegerValue))
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference refer, XYZ point)
            {
                return true;
            }
        }

        ///
        /// Strut Hanger Geometry Events
        ///

        private static ConduitStrutHangerModelCreation handler_create_conduit_strut_hanger = null;
        private static ExternalEvent exEvent_create_conduit_strut_hanger = null;

        // Sign up the event handlers
        public static void ConduitStrutHangerModelCreationSignUp()
        {
            handler_create_conduit_strut_hanger = new ConduitStrutHangerModelCreation();
            exEvent_create_conduit_strut_hanger = ExternalEvent.Create(handler_create_conduit_strut_hanger.Clone() as IExternalEventHandler);
        }

        // Create a strut hanger in the revit model and bind it to this program
        private static async Task<FamilyInstance> GenerateConduitStrutHangerGeometry(
            ModelInfo info, View view, StrutHanger hanger,
            HangerOptions opts, ConduitHangerPlacementData placement_data)
        {

            handler_create_conduit_strut_hanger.Info = info;
            handler_create_conduit_strut_hanger.View = view;
            handler_create_conduit_strut_hanger.PlacementData = placement_data;
            handler_create_conduit_strut_hanger.Hanger = hanger;
            handler_create_conduit_strut_hanger.Options = opts;

            exEvent_create_conduit_strut_hanger.Raise();

            while (exEvent_create_conduit_strut_hanger.IsPending)
            {
                await Task.Delay(100);
            }

            return handler_create_conduit_strut_hanger.ReturnHanger;
        }

        private class ConduitStrutHangerModelCreation : IExternalEventHandler, ICloneable
        {
            public ModelInfo Info { get; set; }
            public View View { get; set; }
            public StrutHanger Hanger { get; set; }
            public ConduitHangerPlacementData PlacementData { get; set; }
            public HangerOptions Options { get; set; }
            public FamilyInstance ReturnHanger { get; set; } = null;

            public object Clone() => this;

            private static Dictionary<int, string> TierCountToFamilyType { get; } = new Dictionary<int, string>()
            {
                {1, "1-5/8\" Strut w/ 1/2\" Rod"},
                {2, "(2) 1-5/8\" Strut w/ 1/2\" Rod"},
                {3, "(3) 1-5/8\" Strut w/ 1/2\" Rod"},
                {4, "(4) 1-5/8\" Strut w/ 1/2\" Rod"}
            };

            private readonly string[] TierSpacingParamPrefixes = new string[] { "Second", "Third", "Fourth" };
            private readonly string TierSpacingParamSuffix = " Tier Offset";

            public void Execute(UIApplication app)
            {
                /*
				// debug points
				FilteredElementCollector d_coll = new FilteredElementCollector(Info.DOC);
				FamilySymbol debug_sym = d_coll
					.OfClass(typeof(FamilySymbol)).Where(x =>
                                                         (x as FamilySymbol).FamilyName.Equals(HangerDrawing.DebugPointFamilyNameNoExt))
					.First() as FamilySymbol;
                */

                // debugger.debug_show(err:Hanger.ToString(Info));
                using var transGroup = new TransactionGroup(Info.DOC, "Model Strut Hangers");
                transGroup.Start();
                try
                {
                    FilteredElementCollector el_coll = new FilteredElementCollector(Info.DOC);
                    FamilySymbol sym = null;
                    var type_name = "1-5/8\" Strut w/ 1/2\" Rod";

                    if (Options.CeilingHangers)
                    {
                        var syms = el_coll
                            .OfClass(typeof(FamilySymbol))
                            .Where(x =>
                                   (x as FamilySymbol).FamilyName.Equals(StrutHanger.StrutCeilingHangerFamilyNameNoExt) &&
                                   (x as FamilySymbol).Name.Equals(type_name));

                        if (syms.Any()) sym = syms.First() as FamilySymbol;
                    }
                    else
                    {
                        bool s = TierCountToFamilyType.TryGetValue(Hanger.TierCount, out type_name);
                        if (!s) type_name = "1-5/8\" Strut w/ 1/2\" Rod";

                        var syms = el_coll
                            .OfClass(typeof(FamilySymbol))
                            .Where(x =>
                                   (x as FamilySymbol).FamilyName.Equals(StrutHanger.StrutHangerFamilyNameNoExt) &&
                                   (x as FamilySymbol).Name.Equals(type_name));


                        if (syms.Any()) sym = syms.First() as FamilySymbol;
                    }

                    if (sym == null)
                    {
                        throw new Exception("Hanger symbol is null");
                    }

                    using var tx1 = new Transaction(Info.DOC, "Create Sketch Plane");
                    tx1.Start();

                    //create sketch plane
                    Plane plane = Plane.CreateByNormalAndOrigin(
                        Info.DOC.ActiveView.ViewDirection, Info.UIDOC.Document.ActiveView.Origin);
                    SketchPlane sp = SketchPlane.Create(Info.DOC, plane);
                    Info.DOC.ActiveView.SketchPlane = sp;

                    tx1.Commit();

                    using var tx2 = new Transaction(Info.DOC, "Activate Symbol");
                    tx2.Start();

                    if (!sym.IsActive) FamilyLoader.ActivateSymbol(sym);

                    tx2.Commit();

                    using var tx3 = new Transaction(Info.DOC, "Place Family");
                    tx3.Start();

                    //place hanger
                    FamilyInstance fam = Info.DOC.Create.NewFamilyInstance(
                        Hanger.OriginPt.RevitPoint(), sym, Internal.HangerUtil.GetLevel(
                            Info, PlacementData.RawTierConduitIds[0]), StructuralType.NonStructural);

                    tx3.Commit();

                    using var tx4 = new Transaction(Info.DOC, "Position Family");
                    tx4.Start();

                    // rotate hanger
                    double get_angle(Line line1, Line line2)
                    {
                        var x1 = line1.GetEndPoint(0).X;
                        var y1 = line1.GetEndPoint(0).Y;
                        var x2 = line1.GetEndPoint(1).X;
                        var y2 = line1.GetEndPoint(1).Y;

                        var x3 = line2.GetEndPoint(0).X;
                        var y3 = line2.GetEndPoint(0).Y;
                        var x4 = line2.GetEndPoint(1).X;
                        var y4 = line2.GetEndPoint(1).Y;
                        return Math.Atan2(y2 - y1, x2 - x1) - Math.Atan2(y4 - y3, x4 - x3);
                    }

                    var up_sec_pt = new XYZ(Hanger.OriginPt.X, Hanger.OriginPt.Y, Hanger.OriginPt.Z + 5.0);
                    var axis = Line.CreateBound(Hanger.OriginPt.RevitPoint(), up_sec_pt);

                    // get nested anchor points to get a strut line for the family instance
                    var nested_families = fam.GetSubComponentIds().Select(x => Info.DOC.GetElement(x)).ToList();
                    var anchor1 = (nested_families[0].Location as LocationPoint).Point;
                    var anchor2 = (nested_families[1].Location as LocationPoint).Point;
                    var selected_pt = anchor1.DistanceTo(Hanger.OriginPt.RevitPoint()) > anchor2.DistanceTo(Hanger.OriginPt.RevitPoint()) ? anchor1 : anchor2;
                    var fam_strut_line = Line.CreateBound(Hanger.OriginPt.RevitPoint(), selected_pt);


                    var rightmost_curve = ((Info.DOC.GetElement(PlacementData.RightmostConduitId).Location) as LocationCurve).Curve;
                    var rightmost_aligned_origin = rightmost_curve.Project(Hanger.OriginPt.RevitPoint()).XYZPoint;
                    var real_strut_line = Line.CreateBound(Hanger.OriginPt.RevitPoint(), rightmost_aligned_origin);
                    var angle = get_angle(fam_strut_line, real_strut_line);

                    var degrees_90 = RMeasure.AngleDbl(Info.DOC, "90");

                    // double check against Basis axis to determine world flip
                    var x_basis_line = Line.CreateBound(
                        XYZ.BasisX, new XYZ(XYZ.BasisX.X + 2, XYZ.BasisX.Y, XYZ.BasisX.Z));

                    bool flip1 = !RGeo.IsLeft(x_basis_line, selected_pt);
                    bool flip2 = !RGeo.IsLeft(real_strut_line, selected_pt);
                    if (flip1) angle = angle + (degrees_90 * 2);
                    if (flip2) angle = angle - (degrees_90 * 2);

                    //angle = pi / 2;
                    // angle = pi - angle;
                    /*
                    if(!RGeo.IsLeft(x_basis_line, selected_pt)) 
                    {
                        if(RGeo.IsLeft(real_strut_line, selected_pt)) 
                        {
                            angle = pi - angle;
                        }
                        else
                        {
                            angle = pi + (pi - angle);
                        }
                    }
                    */

                    var ang_str = RMeasure.AngleFromDouble(Info.DOC, angle);
                    fam.Location.Rotate(axis, angle);

                    // set workset of hanger
                    var workset = Info.DOC.GetElement(PlacementData.LeftmostConduitId)
                        .get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).AsInteger();

                    Parameter workset_param = fam.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                    workset_param.Set(workset);

                    // dimension the hanger
                    fam.LookupParameter("Extra Outside Strut Length").Set(Options.OutsideRodExtraLength);
                    fam.LookupParameter("Inside Rod Gap").Set(Options.InsideRodGap + (PlacementData.LeftMostDiameter(Info) / 2.0));
                    fam.LookupParameter("Length").Set(Hanger.StrutLine.Length);

                    var y_origin = PlacementData.GetTierDiameter(Info, 0) / 2.0;
                    fam.LookupParameter("Y Origin").Set(y_origin);

                    var inch = RMeasure.AngleDbl(Info.DOC, "1\"");

                    var rl1 = inch;
                    var rl2 = inch;

                    if (Hanger.RodOneLength != 0.0) rl1 = Hanger.RodOneLength;
                    if (Hanger.RodTwoLength != 0.0) rl2 = Hanger.RodTwoLength;

                    if (Options.CeilingHangers)
                    {
                        fam.LookupParameter("Rod Length").Set(rl1);
                    }
                    else
                    {
                        fam.LookupParameter("Extra Bottom Rod Length").Set(0.2);
                        fam.LookupParameter("Rod 1 Length").Set(rl1);
                        fam.LookupParameter("Rod 2 Length").Set(rl2);

                        // tier spacing
                        var accumuated_spacing = 0.0;
                        for (int i = 0; i < Hanger.TierSpacings.Count(); i++)
                        {

                            var spacing = Hanger.TierSpacings[i];
                            var prefix = TierSpacingParamPrefixes[i];
                            var final_spacing = accumuated_spacing + (spacing.Spacing - spacing.RadiusOffset);
                            fam.LookupParameter(prefix + TierSpacingParamSuffix).Set(final_spacing);
                            accumuated_spacing += (spacing.Spacing - spacing.RadiusOffset);
                        }
                    }

                    ReturnHanger = fam;

                    tx4.Commit();
                }
                catch (Exception ex)
                {
                    debugger.show(header: "Strut Hangers", err: ex.Message);
                    transGroup.RollBack();
                }

                transGroup.Assimilate();
            }

            public string GetName()
            {
                return "Insert Strut Hanger Models";
            }
        }

        ///
        /// Cable Tray Hanger Model Placement
        ///

        private static CableTrayStrutHangerModelCreation handler_create_cable_tray_strut_hanger = null;
        private static ExternalEvent exEvent_create_cable_tray_strut_hanger = null;

        // Sign up the event handlers
        public static void CableTrayStrutHangerModelCreationSignUp()
        {
            handler_create_cable_tray_strut_hanger = new CableTrayStrutHangerModelCreation();
            exEvent_create_cable_tray_strut_hanger = ExternalEvent.Create(handler_create_cable_tray_strut_hanger.Clone() as IExternalEventHandler);
        }

        // Create a strut hanger in the revit model and bind it to this program
        private static async Task<FamilyInstance> GenerateCableTrayStrutHangerGeometry(
            ModelInfo info, View view, StrutHanger hanger,
            HangerOptions opts, CableTrayHangerPlacementData placement_data)
        {

            handler_create_cable_tray_strut_hanger.Info = info;
            handler_create_cable_tray_strut_hanger.View = view;
            handler_create_cable_tray_strut_hanger.PlacementData = placement_data;
            handler_create_cable_tray_strut_hanger.Hanger = hanger;
            handler_create_cable_tray_strut_hanger.Options = opts;

            exEvent_create_cable_tray_strut_hanger.Raise();

            while (exEvent_create_cable_tray_strut_hanger.IsPending)
            {
                await Task.Delay(100);
            }

            return handler_create_cable_tray_strut_hanger.ReturnHanger;
        }

        private class CableTrayStrutHangerModelCreation : IExternalEventHandler, ICloneable
        {
            public ModelInfo Info { get; set; }
            public View View { get; set; }
            public StrutHanger Hanger { get; set; }
            public CableTrayHangerPlacementData PlacementData { get; set; }
            public HangerOptions Options { get; set; }
            public FamilyInstance ReturnHanger { get; set; } = null;

            public object Clone() => this;

            private readonly string[] TierSpacingParamPrefixes = new string[] { "Second", "Third", "Fourth" };
            // private readonly string TierSpacingParamSuffix = " Tier Offset";

            public void Execute(UIApplication app)
            {
                using var transGroup = new TransactionGroup(Info.DOC, "Model Strut Hangers");
                transGroup.Start();

                try
                {
                    FilteredElementCollector el_coll = new FilteredElementCollector(Info.DOC);
                    FamilySymbol sym = null;
                    var type_name = "1-5/8\" Strut w/ 1/2\" Rod";

                    var syms = el_coll
                        .OfClass(typeof(FamilySymbol))
                        .Where(x =>
                                (x as FamilySymbol).FamilyName.Equals(StrutHanger.StrutHangerFamilyNameNoExt) &&
                                (x as FamilySymbol).Name.Equals(type_name));

                    if (syms.Any()) sym = syms.First() as FamilySymbol;
                    if (sym == null) throw new Exception("Hanger symbol is null");

                    using var tx1 = new Transaction(Info.DOC, "Create Sketch Plane");
                    tx1.Start();

                    //create sketch plane
                    Plane plane = Plane.CreateByNormalAndOrigin(
                        Info.DOC.ActiveView.ViewDirection, Info.UIDOC.Document.ActiveView.Origin);
                    SketchPlane sp = SketchPlane.Create(Info.DOC, plane);
                    Info.DOC.ActiveView.SketchPlane = sp;

                    tx1.Commit();

                    using var tx2 = new Transaction(Info.DOC, "Activate Symbol");
                    tx2.Start();

                    if (!sym.IsActive) FamilyLoader.ActivateSymbol(sym);

                    tx2.Commit();

                    using var tx3 = new Transaction(Info.DOC, "Place Family");
                    tx3.Start();

                    //place hanger
                    FamilyInstance fam = Info.DOC.Create.NewFamilyInstance(
                        Hanger.OriginPt.RevitPoint(), sym, Internal.HangerUtil.GetLevel(
                            Info, PlacementData.TrayId), StructuralType.NonStructural);

                    tx3.Commit();

                    using var tx4 = new Transaction(Info.DOC, "Position Family");
                    tx4.Start();

                    // rotate hanger
                    /* double get_angle(Line line1, Line line2)
                    {
                        var x1 = line1.GetEndPoint(0).X;
                        var y1 = line1.GetEndPoint(0).Y;
                        var x2 = line1.GetEndPoint(1).X;
                        var y2 = line1.GetEndPoint(1).Y;

                        var x3 = line2.GetEndPoint(0).X;
                        var y3 = line2.GetEndPoint(0).Y;
                        var x4 = line2.GetEndPoint(1).X;
                        var y4 = line2.GetEndPoint(1).Y;
                        return Math.Atan2(y2 - y1, x2 - x1) - Math.Atan2(y4 - y3, x4 - x3);
                    } */

                    /* var up_sec_pt = new XYZ(Hanger.OriginPt.X, Hanger.OriginPt.Y, Hanger.OriginPt.Z + 5.0);
					var axis = Line.CreateBound(Hanger.OriginPt.RevitPoint(), up_sec_pt);

                    // get nested anchor points to get a strut line for the family instance
					var nested_families = fam.GetSubComponentIds().Select(x => Info.DOC.GetElement(x)).ToList();
                    var anchor1 = (nested_families[0].Location as LocationPoint).Point;
					var anchor2 = (nested_families[1].Location as LocationPoint).Point;
					var selected_pt = anchor1.DistanceTo(Hanger.OriginPt.RevitPoint()) > anchor2.DistanceTo(Hanger.OriginPt.RevitPoint()) ? anchor1 : anchor2;
					var fam_strut_line = Line.CreateBound(Hanger.OriginPt.RevitPoint(), selected_pt);

                    var rightmost_curve = ((Info.DOC.GetElement(PlacementData.MinBBLine).Location) as LocationCurve).Curve;
                    var rightmost_aligned_origin = rightmost_curve.Project(Hanger.OriginPt.RevitPoint()).XYZPoint;
                    var real_strut_line = Line.CreateBound(Hanger.OriginPt.RevitPoint(), rightmost_aligned_origin);
                    var angle = get_angle(fam_strut_line, real_strut_line);

                    var degrees_90 = RMeasure.AngleDbl(Info.DOC, "90");

                    // double check against Basis axis to determine world flip
                    var x_basis_line = Line.CreateBound(
                        XYZ.BasisX, new XYZ(XYZ.BasisX.X + 2, XYZ.BasisX.Y, XYZ.BasisX.Z));

                    bool flip1 = !RGeo.IsLeft(x_basis_line, selected_pt);
                    bool flip2 = !RGeo.IsLeft(real_strut_line, selected_pt);
                    if(flip1) angle = angle + (degrees_90 * 2);
                    if(flip2) angle = angle - (degrees_90 * 2);

                    var ang_str = RMeasure.AngleFromDouble(Info.DOC, angle);
                    fam.Location.Rotate(axis, angle);

					// set workset of hanger
					var workset = Info.DOC.GetElement(PlacementData.LeftmostConduitId)
                        .get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM ).AsInteger();
                    
					Parameter workset_param = fam.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM );
					workset_param.Set(workset);

					// dimension the hanger
					fam.LookupParameter("Extra Outside Strut Length").Set(Options.OutsideRodExtraLength);
					fam.LookupParameter("Inside Rod Gap").Set(Options.InsideRodGap + (PlacementData.LeftMostDiameter(Info) / 2.0));
					fam.LookupParameter("Length").Set(Hanger.StrutLine.Length);

                    var y_origin = PlacementData.GetTierDiameter(Info,0) / 2.0;
					fam.LookupParameter("Y Origin").Set(y_origin);

                    var inch = RMeasure.AngleDbl(Info.DOC, "1\"");

                    var rl1 = inch;
                    var rl2 = inch;

                    if(Hanger.RodOneLength != 0.0) rl1 = Hanger.RodOneLength;
                    if(Hanger.RodTwoLength != 0.0) rl2 = Hanger.RodTwoLength;
                    
					if(Options.CeilingHangers) {
						fam.LookupParameter("Rod Length").Set(rl1);
					}
					else {
						fam.LookupParameter("Extra Bottom Rod Length").Set(0.2);
						fam.LookupParameter("Rod 1 Length").Set(rl1);
						fam.LookupParameter("Rod 2 Length").Set(rl2);

						// tier spacing
						var accumuated_spacing = 0.0;
						for(int i = 0; i < Hanger.TierSpacings.Count(); i++) {
                            
							var spacing = Hanger.TierSpacings[i];
							var prefix = TierSpacingParamPrefixes[i];
							var final_spacing = accumuated_spacing + (spacing.Spacing - spacing.RadiusOffset);
							fam.LookupParameter(prefix + TierSpacingParamSuffix).Set(final_spacing);
							accumuated_spacing += (spacing.Spacing - spacing.RadiusOffset);
						}
					}

					ReturnHanger = fam; */

                    tx4.Commit();
                }
                catch (Exception ex)
                {
                    debugger.show(header: "Strut Hangers", err: ex.ToString());
                    transGroup.RollBack();
                }

                transGroup.Assimilate();
            }

            public string GetName()
            {
                return "Insert Strut Hanger Models";
            }
        }
    }

    [DataContract]
    public class ConduitStrap
    {
        [DataMember]
        public string Diameter { get; private set; }
        [DataMember]
        public int Count { get; private set; }

        public ConduitStrap(string diameter, int count)
        {
            Diameter = diameter;
            Count = count;
        }

        public static IEnumerable<ConduitStrap> GenerateConduitStraps(
            ModelInfo info, IEnumerable<ElementId> ids)
        {

            var straps = new List<ConduitStrap>();
            foreach (var id in ids)
            {
                var el = info.DOC.GetElement(id);
                var diameter = el.LookupParameter("Diameter(Trade Size)").AsValueString();
                var strap = new ConduitStrap(diameter, 1);
                straps.AddStrap(strap);
            }

            return straps;
        }
    }

    internal static class HangerExt
    {
        public static void AddStraps(this List<ConduitStrap> straps, IEnumerable<ConduitStrap> add_straps) =>
            add_straps.ToList().ForEach(x => straps.AddStrap(x));

        public static void AddStrap(this List<ConduitStrap> straps, ConduitStrap strap)
        {

            var idx = straps.FindIndex(x => x.Diameter.Equals(strap.Diameter));

            if (idx == -1)
                straps.Add(strap);
            else
            {
                var old_cnt = straps[idx].Count;
                straps.Add(new ConduitStrap(strap.Diameter, old_cnt + 1));
            }
        }
    }

    public enum StrutHangerRackType
    {
        Conduit,
        CableTray,
    }
}
