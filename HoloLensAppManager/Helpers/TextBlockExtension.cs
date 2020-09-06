using System;
using System.Text.RegularExpressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace HoloLensAppManager.Helpers
{
    // ref: https://stackoverflow.com/questions/27734084/create-hyperlink-in-textblock-via-binding
    public static class TextBlockExtension
    {
        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.Register("FormattedText", typeof(string), typeof(TextBlockExtension),
            new PropertyMetadata(string.Empty, (sender, e) =>
            {
                string text = e.NewValue as string;
                if (sender is TextBlock textBl)
                {
                    textBl.Inlines.Clear();
                    Regex regx = new Regex(@"(https?://[^\s]+)", RegexOptions.IgnoreCase);
                    var str = regx.Split(text);
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (i % 2 == 0)
                        {
                            textBl.Inlines.Add(new Run { Text = str[i] });
                        }
                        else
                        {
                            Hyperlink link = new Hyperlink
                            {
                                NavigateUri = new Uri(str[i])
                            };
                            link.Inlines.Add(new Run { Text = str[i] });
                            textBl.Inlines.Add(link);
                        }
                    }
                }
            }));
    }
}
