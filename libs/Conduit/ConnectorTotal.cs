using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Text;
using JPMorrow.Revit.Wires;

namespace JPMorrow.Revit.Connectors {

     public class TotaledConnector {
			public string Type { get; set; }
			public string Diameter { get; set; }
			public int Count { get; set; }

			public TotaledConnector(string type, string dia, int count) {
				Type = type;
				Diameter = dia;
				Count = count;
			}
		}

	/// <summary>
	/// Container for combining Connector into single entries for BOM output
	/// </summary>
	public class ConnectorTotal
	{
		public List<TotaledConnector> Connectors { get; private set; }

		public bool IsEmpty { get => !Connectors.Any(); }

		public ConnectorTotal()
		{
			Connectors = new List<TotaledConnector>();
		}

		private void OrderConnectors() {
			Connectors = Connectors.OrderBy(x => x.Type).ThenBy(y => y.Diameter).ToList();
		}

		public static ConnectorTotal GetTotaledConnectors(ModelInfo info, MasterDataPackage package, WireType type) {
			
			ConnectorTotal t = new ConnectorTotal();
			t.PushConnectors(info, package.Cris, type);
			t.OrderConnectors();
			return t;
		}

		private void PushConnectors(ModelInfo info, IEnumerable<ConduitRunInfo> cris, WireType type) {

            List<TotaledConnector> totaled_Connectors = new List<TotaledConnector>();

			foreach(var run in cris.Where(x => !x.ConduitMaterialType.ToLower().Contains("flex")).ToList()) {

				var diameter = RMeasure.LengthFromDbl(info.DOC, run.Diameter);
				
                int index = totaled_Connectors
                    .FindIndex(ind => ind.Type.Equals(run.ConduitMaterialType) && ind.Diameter.Equals(diameter));

                if (index > -1)
                {
                    var existing_cnt = totaled_Connectors[index].Count;
                    var new_con = new TotaledConnector(run.ConduitMaterialType, diameter, existing_cnt + 2);
                    totaled_Connectors[index] =  new_con;
                }
                else
                    totaled_Connectors.Add(new TotaledConnector(run.ConduitMaterialType, diameter, 2));
			}

			Connectors.AddRange(totaled_Connectors);
		}
	}
}