﻿using System;
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
using System.Linq;
using System.IO;

namespace JPMorrow.Revit.BOMPackage
{
    /// <summary>
    /// A 
    /// </summary>
    [DataContract]
    public class ConduitSubDataPackage
    {
        [DataMember]
        public string PackageName { get; set; } = "";
        [DataMember]
        public List<ConduitRunInfo> Cris { get; private set; } = new List<ConduitRunInfo>();
        [DataMember]
        public WireManager WireManager { get; private set; } = new WireManager(new List<HashedWire>());

        public ConduitSubDataPackage(string package_name)
        {
            PackageName = package_name;
        }
    }

    /// <summary>
    /// A 
    /// </summary>
    [DataContract]
    public class HangerSubDataPackage
    {
        [DataMember]
        public string PackageName { get; set; } = "";
        [DataMember]
        public List<SingleHanger> SingleHangers { get; private set; } = new List<SingleHanger>();
        [DataMember]
        public List<StrutHanger> StrutHangers { get; private set; } = new List<StrutHanger>();
        [DataMember]
        public List<FixtureHanger> FixtureHangers { get; private set; } = new List<FixtureHanger>();

        public HangerSubDataPackage(string package_name)
        {
            PackageName = package_name;
        }
    }

    /// <summary>
    /// A 
    /// </summary>
    [DataContract]
    public class P3SubDataPackage
    {
        [DataMember]
        public string PackageName { get; set; } = "";
        [DataMember]
        public List<P3Box> P3Boxes { get; private set; } = new List<P3Box>();

        public P3SubDataPackage(string package_name)
        {
            PackageName = package_name;
        }
    }

    /// <summary>
    /// A 
    /// </summary>
    [DataContract]
    public class HardwareSubDataPackage
    {
        [DataMember]
        public string PackageName { get; set; } = "";
        [DataMember]
        public List<HardwareEntry> MiscHardwareEntries { get; private set; } = new List<HardwareEntry>();

        public HardwareSubDataPackage(string package_name)
        {
            PackageName = package_name;
        }
    }

    /// <summary>
    /// A 
    /// </summary>
    [DataContract]
    public class ElectricalRoomSubDataPackage
    {
        [DataMember]
        public string PackageName { get; set; } = "";
        [DataMember]
        public ElecRoomPack ElectricalRoomPack { get; private set; } = new ElecRoomPack();

        public ElectricalRoomSubDataPackage(string package_name)
        {
            PackageName = package_name;
        }
    }

    /// <summary>
    /// A 
    /// </summary>
    [DataContract]
    public class GlobalSettingsSubDataPackage
    {
        [DataMember]
        public string PackageName { get; set; } = "";
        [DataMember]
        public double WireMakeupLength { get; set; } = 8.0;
        [DataMember]
        public string ExportTitle { get; set; } = "";
        [DataMember]
        public string AreaTitle { get; set; } = "";

        public GlobalSettingsSubDataPackage(string package_name)
        {
            PackageName = package_name;
        }
    }



    /// <summary>
    /// A class to hold a total package
    /// of conduit, wire, and hangers etc.
    /// </summary>
    [DataContract]
    public class MasterDataPackage
    {
        [DataMember]
        public string ExportRootDirectory { get; set; } = "";
        [DataMember]
        public List<ConduitSubDataPackage> ConduitPackages { get; set; } = new List<ConduitSubDataPackage>();
        [DataMember]
        public List<HangerSubDataPackage> HangerPackages { get; set; } = new List<HangerSubDataPackage>();
        [DataMember]
        public List<P3SubDataPackage> P3Packages { get; set; } = new List<P3SubDataPackage>();
        [DataMember]
        public List<HardwareSubDataPackage> HardwarePackages { get; set; } = new List<HardwareSubDataPackage>();
        [DataMember]
        public List<ElectricalRoomSubDataPackage> ElecRoomPackages { get; set; } = new List<ElectricalRoomSubDataPackage>();
        [DataMember]
        public List<GlobalSettingsSubDataPackage> GlobalSettingsPackages { get; set; } = new List<GlobalSettingsSubDataPackage>();

        [DataMember]
        public List<LaborEntry> LaborHourEntries { get; private set; } = new List<LaborEntry>();
        [DataMember]
        public List<VoltageDropRule> VoltageDropRules { get; private set; } = new List<VoltageDropRule>();
        [DataMember]
        public List<LowVoltageDevicePairing> LowVoltageDevicePairings { get; private set; } = new List<LowVoltageDevicePairing>();
        [DataMember]
        public List<LowVoltageWirePairing> LowVoltageWirePairings { get; private set; } = new List<LowVoltageWirePairing>();

