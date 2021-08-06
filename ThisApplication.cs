using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.UI.Views;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Tools;
using System.Diagnostics;
using JPMorrow.Revit.Loader;
using JPMorrow.Revit.Worksets;
using JPMorrow.Revit.Custom.Unistrut;
using JPMorrow.Revit.Custom.View;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Revit.Panels;
using JPMorrow.BICategories;
using JPMorrow.Schedules;
using JPMorrow.Revit.ElementDeletion;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.P3;
using JPMorrow.Test;

namespace MainApp
{
    /// <summary>
    /// Main Execution
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
	[Autodesk.Revit.DB.Macros.AddInId("9BBF529B-520A-4877-B63B-BEF1238B6A05")]
    public partial class ThisApplication : IExternalCommand
    {
		public static View3D Hanger_View { get; set; } = null;
		public static View3D Elec_Room_View { get; set; } = null;
        public bool RunTests { get; } = false;

        public static InsertModelLine handler_iml = null;
		public static ExternalEvent exEvent_iml = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
			string[] dataDirectories = new string[] { "action_log", "master_package", "wire_package", "hanger_opts", "labor", "families", "test_files" };

			//set revit model info
			bool debugApp = false;
			ModelInfo revit_info = ModelInfo.StoreDocuments(commandData, dataDirectories, debugApp);
			IntPtr main_rvt_wind = Process.GetCurrentProcess().MainWindowHandle;

			// run tests
			if(RunTests) 
			{
				TestBed.TestAll(revit_info);
                return Result.Succeeded;
            }

            // check for families and parameters
            string fam_path = ModelInfo.GetDataDirectory("families", true);

			FamilyLoader.LoadFamilies(revit_info, fam_path,
				StrutHanger.StrutHangerFamilyName, StrutHanger.StrutCeilingHangerFamilyName, SingleHanger.K16BatwingFamilyName, SingleHanger.MineralacFamilyName);
			FamilyLoader.LoadFamilies(revit_info, fam_path, Unistrut.UnistrutFamilyName,
				GroundBar.GrdBarFamilyName, PanelBacking.PanelBackingName, Panelboard.PanelboardName);
			// FamilyLoader.LoadFamilies(revit_info, fam_path, P3LightingFixture.P3LightingFixtureFamilyName);

			//Create worksets
			WorksetManager.CreateWorkset(revit_info.DOC, "Hangers");

			//set up hanger view
			Hanger_View = ViewGen.CreateView(revit_info, "HangersGenerated", BICategoryCollection.HangerView);
			Elec_Room_View = ViewGen.CreateView(revit_info, "ElecRoomGenerated", Unistrut.ViewCategories);

			//Set external events
			handler_iml = new InsertModelLine();
			exEvent_iml = ExternalEvent.Create(handler_iml);
			HangerDrawing.HangerDebugPointCreationSignUp();
			StrutHanger.ConduitStrutHangerModelCreationSignUp();
            StrutHanger.CableTrayStrutHangerModelCreationSignUp();
            SingleHanger.SingleHangerCreationSignUp();
			ScheduleCreation.ScheduleCreationSignUp();
			ScheduleUpdate.ScheduleUpdateSignUp();
			RvtElementDeletion.DeleteRevitElementsSignUp();
			LowVoltageDeviceAutomation.NHA_LowVoltageDeviceTagSignUp();
            ConduitParameter.ConduitPushToFromParamSignUp();

            try
			{
				ParentView pv = new ParentView(revit_info, main_rvt_wind);
				pv.Show();
			}
			catch(Exception ex)
			{
				debugger.show(err:ex.ToString());
			}

			return Result.Succeeded;
        }
    }
}