using System.Windows.Input;

namespace MwccInspectorUI.MVVM {
    internal class RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null) : ICommand {
        private Action<object> ExecuteHandler = execute;
        private Func<object, bool>? CanExecuteHandler = canExecute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) {
            if (CanExecuteHandler == null) {
                return true;
            }
            return CanExecuteHandler(parameter!);
        }

        public void Execute(object? parameter) {
            ExecuteHandler(parameter!);
        }
    }
}