        [DataMember]
        private int selectedConduitPackageIdx = 0;
        [DataMember]
        private int selectedHangerPackageIdx = 0;
        [DataMember]
        private int selectedP3PackageIdx = 0;
        [DataMember]
        private int selectedHardwarePackageIdx = 0;
        [DataMember]
        private int selectedElecRoomPackageIdx = 0;
        [DataMember]
        private int selectedGlobalSettingsPackageIdx = 0;

        public int SelectedConduitPackageIdx { get => selectedConduitPackageIdx; }
        public int SelectedHangerPackageIdx { get => selectedHangerPackageIdx; }
        public int SelectedP3PackageIdx { get => selectedP3PackageIdx; }
        public int SelectedHardwarePackageIdx { get => selectedHardwarePackageIdx; }
        public int SelectedElecRoomPackageIdx { get => selectedElecRoomPackageIdx; }
        public int SelectedGlobalSettingsPackageIdx { get => selectedGlobalSettingsPackageIdx; }

        public string PrintSelectedIndexes()
        {
            return string.Format(
                "Conduit: {0},\nHanger: {1},\nP3: {2},\nHardware: {3},\nElec Room: {4},\nGlobal Settings: {5}",
                SelectedConduitPackageIdx.ToString(), SelectedHangerPackageIdx.ToString(),
                SelectedP3PackageIdx.ToString(), SelectedHardwarePackageIdx.ToString(),
                SelectedElecRoomPackageIdx.ToString(), SelectedGlobalSettingsPackageIdx.ToString());
        }

        public string GenerateExcelExportPath(string export_filename, string master_package_name, string area_title)
        {
            var p = ExportRootDirectory + "\\excel" + (string.IsNullOrWhiteSpace(area_title) ? "" : "\\" + area_title);
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
            p += "\\" + export_filename + ".xlsx";
            return p;
        }

        public string GeneratePdfExportPath(string excel_export_filepath, string master_package_name, string area_title)
        {

            var p = ExportRootDirectory + "\\pdf" + (string.IsNullOrWhiteSpace(area_title) ? "" : "\\" + area_title);
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
            var filename_no_ext = Path.GetFileNameWithoutExtension(excel_export_filepath);
            p += "\\" + filename_no_ext + ".pdf";
            return p;
        }

        /// <summary>
        /// Add Package
        /// </summary>

        public bool AddNewConduitSubPackage(string package_name)
        {
            if (ConduitPackages.Any(x => x.PackageName.Equals(package_name))) return false;
            ConduitPackages.Add(new ConduitSubDataPackage(package_name));
            return true;
        }

        public bool AddNewHangerSubPackage(string package_name)
        {
            if (HangerPackages.Any(x => x.PackageName.Equals(package_name))) return false;
            HangerPackages.Add(new HangerSubDataPackage(package_name));
            return true;
        }

        public bool AddNewP3SubPackage(string package_name)
        {
            if (P3Packages.Any(x => x.PackageName.Equals(package_name))) return false;
            P3Packages.Add(new P3SubDataPackage(package_name));
            return true;
        }

        public bool AddNewHardwareSubPackage(string package_name)
        {
            if (HardwarePackages.Any(x => x.PackageName.Equals(package_name))) return false;
            HardwarePackages.Add(new HardwareSubDataPackage(package_name));
            return true;
        }

        public bool AddNewElecRoomSubPackage(string package_name)
        {
            if (ElecRoomPackages.Any(x => x.PackageName.Equals(package_name))) return false;
            ElecRoomPackages.Add(new ElectricalRoomSubDataPackage(package_name));
            return true;
        }

        public bool AddNewGlobalSettingsSubPackage(string package_name)
        {
            if (GlobalSettingsPackages.Any(x => x.PackageName.Equals(package_name))) return false;
            GlobalSettingsPackages.Add(new GlobalSettingsSubDataPackage(package_name));
            return true;
        }

        /// <summary>
        /// Package Selection
        /// </summary>

        public bool SelectConduitPackage(string package_name, out ConduitSubDataPackage package)
        {
            package = null;
            var idx = ConduitPackages.FindIndex(x => x.PackageName.Equals(package_name));
            if (idx == -1) return false;
            package = ConduitPackages[idx];
            selectedConduitPackageIdx = idx;
            return true;
        }

        public bool SelectHangerPackage(string package_name, out HangerSubDataPackage package)
        {
            package = null;
            var idx = HangerPackages.FindIndex(x => x.PackageName.Equals(package_name));
            if (idx == -1) return false;
            package = HangerPackages[idx];
            selectedHangerPackageIdx = idx;
            return true;
        }

        public bool SelectP3Package(string package_name, out P3SubDataPackage package)
        {
            package = null;
            var idx = P3Packages.FindIndex(x => x.PackageName.Equals(package_name));
            if (idx == -1) return false;
            package = P3Packages[idx];
            selectedP3PackageIdx = idx;
            return true;
        }

