using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Loader;
using JPMorrow.Revit.Tools;
using JPMorrow.Revit.Transactions;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.Revit.RvtMiscUtil;
using JPMorrow.Revit.Text;
using JPMorrow.Revit.Measurements;

namespace JPMorrow.P3
{
	[DataContract]
	public struct CircuitWhip
	{
		[DataMember]
		private readonly int line_id;
		[DataMember]
		private readonly WhipType circuit_type;
		[DataMember]
		private readonly double line_length;

		public int Line_Id { get => line_id; }
		public WhipType Whip_Type { get => circuit_type; }
		public double Line_Length { get => line_length; }

		public CircuitWhip(CircuitWhip whip)
		{
			line_id = whip.line_id;
			circuit_type = whip.circuit_type;
			line_length = whip.line_length;
		}

		public CircuitWhip(int id, WhipType type, double length)
		{
			line_id = id;
			circuit_type = type;
			line_length = length;
		}
	}

	public enum WhipType
	{
		One = 1,
		Two = 2,
		Three = 3
	}

	[DataContract]
	public struct P3Box
	{
		[DataMember]
		private readonly int box_id;
		[DataMember]
		private readonly string volt;
		[DataMember]
		private readonly double bf;
		[DataMember]
		private readonly P3BoxConfig config;
		[DataMember]
		private readonly BracketType hbt;
		[DataMember]
		private readonly int conn_cnt;
		[DataMember]
		private readonly BoxConnectorInfo[] box_conn_infos;
		[DataMember]
		private readonly int one_cwc;
		[DataMember]
		private readonly int two_cwc;
		[DataMember]
		private readonly int three_cwc;
		[DataMember]
		private readonly CircuitWhip[] whips;
		[DataMember]
		private readonly double wire_len;


		public int Box_Id { get => box_id; }
		public string Voltage { get => volt; }
		public double Box_Fill { get => bf; }
		public string Box_Config { get => Enum.GetName(typeof(P3BoxConfig), config); }
		public BracketType Helecopter_Bracket_Type { get => hbt; }
		public int Connector_Count { get => conn_cnt; }
		public BoxConnectorInfo[] Connected_Conduit_Info { get => box_conn_infos; }
		public int One_Circuit_Whip_Count { get => one_cwc; }
		public int Two_Circuit_Whip_Count { get => two_cwc; }
		public int Three_Circuit_Whip_Count { get => three_cwc; }
		public CircuitWhip[] Circuit_Whips { get => whips; }
		public double Wire_Length { get => wire_len; }
		public bool Is_Valid { get => box_id >= 0; }


		public static string[] P3_Box_Family_Names {
			get => new string[2] {
				"P3 Box - 100x54.rfa",
				"P3 Box - 120x54.rfa"
		}; }

		public P3Box(
			int id, string voltage, double box_fill, P3BoxConfig box_config,
			BracketType helecopter_bracket_type, int connector_count,
			BoxConnectorInfo[] conn_conduit_info, int one_circuit_whip_count,
			int two_circuit_whip_count, int three_circuit_whip_count,
			CircuitWhip[] c_whips, double wire_length)
		{
			box_id = id;
			volt = voltage;
			bf = box_fill;
			config = box_config;
			hbt = helecopter_bracket_type;
			conn_cnt = connector_count;
			box_conn_infos = conn_conduit_info;
			one_cwc = one_circuit_whip_count;
			two_cwc = two_circuit_whip_count;
			three_cwc = three_circuit_whip_count;
			whips = c_whips;
			wire_len = wire_length;
		}

