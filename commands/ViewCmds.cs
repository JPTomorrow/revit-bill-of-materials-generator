using System;
using System.Windows;
using Autodesk.Revit.DB;
using JPMorrow.Revit.ViewHandler;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        // prepare a bill of material view
        public void PrepView(Window window)
        {
            try
            {
                /*
                bool valid_view = ViewManager.ValidateActiveViewType(Info, out var view, ViewType.FloorPlan, ViewType.CeilingPlan);

                if(!valid_view)
                {
                    debugger.show(err:"Please select a floor plan or cieling plan and try again.");
                    return;
                }

                var vts = ViewManager.GetProjectViewTemplates(Info, out int cnt, "BOM");

                Autodesk.Revit.DB.View vt;
                if(cnt > 0)
                {
                    // create bom view-template view
                    //ViewFamilyType.GetValidTypes();
                    var vft_id = ViewManager.GetViewFamilyTypeId(Info, Info.DOC.ActiveView.ViewType);
                    var l_id  = Info.DOC.ActiveView.LevelId;
                    var v_temp = ViewPlan.Create(Info.DOC, new ElementId(vft_id), l_id);
                    //vt = v_temp.CreateViewTemplate();
                    //debugger.show(err:vt.IsTemplate.ToString());

                }
                */
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
	}
}
