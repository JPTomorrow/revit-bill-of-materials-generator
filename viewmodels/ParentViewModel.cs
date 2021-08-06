using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using JPMorrow.Revit.Documents;
using System.Linq;
using System.IO;
using JPMorrow.Revit.Wires;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.WirePackage;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.Custom.WallInspection;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Views.RelayCmd;
using om = System.Collections.ObjectModel;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Measurements;
using JPMorrow.Data.Globals;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    // observable collection aliases
    using ObsStr = om.ObservableCollection<string>;
    using ObsConduitRun = om.ObservableCollection<ParentViewModel.RunPresenter>;
    using ObsMigConduitRun = om.ObservableCollection<ParentViewModel.MigrantRunPresenter>;
    using ObsWire = om.ObservableCollection<ParentViewModel.WirePresenter>;
    using ObsSingleHanger = om.ObservableCollection<ParentViewModel.SingleHangerPresenter>;
    using ObsStrutHanger = om.ObservableCollection<ParentViewModel.StrutHangerPresenter>;
    using ObsFixtureHanger = om.ObservableCollection<ParentViewModel.FixtureHangerPresenter>;
    using ObsHardware = om.ObservableCollection<ParentViewModel.HardwarePresenter>;
    using ObsLabor = om.ObservableCollection<ParentViewModel.LaborPresenter>;
    using ObsElecRoom = om.ObservableCollection<ParentViewModel.ElecRoomPresenter>;
    using ObsUnistrut = om.ObservableCollection<ParentViewModel.UnistrutPresenter>;
    using ObsGrdBar = om.ObservableCollection<ParentViewModel.GrdBarPresenter>;
    using ObsPanelBacking = om.ObservableCollection<ParentViewModel.BackingPresenter>;
    using ObsPanelboard = om.ObservableCollection<ParentViewModel.PanelboardPresenter>;
    using ObsP3Box = om.ObservableCollection<ParentViewModel.P3BoxPresenter>;
    using ObsP3LightingFixture = om.ObservableCollection<ParentViewModel.P3LightingFixturePresenter>;
    using ObsVDrop = om.ObservableCollection<ParentViewModel.VoltageDropPresenter>;
    using ObsDevicePairing = om.ObservableCollection<ParentViewModel.DevicePairingPresenter>;
    using ObsWirePairing = om.ObservableCollection<ParentViewModel.WirePairingPresenter>;

    public partial class ParentViewModel : Presenter
    {
        // observable collections
        public ObsStr Labor_Code_Items { get; set; } = new ObsStr();
        public ObsStr Dist_Wire_Size_Items { get; set; } = new ObsStr();
        public ObsStr Low_Voltage_Wire_Size_Items { get; set; } = new ObsStr();
        public ObsStr Panel_Voltage_Items { get; set; } = new ObsStr();
        public ObsStr Dist_Wire_Color_Items { get; set; } = new ObsStr();
        public ObsStr Panel_Phase_Items { get; set; } = new ObsStr();
        public ObsStr Load_Panel_Voltage_Items { get; set; } = new ObsStr();
        public ObsStr VD_Cutoff_Items { get; set; } = new ObsStr();
        public ObsStr Rod_Diameter_Items { get; set; } = new ObsStr();
        public ObsStr Single_Att_Type_Items { get; set; } = new ObsStr();
        public ObsStr Coupling_Mat_Items { get; set; } = new ObsStr();
        public ObsStr WallType_Items { get; set; } = new ObsStr();
        public ObsStr Grd_Lug_Size_Items { get; set; } = new ObsStr();
        public ObsStr Masonry_Anchor_Size_Items { get; set; } = new ObsStr();
        public ObsStr Conduit_Material_Items { get; set; } = new ObsStr();
        public ObsStr Dist_Wire_Material_Items { get; set; } = new ObsStr();
        public ObsStr Branch_Wire_Material_Items { get; set; } = new ObsStr();
        public ObsStr Branch_Panel_Phase_Items { get; set; } = new ObsStr();
        public ObsStr Export_Type_Items { get; set; } = new ObsStr();
        public ObsStr Reported_Wire_Panel_Voltage_Items { get; set; } = new ObsStr();
        public ObsConduitRun Run_Items { get; set; } = new ObsConduitRun();
        public ObsConduitRun Selected_Run_Items { get; set; } = new ObsConduitRun();
        public ObsMigConduitRun Migrate_Run_Items { get; set; } = new ObsMigConduitRun();
        public ObsP3Box P3_Items { get; set; } = new ObsP3Box();
        public ObsWire Wire_Items { get; set; } = new ObsWire();
        public ObsSingleHanger Single_Hanger_Items { get; set; } = new ObsSingleHanger();
        public ObsStrutHanger Strut_Hanger_Items { get; set; } = new ObsStrutHanger();
        public ObsFixtureHanger Fixture_Hanger_Items { get; set; } = new ObsFixtureHanger();
        public ObsHardware Hardware_Items { get; set; } = new ObsHardware();
        public ObsLabor Labor_Items { get; set; } = new ObsLabor();
        public ObsElecRoom Elec_Room_Items { get; set; } = new ObsElecRoom();
        public ObsUnistrut Unistrut_Items { get; set; } = new ObsUnistrut();
        public ObsGrdBar Grd_Bar_Items { get; set; } = new ObsGrdBar();
        public ObsPanelBacking Backing_Items { get; set; } = new ObsPanelBacking();
        public ObsPanelboard Panelboard_Items { get; set; } = new ObsPanelboard();
        public ObsVDrop Voltage_Drop_Items { get; set; } = new ObsVDrop();
        public ObsDevicePairing Device_Pairing_Items { get; set; } = new ObsDevicePairing();
        public ObsWirePairing Wire_Pairing_Items { get; set; } = new ObsWirePairing();
        public ObsP3LightingFixture P3_Lighting_Fixture_Items { get; set; } = new ObsP3LightingFixture();

        public int Sel_Dist_Wire_Size { get; set; }
        public int Sel_Low_Voltage_Wire_Size { get; set; }
        public int Sel_Dist_Wire_Color { get; set; }
        public int Sel_Panel_Voltage { get; set; }
        public int Sel_Phase { get; set; }
        public int Sel_Load_Panel_Voltage { get; set; }
        public int Sel_VD_Cutoff { get; set; }
        public int Sel_Single_Rod_Diameter { get; set; }
        public int Sel_Strut_Rod_Diameter { get; set; }
        public int Sel_Single_Att { get; set; }
        public int Sel_Coupling_Mat_Type { get; set; }
        public int Sel_Labor_Letter_Code { get; set; }
        public int Sel_Wall_Type { get; set; }
        public int Sel_Grd_Lug_Size { get; set; }
        public int Sel_Masonry_Anchor_Size { get; set; }
        public int Sel_Voltage_Drop_Voltage { get; set; }
        public int Sel_Voltage_Drop_Wire_Size { get; set; }
        public int Sel_Conduit_Material { get; set; }
        public int Sel_Branch_Wire_Material { get; set; }
        public int Sel_Dist_Wire_Material { get; set; }
        public int Sel_Branch_Panel_Phase { get; set; }
        public int Sel_Export_Type { get; set; }
        public int Sel_Reported_Wire_Panel_Voltage { get; set; }

        public bool Phase_Nuet_Checked {get; set;} = false;
        public bool IG_Checked {get; set;} = false;
        public bool Two_Pole_Override { get; set; } = false;
        public bool Three_Pole_Override { get; set; } = false;
        public bool Draw_Single_Debug {get; set;} = false;
        public bool Draw_Strut_Debug {get; set;} = false;
        public bool BOY_Reverse {get; set;} = false;
        public bool Staggered_Circs {get; set;} = false;
        public bool GB_Lug_Override_Switch {get; set;} = false;
        public bool Wall_Override_Switch {get; set;} = false;
        public bool Ceiling_Supported_Strut_Hanger {get; set;} = false;
        public bool Automate_Wire {get; set;} = false;
        public bool NHA_Tag_Low_Voltage_Devices {get; set;} = false;
        public bool Treat_Dist_As_Branch {get; set;} = false;

        public string VAmps_Txt { get; set; }
        public string Breaker_Size_Txt { get; set; }
        public string Elec_Room_Title_Txt { get; set; }
        public string Labor_Entry_Name_Txt { get; set; }
        public string Labor_Per_Unit_Txt { get; set; }
        public string Hardware_Entry_Name_Txt { get; set; }
        public string Hardware_Qty_Txt { get; set; }
        public string HO_Single_Min_Rod_Len_Txt { get; set; }
        public string HO_Strut_Min_Rod_Len_Txt { get; set; }
        public string HO_Max_Span_Txt { get; set; }
        public string HO_OESLength_Txt { get; set; }
        public string HO_IRGap_Txt { get; set; }
        public string HO_Min_Rod_Len_Txt { get; set; }
        public string Header_Text { get; set; }
        public string Elec_Room_Conduit_Cutoff_Txt { get; set; }
        public string Elec_Room_Conduit_Txt { get; set; }
        public string Nominal_Hanger_Spacing_Txt { get; set; }
        public string Bend_Hanger_Spacing_Txt { get; set; }
        public string Voltage_Drop_Min_Distance_Txt { get; set; }
        public string Voltage_Drop_Max_Distance_Txt { get; set; }
        public string Wire_Makeup_Length_Txt { get; set; }
        public string Branch_Export_Sheet_Name_Txt { get; set; }
        public string Distribution_Export_Sheet_Name_Txt { get; set; }
        public string Low_Voltage_Export_Sheet_Name_Txt { get; set; }
        public string Hangers_Export_Sheet_Name_Txt { get; set; }
        public string Load_Length_Txt { get; set; }
        public string Total_Unistrut_Hanger_Length_Txt { get; set; }
    
        public ICommand MasterCloseCmd => new RelayCommand<Window>(MasterClose);
        public ICommand TestCmd => new RelayCommand<Window>(Test);
        public ICommand NewPackageCmd => new RelayCommand<Window>(NewPackage);
        public ICommand LoadPackageCmd => new RelayCommand<Window>(LoadPackage);
        public ICommand SavePackageCmd => new RelayCommand<Window>(SavePackage);
        public ICommand SaveGlobalSettingsCmd => new RelayCommand<Window>(SaveGlobalSettings);
        public ICommand ResetProjectFileCmd => new RelayCommand<Window>(ResetProjectFile);

        public ICommand AddSingleRunCmd => new RelayCommand<Window>(AddSingleRun);
        public ICommand AddAllRunsCmd => new RelayCommand<Window>(AddAllRuns);
        public ICommand UpdateRunsCmd => new RelayCommand<Window>(UpdateRuns);
        public ICommand RemoveSelectedRunsCmd => new RelayCommand<Window>(RemoveSelectedRuns);
        public ICommand AddAllRunsByWorksetAndLevelCmd => new RelayCommand<Window>(AddAllRunsByWorksetAndLevel);
        public ICommand RunSelectionChangedCmd => new RelayCommand<Window>(SelectRun);
        public ICommand ClearRunsCmd  => new RelayCommand<Window>(ClearRuns);
        public ICommand ChangeConduitMaterialTypeCmd => new RelayCommand<Window>(ChangeConduitMaterialType);
        public ICommand MarkConduitNoWireExportCmd  => new RelayCommand<Window>(MarkConduitNoWireExport);
        public ICommand CombineLikeRunsCmd  => new RelayCommand<Window>(CombineLikeRuns);
        public ICommand SelectAllRunsCmd  => new RelayCommand<Window>(SelectAllRuns);
        public ICommand FixToFromCmd  => new RelayCommand<Window>(FixToFrom);

        public ICommand SelectP3LightingNetworkCmd => new RelayCommand<Window>(SelectP3LightingNetwork);
        public ICommand P3LightingFixtureSelChangedCmd => new RelayCommand<Window>(P3LightingFixtureSelChanged);
        public ICommand AddP3FixturesInViewCmd => new RelayCommand<Window>(AddP3FixturesInView);
        public ICommand DebugSelectedP3LightingFixtureCmd => new RelayCommand<Window>(DebugSelectedP3LightingFixture);

        public ICommand AddBranchWireCmd => new RelayCommand<Window>(AddBranchWire);
        public ICommand AddDistWireCmd => new RelayCommand<Window>(AddDistributionWire);
        public ICommand AddLowVoltageWireCmd => new RelayCommand<Window>(AddLowVoltageWire);
        public ICommand RemoveWireCmd => new RelayCommand<Window>(RemoveWire);
        public ICommand AddReportedWiresCmd => new RelayCommand<Window>(AddReportedWires);

        public ICommand ImportDeviceWirePairingsCmd  => new RelayCommand<Window>(ImportDeviceWirePairings);
        public ICommand RemoveDevicePairingsCmd  => new RelayCommand<Window>(RemoveDevicePairings);
        public ICommand RemoveWirePairingsCmd  => new RelayCommand<Window>(RemoveWirePairings);
        public ICommand AutomateDeviceTaggingCmd  => new RelayCommand<Window>(AutomateDeviceTagging);

        public ICommand LoadMigrateProjectCmd  => new RelayCommand<Window>(LoadMigrateProject);
        public ICommand MigrateWireCmd  => new RelayCommand<Window>(MigrateWire);
        public ICommand MigrateWireSelectionChangedCmd  => new RelayCommand<DataGrid>(MigrateWireSelectionChanged);

        public ICommand AddSingleHangersCmd => new RelayCommand<Window>(AddSingleHangers);
        public ICommand AddFixtureHangersCmd => new RelayCommand<Window>(AddFixtureHangers);
        public ICommand AddStrutHangersCmd => new RelayCommand<Window>(AddStrutHangers);
        public ICommand RemoveSingleHangersCmd => new RelayCommand<Window>(RemoveSingleHangers);
        public ICommand RemoveStrutHangersCmd => new RelayCommand<Window>(RemoveStrutHangers);
        public ICommand RestoreHangersCmd  => new RelayCommand<Window>(RestoreHangers);
        public ICommand SingleHangerSelChangedCmd  => new RelayCommand<Window>(SingleHangerSelChanged);
        public ICommand StrutHangerSelChangedCmd  => new RelayCommand<Window>(StrutHangerSelChanged);

        public ICommand AddLaborEntryCmd => new RelayCommand<Window>(AddLaborEntry);
        public ICommand RemoveLaborEntryCmd => new RelayCommand<Window>(RemoveLaborEntry);
        public ICommand AddHardwareEntryCmd => new RelayCommand<Window>(AddHardwareEntry);
        public ICommand RemoveHardwareEntryCmd => new RelayCommand<Window>(RemoveHardwareEntry);

        public ICommand AddVoltageDropRuleCmd  => new RelayCommand<Window>(AddVoltageDropRule);
        public ICommand RemoveVoltageDropRuleCmd  => new RelayCommand<Window>(RemoveVoltageDropRule);

        public ICommand ElecRoomSelectionChangedCmd  => new RelayCommand<Window>(SelectRoom);
        public ICommand AddElecRoomCmd  => new RelayCommand<Window>(AddElecRoom);
        public ICommand RemoveElecRoomCmd  => new RelayCommand<Window>(RemoveElecRoom);
        public ICommand AddAllUnistrutCmd  => new RelayCommand<Window>(AddUnistrut);
        public ICommand RemoveUnistrutCmd  => new RelayCommand<Window>(RemoveUnistrut);
        public ICommand AddAllGrdBarCmd  => new RelayCommand<Window>(AddGrdBar);
        public ICommand RemoveGrdBarCmd  => new RelayCommand<Window>(RemoveGrdBar);
        public ICommand AddAllPanelBackingCmd  => new RelayCommand<Window>(AddPanelBacking);
        public ICommand RemovePanelBackingCmd  => new RelayCommand<Window>(RemovePanelBacking);
        public ICommand AddAllPanelboardCmd  => new RelayCommand<Window>(AddPanelboard);
        public ICommand RemovePanelboardCmd  => new RelayCommand<Window>(RemovePanelboard);
        public ICommand GetElecRoomConduitCmd  => new RelayCommand<Window>(GetElecRoomConduit);
        public ICommand ClearElecRoomConduitCmd  => new RelayCommand<Window>(ClearElecRoomConduit);

        public ICommand FullActionLogCmd  => new RelayCommand<Window>(FullActionLog);
        public ICommand ExportCmd => new RelayCommand<Window>(ExportSelection);
        public ICommand PrepViewCmd => new RelayCommand<Window>(PrepView);

        public ICommand GetConduitLoadCmd => new RelayCommand<Window>(GetConduitLoad);
        public ICommand GetStrutLengthFromSelectedCmd => new RelayCommand<Window>(GetStrutLengthFromSelected);

        /// <summary>
        /// Package Selection UI
        /// </summary>
        
        public ObsStr ConduitPackageNameItems { get; set; } = new ObsStr();
        public ObsStr HangerPackageNameItems { get; set; } = new ObsStr();
        public ObsStr P3PackageNameItems { get; set; } = new ObsStr();
        public ObsStr HardwarePackageNameItems { get; set; } = new ObsStr();
        public ObsStr ElecRoomPackageNameItems { get; set; } = new ObsStr();
        public ObsStr GlobalSettingsPackageNameItems { get; set; } = new ObsStr();

        public int SelConduitPackage { get; set; }
        public int SelHangerPackage { get; set; }
        public int SelP3Package { get; set; }
        public int SelHardwarePackage { get; set; }
        public int SelElecRoomPackage { get; set; }
        public int SelGlobalSettingsPackage { get; set; }

        public ICommand AddNewConduitSubPackageCmd => new RelayCommand<ComboBox>(AddNewConduitSubPackage);
        public ICommand RemoveConduitSubPackageCmd => new RelayCommand<Window>(RemoveConduitSubPackage);
        public ICommand ConduitSubPackageSelectionChangedCmd => new RelayCommand<Window>(ConduitSubPackageSelectionChanged);

        public ICommand AddNewHangerSubPackageCmd => new RelayCommand<ComboBox>(AddNewHangerSubPackage);
        public ICommand RemoveHangerSubPackageCmd => new RelayCommand<Window>(RemoveHangerSubPackage);
        public ICommand HangerSubPackageSelectionChangedCmd => new RelayCommand<Window>(HangerSubPackageSelectionChanged);

        public ICommand AddNewHardwareSubPackageCmd => new RelayCommand<ComboBox>(AddNewHardwareSubPackage);
        public ICommand RemoveHardwareSubPackageCmd => new RelayCommand<Window>(RemoveHardwareSubPackage);
        public ICommand HardwareSubPackageSelectionChangedCmd => new RelayCommand<Window>(HardwareSubPackageSelectionChanged);

        public ICommand AddNewGlobalSettingsSubPackageCmd => new RelayCommand<ComboBox>(AddNewGlobalSettingsSubPackage);
        public ICommand RemoveGlobalSettingsSubPackageCmd => new RelayCommand<Window>(RemoveGlobalSettingsSubPackage);
        public ICommand GlobalSettingsSubPackageSelectionChangedCmd => new RelayCommand<Window>(GlobalSettingsSubPackageSelectionChanged);
        
        //Action Log Text
        public string Action_Log { get; set; }
        public string Current_Action_String { get; set; }
        public string Action_Log_File_Path { get; } = ModelInfo.GetDataDirectory("action_log") + "\\action_log.txt";

        /// <summary>
        /// Log writing function that can be passed around between views
        /// </summary>
        public void WriteToLog(string str) 
        {

            var append_line = str + "\n";
            Action_Log += append_line;
            Current_Action_String = str;
            File.AppendAllText(Action_Log_File_Path, append_line);
            Update("Action_Log");
            Update("Current_Action_String");
        }

        // package for saved runs and hangers
        private static MasterDataPackage TransferPackage { get; set; } = new MasterDataPackage();
        private static string packagePath;

        public string PackageName() 
        { 
            var p = packagePath.Split('\\').Last().Split('.').ToList();
            p.Remove(p.Last());
            return string.Join(".", p);
        }

        // get master package path
        public string GetPackagePath() 
        {

            string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";

            if(!File.Exists(path_file))
                File.WriteAllText(path_file, "Untitled");

            string ret_path = "";
            
            ret_path = File.ReadAllText(path_file).Trim();

            if(!File.Exists(ret_path)) 
            {
                File.WriteAllText(path_file, "Untitled");
                ret_path = "Untitled";
            }

            return ret_path;
        }

        // load the master package
        public void LoadMasterPackage(string path) 
        {
            
            ALS.WirePackSettings = WirePackageSettings.Load();
            ALS.HangerOptions = HangerOptions.Load(ALS.Info);

            if(path == "Untitled") 
            {
                WriteToLog("No package to load. Loading default package.");
                return;
            }

            ALS.AppData.LoadPackageFromLocation(path);

            // fill in combo boxes
            ConduitPackageNameItems = new ObsStr(ALS.AppData.ConduitPackages.Select(x => x.PackageName));
            HangerPackageNameItems = new ObsStr(ALS.AppData.HangerPackages.Select(x => x.PackageName));
            P3PackageNameItems = new ObsStr(ALS.AppData.P3Packages.Select(x => x.PackageName));
            HardwarePackageNameItems = new ObsStr(ALS.AppData.HardwarePackages.Select(x => x.PackageName));
            ElecRoomPackageNameItems = new ObsStr(ALS.AppData.ElecRoomPackages.Select(x => x.PackageName));
            GlobalSettingsPackageNameItems = new ObsStr(ALS.AppData.GlobalSettingsPackages.Select(x => x.PackageName));

            SelConduitPackage = ALS.AppData.SelectedConduitPackageIdx;
            SelHangerPackage = ALS.AppData.SelectedHangerPackageIdx;
            SelP3Package = ALS.AppData.SelectedP3PackageIdx;
            SelHardwarePackage = ALS.AppData.SelectedHardwarePackageIdx;
            SelElecRoomPackage = ALS.AppData.SelectedElecRoomPackageIdx;
            SelGlobalSettingsPackage = ALS.AppData.SelectedGlobalSettingsPackageIdx;

            Branch_Export_Sheet_Name_Txt = ALS.AppData.GetSelectedGlobalSettingsPackage().BranchExportSheetName;
            Distribution_Export_Sheet_Name_Txt = ALS.AppData.GetSelectedGlobalSettingsPackage().DistributionExportSheetName;
            Low_Voltage_Export_Sheet_Name_Txt = ALS.AppData.GetSelectedGlobalSettingsPackage().LowVoltageExportSheetName;
            Hangers_Export_Sheet_Name_Txt = ALS.AppData.GetSelectedGlobalSettingsPackage().HangerExportSheetName;

            // set makeup length from ALS.AppData
            Wire_Makeup_Length_Txt = RMeasure.LengthFromDbl(ALS.Info.DOC, ALS.AppData.GetSelectedGlobalSettingsPackage().WireMakeupLength);


            WriteToLog("Loaded package at: " + packagePath);
        }

        public ParentViewModel(ModelInfo info)
        {
            //revit documents and pre converted elements
            ALS.Info = info;

            packagePath = GetPackagePath();
            LoadMasterPackage(packagePath);

            //default datagrid header txts
            Header_Text = "Project: " + PackageName();
            Update("Header_Text");

            //load master package
            if(!ALS.AppData.LaborHourEntries.Any()) 
            {
                LaborExchange ex = new LaborExchange(ModelInfo.SettingsBasePath);
                string lfn = LaborExchange.DefaultLaborFilePath;
                ALS.AppData.LaborHourEntries.AddRange(LaborExchange.LoadLaborFromFile(lfn));
            }

            HO_Single_Min_Rod_Len_Txt = "0' 1\"";
            HO_Strut_Min_Rod_Len_Txt = "0' 1\"";
            HO_Max_Span_Txt = "3' 0\"";
            HO_IRGap_Txt = "0' 1\"";
            HO_OESLength_Txt = "0' 2\"";
            Elec_Room_Conduit_Cutoff_Txt = "10' 0\"";
            Elec_Room_Conduit_Txt = "Buildout Conduit Runs: 0";
            Nominal_Hanger_Spacing_Txt = "8'";
            Bend_Hanger_Spacing_Txt = "3'";
            Voltage_Drop_Min_Distance_Txt = "0'";
            Voltage_Drop_Max_Distance_Txt = "100'";
            Load_Length_Txt = "0' 1\"";
            
            UpdateTotalStrutLengthLabel();

            var ws_idx = Wire.WireSizes.ToList().IndexOf("#12");
            var pv_idx = Wire.PanelVoltages.ToList().IndexOf(Wire.PanelVoltages.First());
            var dwc_idx = WireColor.All_Colors.ToList().IndexOf(WireColor.All_Colors.First());
            var coup_idx = WirePackageSettings.Coupling_Mat_Types.ToList().IndexOf(ALS.WirePackSettings.CouplingType);

            Sel_Wall_Type = 0;
            Sel_Grd_Lug_Size = 0;
            Sel_Masonry_Anchor_Size = 0;
            Sel_Single_Att = 0;
            Sel_Single_Rod_Diameter = 0;
            Sel_Strut_Rod_Diameter = 1;
            Sel_Dist_Wire_Color = dwc_idx;
            Sel_Panel_Voltage = pv_idx;
            Sel_Dist_Wire_Size = ws_idx;
            Sel_Low_Voltage_Wire_Size = 0;
            Sel_VD_Cutoff = 0;
            Sel_Load_Panel_Voltage = 0;
            Sel_Phase = 0;
            Sel_Conduit_Material = 0;
            Sel_Branch_Panel_Phase = 0;
            Sel_Export_Type = 0;
            Sel_Reported_Wire_Panel_Voltage = 0;

            Panel_Phase_Items = new ObsStr(new string[] { "1", "3"} );
            Load_Panel_Voltage_Items = new ObsStr(Wire.GranularPanelVoltages);
            Branch_Wire_Material_Items = new ObsStr(Wire.WireMaterialTypes);
            Dist_Wire_Material_Items = new ObsStr(Wire.WireMaterialTypes);
            VD_Cutoff_Items = new ObsStr(VDrop.VDropIdxToWireSize.Values);
            Dist_Wire_Size_Items = new ObsStr(Wire.WireSizes);
            Low_Voltage_Wire_Size_Items = new ObsStr(Wire.LowVoltageWireNames);
            Panel_Voltage_Items = new ObsStr(Wire.PanelVoltages);
            Dist_Wire_Color_Items = new ObsStr(WireColor.All_Colors);
            Rod_Diameter_Items = new ObsStr(HangerConstants.RodDiameterMeasurments);
            Single_Att_Type_Items = new ObsStr(HangerConstants.SingleHangerAttachmentTypes);
            Labor_Code_Items = new ObsStr(LaborExchange.LetterCodes.Pairs.Select(x => x.Letter) );
            WallType_Items = new ObsStr(WallInfo.WallTypes.Select(x => x.WallName));
            Grd_Lug_Size_Items = new ObsStr(GrdBarLug.LugSizes);
            Masonry_Anchor_Size_Items = new ObsStr(Anchor.AnchorSizes);
            Coupling_Mat_Items = new ObsStr(WirePackageSettings.Coupling_Mat_Types);
            Conduit_Material_Items = new ObsStr(ConduitRunInfo.ConduitMaterialTypeFullNames);
            Branch_Panel_Phase_Items = new ObsStr(new[] { "3", "2" });
            Export_Type_Items = new ObsStr(Wire.WireTypeNames);
            Reported_Wire_Panel_Voltage_Items = new ObsStr(Wire.PanelVoltages);

            Draw_Single_Debug = true;
            Draw_Strut_Debug = true;

            Update("Panel_Phase_Items");
            Update("Load_Panel_Voltage_Items");
            Update("Copper_Alum_Items");
            Update("VD_Cutoff_Items");
            Update("Dist_Wire_Size_Items");
            Update("Low_Voltage_Wire_Size_Items");
            Update("Panel_Voltage_Items");
            Update("Dist_Wire_Color_Items");
            Update("Rod_Diameter_Items");
            Update("Single_Att_type_Items");
            Update("Wire_Generation_Items");

            RefreshDataGrids(BOMDataGrid.All);
        }

        // an enumeration of all the datagrids 
        // in the UI used with RefreshDataGrids to determine
        public enum BOMDataGrid 
        {
            All,
            Runs,
            SelectedRuns,
            Wire,
            Hangers,
            Hardware,
            P3,
            Labor,
            ElecRoom,
            VoltageDropRules,
            DeviceWirePairings,
            ExportSheetNames
        }

        /// <summary>
        /// Refresh all ui list elements
        /// </summary>
        private void RefreshDataGrids(params BOMDataGrid[] grids)
        {
            bool p(BOMDataGrid x) => grids.Contains(x) || grids.Contains(BOMDataGrid.All);

            if(p(BOMDataGrid.Runs)) 
            {
                Run_Items.Clear();
                ALS.AppData.GetSelectedConduitPackage().Cris.ForEach(x => Run_Items.Add(new RunPresenter(x, ALS.Info)));
                Update("Run_Items");
            }

            if(p(BOMDataGrid.SelectedRuns)) 
            {
                var presenters = Selected_Run_Items.ToList();
                presenters.ForEach(x => x.RefreshDisplay(ALS.Info) );
                Selected_Run_Items.Clear();
                Selected_Run_Items = new ObsConduitRun(presenters);
                Update("Selected_Run_Items");
            }

            if(p(BOMDataGrid.Hangers)) 
            {
                Single_Hanger_Items.Clear();
                Strut_Hanger_Items.Clear();
                Fixture_Hanger_Items.Clear();

                var hanger_package = ALS.AppData.GetSelectedHangerPackage();
                hanger_package.SingleHangers.ForEach(x => Single_Hanger_Items.Add(new SingleHangerPresenter(x, ALS.Info)));
                hanger_package.StrutHangers.ForEach(x => Strut_Hanger_Items.Add(new StrutHangerPresenter(x, ALS.Info)));
                hanger_package.FixtureHangers.ForEach(x => Fixture_Hanger_Items.Add(new FixtureHangerPresenter(x, ALS.Info)));

                Update("Single_Hanger_Items");
                Update("Strut_Hanger_Items");
                Update("Fixture_Hanger_Items");
            }

            if(p(BOMDataGrid.Hardware)) 
            {
                Hardware_Items.Clear();
                ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.ForEach(x => Hardware_Items.Add(new HardwarePresenter(x, ALS.Info)));
                Update("Hardware_Items");
            }

            if(p(BOMDataGrid.Labor)) 
            {
                Labor_Items.Clear();
                ALS.AppData.LaborHourEntries.ForEach(x => Labor_Items.Add(new LaborPresenter(x, ALS.Info)));
                Update("Labor_Items");
            }

            if(p(BOMDataGrid.P3)) 
            {
                P3_Items.Clear();
                ALS.AppData.GetSelectedP3Package().P3Boxes.ForEach(x => P3_Items.Add(new P3BoxPresenter(x, ALS.Info)));
                Update("P3_Items");
            }

            if(p(BOMDataGrid.Wire))
                Update("Wire_Items");

            if(p(BOMDataGrid.ElecRoom)) 
            {
                Elec_Room_Items.Clear();
                ALS.AppData.GetSelectedElecRoomPackage().ElectricalRoomPack.Rooms.ForEach(room => Elec_Room_Items.Add(new ElecRoomPresenter(room)));
                Update("Elec_Room_Items");

                Unistrut_Items.Clear();
                ALS.ElecRoom.Unistrut.ForEach(x => Unistrut_Items.Add(new UnistrutPresenter(x, ALS.Info)));
                Update("Unistrut_Items");

                Grd_Bar_Items.Clear();
                ALS.ElecRoom.GroundBar.ForEach(x => Grd_Bar_Items.Add(new GrdBarPresenter(x, ALS.Info)));
                Update("Grd_Bar_Items");

                Backing_Items.Clear();
                ALS.ElecRoom.PanelBacking.ForEach(x => Backing_Items.Add(new BackingPresenter(x, ALS.Info)));
                Update("Backing_Items");

                Panelboard_Items.Clear();
                ALS.ElecRoom.Panelboard.ForEach(x => Panelboard_Items.Add(new PanelboardPresenter(x)));
                Update("Backing_Items");
            }

            if(p(BOMDataGrid.VoltageDropRules)) 
            {
                Voltage_Drop_Items.Clear();
                ALS.AppData.VoltageDropRules.ForEach(x => Voltage_Drop_Items.Add(new VoltageDropPresenter(x, ALS.Info)));
                Update("Voltage_Drop_Items");
            }

            if(p(BOMDataGrid.DeviceWirePairings))
            {
                Device_Pairing_Items.Clear();
                Wire_Pairing_Items.Clear();
                ALS.AppData.LowVoltageDevicePairings.ForEach(x => Device_Pairing_Items.Add(new DevicePairingPresenter(x)));
                ALS.AppData.LowVoltageWirePairings.ForEach(x => Wire_Pairing_Items.Add(new WirePairingPresenter(x)));
                Update("Device_Pairing_Items");
                Update("Wire_Pairing_Items");
            }
        }
    }
}