        public bool SelectHardwarePackage(string package_name, out HardwareSubDataPackage package)
        {
            package = null;
            var idx = HardwarePackages.FindIndex(x => x.PackageName.Equals(package_name));
            if (idx == -1) return false;
            package = HardwarePackages[idx];
            selectedHardwarePackageIdx = idx;
            return true;
        }

        public bool SelectElecRoomPackage(string package_name, out ElectricalRoomSubDataPackage package)
        {
            package = null;
            var idx = ElecRoomPackages.FindIndex(x => x.PackageName.Equals(package_name));
            if (idx == -1) return false;
            package = ElecRoomPackages[idx];
            selectedElecRoomPackageIdx = idx;
            return true;
        }

        public bool SelectGlobalSettingsPackage(string package_name, out GlobalSettingsSubDataPackage package)
        {
            package = null;
            var idx = GlobalSettingsPackages.FindIndex(x => x.PackageName.Equals(package_name));
            if (idx == -1) return false;
            package = GlobalSettingsPackages[idx];
            selectedGlobalSettingsPackageIdx = idx;
            return true;
        }

        public ConduitSubDataPackage GetSelectedConduitPackage()
        {
            return ConduitPackages[selectedConduitPackageIdx];
        }

        public HangerSubDataPackage GetSelectedHangerPackage()
        {
            return HangerPackages[selectedHangerPackageIdx];
        }

        public P3SubDataPackage GetSelectedP3Package()
        {
            return P3Packages[selectedP3PackageIdx];
        }

        public HardwareSubDataPackage GetSelectedHardwarePackage()
        {
            return HardwarePackages[selectedHardwarePackageIdx];
        }

        public ElectricalRoomSubDataPackage GetSelectedElecRoomPackage()
        {
            return ElecRoomPackages[selectedElecRoomPackageIdx];
        }

        public GlobalSettingsSubDataPackage GetSelectedGlobalSettingsPackage()
        {
            return GlobalSettingsPackages[selectedGlobalSettingsPackageIdx];
        }

        private void AssimilatePackage(MasterDataPackage other)
        {
            if (other.ConduitPackages != null) ConduitPackages = other.ConduitPackages;
            if (other.P3Packages != null) P3Packages = other.P3Packages;
            if (other.HangerPackages != null) HangerPackages = other.HangerPackages;
            if (other.HardwarePackages != null) HardwarePackages = other.HardwarePackages;
            if (other.ElecRoomPackages != null) ElecRoomPackages = other.ElecRoomPackages;
            if (other.ConduitPackages != null) GlobalSettingsPackages = other.GlobalSettingsPackages;

            if (other.LaborHourEntries != null) LaborHourEntries = other.LaborHourEntries;
            if (other.VoltageDropRules != null) VoltageDropRules = other.VoltageDropRules;
            if (other.LowVoltageDevicePairings != null) LowVoltageDevicePairings = other.LowVoltageDevicePairings;
            if (other.LowVoltageWirePairings != null) LowVoltageWirePairings = other.LowVoltageWirePairings;

            AssignDefaultPackages();

            selectedConduitPackageIdx = other.selectedConduitPackageIdx;
            selectedHangerPackageIdx = other.selectedHangerPackageIdx;
            selectedP3PackageIdx = other.selectedP3PackageIdx;
            selectedHardwarePackageIdx = other.selectedHardwarePackageIdx;
            selectedElecRoomPackageIdx = other.selectedElecRoomPackageIdx;
            selectedGlobalSettingsPackageIdx = other.selectedGlobalSettingsPackageIdx;
        }

        private void AssignDefaultPackages()
        {
            if (!ConduitPackages.Any()) ConduitPackages.Add(new ConduitSubDataPackage("default"));
            if (!P3Packages.Any()) P3Packages.Add(new P3SubDataPackage("default"));
            if (!HangerPackages.Any()) HangerPackages.Add(new HangerSubDataPackage("default"));
            if (!HardwarePackages.Any()) HardwarePackages.Add(new HardwareSubDataPackage("default"));
            if (!ElecRoomPackages.Any()) ElecRoomPackages.Add(new ElectricalRoomSubDataPackage("default"));
            if (!ConduitPackages.Any()) ConduitPackages.Add(new ConduitSubDataPackage("default"));
            if (!GlobalSettingsPackages.Any()) GlobalSettingsPackages.Add(new GlobalSettingsSubDataPackage("default"));
        }

        public MasterDataPackage()
        {
            AssignDefaultPackages();
        }

        public MasterDataPackage(MasterDataPackage other)
        {
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
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }
    }
}
