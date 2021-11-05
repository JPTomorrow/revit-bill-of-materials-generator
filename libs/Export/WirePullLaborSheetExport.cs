using System;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Connectors;
using JPMorrow.Revit.Couplings;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Tools.ConduitFittings;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Revit.WirePackage;
using JPMorrow.Revit.Wires;
using OfficeOpenXml.Style;
using Draw = System.Drawing;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
		/// Export a labor hours breakdown sheet
		/// </summary>
        public void GenerateLaborSheet(ModelInfo info, MasterDataPackage data_package, WireType pull_type)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);

            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            if (pull_type == WireType.Branch)
            {
                InsertHeader(title, "Labor Breakdown", data_package.GetSelectedGlobalSettingsPackage().BranchExportSheetName);
            }
            else if (pull_type == WireType.Distribution)
            {
                InsertHeader(title, "Labor Breakdown", data_package.GetSelectedGlobalSettingsPackage().DistributionExportSheetName);
            }
            else if (pull_type == WireType.LowVoltage)
            {
                InsertHeader(title, "Labor Breakdown", data_package.GetSelectedGlobalSettingsPackage().LowVoltageExportSheetName);
            }

            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            double gt = 0.0; // Grand Total
            double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
            double gt_code_three = 0.0; // 03 WIRE Grand Total
            static double shave_labor(double labor) => labor * 0.82;

            WirePackageSettings wire_pack_settings = WirePackageSettings.Load();
            var h = package.GetSelectedHardwarePackage().MiscHardwareEntries;
            var fixture_hangers = package.GetSelectedHangerPackage().FixtureHangers;
            var single_hangers = package.GetSelectedHangerPackage().SingleHangers;
            var strut_hangers = package.GetSelectedHangerPackage().StrutHangers;
            var totaled_cris = ConduitTotal.GetTotaledConduit(info, package, pull_type).Conduit;
            var couplings = CouplingTotal.GetTotaledCouplings(info, package, pull_type).Couplings;
            var connectors = ConnectorTotal.GetTotaledConnectors(info, package, pull_type).Connectors;
            var wires = WireTotal.GetTotaledWire(package, pull_type).Wires;

            // @DELETE @TODO discontinued services. delete later.
            // var elbows = FittingTotal.GetTotaledFittings(info, package, pull_type).Fittings;

            InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Conduit");

            #region Conduit Labor
            foreach (var t in totaled_cris)
            {

                string type = ConduitRunInfo.ConduitMaterialTypes[0];

                if (ConduitRunInfo.ConduitMaterialTypes.Any(x => t.Type.ToUpper().Contains(x)))
                {
                    type = ConduitRunInfo.ConduitMaterialTypes.ToList()
                        .Find(x => t.Type.ToUpper().Contains(x));
                }

                var run_len = (int)Math.Round(t.Length);

                // subtract 10' elec room conduit will be added separately
                if (run_len >= 15.0)
                    run_len -= ConduitRunInfo.ElecRoomTakeoffLength;

                bool has_item = l.GetItem(out var li, run_len, "Conduit", type, t.Diameter);
                if (!has_item) throw new Exception("no conduit labor item found: " + "Conduit " + type + " " + t.Diameter);

                InsertIntoRow(li.MakeEntryName(postfix: "Dia."), li.DisplayQuantity,
                    li.PerUnitWithSuffix(" per ft."), li.LaborCodeLetter, li.TotalLaborValue);

                gt += li.TotalLaborValue;
                code_one_gt += li.TotalLaborValue;
                NextRow(1);
            }

            InsertGrandTotal("Sub Total", ref gt, true, false, true);
            #endregion

            #region Glue Labor
            if (totaled_cris.Any(x => x.Type.Contains("PVC")))
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Conduit Glue & Cleaner");

                foreach (var total in totaled_cris.Where(x => x.Type.Contains("PVC")))
                {
                    int conduit_segments = (int)Math.Round(total.Length) / 10;
                    int quarts = (int)Math.Ceiling(conduit_segments / 100.0);

                    bool has_glue = l.GetItem(out var gi, quarts, "Glue");
                    bool has_cleaner = l.GetItem(out var ci, quarts, "Cleaner");

                    if (!has_glue || !has_cleaner)
                        throw new Exception("no labor item found for glue or cleaner");

                    string glue_name = "PVC Glue for " + total.Diameter + " conduit";
                    InsertIntoRow(glue_name, quarts + " Quarts", gi.PerUnitWithSuffix("per qt."),
                        gi.LaborCodeLetter, gi.TotalLaborValue);
                    gt += gi.TotalLaborValue; code_one_gt += shave_labor(gi.TotalLaborValue); NextRow(1);

                    string cleaner_name = "PVC Cleaner for " + total.Diameter + " conduit";
                    InsertIntoRow(cleaner_name, quarts + " Quarts", ci.PerUnitWithSuffix("per qt."),
                        ci.LaborCodeLetter, ci.TotalLaborValue);

                    gt += ci.TotalLaborValue;
                    code_one_gt += ci.TotalLaborValue;
                }

                InsertGrandTotal("Sub Total", ref gt, true, false, true);
            }
            #endregion

            /* @DELETE @TODO discontinued service. delete later.
            #region Elbow Fittings Labor
            if (elbows.Any())
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Elbows");

                foreach (var elbow in elbows)
                {

                    string elbow_type = ConduitRunInfo.ConduitMaterialTypes[0];

                    if (ConduitRunInfo.ConduitMaterialTypes.Any(x => elbow.Fitting.Type.ToUpper().Contains(x)))
                    {
                        elbow_type = ConduitRunInfo.ConduitMaterialTypes
                            .Where(x => elbow.Fitting.Type.ToUpper().Contains(x)).First();
                    }

                    var has_item = l.GetItem(out var li, (double)elbow.Count, "Fitting", elbow_type, elbow.Fitting.GetDiameterString(info));
                    if (!has_item) throw new Exception("No Labor item for elbows");

                    var elbow_name = elbow.Fitting.Type + " - " + elbow.Fitting.GetDiameterString(info) + " Dia. - " + elbow.Fitting.GetAngleString(info) + " degrees";
                    InsertIntoRow(elbow_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

                    //@FIX: had needs extra couplings flag, dont know if still need
                    //CouplingTotal.AssimilateCouplings(coupling_totals, new CouplingTotal(elbow_type, elbow.Fitting.GetDiameterString(info), 1));

                    gt += li.TotalLaborValue;
                    code_one_gt += li.TotalLaborValue;

                    NextRow(1);
                }

                InsertGrandTotal("Sub Total", ref gt, true, false, true);
            }
            #endregion
            */

            #region Couplings Labor
            if (couplings.Any())
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Couplings & Connectors");

                foreach (var total in couplings)
                {
                    string coupling_type = ConduitRunInfo.ConduitMaterialTypes[0];
                    if (total.Type.ToUpper().Contains("IMC")) continue;

                    if (ConduitRunInfo.ConduitMaterialTypes.Any(x => total.Type.ToUpper().Contains(x)))
                    {
                        coupling_type = ConduitRunInfo.ConduitMaterialTypes.Where(x => total.Type.ToUpper().Contains(x)).First();
                    }

                    var c_type = coupling_type.ToUpper().Contains("RNC") || coupling_type.Equals("PVC") ? "Standard" : wire_pack_settings.CouplingType;

                    var has_item = l.GetItem(out var li, (double)total.Count, "Coupling", c_type, coupling_type, total.Diameter);
                    if (!has_item) throw new Exception("No Labor item for couplings");

                    string coupling_name = "Coupling - " + coupling_type + " - " + total.Diameter + " Dia.";
                    InsertIntoRow(coupling_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

                    gt += li.TotalLaborValue;
                    code_one_gt += li.TotalLaborValue;
                    NextRow(1);
                }
            }
            #endregion

            #region Connectors Labor
            if (connectors.Any())
            {
                foreach (var total in connectors)
                {
                    string connector_type = ConduitRunInfo.ConduitMaterialTypes[0];
                    if (ConduitRunInfo.ConduitMaterialTypes.Any(x => total.Type.ToUpper().Contains(x)))
                    {
                        connector_type = ConduitRunInfo.ConduitMaterialTypes.Where(x => total.Type.ToUpper().Contains(x)).First();
                    }

                    var c_type = connector_type.ToUpper().Contains("RNC") || connector_type.Equals("PVC") || connector_type.Equals("IMC") ? "Female Adapter" : wire_pack_settings.CouplingType;

                    var has_item = l.GetItem(out var li, (double)total.Count, "Connector", c_type, connector_type, total.Diameter);
                    if (!has_item) throw new Exception("No Labor item for connectors");

                    string connector_name = "Connector - " + connector_type + " - " + total.Diameter + " Dia.";
                    InsertIntoRow(connector_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

                    gt += li.TotalLaborValue;
                    code_one_gt += li.TotalLaborValue;
                    NextRow(1);
                }
            }

            if (couplings.Any() || connectors.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);
            #endregion

            #region Misc Hardware Labor

            // local functions for hanger hardware
            string sslen(SingleHanger x) => RMeasure.LengthFromDbl(info.DOC, x.RodDiameter);
            string stlen(StrutHanger x) => RMeasure.LengthFromDbl(info.DOC, x.RodDiameter);
            string sflen(FixtureHanger x) => RMeasure.LengthFromDbl(info.DOC, x.RodDiameter);

            if (h.Any())
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Misc. Hardware");

                foreach (HardwareEntry entry in h)
                {
                    var has_item = l.GetItem(out var li, (double)entry.qty, entry.name);
                    if (!has_item) throw new Exception("No Labor item for hardware");

                    var final_qty = li.Quantity;

                    // Hanger Washers on this page per elmores request
                    if (li.EntryName.ToLower().Contains("washer"))
                    {
                        HangerTotal ht = new HangerTotal();

                        fixture_hangers.ForEach(x => ht.PushWashers(
                            "Washer", sflen(x), x.Hardware.Where(x => x == "Washer").Count()));
                        single_hangers.ForEach(x => ht.PushWashers(
                            "Washer", sslen(x), x.Hardware.Where(x => x == "Washer").Count()));
                        strut_hangers.ForEach(x => ht.PushWashers(
                            "Washer", stlen(x), x.Hardware.Where(x => x == "Washer").Count()));

                        ht.Washers.RemoveAll(x => x.Count == 0);
                        final_qty += ht.Washers.Select(x => x.Count).Sum();
                    }

                    // Hanger Hex Nuts
                    if (li.EntryName.ToLower().Contains("hex nut"))
                    {
                        HangerTotal ht = new HangerTotal();

                        fixture_hangers.ForEach(x => ht.PushHexNuts(
                            "Hex Nut", sflen(x), x.Hardware.Where(x => x == "Hex Nut").Count()));
                        single_hangers.ForEach(x => ht.PushHexNuts(
                            "Hex Nut", sslen(x), x.Hardware.Where(x => x == "Hex Nut").Count()));
                        strut_hangers.ForEach(x => ht.PushHexNuts(
                            "Hex Nut", stlen(x), x.Hardware.Where(x => x == "Hex Nut").Count()));

                        ht.HexNuts.RemoveAll(x => x.Count == 0);
                        final_qty += ht.HexNuts.Select(x => x.Count).Sum();
                    }

                    InsertIntoRow(li.EntryName, final_qty, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                InsertGrandTotal("Sub Total", ref gt, true, false, true);
            }
            #endregion

            InsertGrandTotal("Code 01 | Empty Raceway | Grand Total", ref code_one_gt, false, false, false);
            code_one_gt = shave_labor(code_one_gt);
            InsertGrandTotal("Code 01 w/ 0.82 Labor Factor", ref code_one_gt, true, false, true);

            #region Wire Labor
            if (wires.Any())
            {
                string size = null;
                foreach (var total in wires.OrderBy(y => y.MaterialType).ThenBy(x => x.Size).ThenBy(y => y.Color))
                {
                    if (size == null || total.Size != size)
                    {
                        NextRow(1);
                        InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Wire " + total.Size + " - " + total.MaterialTypeText);
                        size = total.Size;
                    }

                    double rounded_len = (int)Math.Ceiling(((total.Length + package.GetSelectedGlobalSettingsPackage().WireMakeupLength) / 10.0) * 10.0);
                    var has_item = l.GetItem(out var li, (double)rounded_len, "Wire", total.MaterialTypeText, total.Size);
                    if (!has_item) throw new Exception("No Labor item for hardware");

                    InsertIntoRow(li.EntryName, rounded_len, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                    ApplyColorToColumn('A', total.Color);
                    NextRow(1); gt += li.TotalLaborValue; gt_code_three += li.TotalLaborValue;
                }
            }
            #endregion

            if (wires.Any())
            {
                InsertGrandTotal("Code 03 | Wire | Grand Total", ref gt, false, true, true);
                gt_code_three = shave_labor(gt_code_three);
                InsertGrandTotal("Code 03 w/ 0.82 Labor Factor", ref gt_code_three, true, false, true);
            }

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