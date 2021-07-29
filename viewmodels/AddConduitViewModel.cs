using System.Windows.Input;
using System.Windows;
using JPMorrow.Revit.Documents;
using System.Linq;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Views.RelayCmd;
using System.IO;
using om = System.Collections.ObjectModel;
using Autodesk.Revit.DB;

namespace JPMorrow.UI.ViewModels
{

    using ObsStr = om.ObservableCollection<string>;

	public partial class AddConduitViewModel : Presenter
    {
        public ObsStr Workset_Items { get; set; } = new ObsStr();
        public ObsStr Floor_Items { get; set; } = new ObsStr();

        public ICommand MasterCloseCmd => new RelayCommand<Window>(MasterClose);

        public AddConduitViewModel(ModelInfo info) {
            FilteredWorksetCollector coll = new FilteredWorksetCollector(info.DOC);

            foreach(var ws in coll)
                Workset_Items.Add(ws.Name);

            
            FilteredElementCollector l_coll = new FilteredElementCollector(info.DOC);

            var levels = l_coll.OfClass(typeof(Level)).ToElements();

            foreach(var l in levels)
                Floor_Items.Add(l.Name);
        }
    }
}