		public static P3Box ProcessBox(ModelInfo info, ElementId box_id, View3D hanger_view = null)
		{
			Element box = info.DOC.GetElement(box_id);

			//if not p3 box by family name
			if(!box.Name.Contains("P3 Box")) return new P3Box(-1, null, -1, P3BoxConfig.E, BracketType.CROSS, -1, null, -1, -1, -1, new CircuitWhip[0], 0);

			//connected conduit count
			int connected_cnt = 0;
			List<BoxConnectorInfo> ccis = new List<BoxConnectorInfo>();
			bool par = false;

			Connector[] conns = RvtUtil.GetNonSetConnectors(box, RvtUtil.GetConnectors);
			foreach(var conn in conns)
			{
				//check if elbows are paralel or perpendicular
				if(!conn.IsConnected) continue;
				if(conns.Any(x => x.IsConnected && x.CoordinateSystem.BasisZ.IsAlmostEqualTo(-conn.CoordinateSystem.BasisZ)))
				{
					par = true;
				}
				connected_cnt++;

				//add connectors
				Element connected_conduit = info.DOC.GetElement(GetNextConnectedElement(conn));
				ccis.Add(new BoxConnectorInfo(
					connected_conduit.Id.IntegerValue,
					connected_conduit.LookupParameter("Diameter(Trade Size)").AsDouble(),
					connected_conduit.LookupParameter("From").AsString(),
					connected_conduit.LookupParameter("To").AsString()));
			}

			//box config
			P3BoxConfigConverter pbcc = new P3BoxConfigConverter(connected_cnt, par);

			//voltage
			string voltage = box.LookupParameter("low_voltage_vis").AsInteger() == 1 ? "120V" : "480V";

			//helicopter bracket
			BracketType bracket_type = pbcc.Resolve_Bracket;

			//get curcuit whips
			int ocw = box.LookupParameter("1 Circuit Whips").AsInteger();
			int tcw = box.LookupParameter("2 Circuit Whips").AsInteger();
			int thcw = box.LookupParameter("3 Circuit Whips").AsInteger();

			//make box
			P3Box ret_box = new P3Box(
				box_id.IntegerValue, voltage, 0, pbcc.Resolve_Config,
				bracket_type, connected_cnt, ccis.ToArray(),
				ocw, tcw, thcw, new CircuitWhip[0], 0);

			return ret_box;
		}

