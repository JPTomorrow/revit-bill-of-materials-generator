using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Wires;

namespace JPMorrow.Revit.Tools.ConduitFittings {

    public class TotaledFitting {

		public Fitting Fitting { get; set; }
		public int Count { get; set; }

		public TotaledFitting(Fitting fitting, int cnt) {
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

		public FittingTotal() {
			Fittings = new List<TotaledFitting>();
		}

		private void OrderFittings() {
			Fittings = Fittings.OrderBy(x => x.Fitting.Diameter).ThenBy(y => y.Fitting.Angle).ToList();
		}

		public static FittingTotal GetTotaledFittings(ModelInfo info, MasterDataPackage package, WireType type) {
			
			FittingTotal t = new FittingTotal();
			t.PushFittings(info, package.GetSelectedConduitPackage().Cris, package.GetSelectedConduitPackage().WireManager, type);
			t.OrderFittings();
			return t;
		}

		private void PushFittings(ModelInfo info, IEnumerable<ConduitRunInfo> cris, WireManager wm, WireType type) {

            var fittings = new List<Fitting>();

            foreach(var cri in cris) {
                var wires = wm.GetWires(cri.WireIds);
				if(!wires.Any(x => x.WireType == type)) continue;

				foreach(var id in cri.FittingIds) {
					var fitting = FittingFromConduitId(info, id);
					if(fitting != null) fittings.Add(fitting);
                }
			}
            
			Fittings.AddRange(GetFittingCounts(fittings).ToList());
		}

        private static Fitting FittingFromConduitId(ModelInfo info, int c_id) {

			var id = new ElementId(c_id);
			Element conduit = info.DOC.GetElement(id);

			if (conduit == null || conduit.Category == null ||
			!conduit.Category.Name.Equals("Conduit Fittings") ||
			conduit.LookupParameter("Angle") == null ||
			conduit.LookupParameter("Nominal Diameter") == null) return null;

            if (!(conduit is FamilyInstance inst)) return null;

			var c_name = inst.Symbol.Family.Name;
			var angle = conduit.LookupParameter("Angle").AsDouble();
			var diameter = conduit.LookupParameter("Nominal Diameter").AsDouble();

			Fitting entry = new Fitting(angle, diameter, c_name);

			return entry;
		}

		private static IEnumerable<TotaledFitting> GetFittingCounts(IEnumerable<Fitting> fittings) {
            
			List<TotaledFitting> ret_fc = new List<TotaledFitting>();

			foreach(var fitting in fittings) {

				var entry = new TotaledFitting(fitting, 1);

				var max_angle_tolerance = fitting.Angle + 1;
				var min_angle_tolerance = fitting.Angle - 1;

				if(ret_fc.Any(x => (x.Fitting.Angle >= min_angle_tolerance && x.Fitting.Angle <= max_angle_tolerance) &&
				x.Fitting.Diameter.Equals(fitting.Diameter) &&
				x.Fitting.Type.Equals(fitting.Type)))
				{
					var old_entry = ret_fc.Find(
						x => (x.Fitting.Angle >= min_angle_tolerance &&
                              x.Fitting.Angle <= max_angle_tolerance) &&
						x.Fitting.Diameter.Equals(fitting.Diameter) &&
						x.Fitting.Type.Equals(fitting.Type));

					ret_fc.Remove(old_entry);
					var new_cnt = old_entry.Count + 1;
					ret_fc.Add(new TotaledFitting(fitting, new_cnt));
				}
				else
				{
					ret_fc.Add(entry);
				}
			}

			return ret_fc;
		}

		private static IEnumerable<TotaledFitting> GetFittingCounts(ModelInfo info, IEnumerable<int> ids) {

            var fittings = new List<Fitting>();

            foreach(var id in ids) {
				var fitting = FittingFromConduitId(info, id);
                fittings.Add(fitting);
            }

			return GetFittingCounts(fittings);
		}
	}
}