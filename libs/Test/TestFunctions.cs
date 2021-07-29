using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using OfficeOpenXml;

namespace JPMorrow.Test
{
    public static class TestFunction
	{

		/// <summary>
		/// Combine multiple excel files into one
		/// </summary>
		public static void CombineExcelFiles() {

			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Excel Files|*.xlsx;";
			ofd.Title = "SELECT EXCEL FILES TO MERGE";
			ofd.Multiselect = true;
			var result = ofd.ShowDialog();
			if (result != DialogResult.OK) return;

			List<string> filenames = ofd.FileNames.ToList();

			if(!filenames.Any())
			{
				debugger.show(err:"No files selected to merge.");
				return;
			}

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Excel Files|*.xlsx;";
			sfd.Title = "SELECT AN EXCEL FILE To SAVE TO";
			var save_result = sfd.ShowDialog();

			var save_file = "";
			if (save_result == DialogResult.OK) {
				save_file = sfd.FileName;
			}
			else {
				debugger.show(err:"No excel files selected to save.");
				return;
			}

			var out_excel_file = new ExcelPackage(new FileInfo(save_file));
			ExcelWorksheet out_ws = out_excel_file.Workbook.Worksheets.Add("MERGED");

			int row_idx = 0;

			foreach(var file in filenames)
			{
				var in_excel_file = new ExcelPackage(new FileInfo(file));

				foreach(ExcelWorksheet s in in_excel_file.Workbook.Worksheets)
				{
					for(var row = 1; row <= s.Dimension.Rows; row++)
					{
						var r = s.Cells[string.Format("{0}:{0}", row)];
						r.Copy(out_ws.Cells[string.Format("{0}:{0}", row_idx)]);
						row_idx++;
					}
				}
			}

			out_excel_file.Save();
			Process.Start(save_file);
		}
	}
}
