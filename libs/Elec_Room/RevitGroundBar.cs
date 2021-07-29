using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Custom.Parameters;
using JPMorrow.Revit.Custom.WallInspection;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Tools;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Text;
using JPMorrow.Revit.Measurements;

namespace JPMorrow.Revit.Custom.GroundBar
{
	/// <summary>
	/// Represents a ground bar in revit
	/// </summary>
	[DataContract]
	public class GroundBar : ParamProbe
	{
		[DataMember]
		public string Name { get; private set; }
		[DataMember]
		public double Length { get; private set; }
		[DataMember]
		public double Thickness { get; private set; }
		[DataMember]
		public double Depth { get; private set; }
		[DataMember]
		public GrdBarHardware Hardware { get; private set; }

		public static Dictionary<string, string> ParameterNames { get; } = new Dictionary<string, string>() {
			{ "Width", "Width" },
			{ "Lugs", "Lug Count" },
			{ "Thickness", "Thickness" },
			{ "Depth", "Depth" }
		};

		public static string GrdBarFamilyName { get; } = "Ground Bar.rfa";
		public static string GrdBarFamilyNameNoExt { get; } = GrdBarFamilyName.Split('.').First();

		public static BuiltInCategory[] ClashCategories = new BuiltInCategory[] {
				BuiltInCategory.OST_Conduit
		};

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

		public GroundBar(
			ModelInfo info, Element grd_bar_el,  string anchor_size, string override_wall_type = null,
			 string name = "Ground Bar") : base(grd_bar_el.Id.IntegerValue)
		{
			Name = name;
			Length = this.GetDblParam(info.DOC, "Width", ParameterNames);
			Thickness = this.GetDblParam(info.DOC, "Thickness", ParameterNames);
			Depth = this.GetDblParam(info.DOC, "Depth", ParameterNames);

			Hardware = new GrdBarHardware(info, this, anchor_size, override_wall_type);
		}

		/// <summary>
		/// Search for conduit surrounding the ground bar and get appropriate lugs for the wire size
		/// </summary>
		/// <returns>Number of lugs created</returns>
		public void SearchLugs(
			ModelInfo info, WireManager man,
			List<ConduitRunInfo> cris, string override_lug_size = null)
		{
			var lug_cnt = this.GetIntParam(info.DOC, "Lugs", GroundBar.ParameterNames);

			if(override_lug_size != null)
			{
				for(var i = 0; i < lug_cnt; i++)
					Hardware.Lugs.Add(new GrdBarLug(override_lug_size));
				return;
			}

			Hardware.Lugs.Clear();
			var el = info.DOC.GetElement(new ElementId(this.Element_Id));
			XYZ start_pt = (el.Location as LocationPoint).Point;

			var close_conduits = RevitRaycast.CastSphere(info, start_pt, 1.0, BuiltInCategory.OST_Conduit);
			info.SEL.SetElementIds(close_conduits.ToList());

			foreach(var c in close_conduits)
			{
				int fidx = cris.FindIndex(x => x.ConduitIds.Contains(c.IntegerValue));

				if(fidx != -1)
				{
					var cri = cris[fidx];
					var wires = man.GetWires(cri.WireIds);
					var green_wires = wires
					.Where(x => x.Color.Equals(WireColor.Green) || x.Color.Equals(WireColor.Green_Yellow_Stripe));

					green_wires.ToList().ForEach(x => {
						Hardware.Lugs.Add(new GrdBarLug(GrdBarLug.GetLugSizeFromwWireSize(x.Size)));
					});
				}
			}

			return;
		}

		/// <summary>
		/// Get a string of the dimensions of the ground bar
		/// </summary>
		public string GetDimensions(ModelInfo info)
		{
			string f(double val) => RMeasure.LengthFromDbl(info.DOC, val);
			return string.Format("{0} x {1} x {2}", f(Length), f(Depth), f(Thickness));
		}

		/// <summary>
		/// Print a Ground bar
		/// </summary>
		public override string ToString()
		{
			string o = Name + "\n";
			o += Hardware.ToString() + "\n";
			return o;
		}
	}

	[DataContract]
	public class GrdBarHardware : HardwareCollection
	{
		[DataMember]
		public List<GrdBarLug> Lugs { get; private set; } = new List<GrdBarLug>();

		public int HardwareCount {  get => Lugs.Count + ToggleBolts.Count + Washers.Count + SheetMetalScrews.Count + Anchors.Count; }

