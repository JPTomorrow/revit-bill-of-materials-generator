using System.Windows.Input;
using System.Windows;
using JPMorrow.Revit.Documents;
using System.Linq;
using JPMorrow.Views.RelayCmd;
using System.IO;

namespace JPMorrow.UI.ViewModels
{

	public partial class ActionLogViewModel : Presenter
    {
        public string Action_Log { get; set; }
        public string Action_Log_File_Path { get; } = ModelInfo.GetDataDirectory("action_log") + "\\action_log.txt";

        public ICommand MasterCloseCmd => new RelayCommand<Window>(MasterClose);

        public ActionLogViewModel()
        {
            var al_text = File.ReadAllText(Action_Log_File_Path);
            Action_Log = al_text;
            Update("Action_Log");
        }
    }
}
