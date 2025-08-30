using MwccInspectorUI.MVVM;
using System.Collections.ObjectModel;

namespace MwccInspectorUI.ViewModel {
    class MwccTypeInspectorViewModelLeaf : ViewModelBase {
        public string Name { get; }
        public object? Value { get; }
        public string DisplayValue { get; }
        public MwccTypeInspectorViewModelLeaf(string name, object? value) {
            Name = name;
            DisplayValue = value?.ToString() ?? "";
        }
    }
    class MwccTypeInspectorViewModel : MwccTypeInspectorViewModelLeaf {
        public ObservableCollection<MwccTypeInspectorViewModelLeaf> Children { get; } = [];
        public MwccTypeInspectorViewModel(string name, object? value, int recursionDepth = 10) : base(name, value) {
            if (value == null || recursionDepth == 0) {
                return;
            }
            if (TryAsList(value, out var list)) {
                for (int i = 0; i < list.Count; i++) {
                    Children.Add(new MwccTypeInspectorViewModel($"name[{i}]", list[i], recursionDepth - 1));
                }
                return;
            }
            foreach (var prop in value.GetType().GetProperties()) {
                var v = prop.GetValue(value);
                if (v == null) {
                    continue;
                }
                if (IsLeafType(v.GetType())) {
                    Children.Add(new MwccTypeInspectorViewModelLeaf(prop.Name, v));
                } else {
                    Children.Add(new MwccTypeInspectorViewModel(prop.Name, recursionDepth - 1));
                }
            }
            foreach (var field in value.GetType().GetFields()) {
                var v = field.GetValue(value);
                if (v == null) {
                    continue;
                }
                if (IsLeafType(v.GetType())) {
                    Children.Add(new MwccTypeInspectorViewModelLeaf(field.Name, v));
                } else {
                    Children.Add(new MwccTypeInspectorViewModel(field.Name, v, recursionDepth - 1));
                }
            }
        }

        private static bool IsLeafType(Type t) {
            if (t.IsGenericType) {
                Type gt = t.GetGenericTypeDefinition();
                if (gt == typeof(Dictionary<,>)) {
                    return true;
                }
            }
            return t.IsPrimitive || t == typeof(string) || t.IsEnum;
        }

        private static bool TryAsList(object v, out List<object> l) {
            if (v.GetType().IsGenericType) {
                Type gt = v.GetType().GetGenericTypeDefinition();
                if (gt == typeof(List<>)) {
                    l = ((IEnumerable<object>)v).Cast<object>().ToList();
                    return true;
                }
            }
            l = [];
            return false;
        }
    }
}
