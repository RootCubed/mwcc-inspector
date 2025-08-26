using MwccInspectorUI.MVVM;
using System.Collections.ObjectModel;
using static MwccInspectorUI.Model.MwccDebugger;

namespace MwccInspectorUI.ViewModel {
    internal class SnapshotListViewModel : ViewModelBase {
        public ObservableCollection<SnapshotViewModel> Snapshots { get; } = [];

        private bool _isSnapshotViewVisible = false;
        public bool IsSnapshotViewVisible {
            get => _isSnapshotViewVisible;
            set {
                _isSnapshotViewVisible = value;
                OnPropertyChanged();
            }
        }

        private SnapshotViewModel? _selectedSnapshot;
        public SnapshotViewModel? SelectedSnapshot {
            get => _selectedSnapshot;
            set {
                _selectedSnapshot = value;
                IsSnapshotViewVisible = value != null;
                OnPropertyChanged();
            }
        }

        public void AddSnapshot(Snapshot snapshot) {
            Snapshots.Add(new SnapshotViewModel(snapshot));
        }
    }
}
