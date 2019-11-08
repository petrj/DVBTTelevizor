using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using DVBTTelevizor.Droid;

[assembly: ResolutionGroupName("DVBTTelevizor")]
[assembly: ExportEffect(typeof(AndroidPressedEffect), "PressedEffect")]
namespace DVBTTelevizor.Droid
{
    /// <summary>
    /// https://alexdunn.org/2017/12/27/xamarin-tip-xamarin-forms-long-press-effect/
    /// </summary>
    public class AndroidPressedEffect : PlatformEffect
    {
        private bool _attached;

        public static void Initialize() { }

        protected override void OnAttached()
        {
            if (!_attached)
            {
                if (Control != null)
                {
                    Control.LongClickable = true;
                    Control.LongClick += Control_LongClick;

                    Control.Clickable = true;
                    Control.Click += Control_Click;
                }
                else
                {
                    Container.LongClickable = true;
                    Container.LongClick += Control_LongClick;

                    Container.Clickable = true;
                    Container.Click += Control_Click;
                }
                _attached = true;
            }
        }

        private void Control_Click(object sender, EventArgs e)
        {
            var command = PressedEffect.GetShortPressCommand(Element);
            command?.Execute(PressedEffect.GetShortPressCommandParameter(Element));
        }

        private void Control_LongClick(object sender, Android.Views.View.LongClickEventArgs e)
        {
            var command =  PressedEffect.GetLongPressCommand(Element);
            command?.Execute(PressedEffect.GetLongPressCommandParameter(Element));
        }

        protected override void OnDetached()
        {
            if (_attached)
            {
                if (Control != null)
                {
                    Control.LongClickable = false;
                    Control.LongClick -= Control_LongClick;

                    Control.Clickable = false;
                    Control.Click -= Control_Click;
                }
                else
                {
                    Container.LongClickable = false;
                    Container.LongClick -= Control_LongClick;

                    Container.Clickable = false;
                    Container.Click -= Control_Click;
                }
                _attached = false;
            }
        }
    }

}
