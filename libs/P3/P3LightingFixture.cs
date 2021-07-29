using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Loader;
using JPMorrow.Revit.Tools;
using JPMorrow.Revit.Transactions;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.Revit.RvtMiscUtil;
using JPMorrow.Revit.Text;
using JPMorrow.Revit.Measurements;

namespace JPMorrow.P3
{
	public class P3LightingFixture
	{
        public static string P3LightingFixtureFamilyName { get; } = "'18 - P3 Lighting Fixture.rfa";

		public P3LightingFixture()
		{

		}

		
    }
}