using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JPMorrow.Revit.Labor;

namespace JPMorrow.Revit.Hardware
{
	[DataContract]
	public class HardwareCollection
	{
		[DataMember]
		public List<ToggleBolt> ToggleBolts { get; private set; } = new List<ToggleBolt>();
		[DataMember]
		public List<Washer> Washers { get; private set; } = new List<Washer>();
		[DataMember]
		public List<SheetMetalScrew> SheetMetalScrews { get; private set; } = new List<SheetMetalScrew>();
		[DataMember]
		public List<Anchor> Anchors { get; private set; } = new List<Anchor>();
		[DataMember]
		public List<ChannelNut> ChannelNuts { get; private set; } = new List<ChannelNut>();
		[DataMember]
		public List<PlateFitting> PlateFittings { get; private set; } = new List<PlateFitting>();
		[DataMember]
		public List<PostBase> PostBases { get; private set; } = new List<PostBase>();
		[DataMember]
		public List<MachineScrew> MachineScrews { get; private set; } = new List<MachineScrew>();
	}

	public class HardwareTotal
	{
		public List<ToggleBoltTotal> ToggleBolts { get; } = new List<ToggleBoltTotal>();
		public List<WasherTotal> Washers { get; } = new List<WasherTotal>();
		public List<SheetMetalScrewTotal> SheetMetalScrews { get; } = new List<SheetMetalScrewTotal>();
		public List<AnchorTotal> Anchors { get; } = new List<AnchorTotal>();
		public List<ChannelNutTotal> ChannelNuts { get; } = new List<ChannelNutTotal>();
		public List<PlateFittingTotal> PlateFittings { get; } = new List<PlateFittingTotal>();
		public List<PostBaseTotal> PostBases { get; } = new List<PostBaseTotal>();
		public List<MachineScrewTotal> MachineScrew { get; } = new List<MachineScrewTotal>();
	}

	[DataContract]
	public class ToggleBolt
	{
		[DataMember]
		public string Name { get; private set; } = "Toggle Bolt";
		[DataMember]
		public string Size { get; private set; }

		public ToggleBolt(string name_prefix, string size)
		{
			Size = size;
			Name = name_prefix + " " + Name;
		}
	}

	public class ToggleBoltTotal {
		public ToggleBolt Bolt { get; set; }
		public int Count { get; set; }

		public ToggleBoltTotal(ToggleBolt bolt, int count) {
			Bolt = bolt;
			Count = count;
		}
	}

	[DataContract]
	public class SheetMetalScrew
	{
		[DataMember]
		public string Name { get; private set; } = "Sheet Metal Screw";
		[DataMember]
		public string Size { get; private set; }

		public SheetMetalScrew(string name_prefix, string size)
		{
			Size = size;
			Name = name_prefix + " " + Name;
		}
	}

	public class SheetMetalScrewTotal {
		public SheetMetalScrew Screw { get; set; }
		public int Count { get; set; }
		
		public SheetMetalScrewTotal(SheetMetalScrew screw, int count) {
			Screw = screw;
			Count = count;
		}
	}

	[DataContract]
	public class MachineScrew
	{
		[DataMember]
		public string Name { get; private set; } = "Machine Screw";
		[DataMember]
		public string Size { get; private set; }

		public MachineScrew(string name_prefix, string size)
		{
			Size = size;
			Name = name_prefix + " " + Name;
		}
	}

	public class MachineScrewTotal {
		public MachineScrew Screw { get; set; }
		public int Count { get; set; }

		public MachineScrewTotal(MachineScrew screw, int count) {
			Screw = screw;
			Count = count;
		}
	}

	[DataContract]
	public class ChannelNut
	{
		[DataMember]
		public string Name { get; private set; } = "Channel Spring Nut";
		[DataMember]
		public string Size { get; private set; }

		public ChannelNut(string name_prefix, string size)
		{
			Name = name_prefix + " " + Name;
			Size = size;
		}
	}

	public class ChannelNutTotal {
		public ChannelNut Nut { get; set; }
		public int Count { get; set; }

		public ChannelNutTotal(ChannelNut nut, int count) {
			Nut = nut;
			Count = count;
		}
	}

	[DataContract]
	public class PlateFitting
	{
		[DataMember]
		public string Name { get; private set; } = "Flat Plate Fitting";
		[DataMember]
		public string Type { get; private set; }

		private static List<string> Types { get; } = new List<string>() {
			"L", "T", "X"
		};

		public PlateFitting(string name_prefix, string type)
		{
			Type = Types.Any(x => x.Equals(type)) ? type : Types[1];
			Name = name_prefix + " " + Name;
		}
	}

	public class PlateFittingTotal {
		public PlateFitting Fitting { get; set; }
		public int Count { get; set; }

		public PlateFittingTotal(PlateFitting fitting, int count) {
			Fitting = fitting;
			Count = count;
		}
	}

	[DataContract]
	public class PostBase
	{
		[DataMember]
		public string Name { get; private set; } = "Post Base";

		public PostBase(string name_prefix)
		{
			Name = name_prefix + " " + Name;
		}
	}

	public class PostBaseTotal {
		public PostBase Base { get; set; }
		public int Count { get; set; }

		public PostBaseTotal(PostBase b, int count) {
			Base = b;
			Count = count;
		}
	}

	[DataContract]
	[KnownType(typeof(JPMorrow.Revit.Hardware.MasonryAnchor))]
	public class Anchor
	{
		[DataMember]
		public string Name { get; private set; } = "Anchor";
		[DataMember]
		public string Size { get; private set; }

		public static List<string> AnchorSizes { get; } = new List<string>() {
			"1/4\"", "3/8\""
		};

		public Anchor(string name_prefix, string size)
		{
			Size = size;
			Name = name_prefix + " " + Name;
		}
	}

	public class AnchorTotal {
		public Anchor Anchor { get; set; }
		public int Count { get; set; }

		public AnchorTotal(Anchor anchor, int count) {
			Anchor = anchor;
			Count = count;
		}
	}

	[DataContract]
	[KnownType(typeof(JPMorrow.Revit.Hardware.FenderWasher))]
	public class Washer
	{
		[DataMember]
		public string Name { get; private set; } = "Washer";
		[DataMember]
		public string Size { get; private set; }

		public Washer(string name_prefix, string size)
		{
			Size = size;
			Name = name_prefix + " " + Name;
		}
	}

	public class WasherTotal {
		public Washer Washer { get; set; }
		public int Count { get; set; }

		public WasherTotal(Washer washer, int count) {
			Washer = washer;
			Count = count;
		}
	}

	[DataContract]
	public class MasonryAnchor : Anchor
	{
		public MasonryAnchor(string name_prefix, string size) : base(name_prefix + " Hilti Concrete", size)
		{

		}
	}

	[DataContract]
	public class FenderWasher : Washer
	{
		public FenderWasher(string name_prefix, string size) : base(name_prefix + " Fender", size)
		{

		}
	}
}