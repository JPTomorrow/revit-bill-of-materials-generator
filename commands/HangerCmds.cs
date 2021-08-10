using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.ElementDeletion;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Hardware;
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
            var single_ids = Single_Hanger_Items
            .Where(x => x.IsSelected)
            .Select(x => new ElementId(x.Value.HangerFamilyInstanceId))
            .ToList();

            ALS.Info.SEL.SetElementIds(single_ids);
        }

        public void StrutHangerSelChanged(Window window)
        {
            var strut_ids = Strut_Hanger_Items
                .Where(x => x.IsSelected)
                .Select(x => new ElementId(x.Value.HangerFamilyInstanceId))
                .ToList();

            ALS.Info.SEL.SetElementIds(strut_ids);
        }

        // add single hangers
        public async void AddSingleHangers(Window window)
        {
            try
            {
                List<ElementId> ids = ALS.Info.SEL.GetElementIds().ToList();
                foreach (var id in ids.ToArray())
                    if (ALS.Info.DOC.GetElement(id).Category.Name != "Conduits")
                        ids.Remove(id);

                if (!ids.Any()) return;

                List<SingleHanger> singles = new List<SingleHanger>();
                
                var mrl = RMeasure.LengthDbl(ALS.Info.DOC, HO_Single_Min_Rod_Len_Txt);
                var diameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Single_Rod_Diameter]);
                var nominal_spacing = RMeasure.LengthDbl(ALS.Info.DOC, Nominal_Hanger_Spacing_Txt);
                var bend_spacing = RMeasure.LengthDbl(ALS.Info.DOC, Bend_Hanger_Spacing_Txt);
                var att_type = Single_Att_Type_Items[Sel_Single_Att];
                
                HangerOptions opts = new HangerOptions();
                opts.SingleAttType = att_type;
                opts.RodDiameter = diameter;
                opts.MinRodLength = mrl;
                opts.NominalSpacing = nominal_spacing;
                opts.BendSpacing = bend_spacing;
                opts.DrawSingleHangerModelGeometry = Draw_Single_Debug;
                opts.DrawStrutHangerModelGeometry = Draw_Strut_Debug;

                int single_cnt = 0;
                var hangers = await SingleHanger.CreateSingleHangers(
                    ALS.Info, ThisApplication.Hanger_View, ids, opts);
                
                singles.AddRange(hangers);

                ALS.AppData.GetSelectedHangerPackage().SingleHangers.AddRange(singles);
                single_cnt += singles.Count();

                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Added " + single_cnt + " single hangers.");
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        // add fixture hangers
        public void AddFixtureHangers(Window window)
        {
            try
            {
                List<ElementId> jboxes = new List<ElementId>();
                List<JBoxInfo> jbis = new List<JBoxInfo>();

                //@REFACTOR - this will not work like this in the final version of jbox assemblies
                FilteredElementCollector jbox_coll = new FilteredElementCollector(ALS.Info.DOC, ALS.Info.UIDOC.ActiveView.Id);

                Parameter p(Element x, string str) => x.LookupParameter(str);
                bool p_exists(Element x, string str) => p(x, str) != null;

                // collect jboxes
                jboxes = jbox_coll
                    .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                    .Where(x =>
                        p_exists(x, "From") &&
                        p_exists(x, "To") &&
                        p_exists(x, "Length") &&
                        p_exists(x, "Width") &&
                        p_exists(x, "Height")
                        ).Select(y => y.Id).ToList();

                // add junction boxes to hardware if fixture hangers are coming
                if (!ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Any(x => x.name.Equals("4 in. sq. box")))
                {
                    HardwareEntry entry = new HardwareEntry();
                    entry.name = "4 in. sq. box";
                    entry.qty = jboxes.Count();
                    ALS.AppData.GetSelectedHardwarePackage().MiscHardwareEntries.Add(entry);
                    WriteToLog("Added hardware entry for '4 in. sq. box'");
                }

                // make harware entries for jboxes
                if (!ALS.AppData.LaborHourEntries.Any(x => x.EntryName.Equals("4 in. sq. box")))
                {
                    double jbox_labor = 0.23;
                    var ldata = new LaborData(LaborExchange.LetterCodes.GetByLetter('E'), jbox_labor);
                    LaborEntry entry = new LaborEntry("4 in. sq. box", ldata);
                    if (ALS.AppData.LaborHourEntries.Any(x => x.EntryName == entry.EntryName)) return;
                    ALS.AppData.LaborHourEntries.Add(entry);
                    WriteToLog("Added labor entry for '4 in. sq. box'");
                }

                var mrl = RMeasure.LengthDbl(ALS.Info.DOC, HO_Single_Min_Rod_Len_Txt);
                var diameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Single_Rod_Diameter]);

                HangerOptions opts = new HangerOptions();
                opts.RodDiameter = diameter;
                opts.MinRodLength = mrl;

                foreach (var id in jboxes)
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
                debugger.show(err: ex.ToString());
            }
        }

        // Add strut hangers
        public async void AddStrutHangers(Window window)
        {
            try
            {
                List<ElementId> ids = ALS.Info.SEL.GetElementIds().ToList();

                if(!ids.Any()) {
                    debugger.show(header:"Strut Hangers", err:"No Conduit was selected. Please select a conduit rack.");
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

                var m_span = RMeasure.LengthDbl(ALS.Info.DOC, HO_Max_Span_Txt);
                var irg = RMeasure.LengthDbl(ALS.Info.DOC, HO_IRGap_Txt);
                var oesl = RMeasure.LengthDbl(ALS.Info.DOC, HO_OESLength_Txt);
                var mrl = RMeasure.LengthDbl(ALS.Info.DOC, HO_Strut_Min_Rod_Len_Txt);
                var nominal_spacing = RMeasure.LengthDbl(ALS.Info.DOC, Nominal_Hanger_Spacing_Txt);
                var bend_spacing = RMeasure.LengthDbl(ALS.Info.DOC, Bend_Hanger_Spacing_Txt);
                var rod_diameter = RMeasure.LengthDbl(ALS.Info.DOC, Rod_Diameter_Items[Sel_Strut_Rod_Diameter]);

                HangerOptions opts = new HangerOptions();
                opts.MaxStrutGapSpan = m_span;
                opts.RodDiameter = rod_diameter;
                opts.InsideRodGap = irg;
                opts.OutsideRodExtraLength = oesl;
                opts.MinRodLength = mrl;
                opts.NominalSpacing = nominal_spacing;
                opts.BendSpacing = bend_spacing;
                opts.DrawSingleHangerModelGeometry = Draw_Single_Debug;
                opts.DrawStrutHangerModelGeometry = Draw_Strut_Debug;
                
                List<StrutHanger> hangers = new List<StrutHanger>();
                if (conduit_ids.Any()) {
                    var current_hangers = await StrutHanger.CreateStrutHangers(
                        ALS.Info, ThisApplication.Hanger_View, conduit_ids, opts, StrutHangerRackType.Conduit);
                    if(current_hangers != null) hangers.AddRange(current_hangers);
                }

                if (tray_ids.Any()) {
                    var current_hangers = await StrutHanger.CreateStrutHangers(
                        ALS.Info, ThisApplication.Hanger_View, tray_ids, opts, StrutHangerRackType.CableTray);
                    if(current_hangers != null) hangers.AddRange(current_hangers);
                }
                
                ALS.Info.SEL.SetElementIds(hangers.Select(x => new ElementId(x.HangerFamilyInstanceId)).ToArray());

                ALS.AppData.GetSelectedHangerPackage().StrutHangers.AddRange(hangers);
                var strut_cnt = ALS.AppData.GetSelectedHangerPackage().StrutHangers.Count;
                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Added " + strut_cnt + " strut hangers.");
                UpdateTotalStrutLengthLabel();
            }
            catch (Exception ex) {
                debugger.show(header:"HangerCmds", err: ex.Message);
            }
        }

        // Remove single hangers from model
        public void RemoveSingleHangers(Window window)
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

        // Remove strut hangers from the model
        public void RemoveStrutHangers(Window window)
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

        public void RestoreHangers(Window window)
        {
            /*
            try
            {
                var ids = Run_Items.SelectMany(x => x.Value.ConduitIds).Select(x => new ElementId(x)).ToList();

                if (!ids.Any())
                {
                    debugger.show(err: "There were no conduits in view to process.");
                    return;
                }

                UnitFormatUtils.TryParse(Info.DOC.GetUnits(), UnitType.UT_Length,
                             HO_Single_Min_Rod_Len_Txt, out double mrl, out string mrl_msg);

                var hangers = HangerRestoreUtil.RestoreHangersInView(Info, ids, ThisApplication.Hanger_View, mrl);

                ALS.AppData.SingleHangers.AddRange(hangers.SingleHangers);
                var single_cnt = ALS.AppData.SingleHangers.Count();
                WriteToLog("Added " + single_cnt + " single hangers.");

                ALS.AppData.StrutHangers.AddRange(hangers.StrutHangers);
                var strut_cnt = ALS.AppData.StrutHangers.Count;
                RefreshDataGrids(BOMDataGrid.Hangers);
                WriteToLog("Added " + strut_cnt + " strut hangers.");
            }
            catch (Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
            */
        }

        public void UpdateTotalStrutLengthLabel()
        {
            var len = RMeasure.LengthFromDbl(ALS.Info.DOC, ALS.AppData.GetSelectedHangerPackage().StrutHangers.Select(x => x.StrutLength).Sum());
            Total_Unistrut_Hanger_Length_Txt = "Total Unistrut Length: " + len;
        }
    }
}
