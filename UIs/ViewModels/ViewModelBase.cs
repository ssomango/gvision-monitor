using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using System.Runtime.CompilerServices;

namespace GVisionWpf.UIs.ViewModels
{
    public abstract partial class ViewModelBase : ObservableObject, IDisposable
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        ~ViewModelBase() => Dispose();

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void ShowInspectionResultText(IEnumerable<IInspectionResultModel> results)
        {
            int nGoodDevices = results.Count(result => InspectionResultConverter.ErrorTypeInEResultType(result) == EResultType.Good);
            int nBadDevices = results.Count(result => InspectionResultConverter.ErrorTypeInEResultType(result) != EResultType.Good);

            FixedText totalText = new FixedText($"TOTAL: {nGoodDevices + nBadDevices}, GOOD: {nGoodDevices}, BAD: {nBadDevices}", 1, nBadDevices > 0 ? EColor.Red : EColor.Green);

            CurrentTeachingWindow.Instance?.Window?.Display(totalText);
        }

        protected virtual void ShowTeachingResultText(IInspectionResultModel result)
        {
            var lgaResult = (LgaInspectionResult)result;

            List<FixedText> textList = new List<FixedText>();

            if (lgaResult.HasDevice.Type == EResultType.NoDevice)
            {
                textList.Add(new FixedText("Result : " + EResultType.NoDevice.ToString().ToUpper(), 1, EColor.Red));
            }
            else
            {
                EResultType resultType = InspectionResultConverter.ErrorTypeInEResultType(lgaResult);
                FixedText totalText = new FixedText("Result : " + resultType.ToString().ToUpper(), 1, resultType == EResultType.Good ? EColor.Green : EColor.Red);
                textList.Add(totalText);
            }

            CurrentTeachingWindow.Instance?.Window?.Display(textList.OrderBy(x => x.Sequence).ToList());
        }
    }
}