using GVisionWpf.Exceptions;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.ViewModels;
using System.Windows;
using ErrorWindow = GVisionWpf.UIs.Frames.Windows.ErrorWindow;

namespace GVisionWpf.UIs.UiUpdaters
{
    /// <summary>
    /// Global Error가 처리되는 시점:
    /// 1. App.cs에서 구독된 UnhandledException
    /// 2. 통신 코드 시작 부분
    /// </summary>
    class GlobalErrorHandler
    {
        public static void HandleException(Exception? ex)
        {
            // GVisionException으로 변환되지 않는 경우 기본값 할당
            GVisionException gEx = ex as GVisionException ?? new GVisionException(ex);
            gEx.StackTrace = ex?.StackTrace ?? "StackTrace를 제공할 수 없는 Exception입니다.";

            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Error] {ex?.Message}");

            if (ex is CameraTriggerException)
            {
                return;
            }

            if (ex is OperationCanceledException)
            {
                return;
            }

            if (ex is WrongGridSizeException)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    new AlertWindow("Error", "The grid size of the map teaching does not match the FOV size of the recipe.", AlertWindow.EAlert.YES).ShowDialog();
                });

                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorWindow errorWindow = new ErrorWindow(gEx.Message, $"[{gEx.ErrorCode}] {gEx.Message}", gEx.TroubleShooting, gEx.StackTrace);
                errorWindow.ShowDialog();

            });
        }
    }
}
