using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.BICategories;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Tools;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Hangers
{
    namespace Internal
    {

        internal static class HangerUtil
        {

            public static Level GetLevel(ModelInfo info, ElementId id)
            {

                var el = info.DOC.GetElement(id);
                string level_str = el.LookupParameter("Reference Level").AsValueString();
                info.SEL.SetElementIds(new ElementId[] { el.Id });
                var levels = new FilteredElementCollector(info.DOC).OfClass(typeof(Level)).Where(x => x.Name == level_str);
                return levels.First() as Level;
            }

            public static XYZ[] GetPlacementPoints(double nominal_spacing, double bend_spacing, Curve curve)
            {

                List<XYZ> ret_pts = new List<XYZ>();
                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);
                double cl = curve.ApproximateLength;
                XYZ point = start;

                // if conduit piece is too short for bend spacing and less than nominal
                if (cl <= nominal_spacing)
                {
                    point = RGeo.DerivePointBetween(point, end, cl / 2);
                    ret_pts.Add(point);
                    return ret_pts.ToArray();
                }

                // if conduit is not too short for bend spacing and is less than nominal
                if (cl < nominal_spacing + (bend_spacing * 2))
                {
                    point = RGeo.DerivePointBetween(start, end, bend_spacing);
                    ret_pts.Add(point);

                    if ((cl - (bend_spacing * 2)) >= nominal_spacing)
                    {
                        point = RGeo.DerivePointBetween(start, end, cl / 2.0);
                        ret_pts.Add(point);
                    }

                    point = RGeo.DerivePointBetween(start, end, cl - bend_spacing);
                    ret_pts.Add(point);
                    return ret_pts.ToArray();
                }

                // if conduit is not to short for bend spacing and is greater than nominal
                if (cl >= nominal_spacing + (bend_spacing * 2))
                {
                    bool passed = true;
                    double totalLen = 0;
                    point = RGeo.DerivePointBetween(point, end, bend_spacing);
                    totalLen += bend_spacing;
                    ret_pts.Add(point);

                    double newNominalSpacing = (cl - (bend_spacing * 2)) / Math.Ceiling((cl - (bend_spacing * 2)) / nominal_spacing);

                    while (passed)
                    {
                        point = RGeo.DerivePointBetween(point, end, newNominalSpacing);
                        totalLen += nominal_spacing;
                        if (totalLen <= cl - bend_spacing)
                            ret_pts.Add(point);
                        else
                            passed = false;
                    }

                    point = RGeo.DerivePointBetween(start, end, cl - bend_spacing);
                    ret_pts.Add(point);
                }
                return ret_pts.ToArray();
            }

            // Fix the category of ray element when it hits linked model
            public static string FixLinkedCategory(ModelInfo info, ElementId collision_element_id)
            {
                //fix category if linked file
                Element collision_element = info.DOC.GetElement(collision_element_id);
                string category = collision_element.Category.Name;

                if (collision_element.Category.Name == "RVT Links")
                {
                    RevitLinkInstance link = info.DOC.GetElement(collision_element.Id) as RevitLinkInstance;
                    Document linkDoc = link.GetLinkDocument();
                    FilteredElementCollector link_coll = new FilteredElementCollector(linkDoc)
                        .WherePasses(new ElementMulticategoryFilter(BICategoryCollection.NormalHangerClash));

                    foreach (Element el in link_coll)
                    {
                        Reference refer = new Reference(el).CreateLinkReference(link);
                        if (el.Id != refer.LinkedElementId) continue;
                        category = el.Category.Name;
                    }
                }

                return category;
            }

            // Change the type of anchor based on what
            // the category of the ray collision object is.
            public static string GetAnchortype(string category)
            {

                string anchType = "Hilti KH-EZ I - Concrete Anchor";
                switch (category)
                {

                    case "Floors":
                        anchType = "Hilti KH-EZ I - Concrete Anchor";
                        break;
                    case "Structural Framing (Joist)":
                        anchType = "Steel City Beam Clamp";
                        break;
                    default:
                        break;
                }
                return anchType;
            }

            public static double[] GetStandardizedStrutLengthFromSelected(Document doc, ElementId[] ids)
            {
                if (ids.Length > 2)
                    throw new Exception("More than two conduit element ids were passed.");

                List<double> standard_lengths = new List<double>() {
                    1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0
            };

                double len(Element el) => el.LookupParameter("Length").AsDouble();
                double dia(Element el) => el.LookupParameter("Diameter(Trade Size)").AsDouble();

                var con1 = doc.GetElement(ids[0]);
                var con2 = doc.GetElement(ids[1]);

                if (!con1.Category.Name.Equals("Conduits") || !con2.Category.Name.Equals("Conduits"))
                    throw new Exception("ids are not of type conduit.");

                var sel_con = len(con1) >= len(con2) ? con2 : con1;
                var other = sel_con == con1 ? con2 : con1;

                var line = (sel_con.Location as LocationCurve).Curve as Line;
                var other_line = (other.Location as LocationCurve).Curve as Line;

                var pt = RGeo.DerivePointBetween(line, len(sel_con) / 2.0);
                var second_pt = other_line.Project(pt).XYZPoint;

                var raw_length = pt.DistanceTo(second_pt);
                var extra_strut_length = RMeasure.LengthDbl(doc, "1\"");
                raw_length += extra_strut_length * 2.0;
                raw_length += (dia(con1) + dia(con2));

                var idx = standard_lengths.BinarySearch(raw_length);
                if (idx < 0) idx = ~idx;
                return new[] { raw_length, standard_lengths[idx] };
            }
        }

        /*
          public static class HangerRestoreUtil
          {

          public static RestoredHangers RestoreHangersInView(
          ModelInfo info, IEnumerable<ElementId> view_conduit_ids,
          View3D view, double min_rod_length)
          {
          var ret_single = new List<SingleHanger>();
          var ret_strut = new List<StrutHanger>();
          var collected_ids = new List<ElementId>();
          var search_bbs = new List<BoundingBoxXYZ>();


          //local functions
          static string diameter(Element el) => el.LookupParameter("Diameter(Trade Size)").AsValueString();
                
          foreach (var id in view_conduit_ids)
          {
          var conduit = info.DOC.GetElement(id);
          if (conduit == null || !conduit.Category.Name.Equals("Conduits")) continue;
          search_bbs.Add(conduit.get_BoundingBox(view));
          }
                
          debugger.show(header:"", err:"Conduit to search: " + search_bbs.Count().ToString());
                
          foreach(var bb in search_bbs.Select((value, i) => new {i, value})) {
          var hits = RevitRaycast.CastBoundingBox(info, bb.value, BuiltInCategory.OST_GenericModel, view)
          .Where(x => !collected_ids.Any(y => y.IntegerValue == x.IntegerValue));
          collected_ids.AddRange(hits);
          }
                
          debugger.show(err:"Hangers: " + collected_ids.Count().ToString());
                    
          foreach(var hid in collected_ids)
          {
          var el = info.DOC.GetElement(hid);
          var fam = el as FamilyInstance;

          if (fam.Symbol.FamilyName.ToLower().Contains("strut"))
          {

          var TierCount = new Dictionary<string, int>() {
              { "1-5/8\" Strut w/ 1/2\" Rod", 1 },
              { "(2) 1-5/8\" Strut w/ 1/2\" Rod", 2 },
              { "(3) 1-5/8\" Strut w/ 1/2\" Rod", 3 },
              { "(4) 1-5/8\" Strut w/ 1/2\" Rod", 4 }
              };

              TierCount.TryGetValue(el.Name, out var tier_cnt);

              var bottom_rod_length_dbl = el.LookupParameter("Extra Bottom Rod Length").AsDouble();
              var outside_strut_length_dbl = el.LookupParameter("Extra Outside Strut Length").AsDouble();
              var inside_rod_gap_dbl = el.LookupParameter("Inside Rod Gap").AsDouble();
              var strut_length_dbl = el.LookupParameter("Length").AsDouble();
              var rl1_dbl = el.LookupParameter("Rod 1 Length").AsDouble() + bottom_rod_length_dbl;
              var rl2_dbl = el.LookupParameter("Rod 2 Length").AsDouble() + bottom_rod_length_dbl;
              var cc = (int)(rl1_dbl + rl2_dbl) / 10;
              var ss = "1 5/8\"";
              var rdia = RMeasure.LengthDbl(info, "1/2\"");

              var bb = el.get_BoundingBox(view);
              var bb_pts = RGeo.GetBoundingBoxLowerPts(bb).ToArray();

              var center_line = Line.CreateBound(bb_pts.First(), bb_pts.Last());
              var center = RGeo.DerivePointBetween(center_line, center_line.Length / 2.0);

              var strap_ids = RevitRaycast.CastBoundingBox(info, bb, BuiltInCategory.OST_Conduit);

              var straps = new List<ConduitStrap>();
              strap_ids.ToList().ForEach(x =>
              {
              var el = info.DOC.GetElement(x);
              var dia = diameter(el);
              var strap = new ConduitStrap(dia, 1);
              straps.AddStrap(strap);
              });


              var apts = bb_pts.Take(2).ToList();
              var anchor_detail = Internal.HangerUtil.GetStrutAnchorDetail(info, view, apts, min_rod_length, false);
              var hardware = new string[] {
              "Washer", "Washer", "Washer", "Washer",
              "Hex Nut", "Hex Nut", "Spring Nut", "Spring Nut" };

              StrutHanger sh = new StrutHanger(
              rl1_dbl, rl2_dbl, rdia, cc, strut_length_dbl,
              ss, tier_cnt, anchor_detail.Anchor1Type, anchor_detail.Anchor2Type,
              hardware, straps, hid.IntegerValue);

              ret_strut.Add(sh);


              }
              else if (fam.Symbol.FamilyName.ToLower().Contains("mineralac") ||
              fam.Symbol.FamilyName.ToLower().Contains("batwing"))
              {
              var rl_dbl = el.LookupParameter("Rod Length").AsDouble();
              var cc = (int)(rl_dbl) / 10;
              var rdia = RMeasure.LengthDbl(info, "1/2\"");

              var att_type = fam.Symbol.FamilyName.ToLower().Contains("mineralac") ? "Mineralac" : "Batwing";

              var bb = el.get_BoundingBox(view);
              var bb_pts = RGeo.GetBoundingBoxLowerPts(bb).ToArray();
              var center_line = Line.CreateBound(bb_pts.First(), bb_pts.Last());
              var center = RGeo.DerivePointBetween(center_line, center_line.Length / 2.0);

              var host_ids = RevitRaycast.CastBoundingBox(info, bb, BuiltInCategory.OST_Conduit);
              var host_id = host_ids.Any() ? host_ids.First() : new ElementId(-1);
              if(host_id.IntegerValue == -1) continue;
                        
              var host_el = info.DOC.GetElement(host_id);
              var dia = host_el.LookupParameter("Diameter(Trade Size)").AsValueString();

              var hardware = new string[] { "Washer", "Washer", "Hex Nut", "Spring Nut" };

              var anchor_detail = Internal.HangerUtil.GetStrutAnchorDetail(info, view, new[] { center, center }, min_rod_length, false);

              SingleHanger sh = new SingleHanger(rl_dbl, rdia, cc, att_type, dia, anchor_detail.Anchor1Type, hardware, hid.IntegerValue);

              ret_single.Add(sh);
              }
              }

              return new RestoredHangers(ret_single, ret_strut);
              }
        */
    }


    /*
    public class RestoredHangers {
            
        private List<SingleHanger> SH { get; set; }
        private List<StrutHanger> StH { get; set; }

        private RestoredHangers() { }

        public RestoredHangers(
            IEnumerable<SingleHanger> single_hangers,
            IEnumerable<StrutHanger> strut_hangers) {

            SH = single_hangers.ToList();
            StH = strut_hangers.ToList();
        }

        public IEnumerable<SingleHanger> SingleHangers {
            get {
                foreach (var s in SH)
                    yield return s;
            }
        }

        public IEnumerable<StrutHanger> StrutHangers {
            get {
                foreach (var s in StH)
                    yield return s;
            }
        }
    }
    */
}


