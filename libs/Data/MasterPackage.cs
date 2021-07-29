using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JPMorrow.Revit.Hangers;
using JPMorrow.Tools.Data;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.Revit.Wires;
using JPMorrow.P3;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.VoltageDrop;

namespace JPMorrow.Revit.BOMPackage
{
    /// <summary>
    /// A class to hold a total package
    /// of conduit, wire, and hangers etc.
    /// </summary>
    [DataContract]
	public class MasterDataPackage
	{
		[DataMember]
		public List<ConduitRunInfo> Cris { get; private set; }
		[DataMember]
		public List<P3Box> P3Boxes { get; private set; }
		[DataMember]
		public List<SingleHanger> SingleHangers { get; private set; }
		[DataMember]
		public List<StrutHanger> StrutHangers { get; private set; }
		[DataMember]
		public List<FixtureHanger> FixtureHangers { get; private set; }
		[DataMember]
		public List<HardwareEntry> MiscHardwareEntries { get; private set; }
		[DataMember]
		public List<LaborEntry> LaborHourEntries { get; private set; }
		[DataMember]
		public WireManager WireManager { get; private set; }
		[DataMember]
		public ElecRoomPack ElectricalRoomPack { get; private set; }
        [DataMember]
		public List<VoltageDropRule> VoltageDropRules { get; private set; }
		[DataMember]
		public List<LowVoltageDevicePairing> LowVoltageDevicePairings { get; private set; }
		[DataMember]
		public List<LowVoltageWirePairing> LowVoltageWirePairings { get; private set; }

		[DataMember]
		public double WireMakeupLength { get; set; }
		[DataMember]
		public string BranchExportSheetName { get; set; }
		[DataMember]
		public string DistributionExportSheetName { get; set; }
		[DataMember]
        public string LowVoltageExportSheetName { get; set; }
		[DataMember]
		public string HangerExportSheetName { get; set; }

		private void Init()
		{
			Cris = new List<ConduitRunInfo>();
			P3Boxes = new List<P3Box>();
			SingleHangers = new List<SingleHanger>();
			StrutHangers = new List<StrutHanger>();
			FixtureHangers = new List<FixtureHanger>();
			MiscHardwareEntries = new List<HardwareEntry>();
			LaborHourEntries = new List<LaborEntry>();
            VoltageDropRules = new List<VoltageDropRule>();
			WireManager = new WireManager(new List<HashedWire>());
			ElectricalRoomPack = new ElecRoomPack();
			WireMakeupLength = 8.0; // 8 foot as default
			LowVoltageDevicePairings = new List<LowVoltageDevicePairing>();
			LowVoltageWirePairings = new List<LowVoltageWirePairing>();
			BranchExportSheetName = "";
			DistributionExportSheetName = "";
            LowVoltageExportSheetName = "";
			HangerExportSheetName = "";
		}

		private void AssimilatePackage(MasterDataPackage other) {

			Cris = other.Cris ?? new List<ConduitRunInfo>();
			P3Boxes = other.P3Boxes ?? new List<P3Box>();
			SingleHangers = other.SingleHangers ?? new List<SingleHanger>();
			StrutHangers = other.StrutHangers ?? new List<StrutHanger>();
			FixtureHangers = other.FixtureHangers ?? new List<FixtureHanger>();
			MiscHardwareEntries = other.MiscHardwareEntries ?? new List<HardwareEntry>();
			LaborHourEntries = other.LaborHourEntries ?? new List<LaborEntry>();
            VoltageDropRules = other.VoltageDropRules ?? new List<VoltageDropRule>();
			WireManager = other.WireManager.HasWire() ? other.WireManager : new WireManager(new List<HashedWire>());
			ElectricalRoomPack = other.ElectricalRoomPack ?? new ElecRoomPack();
			WireMakeupLength = other.WireMakeupLength;
			LowVoltageDevicePairings = other.LowVoltageDevicePairings ?? new List<LowVoltageDevicePairing>();
			LowVoltageWirePairings = other.LowVoltageWirePairings ?? new List<LowVoltageWirePairing>();
			BranchExportSheetName = other.BranchExportSheetName ?? "";
			DistributionExportSheetName = other.DistributionExportSheetName ?? "";
            LowVoltageExportSheetName = other.LowVoltageExportSheetName ?? "";
			HangerExportSheetName = other.HangerExportSheetName ?? "";
		}

		public MasterDataPackage() => Init();

        public MasterDataPackage(MasterDataPackage other) {
			AssimilatePackage(other);
        }

		/// <summary>
		/// Load a data package from a location on disk
		/// </summary>
		/// <param name="file_path">the file path to the data package</param>
		public void LoadPackageFromLocation(string file_path)
		{
			MasterDataPackage p = JSON_Serialization.DeserializeFromFile<MasterDataPackage>(file_path);
			AssimilatePackage(p); 
		}

		

		/// <summary>
		/// Save a data package to a location on disk
		/// </summary>
		/// <param name="pack">the data package</param>
		/// <param name="file_path">the location to save</param>
		public void SavePackageToLocation(string file_path)
		{
			try
			{
				JSON_Serialization.SerializeToFile<MasterDataPackage>(this, file_path);
			}
			catch(Exception ex)
			{
				debugger.show(err:ex.ToString());
			}
		}
	}
}
