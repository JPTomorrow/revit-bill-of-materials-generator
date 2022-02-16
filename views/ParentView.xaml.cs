using System.Windows;
using System.Windows.Markup;
using System.Windows.Input;
using JPMorrow.UI.ViewModels;
using JPMorrow.Revit.Documents;
using System;
using System.Windows.Interop;

namespace JPMorrow.UI.Views
{
    /// <summary>
    /// Code Behind landing for templateForm.xaml
    /// </summary>
    public partial class ParentView : Window, IComponentConnector
    {
        /// <summary>
        /// Default Constructor.static Bind DataContext
        /// </summary>
        public ParentView(ModelInfo info, IntPtr parent_wind_handle)
        {
            InitializeComponent();

            // set parent window to passed handle
            var interop = new WindowInteropHelper(this);
            interop.EnsureHandle();
            interop.Owner = parent_wind_handle;

            var vm = new ParentViewModel(info);
            this.DataContext = vm;
        }

        /// <summary>
        /// Custom Window Drag on DockPanel
        /// </summary>
        private void WindowDrag(object o, MouseEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }

            this.DragMove();
        }

        /// <summary>
        /// Custom Window Drag on DockPanel
        /// </summary>
        private void HelpClick(object o, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Custom Window Drag on DockPanel
        /// </summary>
        private void ExitClick(object o, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
