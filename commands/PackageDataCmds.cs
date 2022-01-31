using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.Windows.IO;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {

        // new package
        public void NewPackage(Window window)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Any unsaved data on your current project will be lost. Do you want to continue??",
                    "New Project", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result.Equals(MessageBoxResult.No)) return;

                // open save file dialog
                var save_result = SaveFileSelection.Prompt("New Project File Location", "bom");
                if (!save_result.IsResult(DialogResult.OK, DialogResult.Yes)) return;

                ALS.AppData = new MasterDataPackage();
                LaborExchange ex = new LaborExchange(ModelInfo.SettingsBasePath);
                string lfn = LaborExchange.DefaultLaborFilePath;
                ALS.AppData.LaborHourEntries.AddRange(LaborExchange.LoadLaborFromFile(lfn));

                RefreshDataGrids(BOMDataGrid.All);

                //save project anew
                string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";
                File.WriteAllText(path_file, string.Empty);

                ALS.AppData.SavePackageToLocation(save_result.Filename);

                using (StreamWriter sw = new StreamWriter(path_file))
                {
                    sw.WriteLine(save_result.Filename);
                }

                packagePath = save_result.Filename;
                ResolveStartupExportDirectory(packagePath);
                Update("Export_Root_Directory_Txt");
                Header_Text = "Project: " + PackageName();
                Update("Header_Text");
                UpdateSubPackages();
                WriteToLog("New project file saved at" + save_result.Filename);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// Load a master Package UI Cmd
        /// </summary>
        public void LoadPackage(Window window)
        {
            try
            {
                var result = OpenFileSelection.Prompt("Choose BOM File to Load", "bom");
                if (!result.IsResult(DialogResult.OK, DialogResult.Yes)) return;

                string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";
                File.WriteAllText(path_file, string.Empty);

                using (StreamWriter sw = new StreamWriter(path_file))
                {
                    sw.WriteLine(result.Filename);
                }

                packagePath = result.Filename;
                LoadMasterPackage(packagePath);

                //header texts
                Wire_Items.Clear();
                RefreshDataGrids(BOMDataGrid.All);

                Header_Text = "Project: " + PackageName();
                Update("Header_Text");
                WriteToLog("Project file loaded from " + packagePath);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// Save the current package
        /// </summary>
        public void SavePackage(Window window)
        {
            try
            {
                if (!packagePath.Equals("Untitled"))
                {
                    ALS.AppData.SavePackageToLocation(packagePath);
                    WriteToLog("saved at: " + packagePath);
                }
                else
                {
                    var result = SaveFileSelection.Prompt("Choose Save Location", "bom");
                    if (!result.IsResult(DialogResult.OK, DialogResult.Yes)) return;
                    string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";
                    File.WriteAllText(path_file, string.Empty);

                    ALS.AppData.SavePackageToLocation(result.Filename);

                    using (StreamWriter sw = new StreamWriter(path_file))
                    {
                        sw.WriteLine(result.Filename);
                    }

                    packagePath = result.Filename;
                    Header_Text = "Project: " + PackageName();
                    Update("Header_Text");

                    WriteToLog("package saved at: " + result.Filename);
                }
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }
    }
}