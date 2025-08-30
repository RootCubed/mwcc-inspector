using System.Windows.Input;

namespace MwccInspectorUI.ViewModel {
    class SnapshotCommands {
        public static readonly RoutedCommand TypeClicked = new("TypeClicked", typeof(SnapshotViewModel));
    }
}
