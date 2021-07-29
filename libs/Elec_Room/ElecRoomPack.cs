using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Revit.Custom.Unistrut;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Panels;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.ElectricalRoom
{
	[DataContract]
	public class ElecRoom
	{
		[DataMember]
		public string RoomName { get; set; }
		[DataMember]
		public List<Unistrut> Unistrut { get; set; }
		[DataMember]
		public List<GroundBar> GroundBar { get; set; }
		[DataMember]
		public List<PanelBacking> PanelBacking { get; set; }
		[DataMember]
		public List<Panelboard> Panelboard { get; set; }
		[DataMember]
		public List<ElecRoomConduit> Conduit { get; private set; }

		public bool HasConduit { get => Conduit.Any(); }

		public bool HasData { get {
			return Unistrut.Any() || GroundBar.Any() || PanelBacking.Any() || Panelboard.Any();
		} }

		public ElecRoom()
		{
			RoomName = "";
			Unistrut = new List<Unistrut>();
			GroundBar = new List<GroundBar>();
			PanelBacking = new List<PanelBacking>();
			Panelboard = new List<Panelboard>();
			Conduit = new List<ElecRoomConduit>();
		}

	    public IEnumerable<ElecRoomConduit> AddElecRoomConduit(ModelInfo info, double length_to_remove, IEnumerable<ConduitRunInfo> all_cris,
			IEnumerable<int> room_conduit_ids)
		{
			Conduit.Clear();
			var conduits = ElecRoomConduit.CreateElecRoomConduit(length_to_remove, all_cris, room_conduit_ids);
			
			Conduit.AddRange(conduits);
			return conduits;
			
		}

		public override string ToString() {
			var o = "";
			Conduit.ForEach(c => o += c.MaterialType + "\n" + c.Length + "\n\n");
			return o;
		}
	}

	[DataContract]
	public class ElecRoomConduit
	{
		[DataMember]
		public string MaterialType { get; private set; }
		[DataMember]
		public double Diameter { get; private set; }
		[DataMember]
		public double Length { get; private set; }
		[DataMember]
		public int[] Ids { get; private set; }


		public ElecRoomConduit(string mat_type, double diameter, double length, IEnumerable<int> ids) {
			MaterialType = mat_type;
			Diameter = diameter;
			Length = length;

			Ids = ids.ToArray();
		}

		public static IEnumerable<ElecRoomConduit> CreateElecRoomConduit(
		    double length_to_remove, IEnumerable<ConduitRunInfo> all_cris,IEnumerable<int> room_conduit_ids)
		{
			var ret_conduit = new List<ElecRoomConduit>();
			var cris = all_cris.Where(x => !x.ConduitMaterialType.ToLower().Contains("flex")).ToList();
			var rc_ids = room_conduit_ids.ToList();

			foreach(var id in room_conduit_ids)
			{
				var idx = cris.FindIndex(cri => cri.WireIds.Contains(id));
				if(idx == -1 || ret_conduit.Any(x => x.Ids.Contains(id))) continue;
				var old = cris[idx];
				var erc = new ElecRoomConduit(
					old.ConduitMaterialType, old.Diameter,
					length_to_remove, old.WireIds);
				ret_conduit.Add(erc);
			}

			return ret_conduit;
		}
	}

	[DataContract]
	public class ElecRoomPack
	{
		[DataMember]
		public List<ElecRoom> Rooms { get; private set; }

		public bool HasData { get => Rooms.Any(); }

		public ElecRoomPack()
		{
			Rooms = new List<ElecRoom>();
		}

		public ElecRoomConduit GetMatchingConduit(IEnumerable<int> ids)
		{
			var rm = Rooms.SelectMany(x => x.Conduit).ToList().Find(x => x.Ids.ToList().Except(ids).Count() > 0);
			return rm;
		}

		public IEnumerable<ElecRoomConduit> FlattenConduit(ModelInfo info)
		{
			var conduit = Rooms.SelectMany(x => x.Conduit).ToList();
			var ret_con = new List<ElecRoomConduit>();

			foreach(var c in conduit)
			{
				var i = ret_con.FindIndex(x => x.MaterialType.Equals(c.MaterialType) && x.Diameter == c.Diameter);

				if(i == -1) {
					ret_con.Add(c);
				}
				else {
					var rem_con = ret_con[i];
					ret_con.RemoveAt(i);				      
					var length = rem_con.Length + c.Length;
					var ids = rem_con.Ids.Concat(c.Ids);
					var new_con = new ElecRoomConduit(rem_con.MaterialType, rem_con.Diameter, length, ids);
					ret_con.Add(new_con);
				}
			}

			return ret_con;
		}

		public void AddRoom(ElecRoom room)
		{
			if(room.RoomName.Equals(""))
			{
				debugger.show(err:"Please specify an Electrical Room Title.", header:"Elec Room");
				return;
			}

			if(Rooms.Any(x => x.RoomName.Equals(room.RoomName)))
			{
				var rm = Rooms.Find(x => x.RoomName.Equals(room.RoomName));
				Rooms.Remove(rm);
				Rooms.Add(room);
			}
			else
			{
				Rooms.Add(room);
			}

			Rooms = Rooms.OrderBy(x => x.RoomName).ToList();
		}

		public void RemoveRoom(ElecRoom room) => Rooms.Remove(room);
	}
}
