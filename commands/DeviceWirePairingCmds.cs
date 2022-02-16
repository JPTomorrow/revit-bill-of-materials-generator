using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.ElementCollection;
using JPMorrow.Revit.Measurements;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        // Add Device Pairing
        public void ImportDeviceWirePairings(Window window)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Excel Files|*.xlsx;";
                var result = ofd.ShowDialog();
                if (result != DialogResult.OK && result != DialogResult.Yes) return;
                var filename = ofd.FileName;

                var device_pairings = LowVoltageDeviceAutomation.MakeDevicePairings(filename);
                var wire_pairings = LowVoltageDeviceAutomation.MakeWirePairings(filename).ToList();

                ALS.AppData.LowVoltageDevicePairings.AddRange(device_pairings);

                wire_pairings.AddRange(ALS.AppData.LowVoltageWirePairings);
                ALS.AppData.LowVoltageWirePairings.Clear();
                wire_pairings = wire_pairings.OrderBy(x => int.Parse(x.WireNumber)).ToList();
                ALS.AppData.LowVoltageWirePairings.AddRange(wire_pairings);

                RefreshDataGrids(BOMDataGrid.DeviceWirePairings);
                WriteToLog("Added " + device_pairings.Count().ToString() + " device pairings");
                WriteToLog("Added " + wire_pairings.Count().ToString() + " wire pairings");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Import Device Wire Pairings", err: ex.Message);
            }
        }

        // Remove Device Pairing
        public void RemoveDevicePairings(Window window)
        {
            try
            {
                var pairings = Device_Pairing_Items.Where(x => x.IsSelected).ToList();
                if (!pairings.Any()) return;
                pairings.ForEach(x => ALS.AppData.LowVoltageDevicePairings.Remove(x.Value));
                RefreshDataGrids(BOMDataGrid.DeviceWirePairings);
                WriteToLog("Removed " + pairings.Count().ToString() + " device pairings");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Remove Device Pairings", err: ex.Message);
            }
        }

        // Remove Wire Pairing
        public void RemoveWirePairings(Window window)
        {
            try
            {
                var pairings = Wire_Pairing_Items.Where(x => x.IsSelected).ToList();
                if (!pairings.Any()) return;
                pairings.ForEach(x => ALS.AppData.LowVoltageWirePairings.Remove(x.Value));
                RefreshDataGrids(BOMDataGrid.DeviceWirePairings);
                WriteToLog("Removed " + pairings.Count().ToString() + " wire pairings");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Remove Wire Pairings", err: ex.Message);
            }
        }

        // low voltage device tagging designed for NHA but will be refactored to be generic later
        public void AutomateDeviceTagging(Window window)
        {
            try
            {
                debugger.show(
                header: "Device Tagging",
                err: "This will fill in the Device ID parameter on all the electrical fixtures, that have the appropriate" +
                " parameters loaded, in the current view. The format will be [PANEL_NAME]-[DEVICE_NUMBER]-[WIRE_NUMBER]\n\n" +
                "The following parameters are required on electrical fixtures for this to work:\n\n" +
                "Panel Fed From\n" + "Device #\n" + "Wire #\n");

                List<ElementId> collected_fixtures = ElementCollector
                    .CollectElements(ALS.Info, BuiltInCategory.OST_ElectricalFixtures, false, "BYPASS")
                    .Element_Ids.ToList();

                if (!collected_fixtures.Any()) return;

                LowVoltageDeviceAutomation.NHA_TagLowVoltageDevices(ALS.Info, collected_fixtures);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Automate Device Tagging", err: ex.Message);
            }
        }
    }
}
