

using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using JPMorrow.Data.Globals;
using JPMorrow.Tools.Diagnostics;
using Draw = System.Drawing;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        private string PromptNewPackageName()
        {
            Form input_form = new Form()
            {
                Size = new Draw.Size(350, 225),

            };

            Label desc = new Label()
            {
                Location = new Draw.Point(10, 10),
                Text = "Please enter a new package name"
            };
            desc.Width = 300;

            TextBox input_box = new TextBox()
            {
                Location = new Draw.Point(10, 50),
            };
            input_box.Width = 300;

            Button ok = new Button()
            {
                Location = new Draw.Point(10, 100),
                DialogResult = DialogResult.OK,
                Text = "OK",
            };
            ok.Width = 150;
            ok.Height = 50;

            input_form.Controls.Add(desc);
            input_form.Controls.Add(input_box);
            input_form.Controls.Add(ok);

            string new_package_name = string.Empty;

            if (input_form.ShowDialog() == DialogResult.OK)
            {
                new_package_name = input_box.Text;
            }

            input_form.Dispose();
            return new_package_name;
        }

        public void AddNewConduitSubPackage(System.Windows.Controls.ComboBox box)
        {
            var new_package_name = PromptNewPackageName();
            if(string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewConduitSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err:"Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectConduitPackage(new_package_name, out var package);

            if(!ss)
            {
                debugger.show(err:"Failed to select package..");
                return;
            }

            ConduitPackageNameItems.Add(package.PackageName);
            SelConduitPackage = ALS.AppData.SelectedConduitPackageIdx;
            Update("SelConduitPackage");
        }

        public void RemoveConduitSubPackage(Window window)
        { 

        }

        public void ConduitSubPackageSelectionChanged(Window window)
        {
            try
            {
                Update("SelConduitPackage");
                var package_name = ConduitPackageNameItems[SelConduitPackage];
                if(string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectConduitPackage(package_name, out var package);
                RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Hangers
        /// </summary>
        
        public void AddNewHangerSubPackage(System.Windows.Controls.ComboBox box)
        {
            var new_package_name = PromptNewPackageName();
            if(string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewHangerSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err:"Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectHangerPackage(new_package_name, out var package);

            if(!ss)
            {
                debugger.show(err:"Failed to select package..");
                return;
            }

            HangerPackageNameItems.Add(package.PackageName);
            SelHangerPackage = ALS.AppData.SelectedHangerPackageIdx;
            Update("SelHangerPackage");
        }

        public void RemoveHangerSubPackage(Window window)
        { 

        }

        public void HangerSubPackageSelectionChanged(Window window)
        {
            try
            {
                Update("SelHangerPackage");
                var package_name = HangerPackageNameItems[SelHangerPackage];
                if(string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectHangerPackage(package_name, out var package);
                RefreshDataGrids(BOMDataGrid.Hangers);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Hardware
        /// </summary>
        
        public void AddNewHardwareSubPackage(System.Windows.Controls.ComboBox box)
        {
            var new_package_name = PromptNewPackageName();
            if(string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewHardwareSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err:"Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectHardwarePackage(new_package_name, out var package);

            if(!ss)
            {
                debugger.show(err:"Failed to select package..");
                return;
            }

            HardwarePackageNameItems.Add(package.PackageName);
            SelHardwarePackage = ALS.AppData.SelectedHardwarePackageIdx;
            Update("SelHardwarePackage");
        }

        public void RemoveHardwareSubPackage(Window window)
        { 

        }

        public void HardwareSubPackageSelectionChanged(Window window)
        {
            try
            {
                Update("SelHardwarePackage");
                var package_name = HardwarePackageNameItems[SelHardwarePackage];
                if(string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectHardwarePackage(package_name, out var package);
                RefreshDataGrids(BOMDataGrid.Hardware);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// GlobalSettings
        /// </summary>
        
        public void AddNewGlobalSettingsSubPackage(System.Windows.Controls.ComboBox box)
        {
            var new_package_name = PromptNewPackageName();
            if(string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewGlobalSettingsSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err:"Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectGlobalSettingsPackage(new_package_name, out var package);

            if(!ss)
            {
                debugger.show(err:"Failed to select package..");
                return;
            }

            GlobalSettingsPackageNameItems.Add(package.PackageName);
            SelGlobalSettingsPackage = ALS.AppData.SelectedGlobalSettingsPackageIdx;
            Update("SelGlobalSettingsPackage");
        }

        public void RemoveGlobalSettingsSubPackage(Window window)
        { 

        }

        public void GlobalSettingsSubPackageSelectionChanged(Window window)
        {
            try
            {
                Update("SelGlobalSettingsPackage");
                var package_name = GlobalSettingsPackageNameItems[SelGlobalSettingsPackage];
                if(string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectGlobalSettingsPackage(package_name, out var package);
                Branch_Export_Sheet_Name_Txt = package.BranchExportSheetName;
                Distribution_Export_Sheet_Name_Txt = package.DistributionExportSheetName;
                Low_Voltage_Export_Sheet_Name_Txt = package.LowVoltageExportSheetName;
                Hangers_Export_Sheet_Name_Txt = package.HangerExportSheetName;

                Update("Branch_Export_Sheet_Name_Txt");
                Update("Distribution_Export_Sheet_Name_Txt");
                Update("Low_Voltage_Export_Sheet_Name_Txt");
                Update("Hangers_Export_Sheet_Name_Txt");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
    }
}