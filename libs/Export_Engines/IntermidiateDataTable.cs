using System.Collections.Generic;
using System.Data;

namespace JPMorrow.BOM {
	public class BOMExportTable {
		public DataTable Table { get; private set; }

		private BOMExportTable(
			string title, string sub_title, IEnumerable<string> col_headers) {
			Table = new DataTable(title);

			foreach(var h in col_headers) {

			}
		}

		public static BOMExportTable CreateTable(
			string title, string sub_title, IEnumerable<string> col_headers) {
			var t = new BOMExportTable(title, sub_title, col_headers);
			t.Table.TableName = title;
			return t;
		}

		public void AddRow() {

		}

		public void AddRowData() {
			
		}
	}
}