		public static void PlaceWhips(
			ModelInfo info, P3Box p3_box,
			GraphicsStyle one_circ_line_style,
			GraphicsStyle two_circ_line_style,
			GraphicsStyle three_circ_line_style,
			Action<P3Box> refresh_box_action)
		{
			Element box_el = info.DOC.GetElement(new ElementId(p3_box.Box_Id));
			XYZ box_pt = (box_el.Location as LocationPoint).Point;

			List<XYZ> one_circ_pts = new List<XYZ>();
			List<XYZ> two_circ_pts = new List<XYZ>();
			List<XYZ> three_circ_pts = new List<XYZ>();

			void populate_whip_pts(int whip_count, List<XYZ> pts_list)
			{
				for(int i = 0; i < whip_count; i++)
				{
					Reference r = info.SEL.PickObject(ObjectType.Element, new SelFilter(info));
					XYZ device_pt = (info.DOC.GetElement(r.ElementId).Location as LocationPoint).Point;
					pts_list.Add(device_pt);
				}
			}

			debugger.show(header:"Place Whips", err:"Please select " + p3_box.One_Circuit_Whip_Count.ToString() + " one circuit whips.");
			populate_whip_pts(p3_box.One_Circuit_Whip_Count, one_circ_pts);

			debugger.show(header:"Place Whips", err:"Please select " + p3_box.Two_Circuit_Whip_Count.ToString() + " two circuit whips.");
			populate_whip_pts(p3_box.Two_Circuit_Whip_Count, two_circ_pts);

			debugger.show(header:"Place Whips", err:"Please select " + p3_box.Three_Circuit_Whip_Count.ToString() + " three circuit whips.");
			populate_whip_pts(p3_box.Three_Circuit_Whip_Count, three_circ_pts);

			//create sketch plane
			XYZ origin = XYZ.Zero;
			XYZ normal = new XYZ(0, 0, 1);
			Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

			CreateSketchPlane(info, geomPlane);

			List<DetailArc> one_circ_arcs = new List<DetailArc>();
			List<DetailArc> two_circ_arcs = new List<DetailArc>();
			List<DetailArc> three_circ_arcs = new List<DetailArc>();

			void make_one_whips()
			{
				if(info.DOC.ActiveView.SketchPlane.IsValidObject)
				{
					MakeWhips(info, one_circ_pts, box_pt, one_circ_arcs);
					TransactionTunnel.TakeNextTransaction();
				}
			}

			void make_two_whips()
			{
				if(info.DOC.ActiveView.SketchPlane.IsValidObject)
				{
					MakeWhips(info, two_circ_pts, box_pt, two_circ_arcs);
					TransactionTunnel.TakeNextTransaction();
				}
			}

			void make_three_whips()
			{
				if(info.DOC.ActiveView.SketchPlane.IsValidObject)
				{
					MakeWhips(info, three_circ_pts, box_pt, three_circ_arcs);
					TransactionTunnel.TakeNextTransaction();
				}
			}

			void color_one_whips()
			{
				if(one_circ_arcs.Count >= p3_box.One_Circuit_Whip_Count)
				{
					ChangeLineStyles(info, one_circ_line_style, one_circ_arcs);
					TransactionTunnel.TakeNextTransaction();
				}
			}

			void color_two_whips()
			{
				if(two_circ_arcs.Count >= p3_box.Two_Circuit_Whip_Count)
				{
					ChangeLineStyles(info, two_circ_line_style, two_circ_arcs);
					TransactionTunnel.TakeNextTransaction();
				}
			}

			void color_three_whips()
			{
				if(three_circ_arcs.Count >= p3_box.Three_Circuit_Whip_Count)
				{
					ChangeLineStyles(info, three_circ_line_style, three_circ_arcs);
					TransactionTunnel.TakeNextTransaction();
				}
			}

			void store_whips()
			{
				P3Box box_with_whips = StoreWhips(p3_box, one_circ_arcs, two_circ_arcs, three_circ_arcs);
				refresh_box_action(box_with_whips);
				TransactionTunnel.TakeNextTransaction();
			}

			TransactionTunnel.Register(make_one_whips);
			TransactionTunnel.Register(color_one_whips);

			TransactionTunnel.Register(make_two_whips);
			TransactionTunnel.Register(color_two_whips);

			TransactionTunnel.Register(make_three_whips);
			TransactionTunnel.Register(color_three_whips);

			TransactionTunnel.Register(store_whips);
		}

		private static void MakeWhips(ModelInfo info, List<XYZ> points, XYZ box_pt, List<DetailArc> arcs)
		{
			foreach(var pt in points)
			{
				XYZ b_pt = new XYZ(box_pt.X, box_pt.Y, 0);
				XYZ dv_pt = new XYZ(pt.X, pt.Y, 0);
				XYZ[] model_pts = new[] { b_pt, dv_pt };
				Curve line_curve = Line.CreateBound(model_pts[0], model_pts[1]) as Curve;
				double len = line_curve.Length / 2;

				//bridge points to mutate
				XYZ b1 = DerivePointBetween(b_pt, dv_pt, len);

				//create arc
				Arc arc = Arc.Create(b_pt, dv_pt, new XYZ(b1.X + 1, b1.Y + 1, 0));
				DetailArc da = info.DOC.Create.NewDetailCurve(info.DOC.ActiveView, arc) as DetailArc;
				arcs.Add(da);
			}
		}

