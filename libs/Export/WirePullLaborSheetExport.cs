using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
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
using JPMorrow.Tools.Diagnostics;
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

            InsertHeader(title, "Labor Breakdown", data_package.GetSelectedGlobalSettingsPackage().ExportTitle);

            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package, out string changed_wires);

            double gt = 0.0; // Grand Total
            double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
            double gt_code_three = 0.0; // 03 WIRE Grand Total
            static double shave_labor(double labor) => labor * 0.82;

            WirePackageSettings wire_pack_settings = WirePackageSettings.Load();
            var totaled_cris = ConduitTotal.GetTotaledConduit(info, package, pull_type).Conduit;
            var couplings = CouplingTotal.GetTotaledCouplings(info, package, pull_type).Couplings;
            var connectors = ConnectorTotal.GetTotaledConnectors(info, package, pull_type).Connectors;
            var wires = WireTotal.GetTotaledWire(package, pull_type).Wires;
            var elbows = FittingTotal.GetTotaledFittings(info, package, pull_type).Fittings;

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

            /* TODO: fix elbows code for Rigid pupe and 90's count
            foreach (var elbow in elbows)
            {
                double prune_angle = RMeasure.AngleDbl(info.DOC, "90\u00B0");
                if (elbow.Fitting.Angle < prune_angle) continue;
                var diameter = RMeasure.LengthFromDbl(info.DOC, elbow.Fitting.Diameter);

                // prune diameter into pipe lengths
                var length = elbow.Fitting.Diameter > RMeasure.LengthDbl(info.DOC, "2\"") ? RMeasure.LengthDbl(info.DOC, "18\"") : RMeasure.LengthDbl(info.DOC, "6\"");
                length *= elbow.Count;

                var has_item = l.GetItem(out var li, length, "Conduit", "RMC", diameter);
                if (!has_item) throw new Exception("No Labor item for Conduit");

                string coupling_name = "Conduit - Female Adapter - RNC - " + diameter + " Dia.";
                InsertIntoRow(li.MakeEntryName(postfix: "Dia."), li.DisplayQuantity,
                    li.PerUnitWithSuffix(" per ft."), li.LaborCodeLetter, li.TotalLaborValue);

                gt += li.TotalLaborValue;
                code_one_gt += li.TotalLaborValue;
                NextRow(1);
            }
            */

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

            // /* TODO: fix elbows code for Rigid pupe and 90's count
            // WARNING THIS BYPASSES THE ELBOW CODE ABOVE COMING OUT OF THE SYSTEM
            // INTEGRATE THIS INTO THE ELBOW SYSTEM

            #region Elbow Fittings Labor

            var fitting_ids = new FilteredElementCollector(info.DOC, info.UIDOC.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_ConduitFitting).ToElementIds();

            // @TODO: will crash on branch
            if (fitting_ids.Any() && pull_type == WireType.Distribution)
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Elbows");

                var proc_elbows = new List<TotaledFitting>();
                foreach (var id in fitting_ids)
                {
                    // get fitting element
                    var el = info.DOC.GetElement(id);
                    if (el == null || el.Category.Name != "Conduit Fittings") continue;
                    var fitting = el as FamilyInstance;
                    if (fitting == null) continue;
                    if (el.LookupParameter("Angle") == null || el.LookupParameter("Nominal Diameter") == null) continue;


                    // get family name
                    var name = fitting.Symbol.Family.Name;
                    // get value diameter string of fitting
                    var value_diameter = el.LookupParameter("Nominal Diameter").AsValueString();
                    // get fitting diameter as double
                    var diameter_dbl = el.LookupParameter("Nominal Diameter").AsDouble();
                    // array of fitting marterial types 
                    var material_types = new List<string> { "EMT", "RMC", "RNC", "IMC", "PVC" };
                    // check if fitting name containes any of the material types
                    bool is_valid_material = material_types.Any(x => name.ToUpper().Contains(x));

                    var angle = el.LookupParameter("Angle").AsDouble();
                    double prune_angle = RMeasure.AngleDbl(info.DOC, "90\u00B0");

                    var tolerance = 0.000000000005;
                    bool is_tolerant(double angle) => (angle <= prune_angle + tolerance && angle >= prune_angle - tolerance);
                    bool is_almost_eq(double angle1, double angle2) => (angle1 <= angle2 + tolerance && angle1 >= angle2 - tolerance);

                    if (is_valid_material && is_tolerant(angle))
                    {
                        // get fitting material type
                        var material_type = material_types.First(x => name.ToUpper().Contains(x));
                        // add to existing totaled fitting or add new entry to proc_elbows
                        var idx = proc_elbows.FindIndex(x => is_almost_eq(x.Fitting.Angle, angle) && x.Fitting.Type.Equals(material_type) && x.Fitting.Diameter == diameter_dbl);

                        if (idx >= 0)
                        {
                            proc_elbows[idx].Count++;
                        }
                        else
                        {
                            var fitting_diameter = RMeasure.LengthDbl(info.DOC, value_diameter);
                            Fitting entry = new Fitting(angle, diameter_dbl, material_type);
                            proc_elbows.Add(new TotaledFitting(entry, 1));
                        }
                    }
                }

                foreach (var elbow in proc_elbows)
                {
                    var has_item = l.GetItem(out var li, (double)elbow.Count, "Fitting", elbow.Fitting.Type, elbow.Fitting.GetDiameterString(info));
                    if (!has_item) throw new Exception("No Labor item for elbows");


                    var degrees = elbow.Fitting.GetAngleString(info);
                    var elbow_name = elbow.Fitting.Type + " - " + elbow.Fitting.GetDiameterString(info) + " Dia. - " + degrees + " degrees";
                    InsertIntoRow(elbow_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

                    gt += li.TotalLaborValue;
                    code_one_gt += li.TotalLaborValue;

                    NextRow(1);
                }

                InsertGrandTotal("Sub Total", ref gt, true, false, true);
            }
            #endregion


            #region Coupling and Connector Labor
            if (couplings.Any())
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Couplings & Connectors");
                foreach (var total in couplings) GetCouplingLabor(l, total, wire_pack_settings.CouplingType, ref code_one_gt, ref gt);
            }

            if (connectors.Any())
            {
                foreach (var total in connectors) GetConnectorLabor(l, total, wire_pack_settings.CouplingType, ref code_one_gt, ref gt);
            }

            if (couplings.Any() || connectors.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);
            #endregion

            #region Misc Hardware Labor
            var h = package.GetSelectedHardwarePackage().MiscHardwareEntries;
            var fixture_hangers = package.GetSelectedHangerPackage().FixtureHangers;
            var single_hangers = package.GetSelectedHangerPackage().SingleHangers;
            var strut_hangers = package.GetSelectedHangerPackage().StrutHangers;

            if (h.Any())
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Misc. Hardware");

                HangerTotal ht = new HangerTotal();

                foreach (HardwareEntry entry in h)
                {
                    var has_item = l.GetItem(out var li, (double)entry.qty, entry.name);
                    if (!has_item) throw new Exception("No Labor item for hardware");

                    var final_qty = li.Quantity;

                    // Hanger Washers on this page per elmores request
                    var has_washer = li.EntryName.ToLower().Contains("washer");
                    var has_hex = li.EntryName.ToLower().Contains("hex nut");
                    if (has_washer)
                    {
                        // half inch washer counts from hanger anchor count
                        fixture_hangers.ForEach(x => ht.PushWashers(
                            "Washer", x.Hardware.Where(x => x == "Washer").Count()));
                        single_hangers.ForEach(x => ht.PushWashers(
                            "Washer", x.Hardware.Where(x => x == "Washer").Count()));
                        strut_hangers.ForEach(x => ht.PushWashers(
                            "Washer", x.Hardware.Where(x => x == "Washer").Count()));

                        ht.Washers.RemoveAll(x => x.Count == 0);
                    }

                    // Hanger Hex Nuts
                    if (has_hex)
                    {
                        fixture_hangers.ForEach(x => ht.PushHexNuts(
                            "Hex Nut", x.Hardware.Where(x => x == "Hex Nut").Count()));
                        single_hangers.ForEach(x => ht.PushHexNuts(
                            "Hex Nut", x.Hardware.Where(x => x == "Hex Nut").Count()));
                        strut_hangers.ForEach(x => ht.PushHexNuts(
                            "Hex Nut", x.Hardware.Where(x => x == "Hex Nut").Count()));

                        ht.HexNuts.RemoveAll(x => x.Count == 0);
                    }

                    if (has_washer || has_hex) continue;

                    InsertIntoRow(li.EntryName, final_qty, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                // washers and hex nuts from hanger Total
                if (ht.Washers.Any())
                {
                    foreach (var washer in ht.Washers)
                    {
                        var has_item = l.GetItem(out var li, (double)washer.Count, "Washer", washer.Diameter);
                        if (!has_item) throw new Exception("No Labor item for washers");
                        InsertIntoRow(li.EntryName, washer.Count, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                        gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                    }
                }

                if (ht.HexNuts.Any())
                {
                    foreach (var nut in ht.HexNuts)
                    {
                        var has_item = l.GetItem(out var li, (double)nut.Count, "Hex Nut", nut.Diameter);
                        if (!has_item) throw new Exception("No Labor item for hex nuts");
                        InsertIntoRow(li.EntryName, nut.Count, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                        gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                    }
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

        public void GetCouplingLabor(LaborExchange l, TotaledCoupling total, string coupling_type, ref double code_one_gt, ref double gt)
        {
            string coupling_mat_type = ConduitRunInfo.ConduitMaterialTypes[0];
            if (total.Type.ToUpper().Contains("IMC")) return;

            if (ConduitRunInfo.ConduitMaterialTypes.Any(x => total.Type.ToUpper().Contains(x)))
            {
                coupling_mat_type = ConduitRunInfo.ConduitMaterialTypes.Where(x => total.Type.ToUpper().Contains(x)).First();
            }

            var c_type = coupling_type;
            if (coupling_mat_type.ToUpper().Contains("RNC") || coupling_mat_type.Equals("PVC")) c_type = "Standard";

            var has_item = l.GetItem(out var li, (double)total.Count, "Coupling", c_type, coupling_mat_type, total.Diameter);
            if (!has_item) throw new Exception("No Labor item for couplings " + ("-> Coupling " + c_type + " " + coupling_mat_type + " " + total.Diameter));

            string coupling_name = "Coupling - " + coupling_type + " - " + total.Diameter + " Dia.";
            InsertIntoRow(coupling_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

            gt += li.TotalLaborValue;
            code_one_gt += li.TotalLaborValue;
            NextRow(1);
        }

        /// <summary>
        /// get labor for connectors
        /// </summary>
        private void GetConnectorLabor(LaborExchange l, TotaledConnector total, string coupling_type, ref double code_one_gt, ref double gt)
        {
            string connector_type = ConduitRunInfo.ConduitMaterialTypes[0];
            if (ConduitRunInfo.ConduitMaterialTypes.Any(x => total.Type.ToUpper().Contains(x)))
            {
                connector_type = ConduitRunInfo.ConduitMaterialTypes.Where(x => total.Type.ToUpper().Contains(x)).First();
            }

            var c_type = connector_type.ToUpper().Contains("RNC") || connector_type.Equals("PVC") || connector_type.Equals("IMC") ? "Female Adapter" : coupling_type;

            var has_item = l.GetItem(out var li, (double)total.Count, "Connector", c_type, connector_type, total.Diameter);
            if (!has_item) throw new Exception("No Labor item for connectors");

            string connector_name = "Connector - " + connector_type + " - " + total.Diameter + " Dia.";
            InsertIntoRow(connector_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

            gt += li.TotalLaborValue;
            code_one_gt += li.TotalLaborValue;
            NextRow(1);

            /* TODO: fix elbows code for Rigid pupe and 90's count
            foreach (var elbow in elbows)
            {
                double prune_angle = RMeasure.AngleDbl(info.DOC, "90\u00B0");
                if (elbow.Fitting.Angle < prune_angle) continue;
                var diameter = RMeasure.LengthFromDbl(info.DOC, elbow.Fitting.Diameter);

                var has_item = l.GetItem(out var li, elbow.Count, "Connector", "Female Adapter", "RNC", diameter);
                if (!has_item) throw new Exception("No Labor item for Connector");

                string coupling_name = "Connector - Female Adapter - RNC - " + diameter + " Dia.";
                InsertIntoRow(coupling_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

                gt += li.TotalLaborValue;
                code_one_gt += li.TotalLaborValue;
                NextRow(1);
            }
            */
        }
    }
}