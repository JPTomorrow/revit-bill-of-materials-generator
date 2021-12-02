using System;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Revit.Wires;
using OfficeOpenXml.Style;
using Draw = System.Drawing;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
		/// Export a total of the wire sheet
		/// </summary>
        public void GenerateWireTotalSheet(ModelInfo info, MasterDataPackage data_package, WireType pull_type)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Wire Pull Totals", data_package.GetSelectedGlobalSettingsPackage().ExportTitle);

            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package, out string changed_wires);

            // total the wire into a flat list
            var wires = WireTotal.GetTotaledWire(package, pull_type).Wires;

            foreach (var w in wires)
            {
                if (wires.IndexOf(w) == 0)
                {
                    InsertSingleDivider(Draw.Color.SlateGray, Draw.Color.White, wires[wires.IndexOf(w)].Size + " - " + w.MaterialTypeText);
                }

                var length = Math.Round(w.Length + package.GetSelectedGlobalSettingsPackage().WireMakeupLength);
                InsertIntoRow(w.Size + " - " + w.MaterialTypeText, w.Color, length);
                ApplyColorToColumn('B', w.Color);
                NextRow(1);

                // if next entry is of a different wire size? insert divider.
                if ((wires.IndexOf(w) + 1) < wires.Count() &&
                    wires[wires.IndexOf(w) + 1].Size != w.Size)
                {

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
    }
}