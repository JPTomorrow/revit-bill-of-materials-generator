using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.ElementDeletion;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.JunctionBox;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Tools;
using JPMorrow.Tools.Diagnostics;
using MainApp;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        public void SingleHangerSelChanged(Window window)
        {
            try
            {
                var single_ids = Single_Hanger_Items
                    .Where(x => x.IsSelected)
                    .Select(x => new ElementId(x.Value.HangerFamilyInstanceId))
                    .ToList();

                ALS.Info.SEL.SetElementIds(single_ids);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Single Hanger Selection Changed", err: ex.Message);
            }
        }

        public void StrutHangerSelChanged(Window window)
        {
            try
            {
                var strut_ids = Strut_Hanger_Items
                .Where(x => x.IsSelected)
                .Select(x => new ElementId(x.Value.HangerFamilyInstanceId))
                .ToList();

                ALS.Info.SEL.SetElementIds(strut_ids);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Strut Hanger Selection Changed", err: ex.Message);
            }
        }

        // add single hangers
        public void AddSingleHangers(Window window)
        {
            try
            {
                List<ElementId> ids = ALS.Info.SEL.GetElementIds().ToList();
                foreach (var id in ids.ToArray())
                    if (ALS.Info.DOC.GetElement(id).Category.Name != "Conduits")
                        ids.Remove(id);

                if (!ids.Any()) return;

                List<SingleHanger> singles = new List<SingleHanger>();

                HangerOptions opts = new HangerOptions()
                {
                    SingleAttType = Single_Att_Type_Items[Sel_Single_Att],
                    RodDiameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Single_Rod_Diameter]),
                    MinRodLength = RMeasure.LengthDbl(ALS.Info.DOC, HO_Single_Min_Rod_Len_Txt),
                    NominalSpacing = RMeasure.LengthDbl(ALS.Info.DOC, Nominal_Hanger_Spacing_Txt),
                    BendSpacing = RMeasure.LengthDbl(ALS.Info.DOC, Bend_Hanger_Spacing_Txt),
                    DrawSingleHangerModelGeometry = Draw_Single_Debug,
                    DrawStrutHangerModelGeometry = Draw_Strut_Debug,
                };

                var hangers = SingleHanger.CreateSingleHangers(
                    ALS.Info, ThisApplication.Hanger_View, ids, opts);

                ALS.AppData.GetSelectedHangerPackage().SingleHangers.AddRange(hangers);

                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Added " + hangers.Count() + " single hangers.");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Single Hangers", err: ex.Message);
            }
        }

        // add fixture hangers
        public void AddFixtureHangers(Window window)
        {
            try
            {
                List<JBoxInfo> jbis = new List<JBoxInfo>();

                // collect jboxes
                //@REFACTOR - this will not work like this in the final version of jbox assemblies
                FilteredElementCollector jbox_coll = new FilteredElementCollector(ALS.Info.DOC, ALS.Info.UIDOC.ActiveView.Id);

                Parameter p(Element x, string str) => x.LookupParameter(str);
                bool p_exists(Element x, string str) => p(x, str) != null;

                List<ElementId> box_ids = jbox_coll
                    .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                    .Where(x =>
                        p_exists(x, "From") &&
                        p_exists(x, "To") &&
                        p_exists(x, "Length") &&
                        p_exists(x, "Width") &&
                        p_exists(x, "Height")
                        ).Select(y => y.Id).ToList();

                // add the boxes to hardware and create hoardware labor entry if it is not already created
                JunctionBoxUtil.MakeFourSquareBoxLaborEntries();
                JunctionBoxUtil.AddFourSquareBoxToHardware(box_ids.Count());

                WriteToLog("Added hardware entry for '4 in. sq. box'");
                WriteToLog("Added labor entry for '4 in. sq. box'");

                HangerOptions opts = new HangerOptions()
                {
                    RodDiameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Single_Rod_Diameter]),
                    MinRodLength = RMeasure.LengthDbl(ALS.Info.DOC, HO_Single_Min_Rod_Len_Txt),
                };

                foreach (var id in box_ids)
                {
                    JBoxInfo jbox_info = RJBI.ParseJunctionBox(id, ALS.Info);

                    var jbox_hanger = FixtureHanger.CreateFixtureHanger(
                        ALS.Info, ThisApplication.Hanger_View, new ElementId(jbox_info.jbox), opts);

                    ALS.AppData.GetSelectedHangerPackage().FixtureHangers.Add(jbox_hanger);
                    jbis.Add(jbox_info);


                }

                RefreshDataGrids(BOMDataGrid.Hangers, BOMDataGrid.Hardware);
                WriteToLog("Added " + ALS.AppData.GetSelectedHangerPackage().FixtureHangers.Count().ToString() + " hangers.");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Fixture Hangers", err: ex.Message);
            }
        }

        // Add strut hangers
        public void AddStrutHangers(Window window)
        {
            try
            {
                List<ElementId> ids = ALS.Info.SEL.GetElementIds().ToList();

                if (!ids.Any())
                {
                    debugger.show(header: "Strut Hangers", err: "No Conduit was selected. Please select a conduit rack.");
                    return;
                }

                List<ElementId> conduit_ids = new List<ElementId>();
                List<ElementId> tray_ids = new List<ElementId>(); // cable tray

                foreach (var id in ids.ToArray())
                {
                    if (ALS.Info.DOC.GetElement(id).Category.Name.Equals("Conduits"))
                        conduit_ids.Add(id);

                    if (ALS.Info.DOC.GetElement(id).Category.Name.Equals("Cable Trays"))
                        tray_ids.Add(id);
                }

                HangerOptions opts = new HangerOptions()
                {
                    MaxStrutGapSpan = RMeasure.LengthDbl(ALS.Info.DOC, HO_Max_Span_Txt),
                    RodDiameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Strut_Rod_Diameter]),
                    InsideRodGap = RMeasure.LengthDbl(ALS.Info.DOC, HO_IRGap_Txt),
                    OutsideRodExtraLength = RMeasure.LengthDbl(ALS.Info.DOC, HO_OESLength_Txt),
                    MinRodLength = RMeasure.LengthDbl(ALS.Info.DOC, HO_Strut_Min_Rod_Len_Txt),
                    NominalSpacing = RMeasure.LengthDbl(ALS.Info.DOC, Nominal_Hanger_Spacing_Txt),
                    BendSpacing = RMeasure.LengthDbl(ALS.Info.DOC, Bend_Hanger_Spacing_Txt),
                    DrawSingleHangerModelGeometry = Draw_Single_Debug,
                    DrawStrutHangerModelGeometry = Draw_Strut_Debug,
                };

                List<StrutHanger> hangers = new List<StrutHanger>();
                if (conduit_ids.Any())
                {
                    var current_hangers = StrutHanger.CreateStrutHangers(
                        ALS.Info, ThisApplication.Hanger_View, conduit_ids, opts, StrutHangerRackType.Conduit, false);
                    if (current_hangers != null) hangers.AddRange(current_hangers);
                }

                if (tray_ids.Any())
                {
                    var current_hangers = StrutHanger.CreateStrutHangers(
                        ALS.Info, ThisApplication.Hanger_View, tray_ids, opts, StrutHangerRackType.CableTray, false);
                    if (current_hangers != null) hangers.AddRange(current_hangers);
                }

                ALS.Info.SEL.SetElementIds(hangers.Select(x => new ElementId(x.HangerFamilyInstanceId)).ToArray());

                ALS.AppData.GetSelectedHangerPackage().StrutHangers.AddRange(hangers);
                var strut_cnt = ALS.AppData.GetSelectedHangerPackage().StrutHangers.Count;
                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Added " + strut_cnt + " strut hangers.");
                UpdateTotalStrutLengthLabel();
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Strut Hangers", err: ex.Message);
            }
        }

        public void AddQuickStrutHangers(Window window)
        {
            try
            {
                // get selected element ids
                List<ElementId> ids = ALS.Info.SEL.GetElementIds().ToList();

                if (!ids.Any())
                {
                    debugger.show(
                        header: "Strut Hangers",
                        err: "No Conduit was selected. Please select a conduit rack.");
                    return;
                }

                // filter out anything selected that isnt conduit
                List<ElementId> conduit_ids = new List<ElementId>();

                foreach (var id in ids.ToArray())
                {
                    if (ALS.Info.DOC.GetElement(id).Category.Name.Equals("Conduits"))
                        conduit_ids.Add(id);
                }

                if (!conduit_ids.Any())
                {
                    debugger.show(
                        header: "Quick Strut Hangers",
                        err: "No Conduit was selected. Please select a conduit rack.");
                    return;
                }

                HangerOptions opts = new HangerOptions()
                {
                    MaxStrutGapSpan = RMeasure.LengthDbl(ALS.Info.DOC, HO_Max_Span_Txt),
                    RodDiameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Strut_Rod_Diameter]),
                    InsideRodGap = RMeasure.LengthDbl(ALS.Info.DOC, HO_IRGap_Txt),
                    OutsideRodExtraLength = RMeasure.LengthDbl(ALS.Info.DOC, HO_OESLength_Txt),
                    MinRodLength = RMeasure.LengthDbl(ALS.Info.DOC, HO_Strut_Min_Rod_Len_Txt),
                    NominalSpacing = RMeasure.LengthDbl(ALS.Info.DOC, Nominal_Hanger_Spacing_Txt),
                    BendSpacing = RMeasure.LengthDbl(ALS.Info.DOC, Bend_Hanger_Spacing_Txt),
                    DrawSingleHangerModelGeometry = Draw_Single_Debug,
                    DrawStrutHangerModelGeometry = Draw_Strut_Debug,
                };

                // create hangers
                List<StrutHanger> hangers = new List<StrutHanger>();
                var current_hangers = StrutHanger.CreateStrutHangers(
                        ALS.Info, ThisApplication.Hanger_View, conduit_ids, opts, StrutHangerRackType.Conduit, true);
                if (current_hangers != null) hangers.AddRange(current_hangers);

                // add hangers to UI
                ALS.Info.SEL.SetElementIds(hangers.Select(x => new ElementId(x.HangerFamilyInstanceId)).ToArray());
                ALS.AppData.GetSelectedHangerPackage().StrutHangers.AddRange(hangers);
                var strut_cnt = ALS.AppData.GetSelectedHangerPackage().StrutHangers.Count;
                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Added " + strut_cnt + " strut hangers.");
                UpdateTotalStrutLengthLabel();
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add Quick Strut Hangers", err: ex.Message);
            }
        }

        // Remove single hangers from model
        public void RemoveSingleHangers(Window window)
        {
            try
            {
                var single_presenters = Single_Hanger_Items.Where(x => x.IsSelected).ToList();
                var fixture_presenters = Fixture_Hanger_Items.Where(x => x.IsSelected).ToList();

                var cnt = single_presenters.Count + fixture_presenters.Count;
                if (cnt <= 0) return;

                var hanger_fam_ids = single_presenters.Select(x => x.Value.HangerFamilyInstanceId).ToList();
                RvtElementDeletion.DeleteRevitElements(ALS.Info, hanger_fam_ids.Select(x => new ElementId(x)));

                if (single_presenters.Any())
                {
                    single_presenters.ForEach(x => Single_Hanger_Items.Remove(x));
                    single_presenters.Select(x => x.Value).ToList().ForEach(x => ALS.AppData.GetSelectedHangerPackage().SingleHangers.Remove(x));
                    WriteToLog("Removed " + single_presenters.Count + " single hangers.");
                }

                if (fixture_presenters.Any())
                {
                    fixture_presenters.ForEach(x => Fixture_Hanger_Items.Remove(x));
                    fixture_presenters.Select(x => x.Value).ToList().ForEach(x => ALS.AppData.GetSelectedHangerPackage().FixtureHangers.Remove(x));
                    WriteToLog("Removed " + fixture_presenters.Count + " fixture hangers.");
                }

                RefreshDataGrids(BOMDataGrid.Hangers);
            }
            catch (Exception ex)
            {
                debugger.show(header: "Remove Single Hangers", err: ex.Message);
            }
        }

        // Remove strut hangers from the model
        public void RemoveStrutHangers(Window window)
        {
            try
            {
                var presenters = Strut_Hanger_Items.Where(x => x.IsSelected).ToList();
                if (!presenters.Any()) return;

                var hanger_fam_ids = presenters.Select(x => x.Value.HangerFamilyInstanceId).ToList();
                RvtElementDeletion.DeleteRevitElements(ALS.Info, hanger_fam_ids.Select(x => new ElementId(x)));
                presenters.ForEach(x => Strut_Hanger_Items.Remove(x));
                presenters.Select(x => x.Value).ToList().ForEach(x => ALS.AppData.GetSelectedHangerPackage().StrutHangers.Remove(x));

                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Removed " + presenters.Count + " Strut hangers.");
                UpdateTotalStrutLengthLabel();
            }
            catch (Exception ex)
            {
                debugger.show(header: "Remove Strut Hangers", err: ex.Message);
            }
        }

        public void UpdateTotalStrutLengthLabel()
        {
            var len = RMeasure.LengthFromDbl(ALS.Info.DOC, ALS.AppData.GetSelectedHangerPackage().StrutHangers.Select(x => x.StrutLength).Sum());
            Total_Unistrut_Hanger_Length_Txt = "Total Unistrut Length: " + len;
        }
    }
}