		private static P3Box StoreWhips(
			P3Box box,
			List<DetailArc> one_whips, List<DetailArc> two_whips,
			List<DetailArc> three_whips)
		{
			List<CircuitWhip> whips = new List<CircuitWhip>();

			foreach(var line in one_whips)
			{
				CircuitWhip whip = new CircuitWhip(line.Id.IntegerValue, WhipType.One, line.GeometryCurve.Length);
				whips.Add(whip);
			}

			foreach(var line in two_whips)
			{
				CircuitWhip whip = new CircuitWhip(line.Id.IntegerValue, WhipType.Two, line.GeometryCurve.Length);
				whips.Add(whip);
			}

			foreach(var line in three_whips)
			{
				CircuitWhip whip = new CircuitWhip(line.Id.IntegerValue, WhipType.Three, line.GeometryCurve.Length);
				whips.Add(whip);
			}

			P3Box ret_box = new P3Box(box.Box_Id, box.Voltage, box.Box_Fill, (P3BoxConfig)Enum.Parse(typeof(P3BoxConfig), box.Box_Config), box.Helecopter_Bracket_Type, box.Connector_Count, box.Connected_Conduit_Info, box.One_Circuit_Whip_Count, box.Two_Circuit_Whip_Count, box.Three_Circuit_Whip_Count, whips.ToArray(), box.wire_len);

			return ret_box;
		}

		public static P3Box RemoveWhipsFromBox(P3Box box)
		{
			P3Box ret_box = new P3Box(box.Box_Id, box.Voltage, box.Box_Fill, (P3BoxConfig)Enum.Parse(typeof(P3BoxConfig), box.Box_Config), box.Helecopter_Bracket_Type, box.Connector_Count, box.Connected_Conduit_Info, box.One_Circuit_Whip_Count, box.Two_Circuit_Whip_Count, box.Three_Circuit_Whip_Count, new CircuitWhip[0], box.wire_len);

			return ret_box;
		}

		public static void PlaceBoxes(
			ModelInfo info, double placement_elevation,
			int number_to_place, int one_circuit_whips,
			int two_circuit_whips, int three_circuit_whips,
			string box_voltage, Action<string, bool, bool> write_line_func)
		{
			//get box symbol
			List<FamilySymbol> syms = new FilteredElementCollector(info.DOC)
			.OfClass(typeof(FamilySymbol))
			.Where(x => x.Name.Equals(box_voltage + " P3 Box"))
			.Cast<FamilySymbol>().ToList();
			FamilySymbol voltage_appropriate_box = syms.Where(x => x.FamilyName.Equals(P3_Box_Family_Names[0].Split('.').First())).First();

			//prompt user for placement
			debugger.show(
				header:"Place Boxes", 
				err:"Pick " + number_to_place + 
				" points in the floor plan to place boxes at. They will be placed at the elevation: " + 
				RMeasure.LengthFromDbl(info.DOC, placement_elevation));

			if(info.DOC.ActiveView.ViewType != ViewType.CeilingPlan)
			{
				debugger.show(err:"No cieling plan detected. please only try to place boxes in a cieling plan.");
				return;
			}

			//create sketch plane
			Plane plane = Plane.CreateByNormalAndOrigin(info.DOC.ActiveView.ViewDirection, info.UIDOC.Document.ActiveView.Origin);

			CreateSketchPlane(info, plane);

			void after_sketch_plane()
			{
				if(info.DOC.ActiveView.SketchPlane.IsValidObject)
				{
					Queue<XYZ> pts = new Queue<XYZ>();
					for(int i = 0; i < number_to_place; i++)
						pts.Enqueue(info.SEL.PickPoint());

					CreateBox(
						info, info.DOC.ActiveView,
						voltage_appropriate_box, box_voltage,  placement_elevation, pts,
						new[] { one_circuit_whips, two_circuit_whips, three_circuit_whips });

					TransactionTunnel.TakeNextTransaction();
				}
			}

			void after_box_creation()
			{
				if(handler_create_box.Event_Return_Ids.Count >= number_to_place)
				{
					List<P3Box> ret_boxes = new List<P3Box>();
					foreach(var id in handler_create_box.Event_Return_Ids)
					{
						P3Box box = ProcessBox(info, id);
						if(box.Is_Valid)
							ret_boxes.Add(box);
					}
					write_line_func("Placed " + ret_boxes.Count().ToString() + " P3 boxes.", true, false);
					handler_create_box.Event_Return_Ids.Clear();
					TransactionTunnel.TakeNextTransaction();
				}
			}

			//register transactions to queue
			TransactionTunnel.Register(after_sketch_plane);
			TransactionTunnel.Register(after_box_creation);

			return;
		}

