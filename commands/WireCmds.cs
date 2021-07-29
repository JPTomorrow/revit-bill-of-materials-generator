using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.WPF.Extensions;
using  Controls = System.Windows.Controls;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        // Add a set of branch wire to the wiremanager
        public void AddBranchWire(Window parent_window)
        {
            try
            {
                var sel_presenters = Selected_Run_Items.Where(x => x.IsSelected).ToList();
                if(!sel_presenters.Any()) return;

                if(String.IsNullOrWhiteSpace(Breaker_Size_Txt))
                {
                    debugger.show(header:"Add Brach Wire", err:"Please enter a breaker size");
                    return;
                }

                // user selections
                var voltage = Panel_Voltage_Items[Sel_Panel_Voltage];
                var stag = Staggered_Circs;
                var boy = BOY_Reverse;
                var ig = IG_Checked;
                var phase_nuet = Phase_Nuet_Checked;
                var wire_mat_type = Branch_Wire_Material_Items[Sel_Branch_Wire_Material];
                var branch_panel_phase = Branch_Panel_Phase_Items[Sel_Branch_Panel_Phase];

                var breaker_sizes = Breaker_Size_Txt.Split(',').ToList();

                foreach(var run in sel_presenters.Select(x => x.Value))
                {
                    var type = WireManager.ParseCircuitString(run.To, out int[] c);
                    if(c == null || !c.Any()) continue;

                    var ids = run.WireIds;

                    while(breaker_sizes.Count() < c.Count()) {
                        breaker_sizes.Add(breaker_sizes.First());
                    }

                    var bs_dict = c.Zip(breaker_sizes, (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);

                    foreach(var kvp in bs_dict) {
                        Wire.GetWireSizeFromBreakerSize(kvp.Value, out string wire_size);

                        WireCreationData wire_data = new WireCreationData(
                            wire_size, wire_size, wire_size, stag, boy, 
                            phase_nuet, ig, wire_mat_type, branch_panel_phase);

                        ALS.AppData.WireManager.StoreBranchWire(ids, voltage, new[] { kvp.Key }, wire_data);
                    }
                }

                List<Wire> display_wire = new List<Wire>();
                Selected_Run_Items.Select(x => x.Value).ToList()
                    .ForEach(x => display_wire.AddRange(ALS.AppData.WireManager.GetWires(x.WireIds)));

                Wire_Items.Clear();
                display_wire.ForEach(x => Wire_Items.Add(new WirePresenter(x, ALS.Info)));
                RefreshDataGrids(BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
                WriteToLog("Added branch wire.");
            }
            catch(Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        // Add a set of distribution wire to the wiremanager
        public void AddDistributionWire(Window parent_window)
        {
            try
            {
                var sel_presenters = Selected_Run_Items.Where(x => x.IsSelected).ToList();
                if(!sel_presenters.Any()) return;

                // user selections
                var voltage = Panel_Voltage_Items[Sel_Panel_Voltage];
                var dist_color = Dist_Wire_Color_Items[Sel_Dist_Wire_Color];
                var dist_size = Dist_Wire_Size_Items[Sel_Dist_Wire_Size];
                var wire_type = Treat_Dist_As_Branch ? WireType.Branch : WireType.Distribution;
                var wire_mat_type = Dist_Wire_Material_Items[Sel_Dist_Wire_Material];

                if(Wire.LowVoltageWireNames.Any(x => x.Equals(dist_size))) {
                    dist_color = WireColor.GetLowVoltageWireColor(dist_size);
                    wire_type = WireType.LowVoltage;
                }

                // make wires per run
                foreach(var run in sel_presenters.Select(x => x.Value))
                {
                    var ids = run.WireIds;

                    var wire = new Wire(
                        run.To, dist_size, dist_color, wire_type, 
                        WireCreationData.GetMaterialTypeFromString(wire_mat_type));

                    ALS.AppData.WireManager.StoreDistWire(ids, wire);
                }

                List<Wire> display_wire = new List<Wire>();
                Selected_Run_Items.Select(x => x.Value).ToList()
                    .ForEach(x => display_wire.AddRange(ALS.AppData.WireManager.GetWires(x.WireIds)));

                Wire_Items.Clear();
                display_wire.ForEach(x => Wire_Items.Add(new WirePresenter(x, ALS.Info)));
                RefreshDataGrids(BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
                WriteToLog("Added distribution wire.");
            }
            catch(Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        // Add a set of distribution wire to the wiremanager
        public void AddLowVoltageWire(Window parent_window)
        {
            try
            {
                var sel_presenters = Selected_Run_Items.Where(x => x.IsSelected).ToList();
                if(!sel_presenters.Any()) return;

                // user selections
                var size = Low_Voltage_Wire_Size_Items[Sel_Low_Voltage_Wire_Size];
                var color = WireColor.GetLowVoltageWireColor(size);
                var wire_type = WireType.LowVoltage;

                // make wires per run
                foreach(var run in sel_presenters.Select(x => x.Value)) {
                    var ids = run.WireIds;
                    var wire = new Wire(run.To, size, color, wire_type, WireMaterialType.Special);
                    ALS.AppData.WireManager.StoreDistWire(ids, wire);
                }

                List<Wire> display_wire = new List<Wire>();
                Selected_Run_Items.Select(x => x.Value).ToList()
                    .ForEach(x => display_wire.AddRange(ALS.AppData.WireManager.GetWires(x.WireIds)));

                Wire_Items.Clear();
                display_wire.ForEach(x => Wire_Items.Add(new WirePresenter(x, ALS.Info)));
                RefreshDataGrids(BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
                WriteToLog("Added low voltage wire.");
            }
            catch(Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        public void MarkConduitNoWireExport(Window window) {

            var runs = Run_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();

            if (!runs.Any()) return;

            WireType type = Wire.GetWireType(Export_Type_Items[Sel_Export_Type]);

            bool unmark(int[] ids) => 
                ALS.AppData.WireManager.GetWires(ids)
                .Any(x => x.IsNoWireExport(type));

            foreach(var run in runs)
            {
                var ids = run.WireIds;

                if(unmark(ids)) {
                    ALS.AppData.WireManager.RemoveWires(ids);
                }
                else {
                    ALS.AppData.WireManager.RemoveWires(ids);
                    var wire = new Wire("NO WIRE EXPORT", "NO WIRE EXPORT", "NO WIRE EXPORT", type, WireMaterialType.Special);
                    ALS.AppData.WireManager.StoreDistWire(ids, wire);
                }
            }
            
            RefreshDataGrids(BOMDataGrid.Runs);
        }
        
        // add the wire that is reported in the wire size parameter
        public void AddReportedWires(Window window)
        {
            var selected_pres = Selected_Run_Items;

            if(!selected_pres.Any())
            {
                debugger.show(
                    header:"Add Reported Wire", 
                    err:"You must select some conduit runs to add their reported wire");
                return;
            }

            ReportableWireSizes wire_sizes = new ReportableWireSizes(Wire.WireSizes);
            string panel_voltage = Reported_Wire_Panel_Voltage_Items[Sel_Reported_Wire_Panel_Voltage];

            foreach(var presenter in selected_pres)
            {
                var rep_wire_str = presenter.Value.ReportedWireSizes;
                ReportedWireCollection wires = ReportedWireConverter
                    .GetWireFromReportedWires(wire_sizes, rep_wire_str, panel_voltage);
                

            }
        }

        // remove selected wire from the wiremanager
        public void RemoveWire(Window window)
        {
            var sel_presenters = Selected_Run_Items.ToList();
            if(!sel_presenters.Any()) return;

            var sel_wires = Wire_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();
            if(!sel_wires.Any()) return;

            List<Wire> display_wire = new List<Wire>();

            foreach(var run in sel_presenters.Select(x => x.Value))
            {
                var ids = run.WireIds;

                foreach(var wire in sel_wires) {
                    if(ALS.AppData.WireManager.ContainsWire(ids, wire))
                        ALS.AppData.WireManager.RemoveWire(ids, wire, out int rem);
                }
                    

                display_wire.AddRange(ALS.AppData.WireManager.GetWires(ids));
            }

            Wire_Items.Clear();
            display_wire.ForEach(x => Wire_Items.Add(new WirePresenter(x, ALS.Info)));
            RefreshDataGrids(BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            WriteToLog("Removed selected wire.");
        }

        private MasterDataPackage OtherProject = null;
        public void LoadMigrateProject(Window window)
        {
            try
            {
                // select migrant project
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Project Files|*.bom;";
                ofd.Title = "Select Project File";
                DialogResult orr = ofd.ShowDialog();

                if (orr != DialogResult.OK)
                {
                    debugger.show(header:"Migrate Project", err:"A valid project file was not selected");
                    return;
                }

                string txt_filename = ofd.FileName;
                OtherProject = new MasterDataPackage();
                OtherProject.LoadPackageFromLocation(txt_filename);
                var cris = OtherProject.Cris;
                Migrate_Run_Items.Clear();
                cris.ForEach(x => {
                    var wire_cnt = OtherProject.WireManager.GetWires(x.WireIds).Count();
                    if(wire_cnt > 0) Migrate_Run_Items.Add(new MigrantRunPresenter(x, ALS.Info));
                });
                Update("Migrate_Run_Items");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.Message);
            }
        }

        public void MigrateWire(Window window)
        {
            try
            {
                if(OtherProject == null) return;

                var current_sel = Run_Items.Where(x => x.IsSelected).Select(x => x.Value);
                var other_sel = Migrate_Run_Items.Where(x => x.IsSelected).Select(x => x.Value);

                if(!current_sel.Any() || !other_sel.Any()) return;

                var current_run = current_sel.First();
                var other_run = other_sel.First();

                var other_wires = OtherProject.WireManager.GetWires(other_run.WireIds);

                if(!other_wires.Any())
                    debugger.show(header:"Migrate Wire", err:"The other project run has no wire to migrate.");

                ALS.AppData.WireManager.AddWires(current_run.WireIds, other_wires);

                debugger.show( 
                    header:"Migrate Wire", 
                    err:"The following wire has been migrated:\n" + string.Join("\n", other_wires.Select(x => x.ToString())));

                RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.Message);
            }
        }

        public void MigrateWireSelectionChanged(Controls.DataGrid grid)
        {
            try
            {
                if(OtherProject == null) return;

                var current_sel = Run_Items.Where(x => x.IsSelected).Select(x => x.Value);

                if(!current_sel.Any()) return;

                var selected_run = current_sel.First();
                var from = selected_run.From.Trim().ToLower();
                var to = selected_run.To.Trim().ToLower();

                object stored_item = null;
                foreach(var item in grid.Items)
                {
                    var pres = item as MigrantRunPresenter;
                    var pfrom = pres.Value.From.Trim().ToLower();
                    var pto = pres.Value.To.Trim().ToLower();
                    if(pfrom.Equals(from) && pto.Equals(to))
                    {
                        stored_item = item;
                        break;
                    }
                }

                if(stored_item == null) return;
                grid.SelectItem(stored_item);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.Message);
            }
        }


        // flip staggered circuits on the current wire to seq and vica versa
        public void FlipStaggered(Window window)
        {
            throw new NotImplementedException("Flip Staggered Circuits");
        }
	}
}