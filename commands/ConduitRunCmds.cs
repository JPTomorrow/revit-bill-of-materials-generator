using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.ElementCollection;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.UI.Views;
using System.Diagnostics;
using JPMorrow.Revit.Hangers.Internal;
using JPMorrow.Revit.Measurements;
using JPMorrow.Data.Globals;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        public void AddAllRuns(Window window)
        {
            try
            {
                List<ElementId> collected_els = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_Conduit, false, "BYPASS").Element_Ids.ToList();

                FilteredElementCollector coll = new FilteredElementCollector(
                    ALS.Info.DOC, ALS.Info.DOC.ActiveView.Id);

                collected_els.AddRange(coll.OfClass(typeof(FlexPipe)).ToElementIds().ToList());

                if (!collected_els.Any())
                {
                    debugger.show(err: "No conduit in view to process.");
                    return;
                }

                ConduitRunInfo.ProcessCRIFromConduitId(
                    ALS.Info, collected_els, ALS.AppData.GetSelectedConduitPackage().Cris);

                // add low voltage wire automatically if devices are recognized in to parameter
                if (Automate_Wire)
                {
                    LowVoltageDeviceAutomation.AddLowVoltageDeviceWire(
                        ALS.AppData.GetSelectedConduitPackage().Cris,
                        ALS.AppData.GetSelectedConduitPackage().WireManager,
                        ALS.AppData.LowVoltageDevicePairings,
                        ALS.AppData.LowVoltageWirePairings);
                }


                WriteToLog("Runs Processed: " + ALS.AppData.GetSelectedConduitPackage().Cris.Count().ToString());
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        public void AddAllRunsByWorksetAndLevel(Window window)
        {

            try
            {
                // prompt user to select worksets and floors
                IntPtr main_rvt_wind = Process.GetCurrentProcess().MainWindowHandle;
                AddConduitView acv = new AddConduitView(main_rvt_wind, ALS.Info);
                acv.ShowDialog();

                List<ElementId> collected_els = ElementCollector.CollectElementsByFloorAndWorkset(
                    ALS.Info, BuiltInCategory.OST_Conduit, new string[] { }, new string[] { }).Element_Ids.ToList();

                FilteredElementCollector coll = new FilteredElementCollector(
                    ALS.Info.DOC, ALS.Info.DOC.ActiveView.Id);

                collected_els.AddRange(coll.OfClass(typeof(FlexPipe)).ToElementIds().ToList());

                if (!collected_els.Any())
                {
                    debugger.show(err: "No conduit in view to process.");
                    return;
                }

                var old_cris = new List<ConduitRunInfo>(ALS.AppData.GetSelectedConduitPackage().Cris);

                ConduitRunInfo.ProcessCRIFromConduitId(
                    ALS.Info, collected_els, ALS.AppData.GetSelectedConduitPackage().Cris);

                // add low voltage wire automatically if devices are recognized in to parameter
                if (Automate_Wire)
                    LowVoltageDeviceAutomation.AddLowVoltageDeviceWire(
                        ALS.AppData.GetSelectedConduitPackage().Cris,
                        ALS.AppData.GetSelectedConduitPackage().WireManager,
                        ALS.AppData.LowVoltageDevicePairings,
                        ALS.AppData.LowVoltageWirePairings);

                WriteToLog("Runs Processed: " + ALS.AppData.GetSelectedConduitPackage().Cris.Count().ToString());
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        // add a single distribution run
        public void AddSingleRun(Window window)
        {
            try
            {
                var ids = ALS.Info.SEL.GetElementIds();
                ids = ids.ToList().FindAll(x => ALS.Info.DOC.GetElement(x).Category.Name.Equals("Conduits"));

                if (!ids.Any())
                {
                    WriteToLog("No conduit is selected");
                }

                ConduitRunInfo.ProcessCRIFromConduitId(
                    ALS.Info, ids, ALS.AppData.GetSelectedConduitPackage().Cris);

                // add low voltage wire automatically if devices are recognized in to parameter
                if (Automate_Wire)
                    LowVoltageDeviceAutomation.AddLowVoltageDeviceWire(
                        ALS.AppData.GetSelectedConduitPackage().Cris,
                        ALS.AppData.GetSelectedConduitPackage().WireManager,
                        ALS.AppData.LowVoltageDevicePairings,
                        ALS.AppData.LowVoltageWirePairings);

                WriteToLog("Runs Processed: " + ALS.AppData.GetSelectedConduitPackage().Cris.Count().ToString());
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// Update all the currently added runs
        /// </summary>
        public void UpdateRuns(Window window)
        {

            if (!ALS.AppData.GetSelectedConduitPackage().Cris.Any()) return;

            var gen_ids = ALS.AppData.GetSelectedConduitPackage().Cris
                .Select(x => new ElementId(x.ConduitIds.First())).ToList();

            ALS.AppData.GetSelectedConduitPackage().Cris.Clear();
            ConduitRunInfo.ProcessCRIFromConduitId(ALS.Info, gen_ids, ALS.AppData.GetSelectedConduitPackage().Cris);
            RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            WriteToLog("All runs have been updated");
        }

        /// <summary>
        /// Remove selected runs from the run table
        /// </summary>
        public void RemoveSelectedRuns(Window window)
        {
            var presenters = Run_Items.Where(x => x.IsSelected).ToList();

            Selected_Run_Items.Clear();
            Update("Selected_Run_Items");

            if (!presenters.Any()) return;

            presenters.ForEach(x => Run_Items.Remove(x));
            var runs = presenters.Select(x => x.Value).ToList();
            runs.ForEach(x => ALS.AppData.GetSelectedConduitPackage().Cris.Remove(x));

            // clear wire
            runs.ForEach(x => ALS.AppData.GetSelectedConduitPackage().WireManager.RemoveWires(x.WireIds));

            RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
            WriteToLog(presenters.Count().ToString() + " selected runs were removed");
        }

        public void ClearRuns(Window window)
        {

            var cri_cnt = ALS.AppData.GetSelectedConduitPackage().Cris.Count();

            ALS.AppData.GetSelectedConduitPackage().Cris
                .ForEach(x => ALS.AppData.GetSelectedConduitPackage().WireManager.RemoveWires(x.WireIds));

            ALS.AppData.GetSelectedConduitPackage().Cris.Clear();
            Run_Items.Clear();

            WriteToLog("Cleared " + cri_cnt + " conduit runs");
            RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
        }


        /// <summary>
        /// Remove selected runs from the run table
        /// </summary>
        public void SelectRun(Window window)
        {
            var presenters = Run_Items.Where(x => x.IsSelected);

            Selected_Run_Items.Clear();
            presenters.ToList().ForEach(x => Selected_Run_Items.Add(x));
            Update("Selected_Run_Items");

            List<ElementId> selected_runs = new List<ElementId>();

            presenters
                .Select(x => x.Value)
                .ToList().ForEach(x => x.WireIds.Concat(x.FittingIds).ToArray()
                .ToList().ForEach(z => selected_runs
                .Add(new ElementId(z))));

            if (!selected_runs.Any()) return;
            ALS.Info.SEL.SetElementIds(selected_runs);

            List<Wire> wires = new List<Wire>();
            Run_Items.Where(x => x.IsSelected).ToList().ForEach(x =>
                wires.AddRange(ALS.AppData.GetSelectedConduitPackage().WireManager.GetWires(x.Value.WireIds)));

            Wire_Items.Clear();
            wires.ForEach(x => Wire_Items.Add(new WirePresenter(x, ALS.Info)));

            RefreshDataGrids(BOMDataGrid.Wire);
        }

        public void ChangeConduitMaterialType(Window window)
        {

            var runs = Run_Items.Where(x => x.IsSelected).Select(x => x.Value).ToList();

            if (!runs.Any()) return;

            var change_mat = Conduit_Material_Items[Sel_Conduit_Material];
            runs.ForEach(x => x.OverrideMaterialType(change_mat));

            RefreshDataGrids(BOMDataGrid.Runs, BOMDataGrid.SelectedRuns, BOMDataGrid.Wire);
        }

        public void CombineLikeRuns(Window window)
        {
            try
            {
                var presenters = Run_Items.Where(x => x.IsSelected).ToList();

                if (!presenters.Any())
                {
                    debugger.show(header: "Combine Like Runs", err: "Please select runs to combine");
                }

                Selected_Run_Items.Clear();
                Update("Selected_Run_Items");

                /* presenters.ForEach(x => Run_Items.Remove(x));
                var runs = presenters.Select(x => x.Value).ToList();
                runs.ForEach(x => ALS.AppData.Cris.Remove(x));

                // clear wire
                runs.ForEach(x => ALS.AppData.WireManager.RemoveWires(x.WireIds)); */

                foreach (var p in presenters)
                {
                    var new_cri = ConduitRunInfo.CombineRuns(ALS.Info, p.Value, ALS.AppData.GetSelectedConduitPackage().Cris, out var old_cris);
                    if (new_cri == null) continue;

                    // remove old presenters
                    var rem_presenters = Run_Items.Where(x => old_cris.Any(y => y.DeepEquals(x.Value))).ToList();
                    rem_presenters.ForEach(x => Run_Items.Remove(x));
                    var rem_runs = rem_presenters.Select(x => x.Value).ToList();
                    rem_runs.ForEach(x => ALS.AppData.GetSelectedConduitPackage().Cris.Remove(x));

                    ALS.AppData.GetSelectedConduitPackage().Cris.Add(new_cri);
                    Run_Items.Add(new RunPresenter(new_cri, ALS.Info));
                }

                WriteToLog("Combined runs");
                RefreshDataGrids(BOMDataGrid.All);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        public void SelectAllRuns(Window window)
        {
            var presenters = Run_Items.ToList();

            List<ElementId> selected_runs = new List<ElementId>();

            presenters
                .Select(x => x.Value)
                .ToList().ForEach(x => x.WireIds.Concat(x.FittingIds).ToArray()
                .ToList().ForEach(z => selected_runs
                .Add(new ElementId(z))));

            if (!selected_runs.Any()) return;
            ALS.Info.SEL.SetElementIds(selected_runs);
        }

        public void GetConduitLoad(Window window)
        {
            try
            {
                List<ConduitRunInfo> cris = Selected_Run_Items.Select(x => x.Value).ToList();
                if (!cris.Any()) return;

                /* 
                    // get strut line from user selection
                    var ids = Info.SEL.GetElementIds().ToArray();
                    if(!ids.Any()) return;
                    double[] strut_lens = HangerUtil.GetStandardizedStrutLengthFromSelected(Info.DOC, ids);
                */
                var length = RMeasure.LengthDbl(ALS.Info.DOC, Load_Length_Txt);

                var calc = ConduitLoadCalc.CalcSupportLoad(
                    ALS.Info.DOC, cris, ALS.AppData.GetSelectedConduitPackage().WireManager, length);

                if (calc.HasFailedEntries) debugger.show(header: "Failed Load Calcs", err: calc.PrintFailedEntries(ALS.Info.DOC));
                debugger.show(header: "Conduit Load Calc", err: calc.PrintCalcs(ALS.Info.DOC));
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        public void GetStrutLengthFromSelected(Window window)
        {
            try
            {
                var ids = ALS.Info.SEL.GetElementIds().ToArray();
                if (!ids.Any()) return;
                double[] strut_lens = HangerUtil.GetStandardizedStrutLengthFromSelected(ALS.Info.DOC, ids);
                debugger.show(err: strut_lens[0].ToString() + " | " + strut_lens[1].ToString());
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        public void FixToFrom(Window window)
        {
            try
            {
                var cris = ALS.AppData.GetSelectedConduitPackage().Cris
                    .Where(x => x.To.ToLower().Equals("unset") || x.From.ToLower().Equals("unset"));

                ConduitParameter.PushToFromParam(ALS.Info.DOC, cris);
                //ClearRuns(null);
                //AddAllRuns(null);
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }
    }
}

