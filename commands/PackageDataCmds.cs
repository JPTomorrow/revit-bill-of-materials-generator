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
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        private void ResetSubPackageComboBoxes()
        { 
            
        }

        // new package
        public void NewPackage(Window window)
        {
            try
            {
                var result = System.Windows.MessageBox.Show("Any unsaved data on your current project will be lost. Do you want to continue??", "New Project", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if(result.Equals(MessageBoxResult.No)) return;

                // open save file dialog
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "New Project File Location";
                sfd.Filter = "BOM files (*.bom)|*.bom";
                var save_result = sfd.ShowDialog();
                if(save_result != DialogResult.OK && save_result != DialogResult.Yes) return;

                ALS.AppData = new MasterDataPackage();
                RefreshDataGrids(BOMDataGrid.All);

                //save project anew
                string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";
                File.WriteAllText(path_file, string.Empty);

                ALS.AppData.SavePackageToLocation(sfd.FileName);

                using(StreamWriter sw = new StreamWriter(path_file)) {
                    sw.WriteLine(sfd.FileName);
                }

                packagePath = sfd.FileName;


                Header_Text = "Project: " + PackageName();
                Update("Header_Text");

                WriteToLog("New project file saved at" + sfd.FileName);


            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Load a master Package UI Cmd
        /// </summary>
        public void LoadPackage(Window window)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "BOM files (*.bom)|*.bom";
                var result = ofd.ShowDialog();
                if(result == DialogResult.OK || result == DialogResult.Yes)
                {
                    string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";
                    File.WriteAllText(path_file, string.Empty);

                    using(StreamWriter sw = new StreamWriter(path_file))
                    {
                        sw.WriteLine(ofd.FileName);
                    }

                    ALS.HangerOptions = HangerOptions.Load(ALS.Info);

                    packagePath = ofd.FileName;
                    LoadMasterPackage(packagePath);

                    //header texts
                    Wire_Items.Clear();
                    RefreshDataGrids(BOMDataGrid.All);

                    Header_Text = "Project: " + PackageName();
                    Update("Header_Text");
                    WriteToLog("Project file loaded from " + packagePath);
                }
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Save the current package
        /// </summary>
        public void SavePackage(Window window)
        {
            try
            {
                if(!packagePath.Equals("Untitled"))
                {
                    ALS.AppData.SavePackageToLocation(packagePath);
                    WriteToLog("saved at: " + packagePath);
                }
                else
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "BOM files (*.bom)|*.bom";
                    var result = sfd.ShowDialog();
                    if(result == DialogResult.OK || result == DialogResult.Yes)
                    {
                        string path_file = ModelInfo.GetDataDirectory("master_package", true) + "package_path.txt";
                        File.WriteAllText(path_file, string.Empty);
                        
                        ALS.AppData.SavePackageToLocation(sfd.FileName);

                        using(StreamWriter sw = new StreamWriter(path_file)) {
                            sw.WriteLine(sfd.FileName);
                        }

                        packagePath = sfd.FileName;
                        Header_Text = "Project: " + PackageName();
                        Update("Header_Text");

                        WriteToLog("package saved at: " + sfd.FileName);
                    }
                }
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
	}
}