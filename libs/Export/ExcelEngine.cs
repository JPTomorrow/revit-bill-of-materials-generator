using System.Diagnostics;
using System.IO;
using System.Linq;
using JPMorrow.Tools.Diagnostics;
using OfficeOpenXml;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExcelInst =  Microsoft.Office.Interop.Excel;

namespace JPMorrow.Excel
{
    public class ExcelEngine {
        public ExcelPackage ExcelInstance { get; private set; }
        public string FilePath { get; private set; } = "";
        public string Filename { get => Path.GetFileName(FilePath); }

        /// <summary>
        /// Get the current workbook file name with 
        /// no extensions and underscores treated as spaces
        /// </summary>
        private string CleanFileName { get {
            string clean_file_name = System.IO.Path.GetFileNameWithoutExtension(FilePath);
			clean_file_name = clean_file_name.Replace("_", " ");
			return clean_file_name;
        }}

        public ExcelEngine(string file_path) 
		{
            FilePath = file_path;
            ExcelInstance = new ExcelPackage(new FileInfo(FilePath));
        }

        /// <summary>
        /// add a sheet to the sheets for this ExcelInstance
        /// </summary>
        public void RegisterSheets(string sheet_name_prefix, params ExcelOutputSheet[] sheets) 
		{
			try
			{
				foreach(var s in sheets) 
				{
					s.SetSheet(this, sheet_name_prefix);
				}
			}
			catch
			{
                ExcelInstance.Dispose();
            }
        }
        
        /// <summary>
        /// Close the current excel file
        /// </summary>
		public void Close() 
		{
			ExcelInstance.Save();
			ExcelInstance.Dispose();
		}

		/// <summary>
        /// Opens an excel file of the current export
        /// </summary>
        /// <returns>true if the file opened, false if it failed, or was already open</returns>
        public bool OpenExcel()
		{
            try 
            {
                Process.Start(FilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

		/// <summary>
        /// Opens an pdf file of the current export
        /// </summary>
        /// <returns>true if the file opened, false if it failed, or was already open</returns>
		public bool OpenPDF(string pdf_filename)
		{
            try 
            {
                Process.Start(pdf_filename);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Print Excel file to PDF
        /// </summary>
        /// <param name="excelLocation">The location of the Excel file</param>
        /// <param name="outputLocation">The output location of the PDF file</param>
		public bool ExportToPdf(string pdf_output_location)
		{
			ExcelInst.Application app = new ExcelInst.Application();
			app.Visible = false;
			ExcelInst.Workbook wkb = app.Workbooks.Open(FilePath);
            bool created = false;
            try
			{
				wkb.ExportAsFixedFormat(ExcelInst.XlFixedFormatType.xlTypePDF, pdf_output_location);
                created = true;
            }
			catch
			{
                created = false;
            }
			finally
			{
				wkb.Close();
				app.Quit();
			}

            return created;
        }

        /// <summary>
        /// perform all necessary checks on an export file
        /// </summary>
        public static bool PrepExportFile(string filename) 
		{
            if(!File.Exists(filename)) 
			{
                var file = File.Create(filename);
                file.Close();
            }

            if(IsFileLocked(new FileInfo(filename))) 
			{
                debugger.show(err:"Excel file must be closed in order to export.");
                return false;
            }

            if(File.Exists(filename)) File.Delete(filename);
            return true;
        }

		/// <summary>
        /// check to see if the file is locked
        /// </summary>
        private static bool IsFileLocked(FileInfo file) 
		{
            FileStream stream = null;

            try 
			{
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException) 
			{
                return true;
            }
            finally 
			{
                if (stream != null) stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}

namespace JPMorrow.Test
{
    using JPMorrow.Excel;

    public static partial class TestBed
    {
        public static TestResult TestExcelEngine(string settings_path, Document doc, UIDocument uidoc)
        {
            var test_path = settings_path + "test/" + "TestExcelWorkbook.xlsx";
            debugger.show(err:settings_path);
            ExcelEngine e = new ExcelEngine(test_path);

            ExcelOutputSheet s1 = new ExcelOutputSheet(ExportStyle.WirePull);
            e.RegisterSheets("Test", s1);
            

            return new TestResult("Excel Engine", true);
        }
    }
}