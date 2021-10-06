using System;
using System.Linq;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using OfficeOpenXml.Style;

namespace JPMorrow.Excel
{
    public partial class ExcelOutputSheet
    {
        /// <summary>
		/// Export a Conduit Only sheet
		/// </summary>
        public void GenerateConduitOnlySheet(ModelInfo info, string project_file_name, MasterDataPackage data_package)
        {

            if (HasData) throw new Exception("The sheet already has data");

            var package = data_package;
            var project_title = info.DOC.ProjectInformation.Name;
            string title = "M.P.A.C.T. - " + project_title;

            InsertHeader(title, "Conduit Only", project_file_name);

            var group_mat_types = data_package.GetSelectedConduitPackage().Cris
                .GroupBy(x => new { Mat = x.ConduitMaterialType, Diameter = RMeasure.LengthFromDbl(info.DOC, x.Diameter) })
                .Select(x => new { Length = RMeasure.LengthFromDbl(info.DOC, x.Sum(x => x.Length)), Diameter = x.Key.Diameter, Material = x.Key.Mat })
                .ToList();

            foreach (var g in group_mat_types)
            {
                InsertIntoRow(g.Length, g.Diameter, g.Material);
                NextRow(1);
            }

            FormatExcelSheet(0.1M);
            ChangeColumnAlignment(4, new char[] { 'A', 'A' }, ExcelHorizontalAlignment.Right);
            ChangeColumnAlignment(4, new char[] { 'B', 'B' }, ExcelHorizontalAlignment.Center);
            ChangeColumnAlignment(4, new char[] { 'C', 'C' }, ExcelHorizontalAlignment.Right);

            HasData = true;
        }
    }
}