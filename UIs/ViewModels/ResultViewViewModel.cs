using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using log4net.Filter;
using System.Collections.ObjectModel;

namespace GVisionWpf.UIs.ViewModels
{
    public class ResultViewViewModel : ViewModelBase
    {
        protected readonly ResultType2ItemTypeConverter Converter = new ResultType2ItemTypeConverter();
        private int totalCount = 0;
        private double averageDuration = 0;
        private readonly object updateLock = new object();

        public ObservableCollection<ResultViewStackViewModel> InspectionViewModels { get; } = new ObservableCollection<ResultViewStackViewModel>();

        #region Property

        public int TotalCount
        {
            get
            {
                return this.totalCount;
            }
            set
            {
                SetField(ref this.totalCount, value);
            }
        }

        public double AverageDuration
        {
            get
            {
                return this.averageDuration;
            }
            set
            {
                SetField(ref this.averageDuration, value);
            }
        }

        #endregion

        protected ResultViewViewModel() { }


        // 여기도 뷰모델에 분기처리가 있는데, 확인해 봐야 겠다
        protected void Initialize(EInspection inspection, Dictionary<EResultType, EColor> colors)
        {
            foreach ((EResultType item, EColor color) in colors)
            {
                if (item == EResultType.Good) { continue; }
                if (item == EResultType.NotInUsePicker) { continue; }

                var standardValue  = getStandardValue(inspection, item);
                var shouldShowStandardValue = GlobalSetting.Instance.SystemType == ESystemType.HanaMicron 
                    ? System.Windows.Visibility.Visible 
                    : System.Windows.Visibility.Collapsed;

                shouldShowStandardValue = System.Windows.Visibility.Collapsed;

                switch (inspection)
                {
                    case EInspection.Bga:
                        if (GlobalSetting.Instance.Inspection.BgaItems.Contains(this.Converter.Bga[item]))
                        {
                            ResultViewStackViewModel stackViewModel = new ResultViewStackViewModel(item, color.ToBrush(), standardValue, shouldShowStandardValue);
                            InspectionViewModels.Add(stackViewModel);
                        }
                        break;
                    case EInspection.Qfn:
                        if (GlobalSetting.Instance.Inspection.QfnItems.Contains(this.Converter.Qfn[item]))
                        {
                            ResultViewStackViewModel stackViewModel = new ResultViewStackViewModel(item, color.ToBrush(), standardValue, shouldShowStandardValue);
                            InspectionViewModels.Add(stackViewModel);
                        }
                        break;
                    case EInspection.Lga:
                        if (GlobalSetting.Instance.Inspection.LgaItems.Contains(this.Converter.Lga[item]))
                        {
                            ResultViewStackViewModel stackViewModel = new ResultViewStackViewModel(item, color.ToBrush(), standardValue, shouldShowStandardValue);
                            InspectionViewModels.Add(stackViewModel);
                        }
                        break;
                    case EInspection.Mark:
                        if (GlobalSetting.Instance.Inspection.MoldItems.Contains(this.Converter.Mold[item]))
                        {
                            ResultViewStackViewModel stackViewModel = new ResultViewStackViewModel(item, color.ToBrush(), standardValue, shouldShowStandardValue);
                            InspectionViewModels.Add(stackViewModel);
                        }
                        break;
                }
            }
        }

        public void Update(List<InspectionResult> results)
        {
            lock (this.updateLock)
            {
                foreach (InspectionResult result in results)
                {
                    update(result);
                }
            }
        }

        private void update(InspectionResult result)
        {
            if (result is SkipInspectionResult)
            {
                return;
            }

            double scaling = (result.InspectionType() == EInspection.Mapping) ? 0.5 : 0.2;
            result.Duration = (long)(result.Duration * scaling);

            TotalCount++;
            AverageDuration = (AverageDuration * (TotalCount - 1) + result.Duration) / TotalCount;

            EResultType errorType = result.ErrorType();

            foreach (ResultViewStackViewModel stackViewModel in InspectionViewModels)
            {
                if (stackViewModel.ErrorType == errorType)
                {
                    stackViewModel.Count++;
                }
            }
        }

        private string getStandardValue(EInspection inspection, EResultType resultType)
        {
            switch (inspection)
            {
                case EInspection.Mark:
                    return resultType switch
                    {
                        EResultType.PackageOffset => GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize.ToString(),
                        EResultType.PackageSize => GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize.ToString(),

                        EResultType.TextAngle => "0",
                        EResultType.TextOffset => "0",
                        _ => string.Empty
                    };
                case EInspection.Bga:
                    return string.Empty;

                case EInspection.Lga:
                    return string.Empty;

                case EInspection.Qfn:
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }
    }
}