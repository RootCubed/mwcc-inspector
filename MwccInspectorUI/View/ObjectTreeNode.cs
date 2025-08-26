using System.Collections.ObjectModel;

namespace MwccInspectorUI.View {
    class ObjectTreeNode {
        public string Name { get; }
        public object? Value { get; }
        public string DisplayValue { get; }
        public ObservableCollection<ObjectTreeNode> Children { get; } = [];

        public ObjectTreeNode(string name, object? value, int recursionDepth = 10) {
            Name = name;
            Value = value;

            if (value != null && !IsPrimitive(value.GetType()) && recursionDepth > 0) {
                foreach (var prop in value.GetType().GetProperties()) {
                    Children.Add(new ObjectTreeNode(prop.Name, prop.GetValue(value), recursionDepth - 1));
                }
                foreach (var field in value.GetType().GetFields()) {
                    Children.Add(new ObjectTreeNode(field.Name, field.GetValue(value), recursionDepth - 1));
                }
                DisplayValue = "";
            } else {
                DisplayValue = value?.ToString() ?? "";
            }
        }

        private static bool IsPrimitive(Type t) {
            if (t.IsGenericType) {
                Type gt = t.GetGenericTypeDefinition();
                if (gt == typeof(List<>) || gt == typeof(Dictionary<,>)) {
                    return true;
                }
            }
            return t.IsPrimitive || t == typeof(string) || t.IsEnum;
        }
    }
}
