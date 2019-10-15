using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace HoloLensAppManager.Helpers
{
    public class BoolToEnumConverter : IValueConverter
    {
        // ConverterParameterをEnumに変換するメソッド
        private ProcessorArchitecture ConvertFromConverterParameter(object parameter)
        {
            string parameterString = parameter as string;
            return (ProcessorArchitecture)Enum.Parse(typeof(ProcessorArchitecture), parameterString);
        }

        #region IValueConverter メンバー
        // Enum → bool
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            // XAMLに定義されたConverterParameterをEnumに変換する
            ProcessorArchitecture parameterValue = ConvertFromConverterParameter(parameter);

            // ConverterParameterとバインディングソースの値が等しいか？
            return parameterValue.Equals(value);
        }

        // bool → Enum
        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            // true→falseの変化は無視する
            // ※こうすることで、選択されたラジオボタンだけをデータに反映させる
            if (!(bool)value)
                return DependencyProperty.UnsetValue;

            // ConverterParameterをEnumに変換して返す
            return ConvertFromConverterParameter(parameter);
        }
        #endregion
    }
}
