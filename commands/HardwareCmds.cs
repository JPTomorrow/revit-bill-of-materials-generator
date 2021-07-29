using System.Linq;
using System.Windows;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Hardware;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        // Add hardware entry
        public void AddHardwareEntry(Window window)
        {
            HardwareEntry entry = new HardwareEntry();
            entry.name = Hardware_Entry_Name_Txt;
            entry.qty = int.Parse(Hardware_Qty_Txt);

            if(ALS.AppData.MiscHardwareEntries.Any(x => x.name == entry.name)) return;

            ALS.AppData.MiscHardwareEntries.Add(entry);
            RefreshDataGrids(BOMDataGrid.Hardware);

            WriteToLog("Labor entry added: " +
            entry.name + " | " + entry.qty );
        }

        // Remove hardware entry
        public void RemoveHardwareEntry(Window window)
        {
            var sel_entries = Hardware_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();
            sel_entries.ForEach(x => ALS.AppData.MiscHardwareEntries.Remove(x));
            RefreshDataGrids(BOMDataGrid.Hardware);

            WriteToLog(sel_entries.Count + " harware entry removed.");
        }
	}
}