

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
                if (string.IsNullOrWhiteSpace(new_package_name)) return;

                var s = ALS.AppData.AddNewAllSubPackage(new_package_name);

                if (!s)
                {
                    debugger.show(err: "Package name already exists. Cancelling creation of new package.");
                    return;
                }

                bool ss = ALS.AppData.SelectAllPackage(new_package_name);

                if (!ss)
                {
                    debugger.show(err: "Failed to select package..");
                    return;
                }

                ConduitPackageNameItems.Add(new_package_name);
                HangerPackageNameItems.Add(new_package_name);
                HardwarePackageNameItems.Add(new_package_name);
                GlobalSettingsPackageNameItems.Add(new_package_name);

                UpdateComboboxPackageNames(new_package_name);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add New Sub Package", err: ex.Message);
            }
        }

        public void ConduitSubPackageSelectionChanged(Window window)
        {
            try
            {
                UpdateSubPackageText();
                var package_name = ConduitPackageNameItems[SelConduitPackage];
                UpdateComboboxPackageNames(package_name);
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Conduit Sub Package Selection Changed", err: ex.Message);
            }
        }

        public void HangerSubPackageSelectionChanged(Window window)
        {
            try
            {
                UpdateSubPackageText();
                var package_name = HangerPackageNameItems[SelHangerPackage];
                UpdateComboboxPackageNames(package_name);
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Hanger Sub Package Selection Changed", err: ex.Message);
            }
        }

        public void HardwareSubPackageSelectionChanged(Window window)
        {
            try
            {
                UpdateSubPackageText();
                var package_name = HardwarePackageNameItems[SelHardwarePackage];
                UpdateComboboxPackageNames(package_name);
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Hardware Sub Package Selection Changed", err: ex.Message);
            }
        }


        public void GlobalSettingsSubPackageSelectionChanged(Window window)
        {
            try
            {
                UpdateSubPackageText();
                var package_name = GlobalSettingsPackageNameItems[SelGlobalSettingsPackage];
                UpdateComboboxPackageNames(package_name);
                RefreshDataGrids(BOMDataGrid.All);

                var package = ALS.AppData.GetSelectedGlobalSettingsPackage();
                Export_Title_Txt = package.ExportTitle;
                Area_Title_Txt = package.AreaTitle;

                Update("Export_Title_Txt");
                Update("Area_Title_Txt");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Global Settings Sub Package Selection Changed", err: ex.Message);
            }
        }

        public void UpdateSubPackageText()
        {
            Update("SelConduitPackage");
            Update("ConduitPackageNameItems");
            Update("SelGlobalSettingsPackage");
            Update("SelHangerPackage");
            Update("HangerPackageNameItems");
            Update("SelHardwarePackage");
        }

        public void UpdateComboboxPackageNames(string package_name)
        {
            ALS.AppData.SelectConduitPackage(package_name, out _);
            ALS.AppData.SelectHangerPackage(package_name, out _);
            ALS.AppData.SelectHardwarePackage(package_name, out _);
            ALS.AppData.SelectGlobalSettingsPackage(package_name, out _);

            SelConduitPackage = ALS.AppData.SelectedConduitPackageIdx;
            SelHangerPackage = ALS.AppData.SelectedHangerPackageIdx;
            SelHardwarePackage = ALS.AppData.SelectedHardwarePackageIdx;
            SelGlobalSettingsPackage = ALS.AppData.SelectedGlobalSettingsPackageIdx;

            Update("SelConduitPackage");
            Update("SelHangerPackage");
            Update("SelHardwarePackage");
            Update("SelGlobalSettingsPackage");
        }

        public void RemoveGlobalSettingsSubPackage(Window window)
        {
            return;
        }

        public void RemoveHardwareSubPackage(Window window)
        {
            return;
        }

        public void RemoveConduitSubPackage(Window window)
        {
            return;
        }

        public void RemoveHangerSubPackage(Window window)
        {
            return;
        }
    }
}