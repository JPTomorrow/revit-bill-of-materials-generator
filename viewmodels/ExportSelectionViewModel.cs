using System.Windows.Input;
using System.Windows;
using JPMorrow.Revit.Documents;
using System.Linq;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Views.RelayCmd;
using System.IO;

namespace JPMorrow.UI.ViewModels
{

	public partial class ExportSelectionViewModel : Presenter
    {
        /*
        public bool ResetRunsChecked {get; set;} = false;
        public bool ResetWireChecked {get; set;} = false;
        public bool ResetP3InWallChecked {get; set;} = false;
        public bool ResetLaborChecked {get; set;} = false;
        public bool ResetHardwareChecked {get; set;} = false;
        public bool ResetHangersChecked {get; set;} = false;
        public bool ResetElectricalRoomChecked {get; set;} = false;
        */

        public ICommand MasterCloseCmd => new RelayCommand<Window>(MasterClose);
        public ICommand CloseCmd => new RelayCommand<Window>(Close);

        public ExportSelectionViewModel() {
            
        }
    }
}