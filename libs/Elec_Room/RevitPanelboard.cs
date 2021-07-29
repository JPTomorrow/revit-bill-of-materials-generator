using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Custom.Parameters;
using JPMorrow.Revit.Custom.WallInspection;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Panels
{
	[DataContract]
	public class Panelboard : ParamProbe
	{
		[DataMember]
		public string Name { get; private set; }
		[DataMember]
		public string PanelName { get; private set; }
		[DataMember]
		public double PadLength { get; private set; }
		[DataMember]
		public double PadWidth { get; private set; }
		[DataMember]
		public double PadDepth { get; private set; }
		[DataMember]
		public string PadDimensions { get; private set; }
		[DataMember]
		public string Amperage { get; private set; }
		[DataMember]
		public PanelboardHardware Hardware { get; private set; }

		public bool HasPad { get {
			if(PadWidth == 0 || PadLength == 0 || PadDepth == 0)
				return false;
			return true;
		} }

		private Panelboard(
			ModelInfo info, int id, string amperage,
			string anchor_size, double[] pad_dimensions, string override_wall_type = null) : base(id)
		{

			Name = "Panelboard Can";

			var panelboard = info.DOC.GetElement(new ElementId(id));
			var pname = panelboard.LookupParameter("Panel Name").AsString();
			PanelName = pname;

			// pad dimensions are W x L x D
			if(pad_dimensions.Length != 3) {
				PadWidth = 0;
				PadLength = 0;
				PadDepth = 0;
			}
			else {
				PadWidth = pad_dimensions[0];
				PadLength = pad_dimensions[1];
				PadDepth = pad_dimensions[2];
			}


			Amperage = amperage;
			Hardware = new PanelboardHardware(info, this, anchor_size, override_wall_type);
		}

		/// <summary>
		/// Panelboard instance factory
		/// </summary>
		public static Panelboard CreatePanelboard(ModelInfo info, Element panel, string anchor_size, string override_wall_type = null)
		{
			string amperage = GetPanelAmperage(panel);
			if(amperage == null) return null;

			/*
			var pwidth = panel.LookupParameter("Switch Width").AsDouble();
			var plength = panel.LookupParameter("Switch Depth").AsDouble();

			UnitFormatUtils.TryParse(info.DOC.GetUnits(), UnitType.UT_Length, "6\"", out double standard_depth);
			*/

			List<double> pad_dims = new List<double>();

			var pb = new Panelboard(
				info, panel.Id.IntegerValue, amperage,
				anchor_size, pad_dims.ToArray(), override_wall_type);

			return pb;
		}

		public static Dictionary<string, string> ParameterNames { get; } = new Dictionary<string, string>() {
			{ "LS Nipple Length", 				"Left Side Nipple Length" },
			{ "RS Nipple Length", 				"Right Side Nipple Length" },
			{ "TL Nipple Offset", 				"Top Left Nipple Offset" },
			{ "Top Nipple Length", 				"Top Left Nipple Length" },
			{ "Top Right Nipple Offset", 		"Top Right Nipple Offset" },
			{ "Bottom Left Nipple Offset", 		"Bottom Left Nipple Offset" },
			{ "Bottom Right Nipple Offset", 	"Bottom Right Nipple Offset" },
			{ "Offset Mount", 					"Offset Mount" },
			{ "Side Nipple Offset", 			"Side Nipple Offset" },
			{ "Amperage", 						"Mains" },
			{ "Name", 							"Panel Name" },
		};

		public static string PanelboardName { get; } = "Panelboard.rfa";
		public static string PanelboardNameNoExt { get; } = PanelboardName.Split('.').First();

		private static string GetPanelAmperage(Element panel_element)
		{
			bool s = ParameterNames.TryGetValue("Amperage", out string amp_param_name);

			if(!s || amp_param_name.Equals(string.Empty))
			{
				debugger.show(err:"Panel does not have a valid amperage set on the mains.", header:"Panel Amperage");
				return null;
			}

			var amperage = panel_element.LookupParameter(amp_param_name).AsValueString();
			amperage = Regex.Match(amperage, @"\d+").Value;
			return amperage;
		}
	}

	/// <summary>
	/// Hardware Collection for a single panelboard
	/// </summary>
	[DataContract]
	public class PanelboardHardware : HardwareCollection
	{
		public int HardwareCount {  get => ToggleBolts.Count + Washers.Count + SheetMetalScrews.Count + Anchors.Count; }

		public PanelboardHardware(
			ModelInfo info, Panelboard pb, string anchor_size, string override_wall_type = null)
		{
			string prefix = "Panelboard";
			string tb_size = "1/4\" x 4\"";
			string washer_size = anchor_size + " x 1\"";
			string tec_screw_size = "#8 x 1/2\"";

			if(override_wall_type == null)
			{
				// get wall type and generate fasteners
				var u_el = info.DOC.GetElement(new ElementId(pb.Element_Id));
				Wall wall = WallInspector.GetHostedWall(u_el);

				Document l_doc = null;
				wall = wall ?? WallInspector.GetRevitLinkHostedWall(info, u_el, out l_doc);
				l_doc = l_doc ?? info.DOC;

				if(wall == null)
				{
					info.SEL.SetElementIds(new ElementId[] { new ElementId(pb.Element_Id) });
					throw new Exception("Unistrut is not hosted to a wall. The Unistrut in question has been selected in revit.");
				}

				WallInfo wi = new WallInfo(wall, l_doc);
				var custom_wall_type = wi.DerivedWallType;

				switch(custom_wall_type)
				{
					case CustomWallType.Default:
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					case CustomWallType.BlockWall:
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					case CustomWallType.DryWallMetalStud:
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					case CustomWallType.SolidConcrete:
						Anchors.Add(new MasonryAnchor(prefix, anchor_size));
						Anchors.Add(new MasonryAnchor(prefix, anchor_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					default:
						throw new Exception("Default case reached on wall type selection.");
				}
			}
			else
			{
				var custom_wall_type = WallInfo.WallTypes.Find(x => x.WallName.Equals(override_wall_type)).WallType;

				switch(custom_wall_type)
				{
					case CustomWallType.Default:
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					case CustomWallType.BlockWall:
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						ToggleBolts.Add(new ToggleBolt(prefix, tb_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					case CustomWallType.DryWallMetalStud:
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew(prefix, tec_screw_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));
						break;
					case CustomWallType.SolidConcrete:
						Anchors.Add(new MasonryAnchor(prefix, anchor_size));
						Anchors.Add(new MasonryAnchor(prefix, anchor_size));
						Washers.Add(new Washer(prefix, washer_size));
						Washers.Add(new Washer(prefix, washer_size));

						break;
					default:
						throw new Exception("Default case reached on wall type selection.");
				}
			}
		}

		public override string ToString()
		{
			string o = "Panelboard Hardware:\n";
			ToggleBolts.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Washers.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			SheetMetalScrews.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Anchors.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			return o;
		}
	}

	public static class PanelboardExportExt
	{
		public static PanelboardTotal FlattenPanelboard(this IEnumerable<Panelboard> source)
		{
			PanelboardTotal t = new PanelboardTotal();

			foreach(var board in source)
			{

			int index = t.Panelboards
				.FindIndex(ind => ind.Board.PanelName.Equals(board.PanelName) && ind.Board.Amperage.Equals(board.Amperage));

				if (index > -1)
				{
					var existing_cnt = t.Panelboards[index].Count;
					var new_entry = new SinglePanelboardTotal(board, existing_cnt + 1);
					t.Panelboards[index] = new_entry;
				}
				else
					t.Panelboards.Add(new SinglePanelboardTotal(board, 1));

				foreach(var tb in board.Hardware.ToggleBolts)
				{
					int tb_idx = t.ToggleBolts.FindIndex(x => x.Bolt.Size.Equals(tb.Size));
					if (tb_idx > -1)
					{
						var existing_cnt = t.ToggleBolts[tb_idx].Count;
						var new_entry = new ToggleBoltTotal(tb, existing_cnt + 1);
						t.ToggleBolts[tb_idx] = new_entry;
					}
					else
						t.ToggleBolts.Add(new ToggleBoltTotal(tb, 1));
				}

				foreach(var w in board.Hardware.Washers)
				{
					int w_idx = t.Washers.FindIndex(x => x.Washer.Size.Equals(w.Size));
					if (w_idx > -1)
					{
						var existing_cnt = t.Washers[w_idx].Count;
						var new_entry = new WasherTotal(w, existing_cnt + 1);
						t.Washers[w_idx] = new_entry;
					}
					else
						t.Washers.Add(new WasherTotal(w, 1));
				}

				foreach(var s in board.Hardware.SheetMetalScrews)
				{
					int s_idx = t.SheetMetalScrews.FindIndex(x => x.Screw.Size.Equals(s.Size));
					if (s_idx > -1)
					{
						var existing_cnt = t.SheetMetalScrews[s_idx].Count;
						var new_entry = new SheetMetalScrewTotal(s, existing_cnt + 1);
						t.SheetMetalScrews[s_idx] = new_entry;
					}
					else
						t.SheetMetalScrews.Add(new SheetMetalScrewTotal(s, 1));
				}

				foreach(var s in board.Hardware.Anchors)
				{
					int s_idx = t.Anchors.FindIndex(x => x.Anchor.Size.Equals(s.Size));
					if (s_idx > -1)
					{
						var existing_cnt = t.Anchors[s_idx].Count;
						var new_entry = new AnchorTotal(s, existing_cnt + 1);
						t.Anchors[s_idx] = new_entry;
					}
					else
						t.Anchors.Add(new AnchorTotal(s, 1));
				}
			}

			return t;
		}
	}

	public class SinglePanelboardTotal {
		public Panelboard Board { get; set; }
		public int Count  { get; set; }

		public SinglePanelboardTotal(Panelboard board, int count) {
			Board = board;
			Count = count;
		}
	}

	/// <summary>
	/// Total up all of the ground bar hardware for BOM export
	/// </summary>
	public class PanelboardTotal : HardwareTotal
	{
		public List<SinglePanelboardTotal> Panelboards { get; }

		public PanelboardTotal()
		{
			Panelboards = new List<SinglePanelboardTotal>();
		}
	}

}