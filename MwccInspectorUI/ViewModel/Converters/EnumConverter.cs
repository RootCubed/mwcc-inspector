using MwccInspector.MwccTypes;
using System.Globalization;
using System.Windows.Data;

namespace MwccInspectorUI.ViewModel.Converters {
    class AccessTypeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                return (AccessType)value switch {
                    AccessType.ACCESSPUBLIC => "P",
                    AccessType.ACCESSPRIVATE => "P",
                    AccessType.ACCESSPROTECTED => "P",
                    _ => "-"
                };
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
