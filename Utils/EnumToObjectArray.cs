using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace GVisionWpf.Utils
{
    class EnumToObjectArray : MarkupExtension
    {
        public BindingBase? SourceEnum { get; set; }

        private static readonly DependencyProperty sourceEnumBindingSinkProperty =
            DependencyProperty.RegisterAttached("sourceEnumBindingSink", typeof(Enum), typeof(EnumToObjectArray),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            DependencyObject targetObject;

            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget
                {
                    TargetObject: DependencyObject, TargetProperty: DependencyProperty
                } target)
            {
                targetObject = (DependencyObject)target.TargetObject;
            }
            else
            {
                return this;
            }

            BindingOperations.SetBinding(targetObject, EnumToObjectArray.sourceEnumBindingSinkProperty, SourceEnum);

            var type = targetObject.GetValue(sourceEnumBindingSinkProperty).GetType();

            if (type.BaseType != typeof(System.Enum)) return this;

            return Enum.GetValues(type)
                .Cast<Enum>()
                .Select(e => new { Value = e, Name = e.ToString(), DisplayName = Description(e) });
        }

        public static string? Description(Enum value)
        {
            object[]? attributes = value.GetType().GetField(value.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Any())
                return (attributes.First() as DescriptionAttribute)?.Description;

            return value.ToString().Replace("_", " ");
        }
    }
}