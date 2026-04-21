using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GVisionWpf.UIs.ViewModels.Calibrations;

namespace GVisionWpf.UIs.Frames.Tabs.Calibrations
{
    public abstract class CalibrationTabBase : UserControl
    {
        public abstract void OnAppear();
        public abstract void OnDisappear();

        protected CalibrationTabBase()
        {
            IsVisibleChanged += OnIsVisibleChanged;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!CalibrationViewModel.Instance.IsWindowOpened)
            {
                // HSmartWindow는 부모 Window의 Loaded 이벤트 이후 완전히 생성됩니다.
                // 그 전에 HSmartWindow에 ROI를 Attach하는 등의 작업은 할 수 없습니다.
                return;
            }

            if (IsVisible)
            {
                Dispatcher.BeginInvoke(new Action(OnAppear), DispatcherPriority.Loaded);
            }
            else
            {
                OnDisappear();
            }
        }
    }
}