using System;
using System.Windows;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
		public void Test(Window window)
        {
            
            try
            {
                /*
                ALS.AppData.StrutHangers.AddRange(hangers);
                var strut_cnt = ALS.AppData.StrutHangers.Count;
                RefreshDataGrids(false, false, true, false, false, false, false, false);
                WriteToLog("Added " + strut_cnt + " strut hangers.");

                var box = Info.SEL.GetElementIds().ToList().First();
                var box_el = Info.DOC.GetElement(box);

                var comments = box_el.LookupParameter("Comments").AsString();

                string o = "";
                comments.ToList().ForEach(x => o += x + " : " + char.IsDigit(x) + "\n");
                debugger.show(err:o);

                
                ElementCollection coll = ElementCollector.CollectElements(Info, BuiltInCategory.OST_ElectricalFixtures, false, InWallDevice.InWallDeviceFamilyNameNoExt);

                if(!coll.Element_Ids.Any()) {
                    debugger.show(err:"Collected no elements.");
                    return;
                }

                var boxes = P3InWall.GetInWallDevices(Info, coll.Element_Ids);

                string o = "";
                boxes.ToList().ForEach(box => {
                    var entry = "Dimensions: {0}\nDevice Code: {1}\n";
                    o += string.Format(entry, box.BoxDimensions(Info), box.GetDeviceCode());
                });

                debugger.show(err:o);
                */

                /*
                var r = new Color(255,0,0);
                var g = new Color(0,255,0);
                var b = new Color(0,0,255);
                var black = new Color(0,0,0);
                var white = new Color(255,255,255);
                var schedule = await ScheduleCreation.GetNewSchedule(Info, "Test", ElementId.InvalidElementId);

                ScheduleTable table = ScheduleTable.CreateScheduleTable(Info, schedule);
                await table.UpdateCell(Info, new XY(5, 5));

                await table.UpdateCell(Info, new XY(2, 1), "Hello World 1");
                await table.UpdateCell(Info, new XY(1, 1), "Hello World 2");
                await table.UpdateCell(Info, new XY(1, 2), "Hello World 3");
                await table.UpdateCell(Info, new XY(1, 3), "Hello World 4");

                await table.UpdateCell(Info, new XY(1, 1), white, black);
                await table.UpdateCell(Info, new XY(1, 2), white, r);
                await table.UpdateCell(Info, new XY(1, 3), white, g);
                */

            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        
	}
}
