using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.BetterFasterStrongerLinq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Tools.ConduitFittings
{

    public class TotaledFitting
    {

        public Fitting Fitting { get; set; }
        public int Count { get; set; } = 0;

        public TotaledFitting(Fitting fitting, int cnt)
        {
            Fitting = fitting;
            Count = cnt;
        }
    }

    /// <summary>
    /// Container for combining wire into single entries for BOM output
    /// </summary>
    public class FittingTotal
    {
        public List<TotaledFitting> Fittings { get; private set; }

        public bool IsEmpty { get => !Fittings.Any(); }

        public FittingTotal()
        {
            Fittings = new List<TotaledFitting>();
        }

        private void OrderFittings()
        {
            Fittings = Fittings.OrderBy(x => x.Fitting.Diameter).ThenBy(y => y.Fitting.Angle).ToList();
        }

        public static FittingTotal GetTotaledFittings(ModelInfo info, MasterDataPackage package, WireType type)
        {
            FittingTotal t = new FittingTotal();

            // collect all fittings in view
            var ids = new FilteredElementCollector(info.DOC, info.UIDOC.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_ConduitFitting).Select(x => x.Id.IntegerValue).ToList();

            t.PushFittings(info, ids);
            t.OrderFittings();
            return t;
        }

        private void PushFittings(ModelInfo info, IEnumerable<int> fitting_ids)
        {
            var fittings = new List<Fitting>();

            foreach (var id in fitting_ids)
            {
                var fitting = FittingFromConduitId(info, id);
                if (fitting != null) fittings.Add(fitting);
            }

            Fittings.AddRange(GetFittingCounts(info.DOC, fittings).ToList());
        }

        private static Fitting FittingFromConduitId(ModelInfo info, int c_id)
        {

            var id = new ElementId(c_id);
            Element conduit = info.DOC.GetElement(id);
            Parameter p(string p_str) => conduit.LookupParameter(p_str);

            var is_not_fitting = conduit == null || conduit.Category == null ||
            !conduit.Category.Name.Equals("Conduit Fittings") ||
            p("Angle") == null || p("Nominal Diameter") == null;

            if (is_not_fitting) return null;
            if (!(conduit is FamilyInstance inst)) return null;

            var c_name = inst.Symbol.Family.Name;
            var angle = conduit.LookupParameter("Angle").AsDouble();
            var diameter = conduit.LookupParameter("Nominal Diameter").AsDouble();

            Fitting entry = new Fitting(angle, diameter, c_name);

            return entry;
        }

        private static IEnumerable<TotaledFitting> GetFittingCounts(Document doc, IEnumerable<Fitting> fittings)
        {
            List<TotaledFitting> ret_fc = new List<TotaledFitting>();

            foreach (var fitting in fittings)
            {
                double prune_angle = RMeasure.AngleDbl(doc, "90\u00B0");
                string str_diameter(double x) => RMeasure.LengthFromDbl(doc, x);
                bool in_tolerance(double angle) => angle > prune_angle || angle.IsAlmostEqual(prune_angle);
                bool is_count_match(TotaledFitting x) => in_tolerance(x.Fitting.Angle) &&
                    str_diameter(x.Fitting.Diameter).Equals(str_diameter(fitting.Diameter)) &&
                    x.Fitting.Type.Equals(fitting.Type);

                var idx = ret_fc.FindIndex(x => is_count_match(x));

                if (idx > -1) ret_fc[idx].Count++;
                else
                {
                    if (in_tolerance(fitting.Angle)) ret_fc.Add(new TotaledFitting(fitting, 1));
                }
            }

            return ret_fc;
        }

        private static IEnumerable<TotaledFitting> GetFittingCounts(ModelInfo info, IEnumerable<int> ids)
        {

            var fittings = new List<Fitting>();

            foreach (var id in ids)
            {
                var fitting = FittingFromConduitId(info, id);
                fittings.Add(fitting);
            }

            return GetFittingCounts(info.DOC, fittings);
        }
    }
}