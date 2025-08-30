using System.Windows.Controls;
using System.Windows.Input;

namespace MwccInspectorUI.View {
    public partial class MwccTypeView : UserControl {
        public MwccTypeView() {
            InitializeComponent();
        }

        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
            // https://stackoverflow.com/questions/61870147/wpf-datagrid-inside-a-scrollviewer
            var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {
                RoutedEvent = ScrollViewer.MouseWheelEvent
            };
            RaiseEvent(args);
        }
    }
}
