using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using GVisionWpf.UIs.ViewModels.Teaching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GVisionWpf.Utils
{
    public static class WindowHelper
    {
        public static async Task<TWindow> OpenWindowAsync<TWindow>() where TWindow : Window, new()
        {
            // TWindow 타입의 윈도우를 생성 및 반환하도록 수정
            var w = await WindowManager.OpenOrActivateAsync<TWindow>();
            return w;
        }

        // xname 가져오기
        //public static async Task SetThresholdAsync<TWindow>(string panelName, Threshold threshold)
        //    where TWindow : Window, new()
        //{
        //    var window = await OpenWindowAsync<TWindow>(); // await 필요
        //    var panel = window.FindName(panelName) as ThresholdControllerPanel;
        //    if (panel != null)
        //    {
        //        panel.Threshold = threshold;
        //        panel.Refresh();
        //    }
        //}

        // row column 값 파싱
        public static int?[]? parseIntArray_rc(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            // 하이픈(-)로 split 시도
            var parts = value.Split('-');

            int?[] result = new int?[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int temp))
                    result[i] = temp;
                else
                    result[i] = null; // 숫자가 아니면 null
            }

            return result;
        }

        public static int[]? parseIntArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            // 하이픈(-)로 split 시도
            var parts = value.Split('-');

            //int?[] result = new int?[parts.Length];

            //for (int i = 0; i < parts.Length; i++)
            //{
            //    if (int.TryParse(parts[i], out int temp))
            //        result[i] = temp;
            //    else
            //        result[i] = null; // 숫자가 아니면 null
            //}

            //return result;

            if (parts.Length == 2)
            {
                // 두 개 값이면 배열 반환
                if (int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                    return new int[] { min, max };
            }
            else if (parts.Length == 4)
            {
                if (int.TryParse(parts[0], out int v1) &&
                    int.TryParse(parts[1], out int v2) &&
                    int.TryParse(parts[2], out int v3) &&
                    int.TryParse(parts[3], out int v4))
                {
                    return new int[] { v1, v2, v3, v4 };
                }
            }
            else if (parts.Length == 1)
            {
                // split 안 됐으면 단일 int 처리
                if (int.TryParse(parts[0], out int single))
                    return new int[] { single };
            }

            // 변환 실패
            return null;
        }

        // 탭 바꿔주기
        //public static void SelectTabByName(LgaTeachingWindow window, string tabName)
        //{
        //    if (string.IsNullOrWhiteSpace(tabName))
        //        return;

        //    // API 이름 → 실제 탭 Header 매핑
        //    var tabMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        //    {
        //        { "RejectMark", "Reject Mark" },
        //        { "DontCare", "Don't Care" }
        //    };

        //    // 매핑된 이름 사용
        //    if (tabMap.TryGetValue(tabName, out var mappedTabName))
        //        tabName = mappedTabName;

        //    // 실제 탭 찾기
        //    var tab = xTabControl.Items
        //                         .OfType<TabItem>()
        //                         .FirstOrDefault(t => t.Header?.ToString().Equals(tabName, StringComparison.OrdinalIgnoreCase) == true);
        //    if (tab != null)
        //    {
        //        xTabControl.SelectedItem = tab;
        //        Debug.WriteLine($"[SelectTabByName] Switched to tab: {tabName}");
        //    }
        //    else
        //    {
        //        Debug.WriteLine($"[SelectTabByName] Tab not found: {tabName}");
        //    }
        //}



        // ViewModel 속성 이름으로 Threshold 값을 설정
        //public static async Task SetPanelThreshold<TWindow>(TWindow window, string panelName, string propertyName, string value)
        //   where TWindow : Window
        //{
        //    if (window == null) return;
        //    if (string.IsNullOrWhiteSpace(panelName) || string.IsNullOrWhiteSpace(propertyName)) return;

        //    // 문자열 value -> int 배열
        //    var thresholdValues = parseIntArray(value);
        //    if (thresholdValues == null || thresholdValues.Length < 2) return;

        //    var threshold = new Threshold(thresholdValues[0], thresholdValues[1]);

        //    await Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        // 1️⃣ Panel 찾기
        //        var panel = window.FindName(panelName) as ThresholdControllerPanel;
        //        if (panel != null)
        //        {
        //            panel.Threshold = threshold;
        //            panel.Refresh();
        //        }

        //        // 2️⃣ ViewModel 속성도 변경
        //        var vmProp = window.DataContext?.GetType().GetProperty("Teaching");
        //        if (vmProp != null)
        //        {
        //            var teachingObj = vmProp.GetValue(window.DataContext);
        //            if (teachingObj != null)
        //            {
        //                var propInfo = teachingObj.GetType().GetProperty(propertyName);
        //                if (propInfo != null && propInfo.CanWrite)
        //                {
        //                    propInfo.SetValue(teachingObj, threshold);
        //                }
        //            }
        //        }
        //    }



        //public static void SetViewModelThreshold(object viewModel, string propertyName, string value)
        //{
        //    var propertyInfo = viewModel?.Teaching.GetType().GetProperty("PackageThreshold");
        //    propertyInfo?.SetValue(viewModel?.Teaching, new Threshold(10, 200));
        //}


        private static void setIntValue(object vm, string propertyName, string value)
        {
            var thresholdValues = parseIntArray(value);
            if (thresholdValues == null || thresholdValues.Length < 2) return;

            if (vm is LgaTeachingViewModel viewModel)
            {
                // propertyName으로 Reflection 사용
                var propertyInfo = viewModel.Teaching.GetType().GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(viewModel.Teaching, new Threshold(thresholdValues[0], thresholdValues[1]));
                }
            }
        }


         // 'var'는 매개변수 타입으로 사용할 수 없으므로 object로 변경
        public static void ClassifyUpdateProperty(object vm, string propertyName, string value)
        {
            ArgumentNullException.ThrowIfNull(propertyName);
            switch (propertyName)
            {
                case string s when s.Contains("Threshold", StringComparison.OrdinalIgnoreCase):
                    setIntValue(vm, propertyName, value);
                    break;
                case "Size":
                    // SetViewModelSize(value);
                    break;
                case "Find":  //티칭 버튼
                    // SetViewModelSize(value);
                    break;
                case "Rois":  // ROI 버튼_add,delete,clear
                    // SetViewModelSize(value);
                    break;
                default:
                    break;
            }
        }

    }
}