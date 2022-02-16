using System;
using System.Linq;
using System.Windows;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Labor;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        // Add labor entry
        public void AddLaborEntry(Window window)
        {
            try
            {
                var lletter = Labor_Code_Items[Sel_Labor_Letter_Code].ToCharArray().First();
                var lval = double.Parse(Labor_Per_Unit_Txt);
                var ldata = new LaborData(LaborExchange.LetterCodes.GetByLetter(lletter), lval);
                LaborEntry entry = new LaborEntry(Labor_Entry_Name_Txt, ldata);
                if (ALS.AppData.LaborHourEntries.Any(x => x.EntryName == entry.EntryName)) return;
                ALS.AppData.LaborHourEntries.Add(entry);
                RefreshDataGrids(BOMDataGrid.Labor);
                WriteToLog("labor entry added");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Labor Entry", err: ex.Message);
            }
        }

        // Remove labor entry
        public void RemoveLaborEntry(Window window)
        {
            try
            {
                var sel_entries = Labor_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();
                sel_entries.ForEach(x => ALS.AppData.LaborHourEntries.Remove(x));
                RefreshDataGrids(BOMDataGrid.Labor);
                WriteToLog(sel_entries.Count + " labor entry removed.");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Remove Labor Entry", err: ex.Message);
            }
        }
    }
}