		/// <summary>
		/// get the other element connected to this connector
		/// </summary>
		private static ElementId GetNextConnectedElement(Connector c)
		{
			if(!c.IsConnected)
				return c.Owner.Id;

			foreach(Connector c2 in c.AllRefs)
			{
				if(!c2.Origin.IsAlmostEqualTo(c.Origin)) continue;
				return c2.Owner.Id;
			}
			return c.Owner.Id;
		}

		/// <summary>
		/// derive a point between two points
		/// </summary>
		private static XYZ DerivePointBetween(XYZ start, XYZ end, double distance = 1)
		{
			double fi = Math.Atan2(end.Y - start.Y, end.X - start.X);
			// Your final point
			XYZ xyz = new XYZ(start.X + distance * Math.Cos(fi),
								start.Y + distance * Math.Sin(fi), end.Z);
			return xyz;
		}

		/// <summary>
		/// selection filter for pickobjects
		/// </summary>
		private class SelFilter : ISelectionFilter
		{
			private ModelInfo info { get; set; }

			public bool AllowElement(Element elem)
			{
				if(elem.Category.Name == "Lighting Fixtures")
					return true;
				return false;
			}

			public bool AllowReference(Reference reference, XYZ position)
			{
				Element el = info.DOC.GetElement(reference.ElementId);

				if(el.Category.Name == "Lighting Fixtures")
					return true;
				return false;
			}

			public SelFilter(ModelInfo i)
			{
				info = i;
			}
		}

		/// <summary>
		/// BOX CREATION
		/// </summary>
		private static BoxCreation handler_create_box = null;
		private static ExternalEvent exEvent_create_box = null;

		public static void BoxCreationSignUp()
		{
			handler_create_box = new BoxCreation();
			exEvent_create_box = ExternalEvent.Create(handler_create_box);
		}

		private static void CreateBox(ModelInfo info, View view, FamilySymbol box, string voltage, double elev, Queue<XYZ> points, int[] whip_counts)
		{
			handler_create_box.Info = info;
			handler_create_box.Points = points;
			handler_create_box.Box_To_Place = box;
			handler_create_box.Voltage = voltage;
			handler_create_box.View = view;
			handler_create_box.Whip_Counts = new int[whip_counts.Length];
			whip_counts.CopyTo(handler_create_box.Whip_Counts, 0);
			handler_create_box.Box_Elevation = elev;
			exEvent_create_box.Raise();
		}

		public class BoxCreation : IExternalEventHandler
		{
			public View View { get; set; }
			public double Box_Elevation { get; set; }
			public Queue<XYZ> Points { get; set; }
			public FamilySymbol Box_To_Place { get; set; }
			public string Voltage { get; set; }
			public ModelInfo Info { get; set; }
			public int[] Whip_Counts { get; set; }
			public List<ElementId> Event_Return_Ids { get; set; } = new List<ElementId>();

