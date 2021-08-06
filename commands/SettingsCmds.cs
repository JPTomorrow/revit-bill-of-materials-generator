using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.WirePackage;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.UI.Views;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
		// save global settings
        public void SaveGlobalSettings(Window window)
        {
            ALS.WirePackSettings.CouplingType = Coupling_Mat_Items[Sel_Coupling_Mat_Type];

            var makeup = RMeasure.LengthDbl(ALS.Info.DOC, Wire_Makeup_Length_Txt);
            ALS.AppData.GetSelectedGlobalSettingsPackage().WireMakeupLength = makeup;
            ALS.AppData.GetSelectedGlobalSettingsPackage().BranchExportSheetName = Branch_Export_Sheet_Name_Txt;
            ALS.AppData.GetSelectedGlobalSettingsPackage().DistributionExportSheetName = Distribution_Export_Sheet_Name_Txt;
            ALS.AppData.GetSelectedGlobalSettingsPackage().LowVoltageExportSheetName = Low_Voltage_Export_Sheet_Name_Txt;
            ALS.AppData.GetSelectedGlobalSettingsPackage().HangerExportSheetName = Hangers_Export_Sheet_Name_Txt;

            WirePackageSettings.Save(ALS.WirePackSettings);
        }

        // reset the project file
        public void ResetProjectFile(Window window)
        {
            bool ResetRuns = false;
            bool ResetWire = false;
            bool ResetHardware = false;
            bool ResetLabor = false;
            bool ResetP3InWall = false;
            bool ResetElecRoom = false;
            bool ResetHangers = false;
            bool ResetAutomaticWire = false;
            bool ResetPackageSettings = false;


            try {
                IntPtr main_rvt_wind = Process.GetCurrentProcess().MainWindowHandle;
                ResetFileView rfv = new ResetFileView(main_rvt_wind);
                rfv.ShowDialog();

                ResetRuns = rfv.ResetRunsBox.IsChecked ?? false;
                ResetWire = rfv.ResetWireBox.IsChecked ?? false;
                ResetHardware = rfv.ResetHardwareBox.IsChecked ?? false;
                ResetLabor = rfv.ResetLaborBox.IsChecked ?? false;
                ResetP3InWall = rfv.ResetP3InWallBox.IsChecked ?? false;
                ResetElecRoom = rfv.ResetElecRoomBox.IsChecked ?? false;
                ResetHangers = rfv.ResetHangersBox.IsChecked ?? false;
                ResetAutomaticWire = rfv.ResetAutoWireBox.IsChecked ?? false;
                ResetPackageSettings = rfv.ResetPackageSettingsBox.IsChecked ?? false;
            }
            catch(Exception ex) {
                debugger.show(err:ex.ToString());
            }

            if(!ResetRuns && !ResetWire && !ResetHangers &&
            !ResetLabor && !ResetHardware && !ResetElecRoom &&
            !ResetP3InWall && !ResetPackageSettings && !ResetAutomaticWire) return;

            if(ResetRuns) {
                ALS.AppData.GetSelectedConduitPackage().WireManager.Clear();
                Wire_Items.Clear();
                ALS.AppData.GetSelectedConduitPackage().Cris.Clear();
                Run_Items.Clear();

                RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            }

            if(ResetWire)
            {
                ALS.AppData.GetSelectedConduitPackage().WireManager.Clear();
                Wire_Items.Clear();

                RefreshDataGrids(BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            }

            if(ResetHangers)
            {
                ALS.AppData.GetSelectedHangerPackage().SingleHangers.Clear();
                Single_Hanger_Items.Clear();
                ALS.AppData.GetSelectedHangerPackage().FixtureHangers.Clear();
                Fixture_Hanger_Items.Clear();
                ALS.AppData.GetSelectedHangerPackage().StrutHangers.Clear();
                Strut_Hanger_Items.Clear();

                RefreshDataGrids(BOMDataGrid.Hangers);
            }

            if(ResetLabor)
            {
                ALS.AppData.LaborHourEntries.Clear();
                Labor_Items.Clear();

                RefreshDataGrids(BOMDataGrid.Labor);
            }

            if(ResetHardware)
            {
                ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Clear();
                Hardware_Items.Clear();

                RefreshDataGrids(BOMDataGrid.Hardware);
            }

            if(ResetElecRoom) {
                ALS.AppData.GetSelectedElecRoomPackage().ElectricalRoomPack.Rooms.Clear();
                ALS.ElecRoom = new ElecRoom();
                Elec_Room_Title_Txt = "";
                Update("Elec_Room_Title_Txt");

                RefreshDataGrids(BOMDataGrid.ElecRoom);
            }

            if(ResetAutomaticWire) {
                ALS.AppData.LowVoltageDevicePairings.Clear();
                ALS.AppData.LowVoltageWirePairings.Clear();
                RefreshDataGrids(BOMDataGrid.DeviceWirePairings);
            }

            if (ResetPackageSettings) {
                Branch_Export_Sheet_Name_Txt = "";
                Distribution_Export_Sheet_Name_Txt = "";
                Low_Voltage_Export_Sheet_Name_Txt = "";
                Hangers_Export_Sheet_Name_Txt = "";

                Update("Branch_Export_Sheet_Name_Txt");
                Update("Distribution_Export_Sheet_Name_Txt");
                Update("Low_Voltage_Export_Sheet_Name_Txt");
                Update(Hangers_Export_Sheet_Name_Txt);

                ALS.AppData.GetSelectedGlobalSettingsPackage().BranchExportSheetName = "";
                ALS.AppData.GetSelectedGlobalSettingsPackage().LowVoltageExportSheetName = "";
                ALS.AppData.GetSelectedGlobalSettingsPackage().DistributionExportSheetName = "";
                ALS.AppData.GetSelectedGlobalSettingsPackage().HangerExportSheetName = "";
            }

            var o = "Reset Project: { ";
            o += ResetRuns ? "Runs" : "";
            o += ResetWire ? " Wire" : "";
            o += ResetHangers ? " Hangers" : "";
            o += ResetLabor ? " Labor" : "";
            o += ResetHardware ? " Hardware " : "";
            o += ResetElecRoom ? " Elec Room" : "";
            o += ResetP3InWall  ? " P3 In Wall" : "";
            o += ResetAutomaticWire ? "Wire Automation" : "";
            o += ResetPackageSettings ? "Package Settings" : "";
            o += " }";
            WriteToLog(o);
        }
	}
}