using MwccInspectorUI.MVVM;
using static MwccInspectorUI.Model.MwccDebugger;

namespace MwccInspectorUI.ViewModel {
    internal class SnapshotViewModel(Snapshot snapshot) : ViewModelBase {
        public List<IRTokenViewModel> Statements { get; } = snapshot.Statements.ConvertAll(s => new IRTokenViewModel(s));

        public string FunctionName => snapshot.FunctionName;

        public string Title => $"Viewing function {FunctionName}";

        public string SnapshotText => string.Join("\n", Statements.Select(s => s.ToString()));
    }
}