			public void Execute(UIApplication app)
			{
				using (Transaction tx = new Transaction(Info.DOC, "Place Box"))
				{
					tx.Start();

					while(Points.Count > 0)
					{
						//create sketch plane
						Plane plane = Plane.CreateByNormalAndOrigin(Info.DOC.ActiveView.ViewDirection, Info.UIDOC.Document.ActiveView.Origin);
						SketchPlane sp = SketchPlane.Create(Info.DOC, plane);
						Info.DOC.ActiveView.SketchPlane = sp;

						XYZ placement_pt = Points.Dequeue();

						//place box
						string level_str = View.LookupParameter("Associated Level").AsString();
						Level level = new FilteredElementCollector(Info.DOC).OfClass(typeof(Level)).Where(x => x.Name == level_str).First() as Level;

						if(!Box_To_Place.IsActive)
							FamilyLoader.ActivateSymbol(Box_To_Place);

						FamilyInstance fam = Info.DOC.Create.NewFamilyInstance(placement_pt, Box_To_Place, level, StructuralType.NonStructural);

						//change voltage type
						ElementId[] types =  fam.GetValidTypes().ToArray();
						ElementId  voltage_type = types.Where(x => Info.DOC.GetElement(x).Name.Contains(Voltage)).First();
						fam.ChangeTypeId(voltage_type);

						Event_Return_Ids.Add(fam.Id);
						fam.LookupParameter("Offset").Set(Box_Elevation);
						fam.LookupParameter("1 Circuit Whips").Set(Whip_Counts[0]);
						fam.LookupParameter("2 Circuit Whips").Set(Whip_Counts[1]);
						fam.LookupParameter("3 Circuit Whips").Set(Whip_Counts[2]);

					}
					tx.Commit();
				}
			}

			public string GetName()
			{
				return "Create Box";
			}
		}

		/// <summary>
		/// Sketchplane CREATION
		/// </summary>
		private static SketchPlaneCreation handler_create_plane = null;
		private static ExternalEvent exEvent_create_plane = null;

		public static void SketchPlaneCreationSignUp()
		{
			handler_create_plane = new SketchPlaneCreation();
			exEvent_create_plane = ExternalEvent.Create(handler_create_plane);
		}

		private static void CreateSketchPlane(ModelInfo info, Plane plane)
		{
			handler_create_plane.Info = info;
			handler_create_plane.Plane_To_Set = plane;
			exEvent_create_plane.Raise();
		}

		public class SketchPlaneCreation : IExternalEventHandler
		{
			public Plane Plane_To_Set { get; set; }
			public ModelInfo Info { get; set; }

			public void Execute(UIApplication app)
			{
				if(Plane_To_Set == null)
					throw new Exception("Bad Sketchplane in external event");
				using (Transaction tx = new Transaction(Info.DOC, "Place Box"))
				{
					tx.Start();
					SketchPlane sp = SketchPlane.Create(Info.DOC, Plane_To_Set);
					Info.DOC.ActiveView.SketchPlane = sp;
					Info.DOC.Regenerate();
					tx.Commit();
				}
			}

			public string GetName()
			{
				return "Create Box";
			}
		}

		/// <summary>
		/// Draw Whips
		/// </summary>
		private static LineStyleChange handler_line_style = null;
		private static ExternalEvent exEvent_line_style = null;

		public static void LineStyleChangeSignUp()
		{
			handler_line_style = new LineStyleChange();
			exEvent_line_style = ExternalEvent.Create(handler_line_style);
		}

		private static void ChangeLineStyles(ModelInfo info, GraphicsStyle style, List<DetailArc> arcs)
		{
			handler_line_style.Info = info;
			handler_line_style.Arcs = arcs;
			handler_line_style.Graphics_Id = style.Id;
			exEvent_line_style.Raise();
		}

		public class LineStyleChange : IExternalEventHandler
		{
			public ModelInfo Info { get; set; }
			public List<DetailArc> Arcs { get; set; }
			public ElementId Graphics_Id { get; set; }

			public void Execute(UIApplication app)
			{
				using(Transaction tx = new Transaction(Info.DOC, "Stylin\' On Um"))
				{
					tx.Start();
					foreach(var da in Arcs)
					{
						da.LineStyle = Info.DOC.GetElement(Graphics_Id);
					}
					tx.Commit();
				}
			}

			public string GetName()
			{
				return "Change Line Styles";
			}
		}

		/// <summary>
		/// Delete Whip lines
		/// </summary>
		private static DeleteBoxEntryModelElements handler_delete_model_elements = null;
		private static ExternalEvent exEvent_delete_model_elements = null;

