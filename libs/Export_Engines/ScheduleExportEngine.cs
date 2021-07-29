using System.Threading.Tasks;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using JPMorrow.Schedules;
using System.Data;

namespace JPMorrow.BOM.ScheduleExport
{

	public class ScheduleTable {

		public string Name { get; private set; }
		private ViewSchedule Schedule { get; set; }
		public int[] Dimensions { get; private set; }
		public DataTable DataTable { get; private set; }

		private ScheduleTable(ModelInfo info, ViewSchedule schedule) {
			Schedule = schedule;
		}

		public static ScheduleTable CreateScheduleTable(ModelInfo info, ViewSchedule schedule) {
			return new ScheduleTable(info, schedule);
		}

		public async Task UpdateCell(
			ModelInfo info, XY dimensions) {

			ScheduleUpdate.ClearHandler();
			ScheduleUpdate.handler_update_schedule.Info = info;
			ScheduleUpdate.handler_update_schedule.Schedule = Schedule;
			ScheduleUpdate.handler_update_schedule.Dimensions = new[] { dimensions.X, dimensions.Y };
			ScheduleUpdate.exEvent_update_schedule.Raise();

			while(ScheduleUpdate.exEvent_update_schedule.IsPending) {
				await Task.Delay(200);
			}
		}

		public async Task UpdateCell(
			ModelInfo info, XY cell_idx, string text_val) {

			ScheduleUpdate.ClearHandler();
			ScheduleUpdate.handler_update_schedule.Info = info;
			ScheduleUpdate.handler_update_schedule.Schedule = Schedule;
			ScheduleUpdate.handler_update_schedule.CellPos = new[] { cell_idx.X, cell_idx.Y };
			ScheduleUpdate.handler_update_schedule.CellModifyTextValue = text_val;
			ScheduleUpdate.exEvent_update_schedule.Raise();

			while(ScheduleUpdate.exEvent_update_schedule.IsPending) {
				await Task.Delay(200);
			}
		}

		public async Task UpdateCell(
			ModelInfo info, XY cell_idx, Color font_color, Color background_color) {

			ScheduleUpdate.ClearHandler();
			ScheduleUpdate.handler_update_schedule.Info = info;
			ScheduleUpdate.handler_update_schedule.Schedule = Schedule;
			ScheduleUpdate.handler_update_schedule.CellPos = new[] { cell_idx.X, cell_idx.Y };
			ScheduleUpdate.handler_update_schedule.CellTextColor = font_color;
			ScheduleUpdate.handler_update_schedule.CellBackgroundColor = background_color;
			ScheduleUpdate.exEvent_update_schedule.Raise();

			while(ScheduleUpdate.exEvent_update_schedule.IsPending) {
				await Task.Delay(200);
			}
		}
	}

	public class XY {
		private int x_store;
		private int y_store;

		public int X => x_store;
		public int Y => y_store;

		public XY(int x, int y) {
			x_store = x;
			y_store = y;
		}
	}
}