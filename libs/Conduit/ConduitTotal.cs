///
/// Totals up conduit for use with the BOMExporter
/// Author: Justin Morrow
///

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Text;
using JPMorrow.Revit.Wires;

namespace JPMorrow.Revit.ConduitRuns {
    public class TotaledConduit {
		public string Type { get; set; }
		public string Diameter { get; set; }
		public double Length { get; set; }

		public TotaledConduit(string type, string dia, double length) {
			Type = type;
			Diameter = dia;
			Length = length;
		}
	}

	public class ConduitTotal {
		public List<TotaledConduit> Conduit { get; private set; }

		public ConduitTotal() {
			Conduit = new List<TotaledConduit>();
		}

		public static ConduitTotal GetTotaledConduit(ModelInfo info, MasterDataPackage package, WireType type) {
			ConduitTotal t = new ConduitTotal();
            t.PushConduit(info, package, type);
			return t;
		}

		private void PushConduit(ModelInfo info, MasterDataPackage package, WireType type) {

			foreach(var run in package.Cris)
			{
                Wire[] wires = package.WireManager.GetWires(run.WireIds.ToArray()).ToArray();
                if(!wires.Any(x => x.WireType == type)) continue;

				var diameter = RMeasure.LengthFromDbl(info.DOC, run.Diameter);
				int index = Conduit.FindIndex(ind => ind.Type.Equals(run.ConduitMaterialType) && ind.Diameter.Equals(diameter));

				var length = run.Length;
				var rm = package.ElectricalRoomPack.GetMatchingConduit(run.WireIds);

				if(rm != null && length - rm.Length <= 0)
					length = 0;
				else {
					if(rm != null) length -= rm.Length;
				}

				if (index > -1)
				{
					var existing_len = Conduit[index].Length;
					length += existing_len;
					var new_pipe = new TotaledConduit(run.ConduitMaterialType, diameter, length);
					Conduit[index] = new_pipe;
				}
				else
					Conduit.Add(new TotaledConduit(run.ConduitMaterialType, diameter, length + package.WireMakeupLength));
			}
		}
	}
}