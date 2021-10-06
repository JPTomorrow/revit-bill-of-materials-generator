using System;
using System.Collections.Generic;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Revit.Custom.Unistrut;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Panels;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Revit.WirePackage;
using OfficeOpenXml.Style;
using Draw = System.Drawing;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
		/// Export a Elec Room Buildout sheet
		/// </summary>
        public void GenerateElecRoomSheet(ModelInfo info, MasterDataPackage data_package, ElecRoom room)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Electrical Room Buildout", "Room Name: " + room.RoomName);

            // voltage drop
            package = VoltageDrop.AllWireDropVoltage(package);

            double gt = 0.0; // Grand Total
            double code_one_gt = 0; // 01 EMPTY RACEWAY Grand Total
                                    // static double shave_labor(double labor) => labor * 0.82;
            string fdbl(double val) => string.Format("{0:N2}", val);

            WirePackageSettings wire_pack_settings = WirePackageSettings.Load();
            LaborExchange l = new LaborExchange(ModelInfo.SettingsBasePath, package.LaborHourEntries);

            InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Conduit", 15);

            List<ElecRoomConduit> conduit = package.GetSelectedElecRoomPackage().ElectricalRoomPack.FlattenConduit(info).ToList();
            UnistrutTotal ut = room.Unistrut.FlattenUnistrut(info);
            GrdBarTotal gbt = room.GroundBar.FlattenGroundBars(info);
            PanelBackingTotal pbt = room.PanelBacking.FlattenPanelBacking();
            PanelboardTotal pbrdt = room.Panelboard.FlattenPanelboard();

            #region Conduit Labor
            foreach (var c in conduit)
            {
                string type = ConduitRunInfo.ConduitMaterialTypes[0];

                if (ConduitRunInfo.ConduitMaterialTypes.Any(x => c.MaterialType.ToUpper().Contains(x)))
                {
                    type = ConduitRunInfo.ConduitMaterialTypes.ToList()
                        .Find(x => c.MaterialType.ToUpper().Contains(x));
                }

                var diameter = RMeasure.LengthFromDbl(info.DOC, c.Diameter);

                var has_item = l.GetItem(out var li, (double)c.Length, "Conduit", type, diameter);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                var con_name = c.MaterialType + " - " + diameter + " Dia.";
                InsertIntoRow(con_name, li.Quantity, li.PerUnitWithSuffix("per ft."), li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            }
            #endregion

            code_one_gt += gt;
            InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Unistrut Labor
            if (ut.Unistrut.Any()) InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Unistrut", 15);

            ut.Unistrut.ForEach(us =>
            {
                int len = (int)Math.Round(us.Length);
                var has_item = l.GetItem(out var li, (double)len, us.Unistrut.Name, us.Unistrut.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                string us_name = us.Unistrut.Name + " - " + us.Unistrut.Size;
                InsertIntoRow(us_name, li.Quantity, li.PerUnitLabor, "Feet", fdbl(li.TotalLaborValue));
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.ConduitStraps.ForEach(cs =>
            {
                var has_item = l.GetItem(out var li, (double)cs.Count, UnistrutConduitStrap.Name, cs.Strap.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.ToggleBolts.ForEach(tb =>
            {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.Washers.ForEach(w =>
            {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.SheetMetalScrews.ForEach(s =>
            {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.MachineScrew.ForEach(m =>
            {
                var has_item = l.GetItem(out var li, (double)m.Count, m.Screw.Name, m.Screw.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.Anchors.ForEach(a =>
            {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.ChannelNuts.ForEach(c =>
            {
                var has_item = l.GetItem(out var li, (double)c.Count, c.Nut.Name, c.Nut.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.PlateFittings.ForEach(p =>
            {
                var has_item = l.GetItem(out var li, (double)p.Count, p.Fitting.Name, p.Fitting.Type + " Type");
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            ut.PostBases.ForEach(p =>
            {
                var has_item = l.GetItem(out var li, (double)p.Count, p.Base.Name);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });
            #endregion

            code_one_gt += gt;
            if (ut.Unistrut.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Ground Bar Labor
            if (gbt.GroundBars.Any()) InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Ground Bar", 15);

            //Ground Bar
            gbt.GroundBars.ForEach(gb =>
            {
                var has_item = l.GetItem(out var li, (double)gb.Count, gb.Bar.Name);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                string gb_name = gb.Bar.Name + " - (" + gb.Bar.GetDimensions(info) + ")";
                InsertIntoRow(gb_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            gbt.Lugs.ForEach(ll =>
            {
                var has_item = l.GetItem(out var li, (double)ll.Count, GrdBarLug.Name, ll.Lug.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            gbt.ToggleBolts.ForEach(tb =>
            {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            gbt.Washers.ForEach(w =>
            {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            gbt.SheetMetalScrews.ForEach(s =>
            {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            gbt.Anchors.ForEach(a =>
            {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });
            #endregion

            code_one_gt += gt;
            if (gbt.GroundBars.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Panel Backing Labor
            if (pbt.PanelBackingFootage != 0.0)
                InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Panel Backing", 15);

            if (pbt.PanelBackingFootage > 0.0)
            {
                var len = (int)Math.Round(pbt.PanelBackingFootage / 4.0); // per 4 ft
                var has_item = l.GetItem(out var li, (double)len, PanelBacking.Name);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                string pb_name = PanelBacking.Name + " - (4 Ft. Segments)";
                InsertIntoRow(pb_name, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            }

            pbt.ToggleBolts.ForEach(tb =>
            {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbt.Washers.ForEach(w =>
            {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbt.SheetMetalScrews.ForEach(s =>
            {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbt.Anchors.ForEach(a =>
            {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });
            #endregion

            code_one_gt += gt;
            if (pbt.PanelBackingFootage != 0.0) InsertGrandTotal("Sub Total", ref gt, true, false, true);

            #region Panelboard Labor
            if (pbrdt.Panelboards.Any()) InsertSingleDivider(Draw.Color.Chocolate, Draw.Color.White, "Panelboards", 15);

            pbrdt.Panelboards.ForEach(pbrd =>
            {
                var namearr = int.Parse(pbrd.Board.Amperage) > 0 ?
                    new[] { pbrd.Board.Name, pbrd.Board.Amperage + "A" } : new[] { "Panelboard Can", "No Amps" };

                var has_item = l.GetItem(out var li, (double)pbrd.Count, namearr[0], namearr[1]);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                var amperage_print = int.Parse(pbrd.Board.Amperage) > 0 ? " - " + pbrd.Board.Amperage + "A" : "";
                var pname = pbrd.Board.Name + " - " + pbrd.Board.PanelName + amperage_print;
                InsertIntoRow(pname, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbrdt.ToggleBolts.ForEach(tb =>
            {
                var has_item = l.GetItem(out var li, (double)tb.Count, tb.Bolt.Name, tb.Bolt.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbrdt.Washers.ForEach(w =>
            {
                var has_item = l.GetItem(out var li, (double)w.Count, w.Washer.Name, w.Washer.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbrdt.SheetMetalScrews.ForEach(s =>
            {
                var has_item = l.GetItem(out var li, (double)s.Count, s.Screw.Name, s.Screw.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });

            pbrdt.Anchors.ForEach(a =>
            {
                var has_item = l.GetItem(out var li, (double)a.Count, a.Anchor.Name, a.Anchor.Size);
                if (!has_item) throw new Exception("No Labor item for conduit straps");
                InsertIntoRow(li.EntryName, li.Quantity, li.PerUnitLabor, li.LaborCodeLetter, li.TotalLaborValue);
                gt += Math.Ceiling(li.TotalLaborValue); NextRow(1);
            });
            #endregion

            code_one_gt += gt;
            if (pbrdt.Panelboards.Any()) InsertGrandTotal("Sub Total", ref gt, true, false, true);

            InsertGrandTotal("Code ?? | Elec Room | Grand Total", ref code_one_gt, false, false, false);
            code_one_gt *= 0.82;
            InsertGrandTotal("Code ?? w/ 0.82 Labor Factor", ref code_one_gt, true, false, true);


            // format the sheet
            FormatExcelSheet(0.1M);
            MakeFooter();
            ChangeColumnAlignment(4, new char[] { 'A', 'E' }, ExcelHorizontalAlignment.Left);
            this['D', 'D'].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ChangeColumnWidth('A', 50);

            HasData = true;
        }
    }
}