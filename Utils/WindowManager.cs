using GVisionWpf.Api;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using GVisionWpf.UIs.Overlays;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.UIs.ViewModels.Teaching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

public static class WindowManager
{
    private static readonly Dictionary<string, string> TitleMapping = new Dictionary<string, string>
    {
        { "Map Teaching Wizard", "mapping" },
        { "BGA Teaching Wizard", "bga" },
        { "LGA Teaching Wizard", "lga" },
        { "QFN Teaching Wizard" , "qfn"},
        { "Strip Teaching Wizard", "strip" },
        { "HistoryWindow", "history"},
        { "SettingsWindow","settings" },
        { "Calibration Wizard", "calibration"},
        { "LightWindow", "light"}
        // 필요한 만큼 추가
    };

    private static readonly HashSet<string> ExcludeList = new()
    {
        "MainWindow", "ChatWindow",
        "Camera 1", "Camera 2", "Camera 3",
        "Camera 4", "Camera 5", "Camera 6",
        "FloatingMenuWindow", ""
    };

    private static Dispatcher Ui => Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    // UI 스레드에서 액션을 실행 (동기 방식)
    private static void OnUI(Action action)
    {
        if (Ui.CheckAccess()) action();
        else Ui.Invoke(action);
    }

    // UI 스레드에서 함수 실행 후 결과 반환 (동기 방식)
    private static T OnUI<T>(Func<T> func)
    {
        return Ui.CheckAccess() ? func() : Ui.Invoke(func);
    }

    // 현재 열린 윈도우 중 지정한 타입의 첫 번째 윈도우 반환
    private static TWindow? FirstWindow<TWindow>() where TWindow : Window
    {
        return Application.Current?.Windows.Cast<Window>().OfType<TWindow>().FirstOrDefault();
    }

    // 열려있는 자식창 리스트(나중에 자식 창 끌 때 필요)
    private static List<Window> _childWindows = new List<Window>();
    public static void ShowWindow<T>() where T : Window, new()
    {
        var chatWindow = Application.Current.Windows
                             .OfType<ChatWindow>()
                             .FirstOrDefault();

        var child = new T
        {
            Owner = chatWindow,
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual
        };

        if (chatWindow != null)
        {
            child.Left = chatWindow.Left - child.Width - 30;
            child.Top = chatWindow.Top;
        }

        child.Show();
        _childWindows.Add(child);
        Debug.WriteLine(child);
    }

    // 티칭 창들은 애를 사용해서 켜야 창이 메인 윈도우 창 뒤로 안감
    // 채팅 창 오너로 지정하고, 위치도 채팅창 기준으로 잡음
    // _childWindows 리스트에도 추가
    // 메인 윈도우 뒤로 안가게 하려면 Owner 지정 필수
    private static void ShowTeachingWindow<T>(Action<T>? initializer = null) where T : System.Windows.Window, new()
    {
        var chatWindow = Application.Current.Windows
                         .OfType<ChatWindow>()
                         .FirstOrDefault();

        var child = new T
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
            Owner = chatWindow,            // ← ChatWindow를 Owner로 지정
            ShowInTaskbar = false,         // 태스크바에 안 나타나게
            Topmost = false                 // 필요시 true 가능, 보통 Owner만으로 충분
        };

        initializer?.Invoke(child);

        // Loaded 시점에서 위치 계산 (Width/Height 확정 후)
        child.Loaded += (s, e) =>
        {
            if (chatWindow != null)
            {
                child.Left = chatWindow.Left - child.Width - 30;
                child.Top = chatWindow.Top;
            }
        };