		public GrdBarHardware(
			ModelInfo info, GroundBar bar, string anchor_size, string override_wall_type = null)
		{
			string prefix = "Ground Bar";
			string tb_size = "1/4\" x 4\"";
			string washer_size = anchor_size + " x 1 1/4\"";
			string tec_screw_size = "#8 x 1/2\"";

			//search for conduit to get wire from to size toggle bolts
			if(override_wall_type == null)
			{
				// get wall type and generate fasteners
				var u_el = info.DOC.GetElement(new ElementId(bar.Element_Id));
				Wall wall = WallInspector.GetHostedWall(u_el);

				Document l_doc = null;
				wall = wall ?? WallInspector.GetRevitLinkHostedWall(info, u_el, out l_doc);
				l_doc = l_doc ?? info.DOC;

				if(wall == null)
				{
					info.SEL.SetElementIds(new ElementId[] { new ElementId(bar.Element_Id) });
					throw new Exception("Ground bar is not hosted to a wall. The ground bar in question has been selected in revit.");
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
			string o = "Ground Bar Hardware:\n";
			Lugs.ForEach(x => o += GrdBarLug.Name + " - " + x.Size + "\n");
			ToggleBolts.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Washers.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			SheetMetalScrews.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Anchors.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			return o;
		}
	}

	/// <summary>
	/// A Ground Bar Lug
	/// </summary>
	[DataContract]
	public class GrdBarLug
	{
		public static string Name { get; } = "Ground Bar Lug";

		[DataMember]
		public string MaterialType { get; private set; }
		[DataMember]
		public string Size { get; private set; }

		public static IEnumerable<string> LugSizes { get => LugSizeIdxResolution.WireSizeToLugSize.Values.ToList().Distinct(); }

		public static string GetLugSizeFromwWireSize(string ws)
		{
			bool s = LugSizeIdxResolution.WireSizeToLugSize.TryGetValue(ws, out string val);
			if(!s) LugSizeIdxResolution.WireSizeToLugSize.TryGetValue("default", out val);
			return val;
		}

		public GrdBarLug(string size, string mat_type = "Copper")
		{
			MaterialType = mat_type;
			Size = GetLugSizeFromwWireSize(size);
		}

		private static class LugSizeIdxResolution
		{
			public static Dictionary<string, string> WireSizeToLugSize { get; set; } = new Dictionary<string, string>() {
				{ "default",  "#6" },
				{ "2000MCM"	, "2000MCM" },
				{ "1750MCM"	, "1750MCM" },
				{ "1500MCM"	, "1500MCM" },
				{ "1250MCM"	, "1250MCM" },
				{ "1000MCM"	, "1000MCM" },
				{ "900MCM"	, "900MCM" },
				{ "800MCM" 	, "800MCM" },
				{ "750MCM" 	, "750MCM" },
				{ "700MCM" 	, "700MCM" },
				{ "600MCM" 	, "600MCM" },
				{ "500MCM" 	, "500MCM" },
				{ "400MCM" 	, "400MCM" },
				{ "350MCM"	, "350MCM" },
				{ "300MCM" 	, "300MCM" },
				{ "250MCM" 	, "250MCM" },
				{ "#4/0" 	, "#4/0" },
				{ "#3/0" 	, "#3/0" },
				{ "#2/0" 	, "#2/0" },
				{ "#1/0" 	, "#1/0" },
				{ "#1" 		, "#1" },
				{ "#2" 		, "#2" },
				{ "#3"		, "#3" },
				{ "#4" 		, "#4" },
				{ "#6" 		, "#6" },
				{ "#8" 		, "#8" },
				{ "#10" 	, "#10" },
				{ "#12" 	, "#12" },
				{ "#14"		, "#14" },
			};
		}
	}

	public static class GrdBarExportExt
	{
		public static GrdBarTotal FlattenGroundBars(this IEnumerable<GroundBar> source, ModelInfo info)
		{
			GrdBarTotal t = new GrdBarTotal();

			foreach(var bar in source)
			{

			int index = t.GroundBars
				.FindIndex(ind => ind.Bar.GetDimensions(info).Equals(bar.GetDimensions(info)));

				if (index > -1)
				{
					var existing_cnt = t.GroundBars[index].Count;
					var new_entry = new SingleGrdBarTotal(bar, existing_cnt + 1);
					t.GroundBars[index] = new_entry;
				}
				else
					t.GroundBars.Add(new SingleGrdBarTotal(bar, 1));

				foreach(var l in bar.Hardware.Lugs)
				{
					int l_idx = t.Lugs.FindIndex(x => x.Lug.Size.Equals(l.Size));
					if (l_idx > -1)
					{
						var existing_cnt = t.Lugs[l_idx].Count;
						var new_entry = new GrdBarLugTotal(l, existing_cnt + 1);
						t.Lugs[l_idx] = new_entry;
					}
					else
						t.Lugs.Add(new GrdBarLugTotal(l, 1));
				}

				foreach(var tb in bar.Hardware.ToggleBolts)
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

				foreach(var w in bar.Hardware.Washers)
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

				foreach(var s in bar.Hardware.SheetMetalScrews)
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

				foreach(var s in bar.Hardware.Anchors)
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

	public class SingleGrdBarTotal {
		public GroundBar Bar { get; set; }
		public int Count { get; set; }

		public SingleGrdBarTotal(GroundBar bar, int count) {
			Bar = bar;
			Count = count;
		}
	}

	public class GrdBarLugTotal {
		public GrdBarLug Lug { get; set; }
		public int Count { get; set; }

		public GrdBarLugTotal(GrdBarLug lug, int count) {
			Lug = lug;
			Count = count;
		}
	}

	/// <summary>
	/// Total up all of the ground bar hardware for BOM export
	/// </summary>
	public class GrdBarTotal : HardwareTotal
	{
		public List<SingleGrdBarTotal> GroundBars { get; }
		public List<GrdBarLugTotal> Lugs { get; }

		public GrdBarTotal()
		{
			GroundBars = new List<SingleGrdBarTotal>();
			Lugs = new List<GrdBarLugTotal>();
		}
	}
} 