using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using System.Windows;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class StatisticsPanelViewModel : ViewModelBase
    {
        #region Property

        [ObservableProperty]
        private EInspection prsInspection;
       
        [ObservableProperty]
        private int prsTotal;

        [ObservableProperty]
        private int prsGood;

        [ObservableProperty]
        private int prsNoDevice;

        [ObservableProperty]
        private int prsReject;

        [ObservableProperty]
        private int prsXOut;
   
        [ObservableProperty]
        private EInspection mappingInspection;   

        [ObservableProperty]
        private int mappingTotal;

        [ObservableProperty]
        private int mappingGood;

        [ObservableProperty]
        private int mappingNoDevice;

        [ObservableProperty]
        private int mappingReject;

        [ObservableProperty]
        private int mappingXOut;

        [ObservableProperty]
        private EInspection side;

        [ObservableProperty]
        private int sideTotal;

        [ObservableProperty]
        private int sideGood;

        [ObservableProperty]
        private int sideNoDevice;

        [ObservableProperty]
        private int sideReject;

        [ObservableProperty]
        private int sideXOut = 0;

        #endregion

        partial void OnPrsInspectionChanged(EInspection value) => InitializeFields();

        partial void OnMappingInspectionChanged(EInspection value) => InitializeFields();

        partial void OnSideChanged(EInspection value) => InitializeFields();

        public StatisticsPanelViewModel()
        {
            var recipe = DeviceRecipeRepository.Instance.GetRecipe();
            PrsInspection = recipe.PrsPackageType;
            MappingInspection = recipe.MapPackageType;

            Side = EInspection.DataCode;

            GVisionMessenger.Instance.RegisterAll(this);
        }


        public void UpdateInfo<T>(EInspection type, List<T> results) where T : InspectionResult
        {
            foreach (T result in results)
            {
                UpdateInfo(type, result.ErrorType());
            }
        }

        // 여기도 확인 필요
        public void UpdateInfo(EInspection type, EResultType errorType)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                switch (type)
                {
                    case EInspection.Qfn:
                    case EInspection.Bga:
                    case EInspection.Lga:
                        updatePrsCounters(errorType);
                        break;

                    case EInspection.Mapping:
                        updateMappingCounters(errorType);
                        break;

                    case EInspection.Mark:
                    case EInspection.DataCode:
                    case EInspection.BottomDataCode:
                        break;
                }
            });
        }

        public void InitializeFields()
        {
            PrsTotal = 0;
            PrsGood = 0;
            PrsNoDevice = 0;
            PrsReject = 0;

            MappingTotal = 0;
            MappingGood = 0;
            MappingNoDevice = 0;
            MappingReject = 0;

            SideTotal = 0;
            SideGood = 0;
            SideNoDevice = 0;
            SideReject = 0;
        }

        private void updatePrsCounters(EResultType errorType)
        {
            PrsTotal++;

            if (errorType == EResultType.XOut || errorType == EResultType.XOut2)
            {
                PrsXOut++;
                return;
            }

            if (errorType == EResultType.Good)
                PrsGood++;
            else
                PrsReject++;

            if (errorType == EResultType.NoDevice)
                PrsNoDevice++;
        }

        private void updateMappingCounters(EResultType errorType)
        {
            MappingTotal++;

            if (errorType == EResultType.XOut || errorType == EResultType.XOut2)
            {
                MappingXOut++;
                return;
            }

            if (errorType == EResultType.Good)
                MappingGood++;
            else
                MappingReject++;

            if (errorType == EResultType.NoDevice)
                MappingNoDevice++;
        }
    }

    partial class StatisticsPanelViewModel : IRecipient<PrsInspectionUIUpdateMessage>, IRecipient<PrsInspectionTypeChangedMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.AddInspectionResult:
                    if (message.RenderableResult is null)
                        return;

                    EInspection type = DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType;
                    UpdateInfo(type, message.RenderableResult.InspectionResult.ErrorType());
                    break;

                default:
                    break;
            }
        }

        public void Receive(PrsInspectionTypeChangedMessage message) => PrsInspection = message.Value;
    }

    partial class StatisticsPanelViewModel : IRecipient<MoldInspectionUIUpdateMessage>, IRecipient<MoldInspectionTypeChangedMessage>
    {
        public void Receive(MoldInspectionUIUpdateMessage message)
        {
           switch (message.UpdateType)
            {
                case EMoldInspectionUIUpdateType.AddInspectionResult:
                    UpdateInfo(EInspection.Mapping, message.RenderableResults.Select(r => r.InspectionResult).ToList());
                    break;

                default:
                    break;
            }
        }

        public void Receive(MoldInspectionTypeChangedMessage message) => MappingInspection = message.Value;
    }
}