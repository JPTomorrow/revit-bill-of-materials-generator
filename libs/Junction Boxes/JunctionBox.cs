
using System.Linq;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;

namespace JPMorrow.Revit.JunctionBox
{
    public static class JunctionBoxUtil
    {
        public static bool AddFourSquareBoxToHardware(int jbox_cnt)
        {
            // add junction boxes to hardware if fixture hangers are coming
            if (!ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Any(x => x.name.Equals("4 in. sq. box")))
            {
                HardwareEntry entry = new HardwareEntry();
                entry.name = "4 in. sq. box";
                entry.qty = jbox_cnt;
                ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Add(entry);
                return true;
            }
            else
                return false;
        }

        public static bool MakeFourSquareBoxLaborEntry()
        {
            // make harware entries for jboxes
            if (!ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("4 in. sq. box")))
            {
                double jbox_labor = 0.23;
                var ldata = new LaborData(LaborExchange.LetterCodes.GetByLetter('E'), jbox_labor);
                LaborEntry entry = new LaborEntry("4 in. sq. box", ldata);
                if (ALS.AppData.LaborHourEntries.Any(x => x.EntryName == entry.EntryName)) return false;
                ALS.AppData.LaborHourEntries.Add(entry);
                return true;
            }
            else
                return false;
        }
    }
}