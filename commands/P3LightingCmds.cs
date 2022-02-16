using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.ElementCollection;
using JPMorrow.Revit.Measurements;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        // Select P3 Lighting Network
        public void AddP3FixturesInView(Window window)
        {
            try
            {
                throw new NotImplementedException("This is not implemented");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Add P3 Fixtures In View", err: ex.Message);
            }
        }

        // Select P3 Lighting Network
        public void SelectP3LightingNetwork(Window window)
        {
            try
            {
                throw new NotImplementedException("This is not implemented");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Select P3 Lighting Network", err: ex.Message);
            }
        }

        // fires when fixture box selection changes
        public void P3LightingFixtureSelChanged(Window window)
        {
            try
            {
                throw new NotImplementedException("This is not implemented");
            }
            catch (Exception ex)
            {
                debugger.show(header: "P3 Lighting Fixture Sel Changed", err: ex.Message);
            }
        }

        public void DebugSelectedP3LightingFixture(Window window)
        {
            try
            {
                debugger.show(err: "debug selected p3 fixture test");
            }
            catch (Exception ex)
            {
                debugger.show(header: "Debug Selected P3 Lighting Fixture", err: ex.Message);
            }
        }
    }
}
