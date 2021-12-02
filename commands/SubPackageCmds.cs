

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using Draw = System.Drawing;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        private string PromptNewPackageName(string description = "Please enter a new package name", string append_first_part = "")
        {
            Form input_form = new Form()
            {
                Size = new Draw.Size(350, 225),

            };

            Label desc = new Label()
            {
                Location = new Draw.Point(10, 10),
                Text = description
            };
            desc.Width = 300;

            TextBox input_box = new TextBox()
            {
                Location = new Draw.Point(10, 50),
                Text = append_first_part
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

        // @TODOD: Make it update view correctly
        public void TransferSelectedConduitToNewSubPackage(Window window)
        {
            try
            {
                var cpack = ALS.AppData.GetSelectedConduitPackage();
                var new_name = PromptNewPackageName(append_first_part: cpack.PackageName);

                var selected_cris = Run_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();
                bool created_csp = ALS.AppData.AddNewConduitSubPackage(new_name);

                if (!created_csp)
                {
                    debugger.show(err: "Package name already exists. Cancelling creation of new package.");
                    return;
                }

                ALS.AppData.SelectConduitPackage(new_name, out ConduitSubDataPackage new_package);
                ConduitPackageNameItems.Add(new_name);

                // transfer wire
                List<Wire> wires = new List<Wire>();
                foreach (var cri in selected_cris)
                {
                    new_package.Cris.Add(cri);
                    var ids = cri.WireIds;
                    var old_wires = cpack.WireManager.GetWires(ids).ToList();
                    new_package.WireManager.AddWires(ids, old_wires);
                }

                selected_cris.ForEach(x => cpack.WireManager.RemoveWires(x.WireIds));
                selected_cris.ForEach(x => cpack.Cris.Remove(x));

                Update("SelConduitPackage");
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Transfer Selected Conduit To New Sub Package", err: ex.Message);
            }
        }

        public void AddNewSubPackage(System.Windows.Controls.ComboBox box)
        {
            try
            {
                var new_package_name = PromptNewPackageName();
                AddNewConduitSubPackage(new_package_name);
                AddNewHangerSubPackage(new_package_name);
                AddNewHangerSubPackage(new_package_name);
                AddNewHardwareSubPackage(new_package_name);
                AddNewGlobalSettingsSubPackage(new_package_name);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add New Sub Package", err: ex.Message);
            }
        }

        private void AddNewConduitSubPackage(string new_package_name)
        {

            if (string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewConduitSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err: "Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectConduitPackage(new_package_name, out var package);

            if (!ss)
            {
                debugger.show(err: "Failed to select package..");
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
                if (string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectConduitPackage(package_name, out var package);
                RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// Hangers
        /// </summary>

        private void AddNewHangerSubPackage(string new_package_name)
        {
            if (string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewHangerSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err: "Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectHangerPackage(new_package_name, out var package);

            if (!ss)
            {
                debugger.show(err: "Failed to select package..");
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
                if (string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectHangerPackage(package_name, out var package);
                RefreshDataGrids(BOMDataGrid.Hangers);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// Hardware
        /// </summary>

        private void AddNewHardwareSubPackage(string new_package_name)
        {
            if (string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewHardwareSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err: "Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectHardwarePackage(new_package_name, out var package);

            if (!ss)
            {
                debugger.show(err: "Failed to select package..");
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
                if (string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectHardwarePackage(package_name, out var package);
                RefreshDataGrids(BOMDataGrid.Hardware);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// GlobalSettings
        /// </summary>

        private void AddNewGlobalSettingsSubPackage(string new_package_name)
        {
            if (string.IsNullOrWhiteSpace(new_package_name)) return;

            bool s = ALS.AppData.AddNewGlobalSettingsSubPackage(new_package_name);

            if (!s)
            {
                debugger.show(err: "Package name already exists. Cancelling creation of new package.");
                return;
            }

            bool ss = ALS.AppData.SelectGlobalSettingsPackage(new_package_name, out var package);

            if (!ss)
            {
                debugger.show(err: "Failed to select package..");
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
                if (string.IsNullOrWhiteSpace(package_name))
                    throw new Exception("The selectd package name does not exist.");

                bool ss = ALS.AppData.SelectGlobalSettingsPackage(package_name, out var package);
                Export_Title_Txt = package.ExportTitle;

                Update("Export_Title_Txt");
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }
    }
}