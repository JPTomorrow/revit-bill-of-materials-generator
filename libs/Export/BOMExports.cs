///
/// This file is a list of all the preconfigured exports as extention functions that can be called by a BOMOutputSheet
/// Author: Justin Morrow
///


using System;
using System.Collections.Generic;
using System.Linq;
using JPMorrow.P3;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Connectors;
using JPMorrow.Revit.Couplings;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Revit.Custom.Unistrut;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Panels;
using JPMorrow.Revit.Tools.ConduitFittings;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Revit.WirePackage;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using OfficeOpenXml.Style;
using Draw = System.Drawing;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet {

        // dictionary for apply color lookup
		private static readonly Dictionary<string, SystemColorInfo> system_color_swap = new Dictionary<string, SystemColorInfo>() {
			{"None", new SystemColorInfo() 						{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.DimGray, Border_Color=Draw.Color.DimGray } },
			{"Black", new SystemColorInfo() 					{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.Black, Border_Color=Draw.Color.Black } },
			{"Red", new SystemColorInfo() 						{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.Red, Border_Color=Draw.Color.Red } },
			{"Blue", new SystemColorInfo() 						{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.Blue, Border_Color=Draw.Color.Blue } },

			{"White w/ Black Stripe", new SystemColorInfo() 	{ Font_Color=Draw.Color.DimGray, Background_Color=Draw.Color.White, Border_Color=Draw.Color.Black } },
			{"White w/ Red Stripe", new SystemColorInfo() 		{ Font_Color=Draw.Color.Red, Background_Color=Draw.Color.White, Border_Color=Draw.Color.Red } },
			{"White w/ Blue Stripe", new SystemColorInfo() 		{ Font_Color=Draw.Color.Blue, Background_Color=Draw.Color.White, Border_Color=Draw.Color.Blue } },
			{"White w/ Orange Stripe", new SystemColorInfo() 	{ Font_Color=Draw.Color.Orange, Background_Color=Draw.Color.White, Border_Color=Draw.Color.Orange } },

			{"Brown", new SystemColorInfo() 					{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.Brown, Border_Color=Draw.Color.Brown } },
			{"Orange", new SystemColorInfo() 					{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.Orange, Border_Color=Draw.Color.Orange } },
			{"Yellow", new SystemColorInfo() 					{ Font_Color=Draw.Color.DimGray, Background_Color=Draw.Color.Yellow, Border_Color=Draw.Color.Yellow } },

			{"Gray w/ Brown Stripe", new SystemColorInfo() 		{ Font_Color=Draw.Color.SandyBrown, Background_Color=Draw.Color.DimGray, Border_Color=Draw.Color.SaddleBrown } },
			{"Gray w/ Orange Stripe", new SystemColorInfo() 	{ Font_Color=Draw.Color.Orange, Background_Color=Draw.Color.DimGray, Border_Color=Draw.Color.Orange } },
			{"Gray w/ Yellow Stripe", new SystemColorInfo() 	{ Font_Color=Draw.Color.Yellow, Background_Color=Draw.Color.DimGray, Border_Color=Draw.Color.Yellow } },

			{"White", new SystemColorInfo() 					{ Font_Color=Draw.Color.DimGray, Background_Color=Draw.Color.White, Border_Color=Draw.Color.White } },
			{"Gray", new SystemColorInfo() 						{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.DimGray, Border_Color=Draw.Color.DimGray } },
			{"Green", new SystemColorInfo() 					{ Font_Color=Draw.Color.White, Background_Color=Draw.Color.Green, Border_Color=Draw.Color.Green } },

			{"Green w/ Yellow Stripe", new SystemColorInfo() 	{ Font_Color=Draw.Color.Yellow, Background_Color=Draw.Color.Green, Border_Color=Draw.Color.Yellow } },
		};

		//Struct to hold info about a system color
		internal struct SystemColorInfo {
			public Draw.Color Font_Color {get; set;}
			public Draw.Color Background_Color {get; set;}
			public Draw.Color Border_Color {get; set;}
		}

        /// <summary>
		/// Export the a per panel breakdown of the wire pull
		/// </summary>
        public void  GenerateWirePullSheet(ModelInfo info, MasterDataPackage data_package, WireType pull_type) {

            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            if(pull_type == WireType.Branch) {
                InsertHeader(title, "Per-Panel Breakdown", data_package.BranchExportSheetName);
            }
            else if(pull_type == WireType.Distribution) {
                InsertHeader(title, "Per-Panel Breakdown", data_package.DistributionExportSheetName);
            }
            else if(pull_type == WireType.LowVoltage) {
                InsertHeader(title, "Per-Panel Breakdown", data_package.LowVoltageExportSheetName);
            }
            
            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            var cris = package.Cris.OrderBy(x => x.From).ToList();

            // print all the wire by panel
            foreach(ConduitRunInfo cri in cris) {
                
                Wire[] wires = package.WireManager.GetWires(cri.WireIds.ToArray()).ToArray();
                wires = wires.Where(x => x.WireType == pull_type).ToArray();

                if(!wires.Any() || wires.Any(x => x.IsNoWireExport(pull_type))) continue;

				InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Panel: " + cri.From);
				int wpn_merge_start_row = R;

				foreach(var wire in wires)
				{
					var wire_length = Math.Round(cri.Length + package.WireMakeupLength);
					var c_num = wire.CircuitNumber == "" ? "Feeder" : wire.CircuitNumber;
					var dia = RMeasure.LengthFromDbl(info.DOC, cri.Diameter);

					InsertIntoRow("", cri.From, c_num, wire.Size, wire.Color, wire_length, wire.WireMaterialType, dia);
					ApplyColorToColumn('E', wire.Color);
					NextRow(1);
				}

                // merge cells for Wire Pull Number Column
                MergeCells('A', 'A', wpn_merge_start_row, R - 1, "---");
			}

            // format the sheet
            FormatExcelSheet(0.1M);
            MakeFooter();
            this['H', 'H'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            HasData = true;
        }

        /// <summary>
		/// Export a total of the wire sheet
		/// </summary>
        public void GenerateWireTotalSheet(ModelInfo info, MasterDataPackage data_package, WireType pull_type) {
            
            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            if(pull_type == WireType.Branch) {
                InsertHeader(title, "Wire Pull Totals", data_package.BranchExportSheetName);
            }
            else if(pull_type == WireType.Distribution) {
                InsertHeader(title, "Wire Pull Totals", data_package.DistributionExportSheetName);
            }
            else if(pull_type == WireType.LowVoltage) {
                InsertHeader(title, "Wire Pull Totals", data_package.LowVoltageExportSheetName);
            }


            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            // total the wire into a flat list
            var wires = WireTotal.GetTotaledWire(package, pull_type).Wires;
            
            foreach(var w in wires) {
                if(wires.IndexOf(w) == 0) {
                    InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, wires[wires.IndexOf(w)].Size + " - " + w.MaterialTypeText);
                }

                var length = Math.Round(w.Length + package.WireMakeupLength);
                InsertIntoRow(w.Size + " - " + w.MaterialTypeText, w.Color, length);
                ApplyColorToColumn('B', w.Color);
                NextRow(1);

                // if next entry is of a different wire size? insert divider.
                if((wires.IndexOf(w) + 1) < wires.Count() &&
                    wires[wires.IndexOf(w) + 1].Size != w.Size) {
                    
                    InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, wires[wires.IndexOf(w) + 1].Size + " - " + wires[wires.IndexOf(w) + 1].MaterialTypeText);
                } 
            }

            // format the sheet
            FormatExcelSheet(0.1M);
            MakeFooter();
            this['G', 'G'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ChangeColumnWidth('A', 35.0);
			ChangeColumnWidth('B', 50.0);
			ChangeColumnWidth('C', 35.0);

            HasData = true;
        }

        /// <summary>
		/// Export a labor hours breakdown sheet
		/// </summary>
        public void GenerateLaborSheet(ModelInfo info, MasterDataPackage data_package, WireType pull_type) {
            
            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);

            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            if(pull_type == WireType.Branch) {
                InsertHeader(title, "Labor Breakdown", data_package.BranchExportSheetName);
            }
            else if(pull_type == WireType.Distribution) {
                InsertHeader(title, "Labor Breakdown", data_package.DistributionExportSheetName);
            }
            else if(pull_type == WireType.LowVoltage) {
                InsertHeader(title, "Labor Breakdown", data_package.LowVoltageExportSheetName);
            }

            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            double gt = 0.0; // Grand Total
			double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
            double gt_code_three = 0.0; // 03 WIRE Grand Total
			static double shave_labor(double labor) => labor * 0.82;

            WirePackageSettings wire_pack_settings = WirePackageSettings.Load();
            var h = package.MiscHardwareEntries;
            var fixture_hangers = package.FixtureHangers;
            var single_hangers = package.SingleHangers;
            var strut_hangers = package.StrutHangers;
            var totaled_cris = ConduitTotal.GetTotaledConduit(info, package, pull_type).Conduit;
            var elbows = FittingTotal.GetTotaledFittings(info, package, pull_type).Fittings;
            var couplings = CouplingTotal.GetTotaledCouplings(info, package, pull_type).Couplings;
            var connectors = ConnectorTotal.GetTotaledConnectors(info, package, pull_type).Connectors;
            var wires = WireTotal.GetTotaledWire(package, pull_type).Wires;

            InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Conduit");

            #region Conduit Labor
            foreach(var t in totaled_cris) {

				string type = ConduitRunInfo.ConduitMaterialTypes[0];

				if(ConduitRunInfo.ConduitMaterialTypes.Any(x => t.Type.ToUpper().Contains(x))) {
					type = ConduitRunInfo.ConduitMaterialTypes.ToList()
						.Find(x => t.Type.ToUpper().Contains(x));
				}

                var run_len = (int)Math.Round(t.Length);
                bool has_item = l.GetItem(out var li, run_len, "Conduit", type, t.Diameter);
                if(!has_item) throw new Exception("no conduit labor item found");

                InsertIntoRow(li.MakeEntryName(postfix: "Dia."), li.DisplayQuantity, 
                    li.PerUnitWithSuffix(" per ft."), li.LaborCodeLetter, li.TotalLaborValue);

				gt += li.TotalLaborValue;
				code_one_gt += li.TotalLaborValue;
				NextRow(1);
			}

            InsertGrandTotal("Sub Total", ref gt, true, false, true);
            #endregion

            #region Glue Labor
			if(totaled_cris.Any(x => x.Type.Contains("PVC")))
			{
				InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Conduit Glue & Cleaner");

				foreach(var total in totaled_cris.Where(x => x.Type.Contains("PVC")))
				{
                    int conduit_segments = (int)Math.Round(total.Length) / 10;
                    int quarts = (int)Math.Ceiling(conduit_segments / 100.0);

                    bool has_glue = l.GetItem(out var gi, quarts, "Glue");
                    bool has_cleaner = l.GetItem(out var ci, quarts, "Cleaner");

                    if(!has_glue || !has_cleaner)
                        throw new Exception("no labor item found for glue or cleaner");

                    string glue_name = "PVC Glue for " + total.Diameter + " conduit";
                    InsertIntoRow(glue_name, quarts  + " Quarts", gi.PerUnitWithSuffix("per qt."), 
                        gi.LaborCodeLetter, gi.TotalLaborValue);
					gt += gi.TotalLaborValue; code_one_gt += shave_labor(gi.TotalLaborValue); NextRow(1);

                    string cleaner_name = "PVC Cleaner for " + total.Diameter + " conduit";
                    InsertIntoRow(cleaner_name, quarts  + " Quarts", ci.PerUnitWithSuffix("per qt."), 
                        ci.LaborCodeLetter, ci.TotalLaborValue);

					gt += ci.TotalLaborValue;
					code_one_gt += ci.TotalLaborValue;
				}

				InsertGrandTotal("Sub Total", ref gt, true, false, true);
			}
            #endregion

            #region Elbow Fittings Labor
			if(elbows.Any()) {
				InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Elbows");

				foreach(var elbow in elbows) {

					string elbow_type = ConduitRunInfo.ConduitMaterialTypes[0];

					if(ConduitRunInfo.ConduitMaterialTypes.Any(x => elbow.Fitting.Type.ToUpper().Contains(x))) {
						elbow_type = ConduitRunInfo.ConduitMaterialTypes
                            .Where(x => elbow.Fitting.Type.ToUpper().Contains(x)).First();
					}

                    var has_item = l.GetItem(out var li, (double)elbow.Count, "Fitting", elbow_type, elbow.Fitting.GetDiameterString(info));
                    if(!has_item) throw new Exception("No Labor item for elbows");

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
            
            #region Couplings Labor
			if(couplings.Any())
			{
				InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Couplings & Connectors");

				foreach(var total in couplings)
				{
					string coupling_type = ConduitRunInfo.ConduitMaterialTypes[0];
					if(total.Type.ToUpper().Contains("IMC")) continue;

					if(ConduitRunInfo.ConduitMaterialTypes.Any(x => total.Type.ToUpper().Contains(x)))
					{
						coupling_type = ConduitRunInfo.ConduitMaterialTypes.Where(x => total.Type.ToUpper().Contains(x)).First();
					}

					var c_type = coupling_type.ToUpper().Contains("RNC") || coupling_type.Equals("PVC") ? "Standard" : wire_pack_settings.CouplingType;

                    var has_item = l.GetItem(out var li, (double)total.Count, "Coupling", c_type, coupling_type, total.Diameter);
                    if(!has_item) throw new Exception("No Labor item for couplings");

                    string coupling_name = "Coupling - " + coupling_type + " - " + total.Diameter + " Dia.";
                    InsertIntoRow(coupling_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

					gt += li.TotalLaborValue;
					code_one_gt += li.TotalLaborValue;
					NextRow(1);
				}
			}
            #endregion

            #region Connectors Labor
            if(connectors.Any())
			{
				foreach(var total in connectors)
				{
					string connector_type = ConduitRunInfo.ConduitMaterialTypes[0];
					if(ConduitRunInfo.ConduitMaterialTypes.Any(x => total.Type.ToUpper().Contains(x)))
					{
						connector_type = ConduitRunInfo.ConduitMaterialTypes.Where(x => total.Type.ToUpper().Contains(x)).First();
					}

					var c_type = connector_type.ToUpper().Contains("RNC") || connector_type.Equals("PVC") || connector_type.Equals("IMC") ? "Female Adapter" : wire_pack_settings.CouplingType;

                    var has_item = l.GetItem(out var li, (double)total.Count, "Connector", c_type, connector_type, total.Diameter);
                    if(!has_item) throw new Exception("No Labor item for connectors");

                    string connector_name = "Connector - " + connector_type + " - " + total.Diameter + " Dia."; 
                    InsertIntoRow(connector_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);

					gt += li.TotalLaborValue;
					code_one_gt += li.TotalLaborValue;
                    NextRow(1);
				}
            }

            if(couplings.Any() || connectors.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);
            #endregion

            #region Misc Hardware Labor
            if(h.Any())
            {
                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Misc. Hardware");

                foreach(HardwareEntry entry in h)
                {
                    var has_item = l.GetItem(out var li, (double)entry.qty, entry.name);
                    if(!has_item) throw new Exception("No Labor item for hardware");

                    InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                InsertGrandTotal("Sub Total", ref gt, true, false, true);
            }
            #endregion

            InsertGrandTotal("Code 01 | Empty Raceway | Grand Total", ref code_one_gt, false, false, false);
            code_one_gt = shave_labor(code_one_gt);
            InsertGrandTotal("Code 01 w/ 0.82 Labor Factor", ref code_one_gt, true, false, true);

            #region Wire Labor
            if(wires.Any())
            {
                string size = null;
                foreach (var total in wires.OrderBy(y => y.MaterialType).ThenBy(x => x.Size).ThenBy(y => y.Color))
                {
                    if(size == null || total.Size != size)
                    {
                        NextRow(1);
                        InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Wire " + total.Size + " - " + total.MaterialTypeText);
                        size = total.Size;
                    }
                    
                    double rounded_len = (int)Math.Ceiling(((total.Length + package.WireMakeupLength) / 10.0) * 10.0);
                    var has_item = l.GetItem(out var li, (double)rounded_len, "Wire", total.MaterialTypeText, total.Size);
                    if(!has_item) throw new Exception("No Labor item for hardware");

                    InsertIntoRow(li.EntryName, rounded_len, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                    ApplyColorToColumn('A', total.Color);
                    NextRow(1); gt += li.TotalLaborValue; gt_code_three += li.TotalLaborValue;
                }
            }
            #endregion

            if(wires.Any()) {
                InsertGrandTotal("Code 03 | Wire | Grand Total", ref gt, false, true, true);
                gt_code_three = shave_labor(gt_code_three);
                InsertGrandTotal("Code 03 w/ 0.82 Labor Factor", ref gt_code_three, true, false, true);
            }
            
            // format the sheet
            FormatExcelSheet(0.1M);
            MakeFooter();
            ChangeColumnAlignment(4, new char[] {'A', 'E'}, ExcelHorizontalAlignment.Left);
            this['D', 'D'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ChangeColumnWidth('A', 50);

            HasData = true;
        }

        /// <summary>
        /// Export a hanger labor hours breakdown sheet
        /// </summary>
        public void GenerateHangerLaborBreakdown(ModelInfo info, MasterDataPackage data_package) {
            
            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Hanger Labor Breakdown", data_package.HangerExportSheetName);

            double gt = 0.0; // Grand Total
			double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
            static double shave_labor(double labor) => labor * 0.82;
            
            LaborExchange l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);
            var fixture_hangers = package.FixtureHangers;
            var single_hangers = package.SingleHangers;
            var strut_hangers = package.StrutHangers;

            #region Hanger Labor
            bool has_hangers = fixture_hangers.Any() || single_hangers.Any() || strut_hangers.Any();
            if(has_hangers) {
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
                single_hangers.ForEach(x => {

                    // change to mineralac attachment if it is above cutoff size
                    var cuttoff = RMeasure.LengthDbl(info.DOC, "1\"");
                    var dia = RMeasure.LengthDbl(info.DOC, x.AttachmentSize);
                    var a_type = x.AnchorType;
                    a_type = dia < cuttoff ? "Batwing" : "Mineralac";

                    ht.PushIndividualAttachments(x.AttachmentType, x.AttachmentSize, 1);
                } );

                // Hex Nuts
                fixture_hangers.ForEach(x => ht.PushHexNuts(
                    "Hex Nut", sflen(x), x.Hardware.Where(x => x == "Hex Nut").Count()));
                single_hangers.ForEach(x => ht.PushHexNuts(
                    "Hex Nut", sslen(x), x.Hardware.Where(x => x == "Hex Nut").Count()));
                strut_hangers.ForEach(x => ht.PushHexNuts(
                    "Hex Nut", stlen(x), x.Hardware.Where(x => x == "Hex Nut").Count()));

                ht.HexNuts.RemoveAll(x => x.Count == 0);

                //spring nuts
                fixture_hangers.ForEach(x => ht.PushSpringNuts(
                    "Spring Nut", sflen(x), x.Hardware.Where(x => x == "Spring Nut").Count()));
                single_hangers.ForEach(x => ht.PushSpringNuts(
                    "Spring Nut", sslen(x), x.Hardware.Where(x => x == "Spring Nut").Count()));
                strut_hangers.ForEach(x => ht.PushSpringNuts(
                    "Spring Nut", stlen(x), x.Hardware.Where(x => x == "Spring Nut").Count()));

                ht.SpringNuts.RemoveAll(x => x.Count == 0);

                // Washers
                fixture_hangers.ForEach(x => ht.PushWashers(
                    "Washer", sflen(x), x.Hardware.Where(x => x == "Washer").Count()));
                single_hangers.ForEach(x => ht.PushWashers(
                    "Washer", sslen(x), x.Hardware.Where(x => x == "Washer").Count()));
                strut_hangers.ForEach(x => ht.PushWashers(
                    "Washer", stlen(x), x.Hardware.Where(x => x == "Washer").Count()));

                ht.Washers.RemoveAll(x => x.Count == 0);

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

                foreach(var s in ht.Strut)
                {
                    var size = s.Size;
                    if(string.IsNullOrWhiteSpace(s.Size)) size = "1 5/8\"";
                    double rounded_len = Math.Ceiling(s.Length / 10.0) * 10.0;
                    var has_item = l.GetItem(out var li, (double)rounded_len, s.Type, size);
                    if(!has_item) throw new Exception("No Labor item for strut");
                    InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                // PRINT ALL HANGER INFO
                foreach(var a in ht.Anchors)
                {
                    var has_item = l.GetItem(out var li, (double)a.Count, a.Type, a.Diameter);
                    if(!has_item) throw new Exception("No Labor item for anchor");
                    string anchor_name = a.Type + " - " + a.Diameter + " Dia.";
                    InsertIntoRow(anchor_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var a in ht.SingleAttachments)
                {
                    string AttachmentType = a.Type;
                    if(AttachmentType == "Batwing")
                    {
                        double diameter_dbl = RMeasure.LengthDbl(info.DOC, a.Size);
                        double limit_diameter = RMeasure.LengthDbl(info.DOC, "3/4\"");

                        if(diameter_dbl == -1 || limit_diameter == -1)
                            throw new Exception("No way to resolve batwing attachment type");
                        if(diameter_dbl <= limit_diameter)
                            AttachmentType += " - K-12";
                        else
                            AttachmentType += " - K-16";
                    }

                    var has_item = l.GetItem(out var li, (double)a.Count, AttachmentType, a.Size);
                    if(!has_item) throw new Exception("No Labor item for attachments");
                    InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var r in ht.ThreadedRod)
                {
                    double rounded_len = Math.Ceiling(r.Length / 10.0) * 10.0;
                    var has_item = l.GetItem(out var li, (double)rounded_len, r.Type, r.Diameter);
                    if(!has_item) throw new Exception("No Labor item for threaded rod");
                    var rod_name = r.Type + " - " + r.Diameter + " Dia.";
                    InsertIntoRow(rod_name, li.Quantity, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var r in ht.RodCouplings)
                {
                    if(r.Count == 0) continue;
                    var has_item = l.GetItem(out var li, (double)r.Count, "Threaded Rod Coupling", r.Diameter);
                    if(!has_item) throw new Exception("No Labor item for rod couplings");
                    var coupling_name = r.Type + " - " + r.Diameter + " Dia.";
                    InsertIntoRow(coupling_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var w in ht.Washers)
                {
                    var has_item = l.GetItem(out var li, (double)w.Count, "Washer", w.Diameter);
                    if(!has_item) throw new Exception("No Labor item for washers");
                    string washer_name = w.Type + " - " + w.Diameter + " Dia.";
                    InsertIntoRow(washer_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var lw in ht.LockWashers)
                {
                    var has_item = l.GetItem(out var li, (double)lw.Count, "Lock Washer", lw.Diameter);
                    if(!has_item) throw new Exception("No Labor item for lock washers");
                    string lw_name = lw.Type + " - " + lw.Diameter + " Dia.";
                    InsertIntoRow(lw_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var hn in ht.HexNuts)
                {
                    var has_item = l.GetItem(out var li, (double)hn.Count, "Hex Nut", hn.Diameter);
                    if(!has_item) throw new Exception("No Labor item for hex nuts");
                    string nut_name = hn.Type + " - " + hn.Diameter + " Dia.";
                    InsertIntoRow(nut_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var sn in ht.SpringNuts)
                {
                    var has_item = l.GetItem(out var li, (double)sn.Count, sn.Type, sn.Diameter);
                    if(!has_item) throw new Exception("No Labor item for spring nuts");
                    string nut_name = sn.Type + " - " + sn.Diameter + " Dia.";
                    InsertIntoRow(nut_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    gt += li.TotalLaborValue; code_one_gt += li.TotalLaborValue; NextRow(1);
                }

                foreach(var c in ht.ConduitStraps)
                {
                    var has_item = l.GetItem(out var li, (double)c.Count, "Strut Strap", c.Diameter);
                    if(!has_item) throw new Exception("No Labor item for conduit straps");
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
            ChangeColumnAlignment(4, new char[] {'A', 'E'}, ExcelHorizontalAlignment.Left);
            this['D', 'D'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ChangeColumnWidth('A', 50);

            HasData = true;
        }

        /// <summary>
		/// Export a Elec Room Buildout sheet
		/// </summary>
        public void GenerateElecRoomSheet(ModelInfo info, MasterDataPackage data_package, ElecRoom room) {
            
            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Electrical Room Buildout", "Room Name: " + room.RoomName);
            
            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            double gt = 0.0; // Grand Total
			double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
			// static double shave_labor(double labor) => labor * 0.82;
            string fdbl(double val) => string.Format("{0:N2}", val);

            WirePackageSettings wire_pack_settings = WirePackageSettings.Load();
            LaborExchange l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);

            InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Conduit", 15);

			List<ElecRoomConduit> conduit = package.ElectricalRoomPack.FlattenConduit(info).ToList();
			UnistrutTotal ut = room.Unistrut.FlattenUnistrut(info);
			GrdBarTotal gbt = room.GroundBar.FlattenGroundBars(info);
			PanelBackingTotal pbt = room.PanelBacking.FlattenPanelBacking();
			PanelboardTotal pbrdt = room.Panelboard.FlattenPanelboard();

            #region Conduit Labor
			foreach(var c in conduit)
			{
				string type = ConduitRunInfo.ConduitMaterialTypes[0];

				if(ConduitRunInfo.ConduitMaterialTypes.Any(x => c.MaterialType.ToUpper().Contains(x))) {
					type = ConduitRunInfo.ConduitMaterialTypes.ToList()
						.Find(x => c.MaterialType.ToUpper().Contains(x));
				}

				var diameter = RMeasure.LengthFromDbl(info.DOC, c.Diameter);

                var has_item = l.GetItem(out var li, (double)c.Length, "Conduit", type, diameter);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                var con_name = c.MaterialType + " - " + diameter + " Dia.";
                InsertIntoRow(con_name, li.Quantity, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			}
            #endregion

			code_one_gt += gt;
			InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Unistrut Labor
			if(ut.Unistrut.Any()) InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Unistrut", 15);

			ut.Unistrut.ForEach(us => {
                int len = (int)Math.Round(us.Length);
                var has_item = l.GetItem(out var li, (double)len, us.Unistrut.Name, us.Unistrut.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                string us_name = us.Unistrut.Name + " - " + us.Unistrut.Size;
                InsertIntoRow(us_name, li.Quantity, li.PerUnitLabor, "Feet", fdbl(li.TotalLaborValue));
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.ConduitStraps.ForEach(cs => {
                var has_item = l.GetItem(out var li, (double)cs.Count, UnistrutConduitStrap.Name, cs.Strap.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.ToggleBolts.ForEach(tb => {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.Washers.ForEach(w => {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.SheetMetalScrews.ForEach(s => {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.MachineScrew.ForEach(m => {
                var has_item = l.GetItem(out var li, (double)m.Count, m.Screw.Name, m.Screw.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.Anchors.ForEach(a => {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.ChannelNuts.ForEach(c => {
                var has_item = l.GetItem(out var li, (double)c.Count, c.Nut.Name, c.Nut.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.PlateFittings.ForEach(p => {
                var has_item = l.GetItem(out var li, (double)p.Count, p.Fitting.Name, p.Fitting.Type + " Type");
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			ut.PostBases.ForEach(p => {
                var has_item = l.GetItem(out var li, (double)p.Count, p.Base.Name);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});
            #endregion

			code_one_gt += gt;
			if(ut.Unistrut.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Ground Bar Labor
			if(gbt.GroundBars.Any()) InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Ground Bar", 15);

			//Ground Bar
			gbt.GroundBars.ForEach(gb => {
                var has_item = l.GetItem(out var li, (double)gb.Count, gb.Bar.Name);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                string gb_name = gb.Bar.Name + " - (" + gb.Bar.GetDimensions(info) + ")";
                InsertIntoRow(gb_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			gbt.Lugs.ForEach(ll => {
                var has_item = l.GetItem(out var li, (double)ll.Count, GrdBarLug.Name, ll.Lug.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			gbt.ToggleBolts.ForEach(tb => {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			gbt.Washers.ForEach(w => {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			gbt.SheetMetalScrews.ForEach(s => {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			gbt.Anchors.ForEach(a => {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});
            #endregion

			code_one_gt += gt;
			if(gbt.GroundBars.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Panel Backing Labor
			if(pbt.PanelBackingFootage != 0.0)
				InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Panel Backing", 15);

			if(pbt.PanelBackingFootage > 0.0)
			{
				var len = (int)Math.Round(pbt.PanelBackingFootage / 4.0); // per 4 ft
                var has_item = l.GetItem(out var li, (double)len, PanelBacking.Name);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                string pb_name = PanelBacking.Name + " - (4 Ft. Segments)";
                InsertIntoRow(pb_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			}

			pbt.ToggleBolts.ForEach(tb => {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbt.Washers.ForEach(w => {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbt.SheetMetalScrews.ForEach(s => {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbt.Anchors.ForEach(a => {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});
            #endregion

			code_one_gt += gt;
			if(pbt.PanelBackingFootage != 0.0) InsertGrandTotal("Sub Total", ref gt, true, false, true);
            
            #region Panelboard Labor
			if(pbrdt.Panelboards.Any()) InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Panelboards", 15);

			pbrdt.Panelboards.ForEach(pbrd => {
                var namearr = int.Parse(pbrd.Board.Amperage) > 0 ? 
                    new[] { pbrd.Board.Name, pbrd.Board.Amperage + "A" } : new[] { "Panelboard Can", "No Amps" };

                var has_item = l.GetItem(out var li, (double)pbrd.Count, namearr[0], namearr[1]);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
				var amperage_print = int.Parse(pbrd.Board.Amperage) > 0 ? " - " + pbrd.Board.Amperage + "A" : "";
                var pname = pbrd.Board.Name + " - " + pbrd.Board.PanelName + amperage_print;
                InsertIntoRow(pname, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbrdt.ToggleBolts.ForEach(tb => {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbrdt.Washers.ForEach(w => {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbrdt.SheetMetalScrews.ForEach(s => {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});

			pbrdt.Anchors.ForEach(a => {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if(!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
			});
            #endregion

			code_one_gt += gt;
			if(pbrdt.Panelboards.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);
            
			InsertGrandTotal("Code ?? | Elec Room | Grand Total", ref code_one_gt, false, false, false);
			code_one_gt *= 0.82;
			InsertGrandTotal("Code ?? w/ 0.82 Labor Factor", ref code_one_gt, true, false, true);

            
            // format the sheet
            FormatExcelSheet(0.1M);
            MakeFooter();
            ChangeColumnAlignment(4, new char[] {'A', 'E'}, ExcelHorizontalAlignment.Left);
            this['D', 'D'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ChangeColumnWidth('A', 50);

            HasData = true;
        }

        /// <summary>
		/// Export a Conduit And Wire Only sheet
		/// </summary>
        public void GenerateConduitAndWireOnlySheet(ModelInfo info, string project_file_name, MasterDataPackage data_package) {

            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Conduit And Wire Only", project_file_name);

			foreach(var run in package.Cris.OrderBy(x => x.From).ThenBy(y => y.To).ThenBy(z => z.ConduitMaterialType).ToList())
			{
				var len = RMeasure.LengthFromDbl(info.DOC, run.Length);
				var ws = run.GetRevitWireSizeString(info);
				ws = ws.Equals("") ? "---" : ws;
				InsertIntoRow(run.From, run.To, len, ws, run.DiameterStr(info.DOC), run.GetSets(info), run.ConduitMaterialType);
				NextRow(1);
			}

            FormatExcelSheet(0.1M);
            MakeFooter();
            HasData = true;
        }

        /// <summary>
		/// Export a Conduit Only sheet
		/// </summary>
        public void GenerateConduitOnlySheet(ModelInfo info, string project_file_name, MasterDataPackage data_package) {

            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Conduit Only", project_file_name);

            var group_mat_types = data_package.Cris
                .GroupBy(x => new { Mat = x.ConduitMaterialType, Diameter = RMeasure.LengthFromDbl(info.DOC, x.Diameter) } )
                .Select(x => new { Length = RMeasure.LengthFromDbl(info.DOC, x.Sum(x => x.Length)), Diameter = x.Key.Diameter, Material = x.Key.Mat })
                .ToList();

            foreach(var g in group_mat_types) {
                InsertIntoRow(g.Length, g.Diameter, g.Material);
                NextRow(1);
            }

            FormatExcelSheet(0.1M);
            ChangeColumnAlignment(4, new char[] {'A', 'A'}, ExcelHorizontalAlignment.Right);
            ChangeColumnAlignment(4, new char[] {'B', 'B'}, ExcelHorizontalAlignment.Center);
            ChangeColumnAlignment(4, new char[] {'C', 'C'}, ExcelHorizontalAlignment.Right);

            HasData = true;
        }

        /// <summary>
		/// Export a Legacy P3 In Wall sheet
		/// </summary>
        public void GenerateLegacyP3InWallSheet(
            ModelInfo info, string project_file_name, 
            MasterDataPackage data_package, IEnumerable<P3PartCollection> colls) {

            if(HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - P3 In Wall";

            InsertHeader(title, "", project_file_name);

            var code_one_gt = 0.0;
			var code_one_sub = 0.0;
            static double shave_labor(double labor) => labor * 0.82;

            //colls = colls.OrderBy(x => x.DeviceCode).ToList();
            var field_hardware = P3PartTotal.GetPartTotals(colls, P3PartCategory.Hardware, P3PartCategory.Clip);
            var per_box_items = P3PartCollection.GetPartTotalsByCategory(colls, P3PartCategory.Box, P3PartCategory.Bracket, P3PartCategory.Plaster_Ring, P3PartCategory.Stinger, P3PartCategory.Connector);
            var item_total = P3PartTotal.GetPartTotals(colls, P3PartCategory.Box, P3PartCategory.Bracket, P3PartCategory.Plaster_Ring, P3PartCategory.Stinger, P3PartCategory.Connector);

            var l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);

            foreach(var t in per_box_items)
			{
				var code = t.DeviceCode;
				InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, code);

                foreach(var p in t.Parts)
                {
                    var has_item = l.GetItem(out var li, (double)p.Qty, p.Name);
                    if(!has_item) throw new Exception("No Labor item for: " + p.Name);
                    InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                    code_one_sub += li.TotalLaborValue; NextRow(1);
                }
                
				code_one_sub = Math.Ceiling(code_one_sub);
				code_one_gt += code_one_sub;
				InsertGrandTotal("Sub Total", ref code_one_sub, true, false, true);
				code_one_sub = 0.0;
			}

			InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Fixture Item Totals");

            foreach(var t in item_total.Parts)
            {
                var has_item = l.GetItem(out var li, (double)t.Qty, t.Name);
                if(!has_item) throw new Exception("No Labor item for: " + t.Name);
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                NextRow(1);
            }

            NextRow(1);
			InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Field Labor Hardware Items");

			foreach(var part in field_hardware.Parts)
			{
                var has_item = l.GetItem(out var li, (double)part.Qty, part.Name);
                if(!has_item) throw new Exception("No Labor item for: " + part.Name);
				InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
				code_one_sub += li.TotalLaborValue; NextRow(1);
			}

			code_one_gt += code_one_sub;
			code_one_sub = Math.Ceiling(code_one_sub);
			InsertGrandTotal("Sub Total", ref code_one_sub, true, false, true);
			code_one_sub = 0.0;

			InsertGrandTotal("Code 01 | Empty Raceway | Grand Total", ref code_one_gt, false, false, false);
			code_one_gt = shave_labor(code_one_gt);
			InsertGrandTotal("Code 01 w/ 0.82 Labor Factor", ref code_one_gt, true, false, true);

            FormatExcelSheet(0.1M);
            MakeFooter();

            // debugger.show(err:PrintRowCrawlGraph());
            HasData = true;
        }
    }
}