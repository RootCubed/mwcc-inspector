using MwccInspectorUI.Model;
using MwccInspectorUI.MVVM;
using static MwccInspectorUI.Model.MwccDebugger;

namespace MwccInspectorUI.ViewModel {
    internal class MainViewModel : ViewModelBase {
        public MwccDebugger Debugger { get; }
        public SnapshotListViewModel Snapshots { get; } = new();

        private bool _canStartDebugger = true;
        public bool CanStartDebugger {
            get => _canStartDebugger;
            set {
                _canStartDebugger = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel(string args) {
            Debugger = new MwccDebugger(args);
            Debugger.SnapshotBuilt += OnSnapshotBuilt;
        }

        private void OnSnapshotBuilt(object? sender, Snapshot snapshot) {
            Snapshots.AddSnapshot(snapshot);
        }

        public RelayCommand RunDebugger => new(execute => {
            Debugger.Run();
            CanStartDebugger = false;
        });
    }
}
