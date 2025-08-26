using MwccInspector.MwccTypes;
using MwccInspectorUI.MVVM;
using MwccInspectorUI.View;
using System.Collections.ObjectModel;

namespace MwccInspectorUI.ViewModel {
    class MwccTypeInspectorViewModel : ViewModelBase {
        public ObservableCollection<ObjectTreeNode> TypeTree { get; } = [];

        public MwccTypeInspectorViewModel(MwccCachedType type) {
            TypeTree.Clear();
            TypeTree.Add(new ObjectTreeNode(type.GetType().Name, type));
        }
    }
}
