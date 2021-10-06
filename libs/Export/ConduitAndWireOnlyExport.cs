using System;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
		/// Export a Conduit And Wire Only sheet
		/// </summary>
        public void GenerateConduitAndWireOnlySheet(ModelInfo info, string project_file_name, MasterDataPackage data_package)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Conduit And Wire Only", project_file_name);

            foreach (var run in package.GetSelectedConduitPackage().Cris.OrderBy(x => x.From).ThenBy(y => y.To).ThenBy(z => z.ConduitMaterialType).ToList())
            {
                var len = RMeasure.LengthFromDbl(info.DOC, run.Length);
                var ws = run.GetRevitWireSizeString(info);
                ws = ws.Equals("") ? "---" : ws;
                InsertIntoRow(run.From, run.To, len, ws, run.DiameterStr(info.DOC), run.GetSets(info), run.ConduitMaterialType);
                NextRow(1);
            }

            FormatExcelSheet(0.1M);
            MakeFooter();
            HasData = true;
        }
    }
}
