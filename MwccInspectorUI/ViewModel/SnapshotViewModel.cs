using MwccInspectorUI.MVVM;
using static MwccInspectorUI.Model.MwccDebugger;

namespace MwccInspectorUI.ViewModel {
    internal class SnapshotViewModel(Snapshot snapshot) : ViewModelBase {
        public Snapshot Snapshot { get; } = snapshot;

        public string FunctionName => Snapshot.FunctionName;

        public string Title => $"Viewing function {FunctionName}";

        public string SnapshotText => string.Join("\n", Snapshot.Statements.Select(s => s.ToString()));
    }
}
