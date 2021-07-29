using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Custom.Parameters;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Tools;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Custom.Unistrut
{
	[DataContract]
	public class UnistrutFrame : ParamProbe
	{
		[DataMember]
		public List<Unistrut> Unistrut { get; private set; }

		public static string UnistrutFamilyName { get; } = "PreFab Strut Frame 20 teir.rfa";
		public static string UnistrutFamilyNameNoExt { get; } = UnistrutFamilyName.Split('.').First();

		public static Dictionary<string, string> ParameterNames { get; } = new Dictionary<string, string>() {
			{"Unistrut Count", "Strut Needed"},
			{"Vertical Strut Height", "Height Of Vertical Strut"},
			{"Horizontal Strut Width", "Width of Frame"},
			{"Base Plates", "Floor Base Plates"},
		};

		public static UnistrutFrame CreateFrame(ModelInfo info, Element frame)
		{
			UnistrutFrame ret_frame = new UnistrutFrame(frame);

			bool c = ParameterNames.TryGetValue("Unistrut Count", out string p_cnt);
			bool v = ParameterNames.TryGetValue("Vertical Strut Height", out string p_vert);
			bool h = ParameterNames.TryGetValue("Horizontal Strut Width", out string p_horiz);
			bool p = ParameterNames.TryGetValue("Base Plates", out string p_plates);

			if(!c || !v || !h || !p) return ret_frame;

			int unistrut_cnt = frame.LookupParameter(p_cnt).AsInteger();
			double vert_len = frame.LookupParameter(p_vert).AsDouble();
			double horiz_len = frame.LookupParameter(p_horiz).AsDouble();
			bool base_plates = frame.LookupParameter(p_plates).AsInteger() == 1;

			string prefix = "Unistrut";
			string washer_size = "1/4\" x 1 1/4\"";
			string screw_size = "1/4\" x 20";
			string channel_nut_size = "1/4\" x 20";
			string anchor_size = "1/4\" x 20";

			for(var i = 0; i < unistrut_cnt; i++)
			{
				var us = new Unistrut(horiz_len);

				us.Hardware.ChannelNuts.Add(new ChannelNut(prefix, channel_nut_size));
				us.Hardware.ChannelNuts.Add(new ChannelNut(prefix, channel_nut_size));
				us.Hardware.MachineScrews.Add(new MachineScrew(prefix, screw_size));
				us.Hardware.MachineScrews.Add(new MachineScrew(prefix, screw_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.PlateFittings.Add(new PlateFitting(prefix, "T"));
				us.Hardware.PlateFittings.Add(new PlateFitting(prefix, "T"));
				ret_frame.Unistrut.Add(us);
			}

			for(var i = 0; i < 2; i++)
			{
				var us = new Unistrut(vert_len);

				us.Hardware.PlateFittings.Add(new PlateFitting(prefix, "L"));
				us.Hardware.PlateFittings.Add(new PlateFitting(prefix, "L"));
				us.Hardware.PlateFittings.Add(new PlateFitting(prefix, "L"));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));
				us.Hardware.Washers.Add(new Washer(prefix, washer_size));

				// add the feet
				if(base_plates)
				{
					us.Hardware.PostBases.Add(new PostBase(prefix));
					us.Hardware.Anchors.Add(new MasonryAnchor(prefix, anchor_size));
					us.Hardware.Anchors.Add(new MasonryAnchor(prefix, anchor_size));
					us.Hardware.Anchors.Add(new MasonryAnchor(prefix, anchor_size));
					us.Hardware.Anchors.Add(new MasonryAnchor(prefix, anchor_size));
				}


				if(i == 1)
				{
					us.Hardware.ConduitStraps.AddRange(ret_frame.GetFrameConduitStraps(info));

					int panel_cnt = ret_frame.GetPanelboardCount(info);
					for(var j = 0; j < panel_cnt; j++)
					{
						us.Hardware.ChannelNuts.Add(new ChannelNut(prefix, channel_nut_size));
						us.Hardware.ChannelNuts.Add(new ChannelNut(prefix, channel_nut_size));
						us.Hardware.ChannelNuts.Add(new ChannelNut(prefix, channel_nut_size));
						us.Hardware.ChannelNuts.Add(new ChannelNut(prefix, channel_nut_size));
						us.Hardware.MachineScrews.Add(new MachineScrew(prefix, screw_size));
						us.Hardware.MachineScrews.Add(new MachineScrew(prefix, screw_size));
						us.Hardware.MachineScrews.Add(new MachineScrew(prefix, screw_size));
						us.Hardware.MachineScrews.Add(new MachineScrew(prefix, screw_size));
						us.Hardware.Washers.Add(new Washer(prefix, washer_size));
						us.Hardware.Washers.Add(new Washer(prefix, washer_size));
						us.Hardware.Washers.Add(new Washer(prefix, washer_size));
						us.Hardware.Washers.Add(new Washer(prefix, washer_size));
					}
				}

				ret_frame.Unistrut.Add(us);
			}

			return ret_frame;
		}

		private UnistrutFrame(Element frame) : base(frame.Id.IntegerValue)
		{
			Unistrut = new List<Unistrut>();
		}

		private IEnumerable<UnistrutConduitStrap> GetFrameConduitStraps(ModelInfo info)
		{
			Element frame = info.DOC.GetElement(new ElementId(this.Element_Id));
			XYZ start_pt = (frame.Location as LocationPoint).Point;

			bool h = ParameterNames.TryGetValue("Horizontal Strut Width", out string p_horiz);
			if(!h) return new List<UnistrutConduitStrap>();

			var radius = frame.LookupParameter(p_horiz).AsDouble() / 2;
			var close_conduits = RevitRaycast.CastSphere(info, start_pt, radius, BuiltInCategory.OST_Conduit);

			List<UnistrutConduitStrap> straps = new List<UnistrutConduitStrap>();
			foreach(var c in close_conduits.Select(x => info.DOC.GetElement(x)))
			{
				straps.Add(new UnistrutConduitStrap(c));
			}

			return straps;
		}

		private int GetPanelboardCount(ModelInfo info)
		{
			Element frame = info.DOC.GetElement(new ElementId(this.Element_Id));
			XYZ start_pt = (frame.Location as LocationPoint).Point;

			bool h = ParameterNames.TryGetValue("Horizontal Strut Width", out string p_horiz);
			if(!h) return 0;

			var radius = frame.LookupParameter(p_horiz).AsDouble() / 2;
			var panels = RevitRaycast.CastSphere(info, start_pt, radius, BuiltInCategory.OST_ElectricalEquipment);

			int cnt = 0;
			foreach(var p in panels)
			{
				var el = info.DOC.GetElement(p);
				var pname = el.LookupParameter("Panel Name");
				if(p == null || string.IsNullOrWhiteSpace(pname.AsString()) || !pname.HasValue) continue;
				cnt++;
			}

			return cnt;
		}
	}
}