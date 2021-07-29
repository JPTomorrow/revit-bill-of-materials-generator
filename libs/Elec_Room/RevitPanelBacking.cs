using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Custom.Parameters;
using JPMorrow.Revit.Custom.WallInspection;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Text;

namespace JPMorrow.Revit.Panels
{
	[DataContract]
	public class PanelBacking : ParamProbe
	{
		[DataMember]
		public double Width { get; private set; }
		[DataMember]
		public double Height { get; private set; }
		[DataMember]
		public double Depth { get; private set; }
		[DataMember]
		public PanelBackingHardware Hardware { get; private set; }

		public static string Name { get; } = "Panel Backing";

		public static Dictionary<string, string> ParameterNames => new Dictionary<string, string>() {
			{ "Width", "Width" },
			{ "Height", "Height" },
			{ "Depth", "Depth" },
		};

		public static string PanelBackingName { get; } = "Panel Backing.rfa";
		public static string PanelBackingNameNoExt { get; } = PanelBackingName.Split('.').First();

		public static BuiltInCategory[] ViewCategories = new BuiltInCategory[] {
				BuiltInCategory.OST_Conduit,
				BuiltInCategory.OST_ElectricalFixtures,
				BuiltInCategory.OST_GenericModel,
				BuiltInCategory.OST_Walls,
				BuiltInCategory.OST_Ceilings,
				BuiltInCategory.OST_Floors,
				BuiltInCategory.OST_Roofs,
				BuiltInCategory.OST_Joist,
				BuiltInCategory.OST_StructuralFraming,
				BuiltInCategory.OST_RvtLinks,
		};

		public PanelBacking(
			ModelInfo info, Element pb, string anchor_size,
			string override_wall_type = null) : base(pb.Id.IntegerValue)
		{
			Width = GetDblParam(info.DOC, "Width", ParameterNames);
			Height = GetDblParam(info.DOC, "Height", ParameterNames);
			Depth = GetDblParam(info.DOC, "Depth", ParameterNames);

			Hardware = new PanelBackingHardware(info, this, anchor_size, override_wall_type);
		}

		/// <summary>
		/// Get a string of the dimensions of the panel backing
		/// </summary>
		public string GetDimensions(ModelInfo info) {
			
			string f(double val) => RMeasure.LengthFromDbl(info.DOC, val);
			return string.Format("{0} x {1} x {2}", f(Width), f(Height), f(Depth));
		}

		public string ToString(ModelInfo info)
		{
			return string.Format("Name: {0}\nDimensions: {1}\n\n",
				Name, GetDimensions(info)
			);
		}
	}

	[DataContract]
	public class PanelBackingHardware : HardwareCollection
	{
		public int HardwareCount {  get => ToggleBolts.Count + Washers.Count + SheetMetalScrews.Count + Anchors.Count;}

		public PanelBackingHardware(ModelInfo info, PanelBacking pb, string anchor_size, string override_wall_type = null)
		{
			string prefix = "Panel Backing";
			string tb_size = "1/4\" x 4\"";
			string washer_size = anchor_size + " x 1 1/4\"";
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
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						break;
					case CustomWallType.BlockWall:
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						ToggleBolts.Add(new ToggleBolt("Panel Backing", tb_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						break;
					case CustomWallType.DryWallMetalStud:
						SheetMetalScrews.Add(new SheetMetalScrew("Panel Backing", tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew("Panel Backing", tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew("Panel Backing", tec_screw_size));
						SheetMetalScrews.Add(new SheetMetalScrew("Panel Backing", tec_screw_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						break;
					case CustomWallType.SolidConcrete:
						Anchors.Add(new MasonryAnchor("Panel Backing", anchor_size));
						Anchors.Add(new MasonryAnchor("Panel Backing", anchor_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						Washers.Add(new Washer("Panel Backing", washer_size));
						break;
					default:
						throw new Exception("Default case reached on wall type selection.");
				}
			}
		}

		public override string ToString()
		{
			string o = "Ground Bar Hardware:\n";
			ToggleBolts.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Washers.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			SheetMetalScrews.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Anchors.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			return o;
		}
	}

	/// <summary>
	/// Total up all of the Panel Backing hardware for BOM export
	/// </summary>
	public class PanelBackingTotal : HardwareTotal
	{
		public double PanelBackingFootage { get; set; } = 0.0;

		public PanelBackingTotal()
		{

		}
	}

	public static class GrdBarExportExt
	{
		public static PanelBackingTotal FlattenPanelBacking(this IEnumerable<PanelBacking> source)
		{
			PanelBackingTotal t = new PanelBackingTotal();

			foreach(var pb in source)
			{
				t.PanelBackingFootage += pb.Width;

				foreach(var tb in pb.Hardware.ToggleBolts)
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

				foreach(var w in pb.Hardware.Washers)
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

				foreach(var s in pb.Hardware.SheetMetalScrews)
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

				foreach(var s in pb.Hardware.Anchors)
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
}