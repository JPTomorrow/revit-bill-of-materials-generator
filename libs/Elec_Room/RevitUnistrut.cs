using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Custom.Parameters;
using JPMorrow.Revit.Custom.WallInspection;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Tools;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Custom.Unistrut
{
	[DataContract]
	public class Unistrut : ParamProbe
	{
		[DataMember]
		public string Name { get; private set; }
		[DataMember]
		public double Length { get; private set; }
		[DataMember]
		public string Size { get; private set; }
		[DataMember]
		public UnistrutHardware Hardware { get; private set; }

		public static string[] UnistrutSizes { get; } = new string[] {
			"7/8\"", "1 5/8\""
		};

		public static string UnistrutFamilyName { get; } = "Single Unistrut.rfa";
		public static string UnistrutFamilyNameNoExt { get; } = UnistrutFamilyName.Split('.').First();

		public static Dictionary<string, string> ParameterNames { get; } = new Dictionary<string, string>() {
			{"Length", "Strut Length"},
			{"Size", "Strut Size"},
		};

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

		public Unistrut( double length, string name = "Unistrut") : base(-1)
		{
			Length = length;
			Name = name;
			Size = UnistrutSizes[1];
			Hardware = new UnistrutHardware();
		}

		public Unistrut(
			ModelInfo info, Element unistrut, string anchor_size,
			View3D hardware_view, string override_wall_type = null,
			string name = "Unistrut")
			: base(unistrut.Id.IntegerValue)
		{
			Size = unistrut.Name.Contains(UnistrutSizes[1]) ? UnistrutSizes[1] : UnistrutSizes[0];

			var length_str = this.GetValStringParam(info.DOC, "Length", ParameterNames);
			var length_val = RMeasure.LengthDbl(info.DOC, length_str);

			if(length_val == -1)
				throw new Exception("Invalid Unistrut Constructor (double Length)");

			Length = length_val;
			Name = name;
			Hardware = new UnistrutHardware(info, this, anchor_size, hardware_view, override_wall_type);
		}

		/// <summary>
		/// Search a piece of unistrut for any attached conduit rack
		/// </summary>
		public IEnumerable<UnistrutConduitStrap> SearchConduitStraps(
			ModelInfo info, View3D view, ref List<int> ex_ids)
		{
			Element el = info.DOC.GetElement(new ElementId(this.Element_Id));
			XYZ start_pt = (el.Location as LocationPoint).Point;

			var td_temp = new XYZ[] {
				RGeo.PrimitiveDirection.XLeft,
				RGeo.PrimitiveDirection.XRight,
				RGeo.PrimitiveDirection.YLeft,
				RGeo.PrimitiveDirection.YRight,
				RGeo.PrimitiveDirection.Up,
				RGeo.PrimitiveDirection.Down,
			};

			var try_directions = new Stack<XYZ>(td_temp);
			var search_len = RMeasure.LengthDbl(info.DOC, "2'");

			XYZ start_direction = null;
			RRay ray = new RRay();
			while(try_directions.Any())
			{
				start_direction = try_directions.Pop();
				ray = RevitRaycast.Cast(info, view, ClashCategories.ToList(), start_pt, start_direction, search_len, ex_ids.Select(x => new ElementId(x)).ToList());

				bool s_test = ray.GetNearestCollision(out RRayCollision n_test);
				if(s_test) break;
			}

			bool s = ray.GetNearestCollision(out RRayCollision near);
			if(!s) return new List<UnistrutConduitStrap>();

			Curve orig_curve = Line.CreateBound(start_pt, near.point);
			XYZ up = RGeo.PrimitiveDirection.Up;
			XYZ right = (orig_curve as Line).Direction.Normalize().CrossProduct(up.Normalize());
			XYZ neg_right = -right;

			// get projected point on other conduit
			var conduit = info.DOC.GetElement(near.other_id);
			Curve c_curve = (conduit.Location as LocationCurve).Curve;
			var corr_near = c_curve.Project(near.point).XYZPoint;

			var us_length = this.Length / 2.0;

			RRay adjacent_conduit_ray = RevitRaycast.Cast(info, view, ClashCategories.ToList(), corr_near, right, us_length);
			var strap_ids = new List<int>();

			if(adjacent_conduit_ray.collisions.Any())
			{
				foreach(var c in adjacent_conduit_ray.collisions)
				{
					var id = c.other_id;
					if(!ex_ids.Any(x => x == id.IntegerValue))
					{
						ex_ids.Add(id.IntegerValue);
						strap_ids.Add(id.IntegerValue);
					}
				}
			}

			adjacent_conduit_ray = RevitRaycast.Cast(info, view, ClashCategories.ToList(), corr_near, neg_right, us_length);

			if(adjacent_conduit_ray.collisions.Any())
			{
				foreach(var c in adjacent_conduit_ray.collisions)
				{
					var id = c.other_id;
					if(!ex_ids.Any(x => x == id.IntegerValue))
					{
						ex_ids.Add(id.IntegerValue);
						strap_ids.Add(id.IntegerValue);
					}

				}
			}

			List<UnistrutConduitStrap> straps = new List<UnistrutConduitStrap>();
			foreach(var id in strap_ids)
			{
				Element c = info.DOC.GetElement(new ElementId(id));
				straps.Add(new UnistrutConduitStrap(c));
			}
			return straps;
		}

		public override string ToString()
		{
			string o = Name + "\n";
			o += "Size: " + Size + "\n";
			o += Hardware.ToString() + "\n";
			return o;

		}
	}

	/// <summary>
	/// Class representing all of the fasteners in a single piece of unistrut
	/// </summary>
	[DataContract]
	public class UnistrutHardware : HardwareCollection
	{
		[DataMember]
		public List<UnistrutConduitStrap> ConduitStraps { get; private set; }

		public int FastenerCount { get => ConduitStraps.Count + ToggleBolts.Count + Washers.Count + SheetMetalScrews.Count + Anchors.Count + ChannelNuts.Count + PlateFittings.Count + PostBases.Count + MachineScrews.Count; }

		public UnistrutHardware()
		{
			ConduitStraps = new List<UnistrutConduitStrap>();
		}

		public UnistrutHardware(
			ModelInfo info, Unistrut unistrut, string anchor_size,
			View3D hardware_view, string override_wall_type = null)
		{
			string prefix = "Unistrut";
			string tb_size = "1/4\" x 4\"";
			string washer_size = anchor_size + " x 1 1/4\"";
			string tec_screw_size = "#8 x 1/2\"";

			if(override_wall_type == null)
			{
				List<int> ex_ids = new List<int>();
				ConduitStraps = unistrut.SearchConduitStraps(info, hardware_view, ref ex_ids).ToList();

				// get wall type and generate fasteners
				var u_el = info.DOC.GetElement(new ElementId(unistrut.Element_Id));
				Wall wall = WallInspector.GetHostedWall(u_el);

				Document l_doc = null;
				wall = wall ?? WallInspector.GetRevitLinkHostedWall(info, u_el, out l_doc);
				l_doc = l_doc ?? info.DOC;

				if(wall == null)
				{
					info.SEL.SetElementIds(new ElementId[] { new ElementId(unistrut.Element_Id) });
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
				List<int> ex_ids = new List<int>();
				ConduitStraps = unistrut.SearchConduitStraps(info, hardware_view, ref ex_ids).ToList();

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
			string o = "Unistrut Hardware:\n";
			ConduitStraps.ForEach(x => o += UnistrutConduitStrap.Name + " - " + x.Size + "\n");
			ToggleBolts.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Washers.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			SheetMetalScrews.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			Anchors.ForEach(x => o += x.Name + " - " + x.Size + "\n");
			return o;
		}
	}

	/// <summary>
	/// A Unistrut Conduit Strap
	/// </summary>
	[DataContract]
	public class UnistrutConduitStrap
	{
		public static string Name { get; } = "Unistrut Conduit Strap";

		[DataMember]
		public string Size { get; private set; }

		public UnistrutConduitStrap(string size)
		{
			Size = size;
		}

		public UnistrutConduitStrap(Element conduit)
		{
			if(conduit.Category.Name != "Conduits")
				throw new ArgumentException("Can not add conduiit strap for none conduit element");

			Size = conduit.LookupParameter("Diameter(Trade Size)").AsValueString();
		}
	}

	public static class UnistrutExportExt
	{
		public static UnistrutTotal FlattenUnistrut(this IEnumerable<Unistrut> source, ModelInfo info)
		{
			UnistrutTotal t = new UnistrutTotal();

			foreach(var us in source)
			{
				int index = t.Unistrut
					.FindIndex(ind => ind.Unistrut.Size.Equals(us.Size));

				if (index > -1)
				{
					var existing_len = t.Unistrut[index].Length;
					var new_entry = new SingleUnistrutTotal(us, existing_len + us.Length);
					t.Unistrut[index] = new_entry;
				}
				else
					t.Unistrut.Add(new SingleUnistrutTotal(us, us.Length));

				foreach(var cs in us.Hardware.ConduitStraps)
				{
					int cs_idx = t.ConduitStraps.FindIndex(x => x.Strap.Size.Equals(cs.Size));
					if (cs_idx > -1)
					{
						var existing_cnt = t.ConduitStraps[cs_idx].Count;
						var new_entry = new SingleUnistrutConduitStrapTotal(cs, existing_cnt + 1);
						t.ConduitStraps[cs_idx] = new_entry;
					}
					else
						t.ConduitStraps.Add(new SingleUnistrutConduitStrapTotal(cs, 1));
				}

				foreach(var tb in us.Hardware.ToggleBolts)
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

				foreach(var w in us.Hardware.Washers)
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

				foreach(var s in us.Hardware.SheetMetalScrews)
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

				foreach(var m in us.Hardware.MachineScrews)
				{
					int s_idx = t.MachineScrew.FindIndex(x => x.Screw.Size.Equals(m.Size));
					if (s_idx > -1)
					{
						var existing_cnt = t.MachineScrew[s_idx].Count;
						var new_entry = new MachineScrewTotal(m, existing_cnt + 1);
						t.MachineScrew[s_idx] = new_entry;
					}
					else
						t.MachineScrew.Add(new MachineScrewTotal(m, 1));
				}

				foreach(var c in us.Hardware.ChannelNuts)
				{
					int c_idx = t.ChannelNuts.FindIndex(x => x.Nut.Name.Equals(c.Name) && x.Nut.Size.Equals(c.Size));
					if (c_idx > -1)
					{
						var existing_cnt = t.ChannelNuts[c_idx].Count;
						var new_entry = new ChannelNutTotal(c, existing_cnt + 1);
						t.ChannelNuts[c_idx] = new_entry;
					}
					else
						t.ChannelNuts.Add(new ChannelNutTotal(c, 1));
				}

				foreach(var p in us.Hardware.PlateFittings)
				{
					int p_idx = t.PlateFittings.FindIndex(x => x.Fitting.Type.Equals(p.Type));
					if (p_idx > -1)
					{
						var existing_cnt = t.PlateFittings[p_idx].Count;
						var new_entry = new PlateFittingTotal(p, existing_cnt + 1);
						t.PlateFittings[p_idx] = new_entry;
					}
					else
						t.PlateFittings.Add(new PlateFittingTotal(p, 1));
				}

				foreach(var b in us.Hardware.PostBases)
				{
					int b_idx = t.PostBases.FindIndex(x => x.Base.Name.Equals(b.Name));
					if (b_idx > -1)
					{
						var existing_cnt = t.PostBases[b_idx].Count;
						var new_entry = new PostBaseTotal(b, existing_cnt + 1);
						t.PostBases[b_idx] = new_entry;
					}
					else
						t.PostBases.Add(new PostBaseTotal(b, 1));
				}
			}
			return t;
		}
	}

	public class SingleUnistrutTotal {
		public Unistrut Unistrut { get; set; }
		public double Length { get; set; }

		public SingleUnistrutTotal(Unistrut unistrut, double length) {
			Unistrut = unistrut;
			Length = length;
		}
	}

	public class SingleUnistrutConduitStrapTotal {
		public UnistrutConduitStrap Strap { get; set; }
		public int Count { get; set; }

		public SingleUnistrutConduitStrapTotal(UnistrutConduitStrap strap, int count) {
			Strap = strap;
			Count = count;
		}
	}

	/// <summary>
	/// Total up all of the unistrut hardware for BOM export
	/// </summary>
	public class UnistrutTotal : HardwareTotal
	{
		public List<SingleUnistrutTotal> Unistrut { get; }
		public List<SingleUnistrutConduitStrapTotal> ConduitStraps { get; }



		public UnistrutTotal()
		{
			Unistrut = new List<SingleUnistrutTotal>();
			ConduitStraps = new List<SingleUnistrutConduitStrapTotal>();
		}
	}
}