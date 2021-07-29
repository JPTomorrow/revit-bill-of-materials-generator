///
/// Totals up wire for use with the BOMExporter
/// Author: Justin Morrow
///

using System;
using System.Collections.Generic;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;

namespace JPMorrow.Revit.Wires {
   public class TotaledWire {
		public string Size { get; set; }
		public string Color { get; set; }
		public double Length { get; set; }
		public WireMaterialType MaterialType { get; set; }

		public string MaterialTypeText => Enum.GetName(typeof(WireMaterialType), MaterialType);

		public TotaledWire(string size, string color, double length, WireMaterialType type) {
			Size = size;
			Color = color;
			Length = length;
			MaterialType = type;
		}

	}

	/// <summary>
	/// Container for combining wire into single entries for BOM output
	/// </summary>
	public class WireTotal
	{
		public List<TotaledWire> Wires { get; private set; }

		public bool IsEmpty { get => !Wires.Any(); }

		public WireTotal()
		{
			Wires = new List<TotaledWire>();
		}

		private void OrderWire() {
			Wires = Wires.OrderBy(x => x.Size).ThenBy(y => y.Color).ToList();
		}

		public static WireTotal GetTotaledWire(MasterDataPackage package, WireType type) {
			
			WireTotal t = new WireTotal();
			t.PushWire(package.Cris, package.WireManager, type);
			t.OrderWire();
			return t;
		}

		private void PushWire(IEnumerable<ConduitRunInfo> cris, WireManager wm, WireType type)
		{
			foreach(var cri in cris) {
				var wires = wm.GetWires(cri.WireIds);
				if(wires.Any(x => x.IsNoWireExport(type))) continue;

				foreach(var w in wires)
				{
					if(w.WireType != type) continue;

					int index = Wires
						.FindIndex(ind => ind.Size.Equals(w.Size) && ind.Color.Equals(w.Color) && ind.MaterialType == w.WireMaterialType);

					if (index > -1)
					{
						var existing_len = Wires[index].Length;
						var new_entry = new TotaledWire(w.Size, w.Color, existing_len + cri.Length, w.WireMaterialType);
						Wires[index] = new_entry;
					}
					else
						Wires.Add(new TotaledWire(w.Size, w.Color, cri.Length, w.WireMaterialType));
				}
			}
		}
	}
}