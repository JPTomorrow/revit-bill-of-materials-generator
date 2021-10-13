
using System.Linq;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;

namespace JPMorrow.Revit.JunctionBox
{
    public static class JunctionBoxUtil
    {
        public static void AddFourSquareBoxToHardware(int jbox_cnt)
        {
            string box_name = "4 in. sq. box";
            string bc_name = "Blank Box Cover";
            string gs_name = "Ground Stinger";
            string washer_name = "Washer for 4 sq. box";
            string hn_name = "Hex nut for 4 sq. box";

            bool has_hardware(string name) => ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Any(x => x.name.Equals(name));
            int remove_hardware(string name) => ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.RemoveAll(x => x.name.Equals(name));

            bool has_box_hw = has_hardware(box_name);
            bool has_blank_cover_hw = has_hardware(bc_name);
            bool has_stinger_hw = has_hardware(gs_name);
            bool has_washer_hw = has_hardware(washer_name);
            bool has_hex_hw = has_hardware(hn_name);

            void make_hardware_entry(string name, int qty)
            {
                HardwareEntry entry = new HardwareEntry();
                entry.name = name;
                entry.qty = qty;
                ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Add(entry);
            }

            if (has_box_hw) remove_hardware(box_name);
            make_hardware_entry(box_name, jbox_cnt);

            if (has_blank_cover_hw) remove_hardware(bc_name);
            make_hardware_entry(bc_name, jbox_cnt);

            if (has_stinger_hw) remove_hardware(gs_name);
            make_hardware_entry(gs_name, jbox_cnt);

            if (has_washer_hw) remove_hardware(washer_name);
            make_hardware_entry(washer_name, jbox_cnt * 3);

            if (has_hex_hw) remove_hardware(hn_name);
            make_hardware_entry(hn_name, jbox_cnt * 3);
        }

        public static void MakeFourSquareBoxLaborEntries()
        {
            bool has_box_entry = ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("4 in. sq. box"));
            bool has_blank_cover_entry = ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("Blank Box Cover"));
            bool has_stinger_entry = ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("Ground Stinger"));
            bool has_washer_entry = ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("Washer for 4 sq. box"));
            bool has_hex_entry = ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("Hex nut for 4 sq. box"));

            void add_entry(string name, double labor, LetterCodePair pair)
            {
                var ldata = new LaborData(pair, labor);
                LaborEntry entry = new LaborEntry(name, ldata);
                ALS.AppData.LaborHourEntries.Add(entry);
            }
            // make harware entries for jboxes
            if (!has_box_entry)
                add_entry("4 in. sq. box", 0.23, LaborExchange.LetterCodes.GetByLetter('E'));

            if (!has_blank_cover_entry)
                add_entry("Blank Box Cover", 0.23, LaborExchange.LetterCodes.GetByLetter('E'));

            if (!has_stinger_entry)
                add_entry("Ground Stinger", 0.23, LaborExchange.LetterCodes.GetByLetter('E'));

            if (!has_washer_entry)
                add_entry("Washer for 4 sq. box", 0.23, LaborExchange.LetterCodes.GetByLetter('E'));

            if (!has_hex_entry)
                add_entry("Hex nut for 4 sq. box", 0.23, LaborExchange.LetterCodes.GetByLetter('E'));
        }

        public static void AddPullBoxesToHardware(string entry_name, int box_cnt)
        {
            bool has_hardware(string name) => ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Any(x => x.name.Equals(name));
            int remove_hardware(string name) => ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.RemoveAll(x => x.name.Equals(name));

            bool has_box_hw = has_hardware(entry_name);

            void make_hardware_entry(string name)
            {
                HardwareEntry entry = new HardwareEntry();
                entry.name = name;
                entry.qty = box_cnt;
                ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Add(entry);
            }

            if (has_box_hw) remove_hardware(entry_name);
            make_hardware_entry(entry_name);
        }

        public static void MakePullBoxLaborEntry(string entry_name)
        {
            bool has_box_entry = ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals(entry_name));

            void add_entry(string name, double labor, LetterCodePair pair)
            {
                var ldata = new LaborData(pair, labor);
                LaborEntry entry = new LaborEntry(name, ldata);
                ALS.AppData.LaborHourEntries.Add(entry);
            }
            // make harware entries for jboxes
            if (!has_box_entry)
                add_entry(entry_name, 1.0, LaborExchange.LetterCodes.GetByLetter('E'));
        }
    }
}