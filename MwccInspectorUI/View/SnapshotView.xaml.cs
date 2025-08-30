using MwccInspector.MwccTypes;
using MwccInspectorUI.ViewModel;
using System.Windows.Controls;

namespace MwccInspectorUI.View {
    public partial class SnapshotView : UserControl {
        public SnapshotView() {
            InitializeComponent();
        }

        private void CommandBinding_TypeClicked(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
            if (DataContext is SnapshotViewModel vm && e.Parameter is MwccCachedType t) {
                vm.OnTypeClicked(t);
            }
        }
    }
}
