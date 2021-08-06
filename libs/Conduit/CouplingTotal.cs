using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Text;
using JPMorrow.Revit.Wires;

namespace JPMorrow.Revit.Couplings {

     public class TotaledCoupling {
			public string Type { get; set; }
			public string Diameter { get; set; }
			public int Count { get; set; }

			public TotaledCoupling(string type, string dia, int count) {
				Type = type;
				Diameter = dia;
				Count = count;
			}
		}

	/// <summary>
	/// Container for combining Coupling into single entries for BOM output
	/// </summary>
	public class CouplingTotal
	{
		public List<TotaledCoupling> Couplings { get; private set; }

		public bool IsEmpty { get => !Couplings.Any(); }

		public CouplingTotal()
		{
			Couplings = new List<TotaledCoupling>();
		}

		private void OrderCoupling() {
			Couplings = Couplings.OrderBy(x => x.Type).ThenBy(y => y.Diameter).ToList();
		}

		public static CouplingTotal GetTotaledCouplings(ModelInfo info, MasterDataPackage package, WireType type) {
			
			CouplingTotal t = new CouplingTotal();
			t.PushCouplings(info, package.GetSelectedConduitPackage().Cris, type);
			t.OrderCoupling();
			return t;
		}

		private void PushCouplings(ModelInfo info, IEnumerable<ConduitRunInfo> cris, WireType type) {

            List<TotaledCoupling> totaled_couplings = new List<TotaledCoupling>();

			foreach(var run in cris.Where(x => !x.ConduitMaterialType.ToLower().Contains("flex")).ToList())
			{
				var diameter = RMeasure.LengthFromDbl(info.DOC, run.Diameter);

                int index = totaled_couplings
                    .FindIndex(ind => ind.Type.Equals(run.ConduitMaterialType) && ind.Diameter.Equals(diameter));

                if (index > -1)
                {
                    var existing_cnt = totaled_couplings[index].Count;
                    var new_con = new TotaledCoupling(run.ConduitMaterialType, diameter, existing_cnt + (int)(run.Length / 10));
                    totaled_couplings[index] =  new_con;
                }
                else
                    totaled_couplings.Add(new TotaledCoupling(run.ConduitMaterialType, diameter, 2));
			}

			Couplings.AddRange(totaled_couplings);
		}
	}
}