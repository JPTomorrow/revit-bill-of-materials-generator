using System.Windows;
using System.Windows.Markup;
using System.Windows.Input;
using JPMorrow.UI.ViewModels;
using System;
using System.Windows.Interop;
using JPMorrow.Revit.Documents;

namespace JPMorrow.UI.Views
{
	/// <summary>
	/// Code Behind landing for templateForm.xaml
	/// </summary>
	public partial class AddConduitView : Window, IComponentConnector
	{
		/// <summary>
		/// Default Constructor.static Bind DataContext
		/// </summary>
		public AddConduitView(IntPtr parent_wind_handle, ModelInfo info)
		{
			InitializeComponent();

			// set parent window to passed handle
			var interop = new WindowInteropHelper(this);
			interop.EnsureHandle();
			interop.Owner = parent_wind_handle;

			this.DataContext = new AddConduitViewModel(info);
		}

		/// <summary>
		/// /// Custom Window Drag on DockPanel
		/// </summary>
		private void WindowDrag(object o, MouseEventArgs e)
		{
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