		public static void DeleteModelElementsSignUp()
		{
			handler_delete_model_elements = new DeleteBoxEntryModelElements();
			exEvent_delete_model_elements = ExternalEvent.Create(handler_delete_model_elements);
		}

		public static void DeleteBoxesAndWhips(ModelInfo info, List<ElementId> ids)
		{
			handler_delete_model_elements.Info = info;
			handler_delete_model_elements.Ids = ids;
			exEvent_delete_model_elements.Raise();
		}

		public class DeleteBoxEntryModelElements : IExternalEventHandler
		{
			public ModelInfo Info { get; set; }
			public List<ElementId> Ids { get; set; }

			public void Execute(UIApplication app)
			{
				using(Transaction tx = new Transaction(Info.DOC, "Delete Model Elements"))
				{
					tx.Start();
					Info.DOC.Delete(Ids);
					tx.Commit();
				}
			}

			public string GetName()
			{
				return "Delete Model Elements";
			}
		}
		//End class
	}



	/// <summary>
	/// Information about what conduit is connected to a connector
	/// </summary>
	[DataContract]
	public struct BoxConnectorInfo
	{
		[DataMember]
		private readonly int id;
		[DataMember]
		private readonly double c_dia;
		[DataMember]
		private readonly string from;
		[DataMember]
		private readonly string to;

		public int Conduit_Id { get => id; }
		public double Conduit_Diameter { get => c_dia; }
		public string From { get => from; }
		public string To { get => to; }

		public BoxConnectorInfo(int conduit_id, double diameter, string conduit_from, string conduit_to)
		{
			id = conduit_id;
			c_dia = diameter;
			from = conduit_from;
			to = conduit_to;
		}
	}

	/// <summary>
	/// Bracket types for helicopter brackets
	/// </summary>
	public enum BracketType
	{
		SINGLE = 1,	//one bracket
		CROSS = 2, 	//two brackets in cross pattern
	}

	/// <summary>
	/// P3 connector configurations
	/// </summary>
	public enum P3BoxConfig
	{
		I = 0, // two conduits across from each other
		X = 1, // all connectors filled with conduit
		L = 2, // two perpendicular connectors have conduit
		T = 3, // two conduits across from each other as well as a one perpendicular
		D = 4, // dead end
		E = 5, // ERROR
	}

	/// <summary>
	/// P3 connector
	/// </summary>
	public struct P3BoxConfigConverter
	{
		private readonly int connection_count;
		private readonly bool parallel_present;

		public int Connection_Count { get => connection_count; }
		public bool Parallel_Present { get => parallel_present; }

		public P3BoxConfig Resolve_Config {get {
			switch(connection_count)
			{
				case 0: // E
					return P3BoxConfig.E;
				case 1: // D
					return P3BoxConfig.D;
				case 2: // I or L
					if(Parallel_Present)
						return P3BoxConfig.I;
					else
						return P3BoxConfig.L;
				case 3: // T
					if(Parallel_Present)
						return P3BoxConfig.T;
					return P3BoxConfig.E;
				case 4: // X
					if(Parallel_Present)
						return P3BoxConfig.X;
					return P3BoxConfig.E;
				default:
					return P3BoxConfig.E;
			}
		}}

		public BracketType Resolve_Bracket { get {
			switch(Resolve_Config)
			{
				case P3BoxConfig.D:
					return BracketType.SINGLE;
				case P3BoxConfig.I:
					return BracketType.SINGLE;
				case P3BoxConfig.L:
					return BracketType.SINGLE;
				case P3BoxConfig.T:
					return BracketType.CROSS;
				case P3BoxConfig.X:
					return BracketType.CROSS;
				case P3BoxConfig.E:
					return BracketType.SINGLE;
				default:
					return BracketType.SINGLE;
			}
		}}

		public P3BoxConfigConverter(int cc, bool par)
		{
			connection_count = cc;
			parallel_present = par;
		}
	}
}