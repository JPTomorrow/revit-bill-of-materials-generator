using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Revit.Custom.Unistrut;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.ElementCollection;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Panels;
using JPMorrow.Tools.Diagnostics;
using MainApp;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        public void AddElecRoom(Window window)
        {
            ALS.ElecRoom.RoomName = Elec_Room_Title_Txt;
            ALS.AppData.ElectricalRoomPack.AddRoom(ALS.ElecRoom);
            ALS.ElecRoom = new ElecRoom();
            RefreshDataGrids(BOMDataGrid.ElecRoom);
        }

        public void RemoveElecRoom(Window window)
        {
            var presenters = Elec_Room_Items.Where(x => x.IsSelected);
            if(!presenters.Any()) return;
            presenters.Select(x => x.Value).ToList().ForEach(p => ALS.AppData.ElectricalRoomPack.RemoveRoom(p));
            RefreshDataGrids(BOMDataGrid.ElecRoom);
        }

        public void SelectRoom(Window window)
        {
            var rooms = Elec_Room_Items.Where(x => x.IsSelected).Select(x => x.Value);
            if(!rooms.Any()) return;


            ALS.ElecRoom = new ElecRoom();
            ALS.ElecRoom.Unistrut.AddRange(rooms.SelectMany(x => x.Unistrut).ToList());
            ALS.ElecRoom.GroundBar.AddRange(rooms.SelectMany(x => x.GroundBar).ToList());
            ALS.ElecRoom.Panelboard.AddRange(rooms.SelectMany(x => x.Panelboard).ToList());
            ALS.ElecRoom.PanelBacking.AddRange(rooms.SelectMany(x => x.PanelBacking).ToList());
            ALS.ElecRoom.Conduit.AddRange(rooms.SelectMany(x => x.Conduit).ToList());

            var count = ALS.ElecRoom.Conduit.Count().ToString();
            Elec_Room_Conduit_Txt = "Buildout Conduit Runs: " + count;
            Update("Elec_Room_Conduit_Txt");

            Elec_Room_Title_Txt = rooms.Count() > 1 ? "" : rooms.First().RoomName;
            Update("Elec_Room_Title_Txt");

            // pop unistrut
            Unistrut_Items.Clear();
            ALS.ElecRoom.Unistrut.ForEach(x => Unistrut_Items.Add(new UnistrutPresenter(x, ALS.Info)));
            Update("Unistrut_Items");

            // pop grd bars
            Grd_Bar_Items.Clear();
            ALS.ElecRoom.GroundBar.ForEach(x => Grd_Bar_Items.Add(new GrdBarPresenter(x, ALS.Info)));
            Update("Grd_Bar_Items");

            // pop panel backing
            Backing_Items.Clear();
            ALS.ElecRoom.PanelBacking.ForEach(x => Backing_Items.Add(new BackingPresenter(x, ALS.Info)));
            Update("Backing_Items");

            //pop panelboards
            Panelboard_Items.Clear();
            ALS.ElecRoom.Panelboard.ForEach(x => Panelboard_Items.Add(new PanelboardPresenter(x)));
            Update("Backing_Items");
        }

        /// <summary>
        /// get all unistrut and override the wall type
        /// </summary>
        public void AddUnistrut(Window window) {
            try
            {
                List<Unistrut> unistrut = new List<Unistrut>();
                List<Unistrut> frame_strut = new List<Unistrut>();

                var collected_els = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_ElectricalFixtures, false, "Single Unistrut", "Unistrut");

                if(!collected_els.Element_Ids.Any())
                {
                    WriteToLog("No unistrut to process for elec room");
                    return;
                }

                var wall_override = WallType_Items[Sel_Wall_Type];
                var masonry_anchor_size = Masonry_Anchor_Size_Items[Sel_Masonry_Anchor_Size];

                foreach(var id in collected_els.Element_Ids)
                {
                    Element el = ALS.Info.DOC.GetElement(id);
                    Unistrut us;

                    if(Wall_Override_Switch)
                        us = new Unistrut(ALS.Info, el, masonry_anchor_size, ThisApplication.Elec_Room_View, wall_override);
                    else
                        us = new Unistrut(ALS.Info, el, masonry_anchor_size, ThisApplication.Elec_Room_View);

                    unistrut.Add(us);
                }

                WriteToLog(unistrut.Count() + " unistrut were added");
                ALS.ElecRoom.Unistrut.AddRange(unistrut);
                RefreshDataGrids(BOMDataGrid.ElecRoom);

                var collected_frames = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_ElectricalFixtures, false, "PreFab Strut Frame 20 teir");

                if(!collected_frames.Element_Ids.Any())
                {
                    WriteToLog("No frames to process");
                    return;
                }

                foreach(var sfid in collected_frames.Element_Ids)
                {
                    Element el = ALS.Info.DOC.GetElement(sfid);
                    UnistrutFrame frame = UnistrutFrame.CreateFrame(ALS.Info, el);
                    frame_strut.AddRange(frame.Unistrut);
                }

                WriteToLog(frame_strut.Count() + " unistrut were added from strut frame");

                ALS.ElecRoom.Unistrut.AddRange(frame_strut);
                RefreshDataGrids(BOMDataGrid.ElecRoom);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Remove selected unistrut
        /// </summary>
        public void RemoveUnistrut(Window window)
        {
            try
            {
                var selected_us = Unistrut_Items.Where(x => x.IsSelected).ToList();
                if(!selected_us.Any()) return;

                selected_us.Select(x => x.Value).ToList().ForEach(x => ALS.ElecRoom.Unistrut.Remove(x));
                selected_us.ForEach(x => Unistrut_Items.Remove(x));
                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(selected_us.Count.ToString() + " unistrut removed");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Add grd bar with an overriden wall type
        /// </summary>
        public void AddGrdBar(Window window)
        {
            try
            {
                var collected_els = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_GenericModel, false, "Ground Bar");

                if(!collected_els.Element_Ids.Any())
                {
                    WriteToLog("No ground bar to process for elec room");
                    return;
                }

                var wall_override = WallType_Items[Sel_Wall_Type];
                var masonry_anchor_size = Masonry_Anchor_Size_Items[Sel_Masonry_Anchor_Size];
                var lug_override =Grd_Lug_Size_Items[Sel_Grd_Lug_Size];

                List<GroundBar> bars = new List<GroundBar>();
                foreach(var id in collected_els.Element_Ids) {
                    GroundBar bar;
                    var el = ALS.Info.DOC.GetElement(id);

                    if(Wall_Override_Switch && GB_Lug_Override_Switch) {
                        bar = new GroundBar(ALS.Info, el, masonry_anchor_size, wall_override);
                        bar.SearchLugs(ALS.Info, ALS.AppData.WireManager, ALS.AppData.Cris, lug_override);
                    }
                    else if(Wall_Override_Switch) {
                        bar = new GroundBar(ALS.Info, el, masonry_anchor_size, wall_override);
                        bar.SearchLugs(ALS.Info, ALS.AppData.WireManager, ALS.AppData.Cris);
                    }
                    else if(GB_Lug_Override_Switch) {
                        bar = new GroundBar(ALS.Info, el, masonry_anchor_size);
                        bar.SearchLugs(ALS.Info, ALS.AppData.WireManager, ALS.AppData.Cris, wall_override);
                    }
                    else {
                        bar = new GroundBar(ALS.Info, el, masonry_anchor_size);
                        bar.SearchLugs(ALS.Info, ALS.AppData.WireManager, ALS.AppData.Cris);
                    }

                    bars.Add(bar);
                }

                ALS.ElecRoom.GroundBar.AddRange(bars);

                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(bars.Count().ToString() + " ground bar was added");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Remove selected grd bar
        /// </summary>
        public void RemoveGrdBar(Window window)
        {
            try
            {
                var selected_gb = Grd_Bar_Items.Where(x => x.IsSelected).ToList();
                if(!selected_gb.Any()) return;

                selected_gb.Select(x => x.Value).ToList().ForEach(x => ALS.ElecRoom.GroundBar.Remove(x));
                selected_gb.ForEach(x => Grd_Bar_Items.Remove(x));
                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(selected_gb.Count.ToString() + " ground bar(s) removed");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Add all panel backing from a given view
        /// </summary>
        public void AddPanelBacking(Window window)
        {
            try
            {
                var collected_els = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_ElectricalEquipment, false, "Panel Backing");

                if(!collected_els.Element_Ids.Any())
                {
                    WriteToLog("No panel backing to process");
                    return;
                }

                var wall_override = WallType_Items[Sel_Wall_Type];
                var masonry_anchor_size = Masonry_Anchor_Size_Items[Sel_Masonry_Anchor_Size];

                List<PanelBacking> pbs = new List<PanelBacking>();
                foreach(var id in collected_els.Element_Ids)
                {
					Element el = ALS.Info.DOC.GetElement(id);
                    PanelBacking pb;

                    if(Wall_Override_Switch) {
                        pb = new PanelBacking(ALS.Info, el, masonry_anchor_size, wall_override);
                    }
                    else {
                        pb = new PanelBacking(ALS.Info, el, masonry_anchor_size);
                    }
                    pbs.Add(pb);
                }

                ALS.ElecRoom.PanelBacking.AddRange(pbs);

                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(pbs.Count() + " panel backings were added");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Remove selected panel backing
        /// </summary>
        public void RemovePanelBacking(Window window)
        {
            try
            {
                var selected_pb = Backing_Items.Where(x => x.IsSelected).ToList();
                if(!selected_pb.Any()) return;

                selected_pb.Select(x => x.Value).ToList().ForEach(x => ALS.ElecRoom.PanelBacking.Remove(x));
                selected_pb.ForEach(x => Backing_Items.Remove(x));
                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(selected_pb.Count().ToString() + " panel backing(s) removed");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Add all panelboards from a given view
        /// </summary>
        public void AddPanelboard(Window window)
        {
            try
            {
                var equipment_ids = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_ElectricalEquipment, false, "Panelboard");

                var transfer_switch_ids = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_ElectricalEquipment, false, "Transfer Switch");

                var transferormer_ids = ElementCollector.CollectElements(
                    ALS.Info, BuiltInCategory.OST_ElectricalEquipment, false, "Transformer");

                var collected_els = new List<ElementId>();

                foreach(var id in equipment_ids.Element_Ids.Concat(transfer_switch_ids.Element_Ids).Concat(transferormer_ids.Element_Ids)) {
                    var el = ALS.Info.DOC.GetElement(id);
                    var pname = el.LookupParameter("Panel Name").AsString();

		    bool mains_chk = el.LookupParameter("Mains") == null;

                    if(mains_chk || pname == null || string.IsNullOrWhiteSpace(pname)) continue;
                    collected_els.Add(id);
                }

                if(!collected_els.Any())
                {
                    WriteToLog("No panelboard to process");
                    return;
                }

                var wall_override = WallType_Items[Sel_Wall_Type];
                var masonry_anchor_size = Masonry_Anchor_Size_Items[Sel_Masonry_Anchor_Size];

                List<Panelboard> pbs = new List<Panelboard>();
                foreach(var id in collected_els)
                {
					Element el = ALS.Info.DOC.GetElement(id);
                    Panelboard pb;

                    if(Wall_Override_Switch) {
                        pb = Panelboard.CreatePanelboard(ALS.Info, el, masonry_anchor_size, wall_override);
                    }
                    else {
                        pb = Panelboard.CreatePanelboard(ALS.Info, el, masonry_anchor_size);
                    }

                    pbs.Add(pb);
                }

                ALS.ElecRoom.Panelboard.AddRange(pbs);

		ALS.Info.SEL.SetElementIds(pbs.Select(x => new ElementId(x.Element_Id)).ToList());

                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(pbs.Count() + " panelboards were added");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        /// <summary>
        /// Remove selected panelboards
        /// </summary>
        public void RemovePanelboard(Window window)
        {
            try
            {
                var selected_pb = Panelboard_Items.Where(x => x.IsSelected).ToList();
                if(!selected_pb.Any()) return;

                selected_pb.Select(x => x.Value).ToList().ForEach(x => ALS.ElecRoom.Panelboard.Remove(x));
                selected_pb.ForEach(x => Panelboard_Items.Remove(x));
                RefreshDataGrids(BOMDataGrid.ElecRoom);
                WriteToLog(selected_pb.Count().ToString() + " panelboard(s) removed");
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void GetElecRoomConduit(Window window)
        {
            var coll = new FilteredElementCollector(ALS.Info.DOC, ALS.Info.DOC.ActiveView.Id);
            var els = coll.OfCategory(BuiltInCategory.OST_Conduit).ToElements().ToList();
            var ids = els.Where(x =>

				!string.IsNullOrWhiteSpace(x.LookupParameter("From").AsString()) &&
				!string.IsNullOrWhiteSpace(x.LookupParameter("To").AsString()))

		.Select(x => x.Id).ToList();

            if(!ids.Any()) return;

            var cutoff = RMeasure.LengthDbl(ALS.Info.DOC, Elec_Room_Conduit_Cutoff_Txt);
            if(cutoff == -1) cutoff = 10.0; // 10' length

            var conduits = ALS.ElecRoom.AddElecRoomConduit(ALS.Info, cutoff, ALS.AppData.Cris, ids.Select(x => x.IntegerValue));

	    ALS.Info.SEL.SetElementIds(conduits.SelectMany(x => x.Ids.Select(y => new ElementId(y))).ToList());


            var count = ALS.ElecRoom.Conduit.Count().ToString();
            Elec_Room_Conduit_Txt = "Buildout Conduit Runs: " + count;
            Update("Elec_Room_Conduit_Txt");

            var rn = ALS.ElecRoom.RoomName.Equals("") ? ALS.ElecRoom.RoomName : "?";
            WriteToLog("Added conduit runs to electrical room " + rn);
        }

        public void ClearElecRoomConduit(Window window)
        {
            ALS.ElecRoom.Conduit.Clear();
            var count = ALS.ElecRoom.Conduit.Count().ToString();
            Elec_Room_Conduit_Txt = "Buildout Conduit Runs: " + count;
            Update("Elec_Room_Conduit_Txt");
        }
	}
}
