using System;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Revit.Wires;
using OfficeOpenXml.Style;
using Draw = System.Drawing;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
        /// Export the a per panel breakdown of the wire pull
        /// </summary>
        public void GenerateWirePullSheet(ModelInfo info, MasterDataPackage data_package, WireType pull_type)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            if (pull_type == WireType.Branch)
            {
                InsertHeader(title, "Per-Panel Breakdown", data_package.GetSelectedGlobalSettingsPackage().BranchExportSheetName);
            }
            else if (pull_type == WireType.Distribution)
            {
                InsertHeader(title, "Per-Panel Breakdown", data_package.GetSelectedGlobalSettingsPackage().DistributionExportSheetName);
            }
            else if (pull_type == WireType.LowVoltage)
            {
                InsertHeader(title, "Per-Panel Breakdown", data_package.GetSelectedGlobalSettingsPackage().LowVoltageExportSheetName);
            }

            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            var cris = package.GetSelectedConduitPackage().Cris.OrderBy(x => x.From).ToList();

            // print all the wire by panel
            foreach (ConduitRunInfo cri in cris)
            {

                Wire[] wires = package.GetSelectedConduitPackage().WireManager.GetWires(cri.WireIds.ToArray()).ToArray();
                wires = wires.Where(x => x.WireType == pull_type).ToArray();

                if (!wires.Any() || wires.Any(x => x.IsNoWireExport(pull_type))) continue;

                InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, "Panel: " + cri.From);
                int wpn_merge_start_row = R;

                foreach (var wire in wires)
                {
                    var wire_length = Math.Round(cri.Length + package.GetSelectedGlobalSettingsPackage().WireMakeupLength);
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
    }
}