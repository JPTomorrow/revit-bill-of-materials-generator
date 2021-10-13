
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Excel;
using JPMorrow.P3;
using JPMorrow.Revit.ElementCollection;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.UI.Views;
using JPMorrow.Windows.IO;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        public void ExportSelection(Window window)
        {
            try
            {

                IntPtr main_rvt_wind = Process.GetCurrentProcess().MainWindowHandle;
                ExportSelectionView esv = new ExportSelectionView(main_rvt_wind);
                esv.ShowDialog();
                if (esv.DialogResult.HasValue && esv.DialogResult.Value) return;

                // get all the export flags from the export selection window
                var export_branch = esv.ExportBranchBox.IsChecked ?? false;
                var export_dist = esv.ExportDistributionBox.IsChecked ?? false;
                var export_lowvoltage = esv.ExportLowVoltageBox.IsChecked ?? false;
                var export_hanger_labor = esv.ExportHangerLaborBox.IsChecked ?? false;
                var export_elecroom = esv.ExportElecRoomBox.IsChecked ?? false;
                var export_legacy_p3_in_wall = esv.ExportLegacyP3InWallBox.IsChecked ?? false;
                var export_conduit_wire_only = esv.ExportConduitAndWireSizeOnlyBox.IsChecked ?? false;
                var export_conduit_only = esv.ExportConduitOnlyBox.IsChecked ?? false;
                var open_excel = esv.OpenExcelBox.IsChecked ?? false;
                var open_pdf = esv.OpenPDFBox.IsChecked ?? false;

                bool conduit_exports_selected = export_branch || export_dist || export_lowvoltage || export_conduit_wire_only || export_conduit_only;
                bool hanger_exports_selected = export_hanger_labor;
                bool other_export_selected = export_elecroom || export_legacy_p3_in_wall;

                if (!ALS.AppData.GetSelectedConduitPackage().Cris.Any() && conduit_exports_selected)
                {
                    debugger.show(
                        header: "Export",
                        err: "There are no conduit runs to export");
                    return;
                }

                if (!conduit_exports_selected && !hanger_exports_selected && !other_export_selected)
                {
                    debugger.show(header: "BOM Export", err: "No exports were selected");
                    return;
                }

                // user selects save file
                var result = SaveFileSelection.Prompt("Select an Excel File for Master BOM Export", "xlsx");

                if (!result.IsResult(DialogResult.OK))
                {
                    debugger.show(header: "BOM Exports", err: "Could not resolve file path, cancelling");
                    return;
                }

                string filename = result.Filename;
                string clean_file_name = System.IO.Path.GetFileNameWithoutExtension(filename);
                clean_file_name = clean_file_name.Replace("_", " ");

                if (!ExcelEngine.PrepExportFile(filename))
                {
                    debugger.show(header: "BOM Export", err: "failed to prep export file");
                    return;
                }

                ExcelEngine exporter = new ExcelEngine(filename);

                #region BOM's
                if (export_branch)
                {
                    debugger.show(err: "test");
                    ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.WirePull);
                    ExcelOutputSheet s2 = new ExcelOutputSheet(ExportStyle.WireTotal);
                    ExcelOutputSheet s3 = new ExcelOutputSheet(ExportStyle.Labor);


                    exporter.RegisterSheets("branch", s1, s2, s3);

                    s1.GenerateWirePullSheet(ALS.Info, ALS.AppData, WireType.Branch);
                    s2.GenerateWireTotalSheet(ALS.Info, ALS.AppData, WireType.Branch);
                    s3.GenerateLaborSheet(ALS.Info, ALS.AppData, WireType.Branch);
                }

                if (export_dist)
                {
                    ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.WirePull);
                    ExcelOutputSheet s2 = new ExcelOutputSheet(ExportStyle.WireTotal);
                    ExcelOutputSheet s3 = new ExcelOutputSheet(ExportStyle.Labor);

                    exporter.RegisterSheets("distribution", s1, s2, s3);

                    s1.GenerateWirePullSheet(ALS.Info, ALS.AppData, WireType.Distribution);
                    s2.GenerateWireTotalSheet(ALS.Info, ALS.AppData, WireType.Distribution);
                    s3.GenerateLaborSheet(ALS.Info, ALS.AppData, WireType.Distribution);
                }

                if (export_lowvoltage)
                {
                    ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.WirePull);
                    ExcelOutputSheet s2 = new ExcelOutputSheet(ExportStyle.WireTotal);
                    ExcelOutputSheet s3 = new ExcelOutputSheet(ExportStyle.Labor);

                    exporter.RegisterSheets("low voltage", s1, s2, s3);

                    s1.GenerateWirePullSheet(ALS.Info, ALS.AppData, WireType.LowVoltage);
                    s2.GenerateWireTotalSheet(ALS.Info, ALS.AppData, WireType.LowVoltage);
                    s3.GenerateLaborSheet(ALS.Info, ALS.AppData, WireType.LowVoltage);
                }

                if (export_hanger_labor)
                {
                    ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.Labor);
                    exporter.RegisterSheets("hanger labor", s1);
                    s1.GenerateHangerLaborBreakdown(ALS.Info, ALS.AppData);
                }

                if (export_elecroom)
                {
                    foreach (var er in ALS.AppData.GetSelectedElecRoomPackage().ElectricalRoomPack.Rooms)
                    {
                        ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.ElecRoom);
                        exporter.RegisterSheets("elec room", s1);
                        s1.GenerateElecRoomSheet(ALS.Info, ALS.AppData, er);
                    }
                }

                if (export_conduit_wire_only)
                {
                    ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.ConduitAndWireOnly);
                    exporter.RegisterSheets("conduit and wire only", s1);
                    s1.GenerateConduitAndWireOnlySheet(ALS.Info, clean_file_name, ALS.AppData);
                }

                if (export_conduit_only)
                {
                    ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.ConduitOnly);
                    exporter.RegisterSheets("conduit only", s1);
                    s1.GenerateConduitOnlySheet(ALS.Info, clean_file_name, ALS.AppData);
                }

                if (export_legacy_p3_in_wall)
                {
                    var dr = debugger.show_yesno(
                        header: "P3 In-Wall Export",
                        err: "You can either run a P3 In-Wall Export on the current revit view, " +
                        "or load a list of devices and quatities from a text file.\n\n",
                        continue_txt: "Would you like to load from a file?");

                    if (dr == DialogResult.Yes)
                    {
                        // user selects save file
                        OpenFileDialog ofd = new OpenFileDialog();
                        ofd.Filter = "Text Files|*.txt;";
                        ofd.Title = "SELECT A TEXT FILE";
                        DialogResult orr = ofd.ShowDialog();

                        if (orr != DialogResult.OK)
                        {
                            debugger.show(header: "Export", err: "A valid text file was not selected");
                            return;
                        }

                        string txt_filename = ofd.FileName;
                        string clean_txt_filename = System.IO.Path.GetFileNameWithoutExtension(txt_filename);
                        var codes = P3InWall.GetDeviceCodeAndQtyFromFile(txt_filename);
                        var parts = P3InWall.GetLegacyDevices(ALS.Info, codes);
                        ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.LegacyP3InWall);
                        exporter.RegisterSheets("P3 in wall", s1);
                        s1.GenerateLegacyP3InWallSheet(ALS.Info, clean_txt_filename, ALS.AppData, parts);
                    }
                    else
                    {
                        // ElementCollection coll = ElementCollector.CollectElementsFromIrregularCropBox(Info, BuiltInCategory.OST_ElectricalFixtures, "BYPASS");
                        ElementCollection coll = ElementCollector.CollectElements(ALS.Info, BuiltInCategory.OST_ElectricalFixtures, false, "BYPASS");
                        var parts = P3InWall.GetLegacyDevices(ALS.Info, coll.Element_Ids);
                        ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.LegacyP3InWall);
                        exporter.RegisterSheets("P3 in wall", s1);
                        s1.GenerateLegacyP3InWallSheet(ALS.Info, clean_file_name, ALS.AppData, parts);
                    }
                }
                #endregion

                exporter.Close();

                // start pdf and excel
                var pdf_filename = filename.Split('.').First() + ".pdf";
                bool pdf_created = exporter.ExportToPdf(pdf_filename);

                if (open_excel) exporter.OpenExcel();
                if (open_pdf && pdf_created) exporter.OpenPDF(pdf_filename);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }
    }
}
