using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.JunctionBox;
using JPMorrow.Revit.Measurements;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        // Add hardware entry
        public void AddHardwareEntry(Window window)
        {
            try
            {
                bool not_filled = String.IsNullOrWhiteSpace(Hardware_Entry_Name_Txt) || String.IsNullOrWhiteSpace(Hardware_Qty_Txt);

                if (not_filled)
                {
                    debugger.show(header: "Add Hardware Entry", err: "Please fill in entry name and quantity");
                    return;
                }

                HardwareEntry entry = new HardwareEntry();
                entry.name = Hardware_Entry_Name_Txt;
                entry.qty = int.Parse(Hardware_Qty_Txt);

                if (ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Any(x => x.name == entry.name)) return;

                ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Add(entry);
                RefreshDataGrids(BOMDataGrid.Hardware);

                WriteToLog("Labor entry added: " + entry.name + " | " + entry.qty);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Hardware Entry", err: ex.Message);
            }
        }

        // Remove hardware entry
        public void RemoveHardwareEntry(Window window)
        {
            try
            {
                var sel_entries = Hardware_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();
                if (!sel_entries.Any()) return;

                sel_entries.ForEach(x => ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Remove(x));
                RefreshDataGrids(BOMDataGrid.Hardware);

                WriteToLog(sel_entries.Count + " harware entry removed.");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Remove Hardware Entry", err: ex.Message);
            }
        }

        public void AddPullBoxHardware(Window window)
        {
            try
            {
                // collect pull boxes
                FilteredElementCollector fec = new FilteredElementCollector(ALS.Info.DOC, ALS.Info.UIDOC.ActiveView.Id);
                GeneratePullBoxHardware(fec.OfCategory(BuiltInCategory.OST_ElectricalEquipment).ToList());
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Pull Box Hardware", err: ex.Message);
            }
        }

        public void AddSelectedPullBoxHardware(Window window)
        {
            try
            {
                // collect pull boxes
                var selected_els = ALS.Info.SEL.GetElementIds().Select(x => ALS.Info.DOC.GetElement(x));
                GeneratePullBoxHardware(selected_els);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Selected Pull Box Hardware", err: ex.Message);
            }
        }

        private void GeneratePullBoxHardware(IEnumerable<Element> passed_els)
        {
            try
            {
                Parameter p(Element el, string name) => el.LookupParameter(name);
                string w = "Width";
                string h = "Height";
                string d = "Depth";

                var els = passed_els
                    .Where(x => x.Category.Name.Equals("Electrical Equipment"))
                    .Where(x => p(x, w) != null && p(x, h) != null && p(x, d) != null)
                    .Where(x => x as FamilyInstance != null)
                    .Where(x => (x as FamilyInstance).Symbol.FamilyName.ToLower().Contains("generic box"));

                debugger.show(header: "Hardware Pull Boxes", err: "Adding " + els.Count().ToString() + " pull boxes");

                var dummy_type = new { entry_name = "a1", qty = 0 };
                var pairs = new[] { dummy_type }.ToList();
                pairs.Clear();

                string get_length(Element el, string pn) => RMeasure.LengthFromDbl(ALS.Info.DOC, p(el, pn).AsDouble(), true);
                foreach (var el in els)
                {
                    var entry_name = "Pull Box - " +
                         get_length(el, w) + "W x " +
                         get_length(el, h) + "H x " +
                         get_length(el, h) + "D";

                    var idx = pairs.FindIndex(x => x.entry_name.Equals(entry_name));

                    if (idx >= 0)
                        pairs[idx] = new { entry_name = entry_name, qty = pairs[idx].qty + 1 };
                    else
                        pairs.Add(new { entry_name = entry_name, qty = 1 });
                }

                foreach (var pair in pairs)
                {
                    JunctionBoxUtil.MakePullBoxLaborEntry(pair.entry_name);
                    JunctionBoxUtil.AddPullBoxesToHardware(pair.entry_name, pair.qty);
                }

                RefreshDataGrids(BOMDataGrid.Hardware, BOMDataGrid.Labor);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Generate Pull Box Hardware", err: ex.Message);
            }
        }
    }
}