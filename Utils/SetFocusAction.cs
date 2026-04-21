using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace GVisionWpf.Utils
{
    public class SetFocusAction : TriggerAction<UIElement>
    {
        protected override void Invoke(object parameter)
        {
            AssociatedObject?.Focus();
        }
    }
}