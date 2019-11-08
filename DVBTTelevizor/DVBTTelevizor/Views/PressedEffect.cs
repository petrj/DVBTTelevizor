using System.Windows.Input;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    /// <summary>
    /// https://alexdunn.org/2017/12/27/xamarin-tip-xamarin-forms-long-press-effect/
    /// </summary>
    public class PressedEffect : RoutingEffect
    {
        public PressedEffect() : base("DVBTTelevizor.PressedEffect") { }

        public static readonly BindableProperty LongPressCommandProperty = BindableProperty.CreateAttached("LongPressCommand", typeof(ICommand), typeof(PressedEffect), (object)null);
        public static readonly BindableProperty ShortPressCommandProperty = BindableProperty.CreateAttached("ShortPressCommand", typeof(ICommand), typeof(PressedEffect), (object)null);

        public static ICommand GetLongPressCommand(BindableObject view)
        {
            return (ICommand)view.GetValue(LongPressCommandProperty);
        }

        public static void SetLongPressCommand(BindableObject view, ICommand value)
        {
            view.SetValue(LongPressCommandProperty, value);
        }

        public static ICommand GetShortPressCommand(BindableObject view)
        {
            return (ICommand)view.GetValue(ShortPressCommandProperty);
        }

        public static void SetShortPressCommand(BindableObject view, ICommand value)
        {
            view.SetValue(ShortPressCommandProperty, value);
        }

        public static readonly BindableProperty LongPressCommandParameterProperty = BindableProperty.CreateAttached("LongPressCommandParameter", typeof(object), typeof(PressedEffect), (object)null);
        public static readonly BindableProperty ShortPressCommandParameterProperty = BindableProperty.CreateAttached("ShortPressCommandParameter", typeof(object), typeof(PressedEffect), (object)null);


        public static object GetLongPressCommandParameter(BindableObject view)
        {
            return view.GetValue(LongPressCommandParameterProperty);
        }

        public static void SetLongPressCommandParameter(BindableObject view, object value)
        {
            view.SetValue(LongPressCommandParameterProperty, value);
        }

        public static object GetShortPressCommandParameter(BindableObject view)
        {
            return view.GetValue(ShortPressCommandParameterProperty);
        }

        public static void SetShortPressCommandParameter(BindableObject view, object value)
        {
            view.SetValue(ShortPressCommandParameterProperty, value);
        }
    }
}