        child.Show();
        _childWindows.Add(child);
    }
    private static async Task<ApiResult> OpenTeachingWindow<T>() where T : Window, new()
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // 이미지 파일 선택 대화상자를 먼저 보여줍니다
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Teaching Image",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.tif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // 이미지를 로드하고 티칭창을 엽니다
                    HOperatorSet.ReadImage(out HObject image, openFileDialog.FileName);

                    ShowTeachingWindow<T>(w =>
                    {
                        // TeachingImage 속성이 있을 때만 설정
                        var prop = typeof(T).GetProperty("TeachingImage");
                        if (prop != null && prop.CanWrite)
                            prop.SetValue(w, image);
                    });

                    // window.Show();
                    SystemInformationViewModel.Instance.Print("LGA teaching window opened via API");
                    return new ApiResult { Success = true, Message = "LGA teaching window opened successfully" };
                }
                else
                {
                    return new ApiResult { Success = false, Message = "Image selection cancelled" };
                }
            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open LGA teaching window: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Failed to open LGA teaching window: {ex.Message}" };
            }
        });
    }
    private static void ShowTeachingWindow2<T>(Func<T>? factory = null) where T : Window
    {
        var chatWindow = Application.Current.Windows.OfType<ChatWindow>().FirstOrDefault();

        // 기존 new() 대신 factory 사용
        var child = factory != null ? factory() : Activator.CreateInstance<T>();

        child.WindowStartupLocation = WindowStartupLocation.Manual;
        child.Owner = chatWindow;
        child.ShowInTaskbar = false;
        child.Topmost = false;

        child.Loaded += (s, e) =>
        {
            if (chatWindow != null)
            {
                child.Left = chatWindow.Left - child.Width - 30;
                child.Top = chatWindow.Top;
            }
        };

        child.Show();
        _childWindows.Add(child);
    }
    private static async Task<ApiResult> openMapTeachingWindow()
    {
        List<string> selectedFileNames = new List<string>();
        try
        {
            //var OpenedMapTeachingWindow = await WindowManager.OpenOrActivateAsync<GridMoldTeachingWindow>();
            //return new ApiResult { Success = true, Message = "Map teaching window opened successfully" };
            // 맵핑 카메라의 shot 수를 가져옵니다
            int nShots = IlluminationService.Instance.GetShotCount(ECamera.Mapping);
            ObservableCollection<HObject> shots = new ObservableCollection<HObject>();
            bool hasTop6 = false;
            int top6ShotIndex = -1;

            // 각 shot에 대해 이미지를 선택합니다
            for (int i = 1; i <= nShots; i++)
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = $"Select Mapping Shot #{i} Image",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.tif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    MappingTeachingStartFlow.NotifyFileSelectionSignal(
                        MappingTeachingStartFlow.FileSelectionSignal.Canceled,
                        -1,
                        selectedFileNames);
                    SystemInformationViewModel.Instance.Print($"Image selection cancelled for shot {i}");
                    return new ApiResult { Success = false, Message = $"Image selection cancelled for shot {i}" };
                }

                // 이미지를 로드하고 shots 컬렉션에 추가합니다
                selectedFileNames.Add(openFileDialog.FileName);
                string selectedFileName = Path.GetFileName(openFileDialog.FileName);
                Debug.WriteLine($"[MapTeachingTutor] selected shot={i}, file={selectedFileName}");
                SystemInformationViewModel.Instance.Print($"[MapTeachingTutor] selected shot={i}, file={selectedFileName}");

                if (!hasTop6 && string.Equals(selectedFileName, "top6.png", StringComparison.OrdinalIgnoreCase))
                {
                    hasTop6 = true;
                    top6ShotIndex = i - 1;
                    Debug.WriteLine($"[MapTeachingTutor] top6 trigger detected at shotIndex={top6ShotIndex} (shotNo={i})");
                    SystemInformationViewModel.Instance.Print($"[MapTeachingTutor] top6 trigger detected at shotIndex={top6ShotIndex} (shotNo={i})");
                }

                HOperatorSet.ReadImage(out HObject image, openFileDialog.FileName);
                shots.Add(image);
            }

            GridMoldTeachingFlow.SetTriggerBySelectedFiles(selectedFileNames, hasTop6, top6ShotIndex);
            MappingTeachingStartFlow.NotifyFileSelectionSignal(
                hasTop6
                    ? MappingTeachingStartFlow.FileSelectionSignal.Top6Selected
                    : MappingTeachingStartFlow.FileSelectionSignal.CompletedWithoutTop6,
                top6ShotIndex,
                selectedFileNames);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = new GridMoldTeachingWindow(shots);
                if (MappingTeachingStartFlow.TryGetActiveMappingTutorToken(out var token))
                {
                    window.MappingTutorGateToken = token;
                    Debug.WriteLine($"[MapTeachingTutor] gate token attached to GridMoldTeachingWindow token={token}");
                    SystemInformationViewModel.Instance.Print($"[MapTeachingTutor] gate token attached token={token}");
                }
                else
                {
                    Debug.WriteLine("[MapTeachingTutor] no active mapping tutor token; window opens without tutor gate token.");
                }

                ShowTeachingWindow2(() => window);
            });

            SystemInformationViewModel.Instance.Print("Mapping teaching window opened via API");
            return new ApiResult { Success = true, Message = "Mapping teaching window opened successfully" };
        }
        catch (Exception ex)
        {
            MappingTeachingStartFlow.NotifyFileSelectionSignal(
                MappingTeachingStartFlow.FileSelectionSignal.Failed,
                -1,
                selectedFileNames);
            SystemInformationViewModel.Instance.Print($"Failed to open mapping teaching window: {ex.Message}");
            return new ApiResult { Success = false, Error = $"Failed to open mapping teaching window: {ex.Message}" };
        }

    }

    // 윈도우가 열려 있으면 활성화, 없으면 새로 생성하여 표시
    public static async Task<TWindow> OpenOrActivateAsync<TWindow>(Func<TWindow>? factory = null)
        where TWindow : Window, new()
    {
        return await OnUI(async () =>   //비동기로 바꿈,,, 티칭 창은 이미지 찾느라 시간이 걸릴테니까
        {
            var w = FirstWindow<TWindow>();
            if (w == null || !w.IsLoaded)
            {
                if (typeof(TWindow) == typeof(GridMoldTeachingWindow))
                {
                    await openMapTeachingWindow();
                }
                else if (typeof(TWindow).Name.Contains("Teaching", StringComparison.OrdinalIgnoreCase))
                {
                    // ShowTeachingWindow<TWindow>();
                    await OpenTeachingWindow<TWindow>();
                }
                else
                {
                    ShowWindow<TWindow>();
                }

                // 새로 연 창 참조 가져오기
                w = FirstWindow<TWindow>();
            }
            else
            {
                // 이미 열려 있으면 활성화
                if (w.WindowState == WindowState.Minimized)
                    w.WindowState = WindowState.Normal;

                w.Activate();
                w.Topmost = true;
                w.Topmost = false;
                w.Focus();
            }

            return w!;
        });
    }


    // 지정한 타입의 윈도우가 열려 있다면 반환
    public static bool TryGetWindow<TWindow>(out TWindow? window) where TWindow : Window
    {
        window = OnUI(FirstWindow<TWindow>);
        return window != null;
    }

    // 윈도우의 DataContext에서 지정한 타입의 ViewModel 가져오기
    public static bool TryGetViewModel<TWindow, TViewModel>(out TViewModel? viewModel)
        where TWindow : Window
        where TViewModel : class
    {
        viewModel = OnUI(() =>
        {
            var w = FirstWindow<TWindow>();
            return w?.DataContext as TViewModel;
        });
        return viewModel != null;
    }

    // 특정 타입의 윈도우가 열려 있는지 확인
    public static bool IsOpen<TWindow>() where TWindow : Window
    {
        return OnUI(() => FirstWindow<TWindow>() != null);
    }

    // 특정 타입의 윈도우를 화면 맨 앞으로 가져오기
    public static void BringToFront<TWindow>() where TWindow : Window
    {
        OnUI(() =>
        {
            var w = FirstWindow<TWindow>();
            if (w == null) return;
            if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
            w.Activate();
            w.Topmost = true;
            w.Topmost = false;
            w.Focus();
        });
    }

    // 다이얼로그 윈도우를 표시하고 결과를 반환
    public static bool ShowDialog<TWindow>(out bool? dialogResult, Func<TWindow>? factory = null) where TWindow : Window, new()
    {
        bool? result = null;
        OnUI(() =>
        {
            var w = FirstWindow<TWindow>() ?? (factory != null ? factory() : new TWindow());
            result = w.ShowDialog();
        });
        dialogResult = result;
        return result.HasValue;
    }

    // UI 스레드에서 비동기로 함수 실행 후 결과 반환
    public static Task<T> OnUIAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        return Ui.CheckAccess() ? Task.FromResult(func()) : Ui.InvokeAsync(func, priority).Task;
    }

    // UI 스레드에서 비동기로 액션 실행
    public static Task OnUIAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        if (Ui.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        return Ui.InvokeAsync(action, priority).Task;
    }

    // 현재 열려있는 모든 윈도우의 타이틀을 리스트로 반환
    public static List<string> GetOpenWindowTitles()
    {
        var titles = new List<string>();

        foreach (Window window in Application.Current.Windows)
        {
            titles.Add(window.Title);  // 각 창의 Title
        }

        return titles;
    }
    //public static async Task CloseAllChildWindowsAsync()
    //{
    //    Debug.WriteLine("CloseAllChildWindows 들어옴");
    //    //_childWindows = WindowManager.GetOpenWindowTitles();
    //    foreach (var child in _childWindows.ToList())
    //    {
    //        if (child.IsLoaded)
    //            child.Close();
    //    }
    //    _childWindows.Clear();
    //}

    public static async Task CloseAllChildWindowsAsync(string windowName)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // 닫을 후보 창 목록
            var windowsToClose = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .Where(w => w != System.Windows.Application.Current.MainWindow &&
                            !ExcludeList.Contains(w.Title))
                .ToList();

            // Debug.WriteLine("from CloseAllChildWindowsAsync WindowName " + windowName);
            // windowName이 비어 있지 않으면 (특정 창만 닫기)
            if (!string.IsNullOrWhiteSpace(windowName))
            {
                // Debug.WriteLine("WindowName 있어유");
                // TitleMapping에서 축약 이름을 실제 창 제목으로 역매핑
                var targetTitle = TitleMapping
                    .FirstOrDefault(x => x.Value.Equals(windowName, StringComparison.OrdinalIgnoreCase))
                    .Key;

                if (!string.IsNullOrEmpty(targetTitle))
                {
                    var window = windowsToClose.FirstOrDefault(w => w.Title == targetTitle);
                    if (window != null && window.IsLoaded)
                        window.Close();
                }
            }
            else
            {
                Debug.WriteLine("WindowName 없어유");
                // windowName이 비어 있으면 모든 창 닫기 (기존 동작 유지)
                foreach (var w in windowsToClose)
                {
                    if (w.IsLoaded)
                        w.Close();
                }
            }
        });
    }



    // 창에서 현재 선택된 탭 이름 반환
    public static string GetSelectedTabNameFromWindow(Window window)
    {
        string tabName = "NoTab";

        window.Dispatcher.Invoke(() =>
        {
            var tabControl = FindTabControl(window);
            if (tabControl?.SelectedItem is TabItem tab)
            {
                tabName = tab.Header?.ToString() ?? "NoTab";
            }
        });
        return tabName;
    }

    // 재귀적으로 자식에서 TabControl 탐색
    private static TabControl? FindTabControl(DependencyObject parent)
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is TabControl tabControl)
                return tabControl;

            var result = FindTabControl(child); // 재귀 탐색
            if (result != null)
                return result;
        }
        return null;
    }

    // 새 함수: 모든 열려있는 창과 선택된 탭 반환
    public static Dictionary<string, string> GetOpenWindowsWithTabsFiltered()
    {
        var result = new Dictionary<string, string>();
        //var excludeTitles = new HashSet<string> 
        //{ "MainWindow", "ChatWindow", "Camera 1", "Camera 2", "Camera 3", "Camera 4", "Camera 5", "Camera 6", "" };

        //var titleMapping = new Dictionary<string, string>
        //{
        //    { "Map Teaching Wizard", "mapping" },
        //    { "BGA Teaching Wizard", "bga" },
        //    { "LGA Teaching Wizard", "lga" },
        //    { "QFN Teaching Wizard" , "qfn"},
        //    { "Strip Teaching Wizard", "strip" },
        //    { "HistoryWindow", "history"},
        //    {"SettingsWindow","settings" },
        //    { "Calibration Wizard", "calibration"},
        //    { "LightWindow", "light"}
        //    // 필요한 만큼 추가
        //};

        foreach (Window window in Application.Current.Windows)
        {
            string title = window.Title;

            if (ExcludeList.Contains(title))
                continue; // 제외

            string tabName = GetSelectedTabNameFromWindow(window);

            // 매핑이 있으면 매핑 이름 사용, 없으면 원본 제목
            string mappedTitle = TitleMapping.ContainsKey(title) ? TitleMapping[title] : title;

            result[mappedTitle] = tabName;
        }
        return result;
    }

}
