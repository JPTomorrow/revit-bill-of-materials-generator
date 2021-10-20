using System;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Measurements;
using OfficeOpenXml.Style;
using Draw = System.Drawing;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
        /// Export a hanger labor hours breakdown sheet
        /// </summary>
        public void GenerateHangerLaborBreakdown(ModelInfo info, MasterDataPackage data_package)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Hanger Labor Breakdown", data_package.GetSelectedGlobalSettingsPackage().HangerExportSheetName);

            double gt = 0.0; // Grand Total
            double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
            static double shave_labor(double labor) => labor * 0.82;

            LaborExchange l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);
            var fixture_hangers = package.GetSelectedHangerPackage().FixtureHangers;
            var single_hangers = package.GetSelectedHangerPackage().SingleHangers;
            var strut_hangers = package.GetSelectedHangerPackage().StrutHangers;

            #region Hanger Labor
            bool has_hangers = fixture_hangers.Any() || single_hangers.Any() || strut_hangers.Any();
            if (has_hangers)
            {
                HangerTotal ht = new HangerTotal();

                string sslen(SingleHanger x) => RMeasure.LengthFromDbl(info.DOC, x.RodDiameter);
                string stlen(StrutHanger x) => RMeasure.LengthFromDbl(info.DOC, x.RodDiameter);
                string sflen(FixtureHanger x) => RMeasure.LengthFromDbl(info.DOC, x.RodDiameter);

                // strut
                strut_hangers.ForEach(x => ht.PushStrut("Slotted Strut", x.StrutSize, x.StrutLength * x.TierCount));

                // Anchors
                fixture_hangers.ForEach(x => ht.PushAnchors(x.AnchorType, sflen(x), 1));
                single_hangers.ForEach(x => ht.PushAnchors(x.AnchorType, sslen(x), 1));
                strut_hangers.ForEach(x => ht.PushAnchors(x.AnchorOneType, stlen(x), 1));
                strut_hangers.ForEach(x => ht.PushAnchors(x.AnchorTwoType, stlen(x), 1));

                // Attachments
                single_hangers.ForEach(x => { ht.PushIndividualAttachments(info.DOC, x); });

                //spring nuts
                fixture_hangers.ForEach(x => ht.PushSpringNuts(
                    "Spring Nut", sflen(x), x.Hardware.Where(x => x == "Spring Nut").Count()));
                single_hangers.ForEach(x => ht.PushSpringNuts(
                    "Spring Nut", sslen(x), x.Hardware.Where(x => x == "Spring Nut").Count()));
                strut_hangers.ForEach(x => ht.PushSpringNuts(
                    "Spring Nut", stlen(x), x.Hardware.Where(x => x == "Spring Nut").Count()));

                ht.SpringNuts.RemoveAll(x => x.Count == 0);

                // Lock Washers
                fixture_hangers.ForEach(x => ht.PushLockWashers(
                    "Lock Washer", sflen(x), x.Hardware.Where(x => x == "Lock Washer").Count()));
                single_hangers.ForEach(x => ht.PushLockWashers(
                    "Lock Washer", sslen(x), x.Hardware.Where(x => x == "Lock Washer").Count()));
                strut_hangers.ForEach(x => ht.PushLockWashers(
                    "Lock Washer", stlen(x), x.Hardware.Where(x => x == "Lock Washer").Count()));

                ht.LockWashers.RemoveAll(x => x.Count == 0);

                // Rod Couplings
                fixture_hangers.ForEach(x => ht.PushRodCouplings("Threaded Rod Couplings", sflen(x), x.RodCouplingCount));
                single_hangers.ForEach(x => ht.PushRodCouplings("Threaded Rod Couplings", sslen(x), x.RodCouplingCount));
                strut_hangers.ForEach(x => ht.PushRodCouplings("Threaded Rod Couplings", stlen(x), x.RodCouplingCount));

                ht.RodCouplings.RemoveAll(x => x.Count == 0);

                // Conduit Straps
                strut_hangers.ForEach(x => x.Straps.ToList().ForEach(y =>
                ht.PushConduitStraps("Strut Strap", y.Diameter, y.Count)));

                ht.ConduitStraps.RemoveAll(x => x.Count == 0);

                // Threaded Rod
                fixture_hangers.ForEach(x => ht.PushThreadedRod("Threaded Rod", sflen(x), x.RodLength));
                single_hangers.ForEach(x => ht.PushThreadedRod("Threaded Rod", sslen(x), x.RodLength));
                strut_hangers.ForEach(x => ht.PushThreadedRod("Threaded Rod", stlen(x), x.RodOneLength));
                strut_hangers.ForEach(x => ht.PushThreadedRod("Threaded Rod", stlen(x), x.RodTwoLength));

                ht.ThreadedRod.RemoveAll(x => x.Length == 0.0);

                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Hangers");

                foreach (var s in ht.Strut)
                {
                    var size = s.Size;
                    if (string.IsNullOrWhiteSpace(s.Size)) size = "1 5/8\"";
                    double rounded_len = Math.Ceiling(s.Length / 10.0) * 10.0;
                    var has_item = l.GetItem(out var li, (double)rounded_len, s.Type, size);
                    if (!has_item) throw new Exception("No Labor item for strut");
                    InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                // PRINT ALL HANGER INFO
                foreach (var a in ht.Anchors)
                {
                    var has_item = l.GetItem(out var li, (double)a.Count, a.Type, a.Diameter);
                    if (!has_item) throw new Exception("No Labor item for anchor");
                    string anchor_name = a.Type + " - " + a.Diameter + " Dia.";
                    InsertIntoRow(anchor_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach (var a in ht.SingleAttachments)
                {
                    var has_item = l.GetItem(out var li, (double)a.Count, a.Type, a.Size);
                    if (!has_item) throw new Exception("No Labor item for attachment -> " + a.Type + " - " + a.Size);
                    InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach (var r in ht.ThreadedRod)
                {
                    double rounded_len = Math.Ceiling(r.Length / 10.0) * 10.0;
                    var has_item = l.GetItem(out var li, (double)rounded_len, r.Type, r.Diameter);
                    if (!has_item) throw new Exception("No Labor item for threaded rod");
                    var rod_name = r.Type + " - " + r.Diameter + " Dia.";
                    InsertIntoRow(rod_name, li.Quantity, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach (var r in ht.RodCouplings)
                {
                    if (r.Count == 0) continue;
                    var has_item = l.GetItem(out var li, (double)r.Count, "Threaded Rod Coupling", r.Diameter);
                    if (!has_item) throw new Exception("No Labor item for rod couplings");
                    var coupling_name = r.Type + " - " + r.Diameter + " Dia.";
                    InsertIntoRow(coupling_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                /* @TODO: washers removed from the page per elmore. Delete later 
                foreach (var w in ht.Washers)
                {
                    var has_item = l.GetItem(out var li, (double)w.Count, "Washer", w.Diameter);
                    if (!has_item) throw new Exception("No Labor item for washers");
                    string washer_name = w.Type + " - " + w.Diameter + " Dia.";
                    InsertIntoRow(washer_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                } */

                foreach (var lw in ht.LockWashers)
                {
                    var has_item = l.GetItem(out var li, (double)lw.Count, "Lock Washer", lw.Diameter);
                    if (!has_item) throw new Exception("No Labor item for lock washers");
                    string lw_name = lw.Type + " - " + lw.Diameter + " Dia.";
                    InsertIntoRow(lw_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                /* @TODO: washers removed from the page per elmore. Delete later 
                foreach (var hn in ht.HexNuts)
                {
                    var has_item = l.GetItem(out var li, (double)hn.Count, "Hex Nut", hn.Diameter);
                    if (!has_item) throw new Exception("No Labor item for hex nuts");
                    string nut_name = hn.Type + " - " + hn.Diameter + " Dia.";
                    InsertIntoRow(nut_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                } */

                /* foreach(var sn in ht.SpringNuts)
                {
                    var has_item = l.GetItem(out var li, (double)sn.Count, sn.Type, sn.Diameter);
                    if(!has_item) throw new Exception("No Labor item for spring nuts");
                    string nut_name = sn.Type + " - " + sn.Diameter + " Dia.";
                    InsertIntoRow(nut_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                } */

                foreach (var c in ht.ConduitStraps)
                {
                    var has_item = l.GetItem(out var li, (double)c.Count, "Strut Strap", c.Diameter);
                    if (!has_item) throw new Exception("No Labor item for conduit straps");
                    string strap_name = "Strut Strap - " + c.Diameter + " Dia.";
                    InsertIntoRow(strap_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }
            }
            #endregion

            NextRow(1);
            InsertGrandTotal("Code 01 | Empty Raceway | Grand Total", ref code_one_gt, false, false, false);
            code_one_gt = shave_labor(code_one_gt);
            InsertGrandTotal("Code 01 w/ 0.82 Labor Factor", ref code_one_gt, true, false, true);

            // format the sheet
            FormatExcelSheet(0.1M);
            MakeFooter();
            ChangeColumnAlignment(4, new char[] { 'A', 'E' }, ExcelHorizontalAlignment.Left);
            this['D', 'D'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ChangeColumnWidth('A', 50);

            HasData = true;
        }
    }
}