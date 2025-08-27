using MwccInspectorUI.MVVM;
using static MwccInspectorUI.Model.MwccDebugger;
using static MwccInspectorUI.ViewModel.IRTokenViewModel;

namespace MwccInspectorUI.ViewModel {
    internal class SnapshotViewModel : ViewModelBase {
        public List<IRTokenViewModel> Statements { get; }

        public string FunctionName { get; }

        public string Title => $"Viewing function {FunctionName}";

        public string SnapshotText => string.Join("\n", Statements.Select(s => s.ToString()));


        public MwccTypeInspectorViewModel? _currentTypeVM = null;
        public MwccTypeInspectorViewModel? CurrentTypeVM {
            get { return _currentTypeVM; }
            set {
                _currentTypeVM = value;
                OnPropertyChanged();
            }
        }

        public SnapshotViewModel(Snapshot snapshot) {
            Statements = snapshot.Statements.ConvertAll(s => {
                var vm = new IRTokenViewModel(s);
                vm.TokenClicked += obj => {
                    if (obj == null) {
                        return;
                    }
                    var token = (IRToken)obj;
                    if (token.Data == null) {
                        return;
                    }
                    CurrentTypeVM = new($"Token {token.Data}", token.Data);
                };
                return vm;
            });
            FunctionName = snapshot.FunctionName;
        }
    }
}
