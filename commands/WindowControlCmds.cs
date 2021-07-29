using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using JPMorrow.Tools.Diagnostics;
using JPMorrow.UI.Views;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        /// <summary>
        /// prompt for save and exit
        /// </summary>
        public void MasterClose(Window window)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Do you want to save before closing?",
                    "Save?",
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if(result.Equals(MessageBoxResult.Yes))
                {
                    SavePackage(null);
                }
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void FullActionLog(Window window) {
            try {
                IntPtr main_rvt_wind = Process.GetCurrentProcess().MainWindowHandle;
                ActionLogView alv = new ActionLogView(main_rvt_wind);
                alv.ShowDialog();
            }
            catch(Exception ex) {
                debugger.show(err:ex.ToString());
            }
        }
	}

    public partial class ActionLogViewModel {
        /// <summary>
        /// prompt for save and exit
        /// </summary>
        public void MasterClose(Window window)
        {
            try
            {
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
    }

    public partial class ResetFileViewModel {
        /// <summary>
        /// prompt for save and exit
        /// </summary>
        public void MasterClose(Window window)
        {
            try
            {
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
    }

    public partial class AddConduitViewModel {
        /// <summary>
        /// prompt for save and exit
        /// </summary>
        public void MasterClose(Window window)
        {
            try
            {
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
    }

    public partial class ExportSelectionViewModel {
        /// <summary>
        /// prompt for save and exit
        /// </summary>
        public void MasterClose(Window window)
        {
            try
            {
                window.DialogResult = true;
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void Close(Window window)
        {
            try
            {
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
    }
}