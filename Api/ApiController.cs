using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using GVisionWpf.GlobalStates;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.UIs.Overlays;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DSMMI;
using GVisionWpf.DSMMI.Inspection;
using GVisionWpf.Illuminations;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using GVisionWpf.UIs.ViewModels.Calibrations;
using GVisionWpf.UIs.ViewModels.Teaching;
using System.Threading;





namespace GVisionWpf.Api
{
    ///
    /// <summary>
    /// Controller for API related operations.
    /// This controller provides information about the API endpoints available and application state.
    /// </summary>
    /// 

    public class ApiController
    {
        private static ApiController? _instance;
        public static ApiController Instance => _instance ??= new ApiController();

        private ApiController()
        {
        }
        // ApiServer가 접근하는 MainWindowViewModel 참조
        public MainWindowViewModel? MainViewModel { get; set; }

        // ✅ "개념 anchor"가 들어오면 "UI 위치 anchor"로 변환
        private static string RouteToUiAnchor(string anchorKey)
        {
            if (string.IsNullOrWhiteSpace(anchorKey)) return anchorKey;

            // ✅ 이미 UI anchor면 그대로
            if (anchorKey.StartsWith("MENU_", StringComparison.OrdinalIgnoreCase))
                return anchorKey;

            if (anchorKey.Equals("RUN_SETUP_TOGGLE", StringComparison.OrdinalIgnoreCase))
                return anchorKey;

            // ✅ 개념/동의어 -> UI anchor로 변환
            switch (anchorKey.Trim().ToUpperInvariant())
            {
                // 그룹
                case "TEACHING": return "MENU_TEACHING";
                case "SPC": return "MENU_SPC";
                case "SYSTEM": return "MENU_SYSTEM";
                case "SETUP": return "MENU_SETUP_GROUP";  
                // SPC 하위
                case "HISTORY": return "MENU_HISTORY";
                case "LOT":
                case "LOTDATA":
                case "LOT_DATA": return "MENU_LOT_DATA";

                // SETUP 하위
                case "SETTINGS": return "MENU_SETTINGS";
                case "CALIBRATION": return "MENU_CALIBRATION";
                case "LIGHT": return "MENU_LIGHT";

                // SYSTEM 하위
                case "MONITOR": return "MENU_MONITOR";
                case "AS":
                case "A/S": return "MENU_AS";
                case "EXIT": return "MENU_EXIT";

                // TEACHING 하위
                case "BGA": return "MENU_BGA";
                case "QFN": return "MENU_QFN";
                case "LGA": return "MENU_LGA";
                case "MAPPING": return "MENU_MAPPING";
                case "SIDE": return "MENU_SIDE";
                case "STRIP": return "MENU_STRIP";
                case "SMART_ALIGN":
                case "SMART ALIGN": return "SMART_ALIGN";

                // 패널류(Resolver가 FindName으로 찾게 만들 예정이면)
                case "MAIN_BOTTOM_INFO": return "MAIN_BOTTOM_INFO";
                case "MAIN_TOP_INFO": return "MAIN_TOP_INFO";

                // 카메라 보기: 결국 Light 메뉴로 안내
                case "SETUP_CAMERA_VIEW": return "MENU_LIGHT";

                default:
                    return anchorKey;
            }
        }



        #region Tutor Mode1 (POST JSON) - NEW

        public sealed class TutorMode1Request
        {
            public string? request_id { get; set; }
            public string? anchor_key { get; set; }
            public TutorCardPayload? card { get; set; }
            public TutorOptionsPayload? options { get; set; }
            public Dictionary<string, object>? meta { get; set; }
        }

        public sealed class TutorCardPayload
        {
            public string? title { get; set; }
            public string? body { get; set; }
        }
        public sealed class UiAnchorKbItem
        {
            public string? anchor_key { get; set; }
            public List<string>? aliases { get; set; }
            public List<string>? ui_elements { get; set; }
            public TutorCardPayload? mode1_card { get; set; }
            public bool supports_practice { get; set; }
            public string? practice_type { get; set; }
            public string? followup_prompt { get; set; }
        }
        private static readonly object _kbLock = new object();
        private static Dictionary<string, UiAnchorKbItem>? _kbByKey;
        private static DateTime _kbLastWriteUtc = DateTime.MinValue;

        private static string GetKbPath()
        {
            // ✅ 프로젝트에 kb/ui_anchor_kb.jsonl을 포함시키고 Output Directory로 복사하도록 설정하는 걸 추천
            // 예: <None Include="kb\ui_anchor_kb.jsonl" CopyToOutputDirectory="PreserveNewest" />
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kb", "ui_anchor_kb.jsonl");
        }
        private static void EnsureKbLoaded(bool forceReload = false)
        {
            var path = GetKbPath();

            lock (_kbLock)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        Debug.WriteLine($"[Tutor] KB not found: {path}");
                        _kbByKey = new Dictionary<string, UiAnchorKbItem>(StringComparer.OrdinalIgnoreCase);
                        _kbLastWriteUtc = DateTime.MinValue;
                        return;
                    }

                    var lastWrite = File.GetLastWriteTimeUtc(path);

                    if (!forceReload && _kbByKey != null && lastWrite == _kbLastWriteUtc)
                        return;

                    var dict = new Dictionary<string, UiAnchorKbItem>(StringComparer.OrdinalIgnoreCase);

                    foreach (var line in File.ReadLines(path))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var item = JsonSerializer.Deserialize<UiAnchorKbItem>(line, _tutorJsonOpt);
                            if (item?.anchor_key == null) continue;
                            dict[item.anchor_key] = item; // 중복 key면 마지막이 덮어씀
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Tutor] KB parse error: {ex.Message}");
                        }
                    }

                    _kbByKey = dict;
                    _kbLastWriteUtc = lastWrite;

                    Debug.WriteLine($"[Tutor] KB loaded: {dict.Count} anchors from {path}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tutor] EnsureKbLoaded failed: {ex}");
                    _kbByKey = new Dictionary<string, UiAnchorKbItem>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        private static UiAnchorKbItem? FindKbItem(string anchorKey)
        {
            EnsureKbLoaded();
            if (_kbByKey == null) return null;

            _kbByKey.TryGetValue(anchorKey, out var item);
            return item;
        }

        public sealed class TutorOptionsPayload
        {
            public bool? spotlight { get; set; }   // default true
            public bool? toast { get; set; }       // default false
            public int? duration_ms { get; set; }  // default 5000
        }

        private static readonly JsonSerializerOptions _tutorJsonOpt = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// POST /tutor/mode1 raw json body 처리
        /// </summary>
        public async Task<ApiResult> HandleTutorMode1Async(string rawJson)
        {
            Debug.WriteLine($"[Tutor] HandleTutorMode1Async ENTER t={DateTime.Now:HH:mm:ss.fff} rawLen={rawJson?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(rawJson))
                return new ApiResult { Success = false, Error = "empty body" };

            TutorMode1Request? req;
            try
            {
                req = JsonSerializer.Deserialize<TutorMode1Request>(rawJson, _tutorJsonOpt);
                Debug.WriteLine($"[Tutor] mode1 parsed rid={req?.request_id ?? "(null)"} key={req?.anchor_key ?? "(null)"} t={DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                return new ApiResult { Success = false, Error = $"invalid json: {ex.Message}" };
            }

            if (req == null)
                return new ApiResult { Success = false, Error = "invalid body" };

            if (string.IsNullOrWhiteSpace(req.anchor_key))
                return new ApiResult { Success = false, Error = "missing anchor_key" };

            Debug.WriteLine($"[Tutor] mode1 incoming anchor_key={req.anchor_key}");

            // ---- Defaults
            var spotlight = req.options?.spotlight ?? true;
            var durationMs = req.options?.duration_ms ?? 5000;

            // ✅ spotlight용 anchorKey 라우팅
            var routedAnchor = RouteToUiAnchor(req.anchor_key!);
            // ✅ KB lookup
            var uiAnchorKey = RouteToUiAnchor(req.anchor_key!);
            var kb = FindKbItem(uiAnchorKey);

            // ✅ KB 우선, 없으면 요청 card fallback
            var title = req.card?.title ?? kb?.mode1_card?.title;
            var body = req.card?.body ?? kb?.mode1_card?.body;
            Debug.WriteLine($"[Tutor] kb lookup key={uiAnchorKey} found={(kb != null)} title={(!string.IsNullOrWhiteSpace(title))} body={(!string.IsNullOrWhiteSpace(body))}");



            // 둘 다 없으면 디버그 로그 남기기
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            {
                Debug.WriteLine($"[Tutor] No card text for anchor={req.anchor_key} (kb missing/empty and request card missing)");
                // 여기서 실패로 처리하고 싶으면:
                // return new ApiResult { Success = false, Error = $"no card text for anchor_key={req.anchor_key}" };
            }

            try
            {
                await NotifyTestStatus($"[Tutor]\nanchor={req.anchor_key}\nuiAnchorKey={uiAnchorKey}\n{title}\n{body}");

                // Application.Current null 방어
                if (Application.Current == null)
                    return new ApiResult { Success = false, Error = "Application.Current is null" };

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!spotlight) return;

                    Debug.WriteLine($"[Tutor] will call TryShowSpotlight rid={req.request_id ?? "(null)"} key={req.anchor_key} routed={routedAnchor} t={DateTime.Now:HH:mm:ss.fff}");
                    TryShowSpotlight(uiAnchorKey, durationMs, title, body, req.request_id);
                    Debug.WriteLine($"[Tutor] route: {req.anchor_key} -> {uiAnchorKey}");

                });

                return new ApiResult { Success = true, Message = "tutor mode1 handled" };
            }
            catch (Exception ex)
            {
                return new ApiResult { Success = false, Error = $"handle failed: {ex.Message}" };
            }
        }


        private void TryShowSpotlight(string anchorKey, int durationMs, string? title, string? body, string? requestId = null)
        {
            try
            {
                if (Application.Current.MainWindow is not Window w)
                    return;

                w.UpdateLayout();

                if (anchorKey.Equals("MAIN_WINDOW_WALKTHROUGH", StringComparison.OrdinalIgnoreCase))
                {
                    if (w is global::GVisionWpf.MainWindow mainWindow)
                    {
                        Debug.WriteLine($"[Tutor] MAIN_WINDOW_WALKTHROUGH -> RunMainWindowSpotlightWalkthroughAsync rid={requestId ?? "(null)"} t={DateTime.Now:HH:mm:ss.fff}\n{Environment.StackTrace}");
                        _ = mainWindow.RunMainWindowSpotlightWalkthroughAsync(durationMs, requestId: requestId);
                        return;
                    }

                    Debug.WriteLine("[Tutor] MAIN_WINDOW_WALKTHROUGH fallback -> MAIN_TITLE_BAR");
                    anchorKey = "MAIN_TITLE_BAR";
                }

                var target = TutorAnchorResolver.Resolve(w, anchorKey);
                if (target == null)
                {
                    Debug.WriteLine($"[Tutor] resolve anchor={anchorKey} -> null");
                    _ = NotifyTestStatus($"[Tutor] UI target not found: {anchorKey}");
                    return;
                }

                Debug.WriteLine($"[Tutor] resolve anchor={anchorKey} -> {target.GetType().Name} name={target.Name}");
                Debug.WriteLine($"[Tutor] overlay show anchor={anchorKey}");

                TutorSpotlightOverlay.Show(
                    hostWindow: w,
                    target: target,
                    title: title,
                    body: body,
                    durationMs: durationMs,
                    padding: 10,
                    dimOpacity: 0.62
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tutor] spotlight failed: {ex}");
            }
        }

        #endregion
    /// <summary>
    /// Gets the list of available API endpoints
    /// </summary>
    /// <returns>A dictionary of API endpoints and their descriptions</returns>
    public Dictionary<string, string> GetApiEndpoints()
        {
            return new Dictionary<string, string>
            {
                ["/"] = "GVision API root - returns API status",
                ["/windows/teaching/bga"] = "Opens the BGA teaching window",
                ["/windows/teaching/mapping"] = "Opens the Mapping teaching window",
                ["/windows/teaching/lga"] = "Opens the LGA teaching window",
                ["/windows/teaching/qfn"] = "Opens the QFN teaching window",
                ["/windows/teaching/strip"] = "Opens the Strip teaching window",
                ["/windows/teaching/qc"] = "Opens the QC teaching window",
                ["/windows/teaching/prs/reteach"] = "Opens teaching window based on current PRS inspection result",
                ["/windows/teaching/mapping/reteach"] = "Opens mapping teaching window with current mapping shots",
                ["/windows/history"] = "Opens the history window",
                ["/windows/light"] = "Opens the light control window",
                ["/windows/settings"] = "Opens the settings window",
                ["/windows/lot"] = "Opens the lot window",
                ["/windows/monitor"] = "Opens the monitor window",
                ["/mode/set?mode=MODE"] = "Changes operation mode (MODE should be either 'RUN' or 'SETUP')",
                ["/live/toggle?switch=ONOFF&no=N"] = "Toggles live mode for camera N (N: 0-5, ONOFF: ON or OFF)",
                ["/api/status"] = "Returns the current application status",
                ["/test/run/prs"] = "Executes test run based on current PRS recipe and teaching information",
                ["/test/run/map"] = "Executes test run based on current mapping recipe and teaching information",
                ["/closeWindows"] = "close all the windows, opened by chat window",
                ["/openWindow/yes"] = "If the user says yes then open the window",
                ["/openWindow/no"] = "If the user says no then don't open the window",
                ["/chat/clear"] = "Clears all chat logs in the chat window",
                ["/windows/history?date="] = "search the specific date of history",
                ["/roi/operation?operationName=OPERATION&..."] = "Executes an ROI operation (AddRoiOperation, DeleteRoiOperation, UpdateRoiOperation, ResetRoisOperation)",
                ["/bga/roi/{type}/{operation}?params..."] = "Manipulates BGA ROIs (type: pattern, ball, surface, dontcare; operation: add, update, delete, reset)",
                ["/recipes"] = "Gets a list of all recipes",
                ["/recipes/add?name=NEW_NAME"] = "Adds a new recipe",
                ["/recipes/copy?source=SOURCE_NAME&dest=DEST_NAME"] = "Copies a recipe",
                ["/recipes/rename?old=OLD_NAME&new=NEW_NAME"] = "Renames a recipe",
                ["/recipes/delete?name=RECIPE_NAME"] = "Deletes a recipe",
                ["/recipes/select?name=RECIPE_NAME"] = "Selects a recipe",
                ["/exit"] = "Exits the application"
            };
        }

        /// <summary>
        /// Gets the current application status
        /// </summary>
        /// <returns>A dictionary with application status information</returns>
        public Dictionary<string, object> GetApplicationStatus()
        {
            var globalSettings = GlobalSetting.Instance;
            Device device = DeviceRecipeRepository.Instance.GetRecipe();
            return new Dictionary<string, object>
            {
                ["runningMode"] = globalSettings.CurrentRunningMode.ToString(),
                ["apiVersion"] = "1.0",
                ["timestamp"] = DateTime.Now,
                ["savingMode"] = globalSettings.Inspection.SaveOption,
                ["recipeName"] = globalSettings.DeviceInfo.RecipeName,
                ["lotNo"] = globalSettings.DeviceInfo.LotNumber,
                ["deviceInfo"] = new Dictionary<string, object>
                {
                    //["visionTableGridPattern"] = device.VisionTableGridPattern,
                    //["packageType"] = device.PackageType,
                    ["Size"] = new Dictionary<string, object>
                    {
                        ["Width"] = device.PackageSize.Width,
                        ["Height"] = device.PackageSize.Height,
                    },
                    ["Fov"] = new Dictionary<string, object>
                    {
                        ["Col"] = device.FovSize.Col,
                        ["Row"] = device.FovSize.Row,
                    },
                    ["Tray"] = new Dictionary<string, object>
                    {
                        ["Col"] = device.TraySize.Col,
                        ["Row"] = device.TraySize.Row,
                    },
                    ["Block"] = new Dictionary<string, object>
                    {
                        ["Col"] = device.BlockSize.Col,
                        ["Row"] = device.BlockSize.Row,
                    },
                },
                ["machineInfo"] = new Dictionary<string, object>
                {
                    //["handlerIp"] = globalSettings.MachineInfo.HandlerIp,
                    //["listeningPort"] = globalSettings.MachineInfo.ListeningPort,
                    //["sendingPort"] = globalSettings.MachineInfo.SendingPort,
                    //["camerasWithQueues"] = globalSettings.MachineInfo.CamerasWithQueue,
                    ["cameras"] = globalSettings.CameraInfos,
                    ["lights"] = globalSettings.LightInfos,
                    ["lightControllers"] = globalSettings.LightControllerInfos,
                    ["inspection"] = globalSettings.Inspection
                }
            };
        }
        private bool ShouldAskBeforeOpenWindow = false;

        // 창 띄울지 말지
        private TaskCompletionSource<bool> _yesOrNoTcs = new TaskCompletionSource<bool>();
        public void SetYesOrNo(bool value)
        {
            _yesOrNoTcs.TrySetResult(value); // 값이 결정되면 Task 완료
        }

       

        /// <summary>
        /// Opens the BGA teaching window
        /// </summary>
        /// <returns>Operation result</returns>
        public async Task<ApiResult> OpenBgaTeachingWindow()
        {
            try
            {
                var OpenedBgaTeachingWindow = await WindowManager.OpenOrActivateAsync<BgaTeachingWindow>();
                return new ApiResult { Success = true, Message = "BGA teaching window opened successfully" };

            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open BGA teaching window: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Failed to open BGA teaching window: {ex.Message}" };
            }

        }
        //A/S 창 래퍼 메서드 추가 2025.11.17
        public async Task<ApiResult> OpenAsWindow()
        {
            return await OpenWindow<ASWindow>("A/S");
        }
        ///// <summary>
        ///// Opens the Mapping teaching window
        ///// </summary>
        ///// <returns>Operation result</returns>
        public async Task<ApiResult> OpenMapTeachingWindow()
        {
            try
            {
                var OpenedMapTeachingWindow = await WindowManager.OpenOrActivateAsync<GridMoldTeachingWindow>();
                return new ApiResult { Success = true, Message = "Map teaching window opened successfully" };

            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open mapping teaching window: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Failed to open mapping teaching window: {ex.Message}" };
            }
           
        }

        ///// <summary>
        ///// Opens the LGA teaching window
        ///// </summary>
        ///// <returns>Operation result</returns>
        public async Task<ApiResult> OpenLgaTeachingWindow()
        {
            try
            {
                var OpenedLgaTeachingWindow = await WindowManager.OpenOrActivateAsync<LgaTeachingWindow>();
                return new ApiResult { Success = true, Message = "LGA teaching window opened successfully" };

            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open LGA teaching window: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Failed to open LGA teaching window: {ex.Message}" };
            }
        }


        ///// <summary>
        ///// Opens the QFN teaching window
        ///// </summary>
        ///// <returns>Operation result</returns>
        public async Task<ApiResult> OpenQfnTeachingWindow()
        {
            try
            {
                var OpenedLgaTeachingWindow = await WindowManager.OpenOrActivateAsync<QfnTeachingWindow>();
                return new ApiResult { Success = true, Message = "QFN teaching window opened successfully" };

            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open QFN teaching window: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Failed to open QFN teaching window: {ex.Message}" };
            }

        }

        ///// <summary>
        ///// Opens the Strip teaching window
        ///// </summary>
        ///// <returns>Operation result</returns>
        public async Task<ApiResult> OpenStripTeachingWindow()
        {
            try
            {
                var OpenedStripTeachingWindow = await WindowManager.OpenOrActivateAsync<StripTeachingWindow>();
                return new ApiResult { Success = true, Message = "Strip teaching window opened successfully" };

            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open Strip teaching window: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Failed to open Strip teaching window: {ex.Message}" };
            }
        }

        ///// <summary>
        ///// Opens the QC teaching window
        ///// </summary>
        ///// <returns>Operation result</returns>
        //public async Task<ApiResult> OpenQcTeachingWindow()
        //{
        //    return await Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        try
        //        {
        //            // 이미지 파일 선택 대화상자를 먼저 보여줍니다
        //            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
        //            {
        //                Title = "Select Teaching Image",
        //                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.tif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif|All files (*.*)|*.*",
        //                FilterIndex = 1,
        //                RestoreDirectory = true
        //            };

        //            if (openFileDialog.ShowDialog() == true)
        //            {
        //                // 이미지를 로드하고 티칭창을 엽니다
        //                HOperatorSet.ReadImage(out HObject image, openFileDialog.FileName);

        //                var window = new QcTeachingWindow
        //                {
        //                    TeachingImage = image
        //                };
        //                window.Show();
        //                SystemInformationViewModel.Instance.Print("QC teaching window opened via API");
        //                return new ApiResult { Success = true, Message = "QC teaching window opened successfully" };
        //            }
        //            else
        //            {
        //                return new ApiResult { Success = false, Message = "Image selection cancelled" };
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            SystemInformationViewModel.Instance.Print($"Failed to open QC teaching window: {ex.Message}");
        //            return new ApiResult { Success = false, Error = $"Failed to open QC teaching window: {ex.Message}" };
        //        }
        //    });
        //}

        ///// <summary>
        ///// Opens the PRS reteach window
        ///// </summary>
        ///// <returns>Operation result</returns>
        //public async Task<ApiResult> OpenPrsReteachWindow()
        //{
        //    return await Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        try
        //        {
        //            // 현재 PRS 검사 결과를 기반으로 적절한 윈도우를 엽니다
        //            var globalSettings = GlobalSetting.Instance;
        //            var device = DeviceRecipeRepository.Instance.GetRecipe();

        //            switch (device.PackageType)
        //            {
        //                case EInspection.Bga:
        //                    var bgaWindow = new BgaTeachingWindow();
        //                    bgaWindow.Show();
        //                    SystemInformationViewModel.Instance.Print("BGA PRS reteach window opened via API");
        //                    return new ApiResult { Success = true, Message = "BGA PRS reteach window opened successfully" };

        //                case EInspection.Lga:
        //                    var lgaWindow = new LgaTeachingWindow();
        //                    lgaWindow.Show();
        //                    SystemInformationViewModel.Instance.Print("LGA PRS reteach window opened via API");
        //                    return new ApiResult { Success = true, Message = "LGA PRS reteach window opened successfully" };

        //                case EInspection.Qfn:
        //                    var qfnWindow = new QfnTeachingWindow();
        //                    qfnWindow.Show();
        //                    SystemInformationViewModel.Instance.Print("QFN PRS reteach window opened via API");
        //                    return new ApiResult { Success = true, Message = "QFN PRS reteach window opened successfully" };

        //                default:
        //                    return new ApiResult { Success = false, Error = $"Unsupported package type: {device.PackageType}" };
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            SystemInformationViewModel.Instance.Print($"Failed to open PRS reteach window: {ex.Message}");
        //            return new ApiResult { Success = false, Error = $"Failed to open PRS reteach window: {ex.Message}" };
        //        }
        //    });
        //}

        ///// <summary>
        ///// Opens the Mapping reteach window
        ///// </summary>
        ///// <returns>Operation result</returns>
        public async Task<ApiResult> OpenMapReteachWindow()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 현재 맵핑 shot들을 가져와서 윈도우를 엽니다
                    var mappingShots = new ObservableCollection<HObject>();
                    // TODO: 현재 맵핑 shot들을 가져오는 로직 구현

                    var window = new GridMoldTeachingWindow(mappingShots);
                    // WindowManager.ShowWindow<window>();
                    
                    window.Show();
                    SystemInformationViewModel.Instance.Print("Mapping reteach window opened via API");
                    return new ApiResult { Success = true, Message = "Mapping reteach window opened successfully" };
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Failed to open mapping reteach window: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Failed to open mapping reteach window: {ex.Message}" };
                }
            });
        }

        public async Task<ApiResult> OpenMonitorWindow(){
            return await OpenWindow<SystemUsageMonitorWindow>("Monitor");
        } 

        // history, setting, light, lot 는 확인로그 안 받도록
        public async Task<ApiResult> OpenWindow<T>(string windowName) where T : System.Windows.Window, new()
        {;
            try
            {
                await WindowManager.OpenOrActivateAsync<T>();

                return new ApiResult
                {
                    Success = true,
                    Message = $"{windowName} window opened successfully"
                };
            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to open {windowName} window: {ex.Message}");
                return new ApiResult
                {
                    Success = false,
                    Error = $"Failed to open {windowName} window: {ex.Message}"
                };
            }
            //_yesOrNoTcs = new TaskCompletionSource<bool>();
        }
        

        //"새 채팅" 코드 추가
        public async Task<ApiResult> CallClearChat ()
        {
            await NotifyTestStatus("해당 명령을 실행할까요?");
            _yesOrNoTcs = new TaskCompletionSource<bool>();
            bool yesOrNo = await _yesOrNoTcs.Task;

            if (!yesOrNo)  //거짓이면 리턴하고 함수 끝
            {
                return new ApiResult
                {
                    Success = false,
                    Message = $"do not clear chat"
                };
            }

            try
            {
                // UI 스레드에서 실행
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ClearChat();
                    SystemInformationViewModel.Instance.Print("History window cleared via API");
                });

                return new ApiResult
                {
                    Success = true,
                    Message = "chat history window cleared successfully"
                };
            }
            catch (Exception ex)
            {
                SystemInformationViewModel.Instance.Print($"Failed to clear history window: {ex.Message}");
                return new ApiResult
                {
                    Success = false,
                    Error = $"Failed to clear history window: {ex.Message}"
                };
            }
        }

        public async Task<ApiResult> OpenLightLiveView(string cameraName)
        {
            cameraName = cameraName.Trim();
            //Debug.WriteLine("ApiResult_알수없는 카메라를 호출함 cameraName " + cameraName);
            try
            {
                if (!Enum.TryParse<ECamera>(cameraName, out ECamera selectedCamera))
                {
                    // 알수없는 카메라를 호출함
                    //Debug.WriteLine("알수없는 카메라를 호출함 cameraName "+ cameraName);
                    //return;
                }

                await WindowManager.OpenOrActivateAsync<LightWindow>();
                if (!WindowManager.TryGetViewModel<LightWindow, LightViewModel>(out LightViewModel? viewModel))
                {
                    // 윈도우 못가져옴 왠지모름
                    //return;
                }
                if (viewModel != null)
                {
                    viewModel.CameraSelectedValue = selectedCamera;
                }

                return new ApiResult { Success = true, Message = "Live started and Light window opened" };
            }
            catch (Exception ex)
            {
                return new ApiResult { Success = false, Message = $"Exception: {ex.Message}" };
            }
        }


        /// <summary>
        /// Sets the operation mode
        /// </summary>
        /// <param name="mode">Mode to set (RUN or SETUP)</param>
        /// <returns>Operation result</returns>
        public async Task<ApiResult> SetOperationMode(string mode)
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 현재 MainWindow 가져오기
                    if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        // 현재 모드 확인
                        string currentMode = mainWindow.CurrentOperationMode;
                        string targetMode = mode.Trim().ToUpperInvariant();

                        // 모드가 다를 경우에만 변경
                        if ((currentMode == "SETUP" && targetMode == "RUN") ||
                            (currentMode == "RUN" && targetMode == "SETUP"))
                        {
                            if (!mainWindow.EnsureOperationMode(targetMode))
                            {
                                return new ApiResult { Success = false, Error = $"Invalid mode: {mode}" };
                            }

                            SystemInformationViewModel.Instance.Print($"Mode changed to {targetMode} via API");
                            return new ApiResult { Success = true, Message = $"Mode changed to {targetMode}" };
                        }
                        else
                        {
                            // 이미 해당 모드인 경우
                            SystemInformationViewModel.Instance.Print($"Already in {targetMode} mode (API request)");
                            return new ApiResult { Success = true, Message = $"Already in {targetMode} mode" };
                        }
                    }
                    else
                    {
                        return new ApiResult { Success = false, Error = "MainWindow not found" };
                    }
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Error changing mode via API: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Error changing mode: {ex.Message}" };
                }
            });
        }

        /// <summary>
        /// Toggles camera live mode
        /// </summary>
        /// <param name="cameraIndex">Camera index (0-5)</param>
        /// <param name="isLive">Whether to turn live on or off</param>
        /// <returns>Operation result</returns>
        /// <summary>
        /// Toggles camera live mode
        /// </summary>
        /// <param name="cameraIndex">Camera index (0-5)</param>
        /// <param name="isLive">Whether to turn live on or off</param>
        /// <returns>Operation result</returns>
        public async Task<ApiResult> ToggleCameraLive(int cameraIndex, bool isLive)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>   // Dispatcher를 사용하면 UI 스레드에서 실행하도록 보장
            {
                try
                {
                    // MainWindow와 SetupPage 가져오기
                    // 현재 실행 중인 앱의 메인 윈도우가 MainWindow 타입이면 mainWindow 변수에 담음
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        // SetupPage 가져오기
                        if (mainWindow.SetupPage != null &&
                            // mainWindow.setupPage.floatingWindows != null &&
                            mainWindow.SetupPage.FloatingWindows.Count > cameraIndex &&
                            mainWindow.IsSetupMode)
                        {
                            // FloatingWindow에서 CameraPanel 가져오기
                            var floatingWindow = mainWindow.SetupPage.FloatingWindows[cameraIndex];
                            Debug.WriteLine($"floatingWindow 이거: {floatingWindow}");
                            if (floatingWindow is CameraWindow camWindow)  // .xMainBorder.Child is
                            {
                                // 라이브 상태 변경
                                if (isLive)
                                {
                                    Debug.WriteLine("켜줘");
                                    camWindow.xCameraPanel.StartLive();
                                    SystemInformationViewModel.Instance.Print($"Camera {cameraIndex} live started via API");
                                    return new ApiResult { Success = true, Message = $"Camera {cameraIndex} live started" };
                                }
                                else
                                {
                                    Debug.WriteLine("꺼줘");
                                    camWindow.StopLive();
                                    // await Task.Delay(300);
                                    camWindow.xCameraPanel.xHSmartWindow.HalconWindow.ClearWindow();
                                    // camWindow.xCameraPanel.stopButton_Click(null, null);
                                    SystemInformationViewModel.Instance.Print($"Camera {cameraIndex} live stopped via API");
                                    return new ApiResult { Success = true, Message = $"Camera {cameraIndex} live stopped" };
                                }
                            }
                            else
                            {
                                return new ApiResult { Success = false, Error = $"Camera panel not found for camera {cameraIndex}" };
                            }
                        }
                        else
                        {
                            // Debug.WriteLine("Setup page or floating windows not available");
                            var result = RequestSetupModeAsync(mainWindow, cameraIndex, isLive);
                            // return ToggleCameraLive(cameraIndex, isLive);
                            return new ApiResult { Success = false, Error = "Setup page or floating windows not available" };
                        }
                    }
                    else
                    {
                        return new ApiResult { Success = false, Error = "Main window not available" };
                    }
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Error toggling camera live status via API: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Error toggling camera live status: {ex.Message}" };
                }
            });
        }
        private async Task<ApiResult> RequestSetupModeAsync(MainWindow mainWindow, int cameraIndex, bool isLive)
        {
            NotifyTestStatus("현재 run mode이므로 실행이 어렵습니다. Set up mode로 바꿔주세요. 바꾸시겠습니까?");
            // NotifyTestStatus("바꾸시겠습니까?");

            _yesOrNoTcs = new TaskCompletionSource<bool>();
            bool yesOrNo = await _yesOrNoTcs.Task;

            if (!yesOrNo)
                return new ApiResult
                {
                    Success = false,
                    Message = "유저가 Setup 모드 전환 취소"
                };

            // Setup 모드 전환 실행
            mainWindow.EnsureOperationMode("SETUP");
            ToggleCameraLive(cameraIndex, isLive);
            // 여기서는 모드 전환 '시도'만 했으므로 성공 여부는 별도 이벤트에서 확인
            return new ApiResult
            {
                Success = true,
                Message = "Setup 모드 전환 시도됨"
            };
        }

        public async Task<ApiResult> RunTestPrs()
        {
            await NotifyTestStatus("해당 명령을 실행할까요?");
            _yesOrNoTcs = new TaskCompletionSource<bool>(); 
            bool yesOrNo = await _yesOrNoTcs.Task;

            if (!yesOrNo)
                return UserRejected(nameof(RunTestMap));

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {
                // 시작 메시지를 채팅창에 표시
                await NotifyTestStatus("PRS 테스트를 시작합니다...");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var window = (MainWindow)Application.Current.MainWindow;
                    if (window.IsSetupMode)
                    {
                        window.EnsureOperationMode("RUN");
                    }
                });

                VinsCommunicator.Instance.ReleaseClient();
                VinsCommunicator.Instance.InitializeClient();

                // PRS 실행을 실제로 기다림
                await Prs.Instance.Run(cancellationTokenSource.Token);

                // 완료 메시지를 채팅창에 표시
                await NotifyTestStatus("PRS 테스트가 완료되었습니다!");

                return new ApiResult { Success = true, Message = "PRS test completed successfully." };
            }
            catch (Exception ex)
            {
                cancellationTokenSource.Cancel();
                await NotifyTestStatus($"PRS 테스트 실행 중 오류가 발생했습니다: {ex.Message}");
                return new ApiResult { Success = false, Error = $"PRS test failed: {ex.Message}" };
            }
        }

        public async Task<ApiResult> RunTestMap()
        {
            await NotifyTestStatus("해당 명령을 실행할까요?");
            _yesOrNoTcs = new TaskCompletionSource<bool>();
            bool yesOrNo = await _yesOrNoTcs.Task;

            if (!yesOrNo)
                return UserRejected(nameof(RunTestMap));

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {
                // 시작 메시지를 채팅창에 표시
                await NotifyTestStatus("Map 테스트를 시작합니다...");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var window = (MainWindow)Application.Current.MainWindow;
                    if (window.IsSetupMode)
                    {
                        window.EnsureOperationMode("RUN");
                    }
                });

                VinsCommunicator.Instance.ReleaseClient();
                VinsCommunicator.Instance.InitializeClient();

                // Map 실행이 완료될 때까지 기다림
                await Mapping.Instance.Run(cancellationTokenSource.Token);

                // 완료 메시지를 채팅창에 표시
                await NotifyTestStatus("Map 테스트가 완료되었습니다!");

                return new ApiResult { Success = true, Message = "Mapping test completed successfully." };
            }
            catch (Exception ex)
            {
                cancellationTokenSource.Cancel();
                await NotifyTestStatus($"Map 테스트 실행 중 오류가 발생했습니다: {ex.Message}");
                return new ApiResult { Success = false, Error = $"Map test failed: {ex.Message}" };
            }
        }


        // 채팅창에 상태 메시지를 전달하는 헬퍼 메서드
        public async Task NotifyTestStatus(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var chatWindow = Application.Current.Windows
                                    .OfType<ChatWindow>()
                                    .FirstOrDefault();

                if (chatWindow?.DataContext is ChatWindowViewModel viewModel)
                {
                    Debug.WriteLine("apiController_ message_ " + message);
                    // textBlock과 같은 스타일로 시스템 메시지 추가
                    viewModel.LogSystemMessage(message);
                }
            });
        }

        // legacy draft method removed

        public async Task<ApiResult> RunCloseAllWindows(string WindowName)
        {
            //prompt 값이 많아지니 yes라는 명령어를 못 알아들어서 확인로그 받는 부분 뺌
            await NotifyTestStatus("해당 명령을 실행할까요?");

            _yesOrNoTcs = new TaskCompletionSource<bool>();
            bool yesOrNo = await _yesOrNoTcs.Task;

            if (!yesOrNo)  //거짓이면 리턴하고 함수 끝
            {
                //_yesOrNoTcs = new TaskCompletionSource<bool>();
                return new ApiResult
                {
                    Success = false,
                    Message = $"Haven't execute RunCloseAllWindows cuz user said no"
                };
            }
            try
            {
                //await Application.Current.Dispatcher.InvokeAsync(() =>
                //{
                //    CloseAllChildWindows();
                //});
                Debug.WriteLine("from apiController WindowName " + WindowName);
                await WindowManager.CloseAllChildWindowsAsync(WindowName);
                return new ApiResult
                {
                    Success = true,
                    Message = "All child windows closed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"Failed to close child windows: {ex.Message}"
                };
            }
        }
        public async Task<ApiResult> executeYes()
        {
            try
            {
                //await Application.Current.Dispatcher.InvokeAsync(() =>
                //{
                //    openWi
                //});
                //_yesOrNo = true; // 값 설정
                SetYesOrNo(true);

                return new ApiResult
                {
                    Success = true,
                    Message = "All child windows closed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"Failed to close child windows: {ex.Message}"
                };
            }
        }
        // 거절 결과 생성함수
        public async Task<ApiResult> executeNo()
        {
            try
            {

                SetYesOrNo(false);

                return new ApiResult
                {
                    Success = true,
                    Message = "All child windows closed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"Failed to close child windows: {ex.Message}"
                };
            }
        }

        // ClearChat 메서드 추가
        private void ClearChat()
        {
            var chatWindow = Application.Current.Windows
                .OfType<ChatWindow>()
                .FirstOrDefault();

            if (chatWindow?.DataContext is ChatWindowViewModel viewModel)
            {
                viewModel.ClearChat();
            }
        }


        // 거절 결과 생성함수
        public static ApiResult UserRejected(string action)
        {
            return new ApiResult
            {
                Success = false,
                Message = $"Haven't execute {action} cuz user said no"
            };
        }

        public void settingUpdateProperty<TWindow, TVM>(TVM vm, string propertyName, string value, TWindow? window = default)
            where TWindow : class
            where TVM : SettingsViewModel
        {
            var type = vm.GetType();
            var prop = type.GetProperty(propertyName);
            if (prop == null)
            {
                Console.WriteLine($"Property '{propertyName}' not found.");
                return;
            }

            object convertedValue;

            if (prop.PropertyType == typeof(bool))
            {
                if (bool.TryParse(value, out bool boolVal))
                {
                    convertedValue = boolVal;
                }
                else
                {
                    Console.WriteLine($"Invalid bool value '{value}' for property '{propertyName}'");
                    return;
                }
            }
            else if (prop.PropertyType.IsEnum)
            {
                try
                {
                    convertedValue = Enum.Parse(prop.PropertyType, value, ignoreCase: true);
                }
                catch
                {
                    Console.WriteLine($"Invalid enum value '{value}' for property '{propertyName}'");
                    return;
                }
            }
            else
            {
                // 기본 변환 (string, int 등)
                convertedValue = Convert.ChangeType(value, prop.PropertyType);
            }

            prop.SetValue(vm, convertedValue);
            //OnPropertyChanged(propertyName);
            Console.WriteLine($"{propertyName} set to {convertedValue}");
        }

        public async Task<ApiResult> UpdateSettingsProperty(string propertyName, string value)
        {
            try
            {
                // Settings 창이 열려있지 않으면 열기 (이미 열려있다면 활성화)
                var OpenedSettingsWindow = await WindowManager.OpenOrActivateAsync<SettingsWindow>();

                // UI 스레드에서 안전하게 ViewModel 업데이트 실행
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!WindowManager.TryGetViewModel<SettingsWindow, SettingsViewModel>(out SettingsViewModel? vm))
                    {
                        Debug.WriteLine("SettingsViewModel_vm 못가져옴 왠지모름");
                        // return;
                    }

                    //SettingsViewModel.Instance.UpdateProperty(propertyName, value);
                    settingUpdateProperty<SettingsWindow, SettingsViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedSettingsWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public void UpdateTeaching<TWindow, TVM>(TVM vm, string propertyName, string value, TWindow? window = default)
            where TWindow : class
            where TVM : class
        {
            Debug.WriteLine("in UpdateTeaching propertyName_" + propertyName + " value_ " + value);
            if (vm == null)
            {
                Debug.WriteLine("ViewModel이 null입니다.");
                return;
            }

            var vmType = typeof(TVM);
            PropertyInfo? propertyInfo = null;
            var teachingObj = vm?.GetType().GetProperty("Teaching")?.GetValue(vm);
            var teachingType = teachingObj.GetType();
            var w = window as System.Windows.Window;
            if (w == null && teachingType == null && teachingObj == null)
            {
                Debug.WriteLine("❌ window, teachingType, teachingObj  변환 실패 (Window 타입 아님)");
                return;
            }

            if (propertyName.EndsWith("Teaching", StringComparison.OrdinalIgnoreCase)) // teaching test
            {
                var methodName = propertyName.Substring(0, propertyName.Length - "Teaching".Length);
                methodName = char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);

                var method = vmType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(vm, null);
                    Debug.WriteLine($"Executed method {methodName} successfully.");
                }
                else
                {
                    Debug.WriteLine($"Method {methodName} not found in ViewModel.");
                }
                return;
            }
            else if (propertyName.Contains("Command", StringComparison.OrdinalIgnoreCase))  // auto roi, auto threshold
            {
                var command = vm?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                   ?.GetValue(vm) as System.Windows.Input.ICommand; //findPackageRoiAuto() 이 함수
                command?.Execute(null);
                return;
            }

            else if (propertyName.Contains("_", StringComparison.OrdinalIgnoreCase))
            {//x name으로 roi panel 찾기
                string panelName = "";
                int roiIndex = propertyName.IndexOf("Roi", StringComparison.OrdinalIgnoreCase);
                if (roiIndex >= 0)
                {
                    propertyName = propertyName.TrimEnd('_');
                    // "Package" + "Roi" + "Top" → "x" + "PackageRoiPanelTop"
                    string beforeRoi = propertyName.Substring(0, roiIndex + 3); // "PackageRoi"
                    string afterRoi = propertyName.Substring(roiIndex + 3);     // "Top"
                    panelName = "x" + beforeRoi + "Panel" + afterRoi;           // "xPackageRoiPanelTop"
                    Debug.WriteLine("panelName_" + panelName);
                }
                var panel = w.FindName(panelName) as RoiPanelV2;
                GridRoiPanel gridPanel = null;
                if (panel == null)
                {
                    gridPanel = w.FindName(panelName) as GridRoiPanel;
                    //gridPanel.Roi = newRoi;
                    //gridPanel.CreateRoi();
                    Debug.WriteLine($"❌ RoiPanel '{panelName}'를 찾을 수 없음");
                    //return;
                }

                var roiValue = teachingObj?.GetType().GetProperty(propertyName)?.GetValue(teachingObj) as Roi;
                var roiValues = WindowHelper.parseIntArray(value);
                if (roiValues != null && roiValues.Length == 4)
                {
                    if (panel != null)
                    {
                        Roi newRoi = new Roi("ROI", roiValues[0], roiValues[1], roiValues[2], roiValues[3]);
                        panel.Roi = newRoi;
                        panel.CreateRoi();
                    }
                    else if (gridPanel != null)
                    {

                        Roi newRoi = new Roi("ROI", roiValues[0], roiValues[1], roiValues[2], roiValues[3]);
                        gridPanel.Roi = newRoi;
                        gridPanel.CreateRoi(newRoi);
                    }
                    return;
                }
                else if (roiValues == null || roiValues.Length < 4)
                {
                    if (panel != null)
                    {
                        panel.roiCreateButton();             // Panel 내부 함수 실행
                    }
                    else if (gridPanel != null)
                    {
                        gridPanel.roiCreateButton();
                    }
                    Debug.WriteLine($"✅ {panelName}에 ROI 생성 완료");
                }
                else
                {
                    Debug.WriteLine($"❌ {propertyName}의 Roi 값이 null");
                }
                return;
            }

            else if (propertyName.Contains("Roi", StringComparison.OrdinalIgnoreCase))
            {
                object panel = null;

                if (propertyName == "Roi")
                {
                    panel = w.FindName("x" + propertyName + "ListPanel");
                    // xRoiListPanel

                }
                else {
                    panel = w.FindName("x" + propertyName + "DataListPanel");
                    
                }
                RoiDataListPanel roiPanel = panel as RoiDataListPanel;

                if (roiPanel == null)
                {
                    MappingRoiDataListPanel mappingPanel = panel as MappingRoiDataListPanel;
                    mappingPanel.ExecuteAction(value);
                    Debug.WriteLine($"✅ {mappingPanel} -> ExecuteAction({value}) 호출 성공");
                    //var mappingPanel = w.FindName("x" + propertyName + "DataListPanel") as MappingRoiDataListPanel;
                }
                else
                {
                    roiPanel.ExecuteAction(value); // add, delete, reset
                    Debug.WriteLine($"✅ {roiPanel} -> ExecuteAction({value}) 호출 성공");
                }
                return;
            }

            else if (propertyName == "OutlineWidth" || propertyName == "PackageThresholdDiff")
            {
                var prop = teachingType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop != null && prop.CanWrite)
                {
                    if (int.TryParse(value?.ToString(), out int numericValue))
                    {
                        // 속성별 범위 제한
                        if (propertyName == "OutlineWidth")
                            numericValue = Math.Max(1, Math.Min(1000, numericValue));
                        else if (propertyName == "PackageThresholdDiff")
                            numericValue = Math.Max(1, Math.Min(255, numericValue));
                        // 실제 속성에 값 설정
                        prop.SetValue(teachingObj, numericValue);
                    }
                    else
                    {
                        Debug.WriteLine($"❌ {propertyName} OutlineWidth 찾을 수 없거나 쓰기 불가");
                    }
                }
                return;
            }

            else if (propertyName.Contains("Threshold", StringComparison.OrdinalIgnoreCase)) // Threshold 처리 예시
            {
                propertyInfo = teachingType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var thresholdValues = WindowHelper.parseIntArray(value);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(teachingObj, new Threshold(thresholdValues[0], thresholdValues[1]));
                    //var threshold = Activator.CreateInstance(propertyInfo.PropertyType, thresholdValues[0], thresholdValues[1]);
                    //propertyInfo.SetValue(vm, threshold);
                }
                return;
            }
            else if (propertyName.Contains("Size", StringComparison.OrdinalIgnoreCase)) // Size 처리 예시
            {
                propertyInfo = teachingType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sizeValues = WindowHelper.parseIntArray(value);
                if (sizeValues?.Length == 2)
                {
                    // 예: propertyName = "PadSize" → baseName = "Pad"
                    string baseName = propertyName.Replace("Size", "", StringComparison.OrdinalIgnoreCase);

                    // Teaching 내부 프로퍼티 찾기
                    var minProp = teachingType.GetProperty(baseName + "MinSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var maxProp = teachingType.GetProperty(baseName + "MaxSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (minProp != null && maxProp != null)
                    {
                        minProp.SetValue(teachingObj, sizeValues[0]);
                        maxProp.SetValue(teachingObj, sizeValues[1]);
                        Debug.WriteLine($"✅ {baseName}MinSize = {sizeValues[0]}, {baseName}MaxSize = {sizeValues[1]} 설정 완료");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ {baseName}MinSize 또는 {baseName}MaxSize 프로퍼티를 찾을 수 없음");
                    }
                    return;
                }
            }
            else if (propertyName.Contains("EdgeDetect", StringComparison.OrdinalIgnoreCase))
            {   // EdgeDetectDirection, EdgeDetectMode
                var prop = teachingType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop != null && prop.CanWrite)
                {
                    var propType = prop.PropertyType;

                    object convertedValue = value;

                    if (propType.IsEnum)
                    {
                        convertedValue = Enum.Parse(propType, value, ignoreCase: true);
                    }

                    prop.SetValue(teachingObj, convertedValue);
                }
                else
                {
                    Debug.WriteLine($"❌ {propertyName} 콤보박스 찾을 수 없거나 쓸 수 없음");
                }
                return;
            }

            else if (propertyName == "Row-Column") // mapping
            {
                var parts = propertyName.Split('-');
                var rowProp = teachingType.GetProperty(parts[0] + "Size",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var colProp = teachingType.GetProperty(parts[1] + "Size",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                var rowColValues = WindowHelper.parseIntArray_rc(value);
                if (rowColValues[0] != null)
                {
                    rowProp.SetValue(teachingObj, rowColValues[0]);
                }
                if (rowColValues[1] != null)
                {
                    colProp.SetValue(teachingObj, rowColValues[1]);
                }
                return;
            }
            
            else if (propertyName == "RotateAngle") // mapping rotate angle
            {
                var prop = vm?.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                //var teachingProp = vm?.GetType().GetProperty("Teaching");
                if (prop != null && prop.CanWrite)
                {
                    if (int.TryParse(value?.ToString(), out int angleValue))
                    {
                        // 필요하면 0,90,180,270 범위 체크
                        // int[] validAngles = { 0, 90, 180, 270 };
                        //if (!validAngles.Contains(angleValue))
                        //    Debug.WriteLine("not Contains");
                        //    angleValue = 0; // 기본값으로 초기화
                        
                        prop.SetValue(vm, angleValue, null);
                        Debug.WriteLine($"✅RotateAngle {propertyName} updated → {angleValue}");
                    }
                    if (vm is GridMoldTeaching gridVm)
                    {
                        gridVm.RotateAngle = angleValue; // PropertyChanged → 라디오버튼 체크됨
                        Debug.WriteLine($"✅ RotateAngle updated → {angleValue}");
                    }
                }
                else
                {
                    Debug.WriteLine($"❌ {propertyName} RotateAngle 찾을 수 없거나 쓰기 불가");
                }
                return;
            }
            else if (propertyName.Equals("FirstPinType", StringComparison.OrdinalIgnoreCase))
            {
                var prop = teachingType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        // value는 "SmallPad", "Notch", "Chamfer" _EFirstPin 
                        var enumValue = Enum.Parse(prop.PropertyType, value?.ToString(), ignoreCase: true);
                        prop.SetValue(teachingObj, enumValue);
                    }
                    catch
                    {
                        Debug.WriteLine($"❌ {propertyName} FirstPinType 값 변환 실패: {value}");
                    }
                }
                return;
            }
            else if (propertyName == "moveTab")   // 탭 이동
            {
                Debug.WriteLine("[UpdateTeaching] window type =" + window?.GetType().FullName);
                //var w = window as System.Windows.Window;
                Debug.WriteLine("windoe not null, 변환 성공");
                SelectTabByName(w, "xTabControl", value);

                return;
            }
        }

        public static void SelectTabByName<TWindow>(TWindow window, string tabControlName, string tabName)
        where TWindow : System.Windows.Window
        {
            Debug.WriteLine("SelectTabByName inside");
            if (window == null || string.IsNullOrWhiteSpace(tabName))
                return;

            // Window 안에서 TabControl 찾기
            var tabControl = window.FindName(tabControlName) as System.Windows.Controls.TabControl;

            if (tabControl == null)
            {
                Debug.WriteLine($"[SelectTabByName] TabControl '{tabControlName}' not found");
                return;
            }

            // API 이름 → 실제 탭 Header 매핑
            var tabMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "RejectMark", "Reject Mark" },
                { "DontCare", "Don't Care" },
                { "padLeads", "Pad/Leads"},
                { "FirstPinPattern", "FirstPin/Pattern"}
            };

            if (tabMap.TryGetValue(tabName, out var mappedTabName))
                tabName = mappedTabName;

            // 실제 탭 찾기
            var tab = tabControl.Items
                .OfType<System.Windows.Controls.TabItem>()
                .FirstOrDefault(t => t.Header?.ToString().Equals(tabName, StringComparison.OrdinalIgnoreCase) == true);

            if (tab != null)
            {
                tabControl.SelectedItem = tab;
                Debug.WriteLine($"[SelectTabByName] Switched to tab: {tabName}");
            }
            else
            {
                Debug.WriteLine($"[SelectTabByName] Tab not found: {tabName}");
            }
        }

        private Dictionary<string, Type> tabMap = new()
        {
            { "SETTING", typeof(SettingJigCalibrationTabViewModel) },
            { "VISION", typeof(VisionTableCalibrationTabViewModel) },
            { "TRAY", typeof(TrayCalibrationTabViewModel) },
            { "BOTTOM", typeof(BottomJigCalibrationTabViewModel)},
            { "PAD", typeof(PadPitchCalibrationTabViewModel)}
        };
        public void calibrationUpdateProperty<TWindow, TVM>(TVM vm, string propertyName, string value, TWindow? window = default)
            where TWindow : class
            where TVM : CalibrationViewModel
        {
           
            // System.Windows.Window OpenedcalibrationWindow, :Window 매개변수는 필요 없음, ViewModel이 이미 DataContext로 바인딩되어 있음
            if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(value))
                return;

            var tabVm = vm.SelectedTabViewModel;
            Debug.WriteLine("form UpdateProperty_propertyName ", propertyName, " value ", value);
            switch (propertyName)
            {
                // ----------------- 버튼 처리 -----------------
                case "BUTTON":
                    switch (value)
                    {
                        case "TEST":
                            tabVm?.RoiPanel?.CreateRoi();
                            vm.TestCommand?.Execute(null);
                            break;
                        case "LIGHTSAVE":
                            vm.LightSaveCommand?.Execute(null);
                            break;
                    }
                    break;

                // ----------------- 탭 전환 -----------------
                case "TAB":
                    if (vm.TabViewModels != null)
                    {
                        if (tabMap.TryGetValue(value.ToUpper(), out var tabType))
                        {
                            // TabViewModels 안에서 tabType과 일치하는 인스턴스를 찾기
                            var selectedTab = vm.TabViewModels.FirstOrDefault(t => t.GetType() == tabType);

                            if (selectedTab != null)
                                vm.SelectedTabViewModel = selectedTab;
                        }
                    }
                    break;

                // ----------------- ROI 처리 -----------------
                case "ROI":
                    if (tabVm != null)
                    {
                        switch (value)
                        {
                            case "CREATE":
                                tabVm.RoiPanel?.roiCreateButton();
                                break;
                            case "RECREATE":
                                tabVm.RoiPanel?.roiDeleteButton();
                                tabVm.ThresholdPanel?.Button();  // 임계값 초기화
                                tabVm.RoiPanel?.roiCreateButton();
                                break;
                        }
                    }
                    break;

                // ----------------- Threshold 처리 -----------------
                case "THRESHOLD":
                    if (tabVm != null)
                    {
                        var parts = value.Split('-');
                        if (parts.Length == 2
                            && int.TryParse(parts[0], out int minVal)
                            && int.TryParse(parts[1], out int maxVal))
                        {
                            tabVm.ThresholdPanel.Threshold = new Threshold(minVal, maxVal);
                        }
                        Debug.WriteLine("parts " + parts);
                    }
                    break;

                // ----------------- Size 처리 -----------------
                case "SIZE":
                    if (tabVm != null)
                    {
                        var parts = value.Split('-');
                        if (parts.Length == 2
                            && int.TryParse(parts[0], out int minVal)
                            && int.TryParse(parts[1], out int maxVal))
                        {
                            tabVm.Teaching.MinSize = minVal;
                            tabVm.Teaching.MaxSize = maxVal;
                        }
                    }
                    break;

                // ----------------- Shape 처리 -----------------
                case "SHAPE":
                    if (tabVm != null)
                    {
                        // value를 하이픈 기준으로 분리
                        var parts = value.Split('-');
                        var letters = parts[0];
                        var similarity = parts.Length > 1 && int.TryParse(parts[1], out var sim) ? sim : 70;

                        // ShapeType 결정
                        tabVm.Teaching.ShapeType =
                            letters.ToUpper() == "CIRCLE" ? EShape.Circle :
                            letters.ToUpper() == "RECTANGLE" ? EShape.Rectangle :
                            tabVm.Teaching.ShapeType;

                        tabVm.Teaching.Similarity = similarity;
                    }
                    break;

                // ----------------- Standard 처리 -----------------
                case "SELECT":
                    if (tabVm != null)
                    {
                        tabVm.Teaching.StandardType =
                            value == "MULTIOBJECT" ? ECalibrationStandard.MultiObject :
                            value == "CENTER" ? ECalibrationStandard.Center :
                            value == "BIGGEST" ? ECalibrationStandard.Biggest :
                            ECalibrationStandard.MultiObject;
                    }
                    break;

                // ----------------- ReticleType 처리 -----------------
                case "RETICLETYPE":   // 아직되는지 확인 못 해봄..사유_할콘 라이선스. 안 켜짐
                    vm.ReticleType = value switch
                    {
                        "NONE" => EReticleType.None,
                        "DEFAULT" => EReticleType.Default,
                        "FULLSIZE" => EReticleType.FullSize,
                        _ => vm.ReticleType
                    };
                    break;

                case "CAMERA":  //이따 디버그 되면 카메라 이름 확인해봐야함
                    if (tabVm != null)
                    {
                        // 문자열 value를 Enum으로 변환 (예: "CAM1" → ECamera.Cam1)
                        if (Enum.TryParse<ECamera>(value, true, out var camera))
                        {
                            vm.CameraSelectedValue = camera;
                        }
                    }
                    break;

                default:
                    Debug.WriteLine($"Unknown property name: {propertyName}");
                    break;
            }
        }

        public async Task<ApiResult> UpdateCalibrationProperty(string propertyName, string value)
        {
            try
            {
                var OpenedCalibrationWindow = await WindowManager.OpenOrActivateAsync<CalibrationWindow>();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!WindowManager.TryGetViewModel<CalibrationWindow, CalibrationViewModel>(out CalibrationViewModel? vm))
                    {
                        Debug.WriteLine("UpdateCalibrationProperty_vm 못가져옴 왠지모름");
                        // return;
                    }

                    calibrationUpdateProperty<CalibrationWindow, CalibrationViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedCalibrationWindow    // Window
                    );
                    //OpenedCalibrationWindow.ViewModel.UpdateProperty(propertyName, value);
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResult> UpdateLgaProperty(string propertyName, string value)
        {
            try
            {   // 윈도우가 열려 있으면 활성화, 없으면 새로 생성하여 표시
                var OpenedLgaTeachingWindow = await WindowManager.OpenOrActivateAsync<LgaTeachingWindow>();
                // // 윈도우의 DataContext에서 지정한 타입의 ViewModel 가져오기
                if (!WindowManager.TryGetViewModel<LgaTeachingWindow, LgaTeachingViewModel>(out LgaTeachingViewModel? vm))
                {
                    Debug.WriteLine("UpdateLgaProperty_vm 못가져옴 왠지모름 idk why tell me why~~~~~");
                    // return;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateTeaching<LgaTeachingWindow, LgaTeachingViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedLgaTeachingWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResult> UpdateBgaProperty(string propertyName, string value)
        {
            try
            {   // 윈도우가 열려 있으면 활성화, 없으면 새로 생성하여 표시
                var OpenedBgaTeachingWindow = await WindowManager.OpenOrActivateAsync<BgaTeachingWindow>();
                // // 윈도우의 DataContext에서 지정한 타입의 ViewModel 가져오기
                if (!WindowManager.TryGetViewModel<BgaTeachingWindow, BgaTeachingViewModel>(out BgaTeachingViewModel? vm))
                {
                    Debug.WriteLine("UpdateBgaProperty_vm 못가져옴 왠지모름 idk why tell me why~~~~~");
                    // return;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateTeaching<BgaTeachingWindow, BgaTeachingViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedBgaTeachingWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResult> UpdateStripProperty(string propertyName, string value)
        {
            try
            {   // 윈도우가 열려 있으면 활성화, 없으면 새로 생성하여 표시
                var OpenedStripTeachingWindow = await WindowManager.OpenOrActivateAsync<StripTeachingWindow>();
                // // 윈도우의 DataContext에서 지정한 타입의 ViewModel 가져오기
                if (!WindowManager.TryGetViewModel<StripTeachingWindow, StripTeachingViewModel>(out StripTeachingViewModel? vm))
                {
                    Debug.WriteLine("UpdateBgaProperty_vm 못가져옴 왠지모름 idk why tell me why~~~~~");
                    // return;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateTeaching<StripTeachingWindow, StripTeachingViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedStripTeachingWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResult> UpdateMappingProperty(string propertyName, string value)
        {
            try
            {   // 윈도우가 열려 있으면 활성화, 없으면 새로 생성하여 표시
                var OpenedMapTeachingWindow = await WindowManager.OpenOrActivateAsync<GridMoldTeachingWindow>();
                // // 윈도우의 DataContext에서 지정한 타입의 ViewModel 가져오기
                if (!WindowManager.TryGetViewModel<GridMoldTeachingWindow, GridMoldTeachingViewModel>(out GridMoldTeachingViewModel? vm))
                {
                    Debug.WriteLine("UpdateLgaProperty_vm 못가져옴 왠지모름 idk why tell me why~~~~~");
                    // return;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateTeaching<GridMoldTeachingWindow, GridMoldTeachingViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedMapTeachingWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResult> UpdateQfnProperty(string propertyName, string value)
        {
            try
            {   // 윈도우가 열려 있으면 활성화, 없으면 새로 생성하여 표시
                var OpenedQfnTeachingWindow = await WindowManager.OpenOrActivateAsync<QfnTeachingWindow>();
                // // 윈도우의 DataContext에서 지정한 타입의 ViewModel 가져오기
                if (!WindowManager.TryGetViewModel<QfnTeachingWindow, QfnTeachingViewModel>(out QfnTeachingViewModel? vm))
                {
                    Debug.WriteLine("UpdateLgaProperty_vm 못가져옴 왠지모름 idk why tell me why~~~~~");
                    // return;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateTeaching<QfnTeachingWindow, QfnTeachingViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedQfnTeachingWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }


        public void HistoryUpdateProperty<TWindow, TVM>(TVM vm, string propertyName, string value, TWindow? window = default)
            where TWindow : class
            where TVM : HistoryViewModel
        {
            // Debug.WriteLine("UpdateProperty_propertyName " + propertyName+ " value "+ value);
            if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(value))
                return;

            // 현재 열려 있는 HistoryWindow 가져오기
            // var historyWindow = ApiController.Instance.GetChildWindow<HistoryWindow>();
            var historyWindow = Application.Current.Windows
                     .OfType<HistoryWindow>()
                     .FirstOrDefault();

            if (historyWindow == null)
            {
                Debug.WriteLine("HistoryWindow가 열려 있지 않습니다.");
                return;
            }

            // DataContext를 ViewModel로 캐스팅
            //var vm = historyWindow.DataContext as HistoryViewModel;

            if (vm == null)
            {
                Debug.WriteLine("HistoryWindow의 ViewModel이 null입니다.");
                return;
            }

            // 실제 UI에 연결된 ViewModel을 대상으로 실행
            switch (propertyName.ToUpper())
            {
                case "DATE":
                    var parts = value.Split('_');
                    if (parts.Length == 2
                        && DateTime.TryParse(parts[0], out var fordate)
                        && DateTime.TryParse(parts[1], out var toDate))
                    {
                        //Debug.WriteLine("toDate ", toDate, " fordate ", fordate);
                        vm.SelectedBeforeDateTime = fordate;
                        vm.SelectedAfterDateTime = toDate;
                        vm.FilterApplyCommand.Execute(null);
                    }
                    break;

                case "INSPECTION":
                    if (Enum.TryParse<EInspection>(value, true, out var inspection))
                    {
                        vm.SelectedInspection = inspection;
                        // vm.OnPropertyChanged(nameof(vm.SelectedInspection));
                    }
                    break;

                case "CAMERA":
                    if (Enum.TryParse<ECamera>(value, true, out var camera))
                    {
                        vm.SelectedCamera = camera; // ComboBox와 바인딩된 속성 
                        // vm.OnPropertyChanged(nameof(SelectedCamera));
                    }
                    break;

                case "BUTTON":
                    switch (value)
                    {
                        case "SAVE":
                            vm.SaveCommand.Execute(null);
                            break;

                        case "OPEN":
                            vm.OpenButtonClickCommand.Execute(null);
                            break;
                    }
                    break;

            }

            vm.FilterApplyCommand.Execute(null);
        }


        public async Task<ApiResult> UpdateHistoryProperty(string propertyName, string value)
        {
            try
            {
                var OpenedHistoryWindow = await WindowManager.OpenOrActivateAsync<HistoryWindow>();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if(OpenedHistoryWindow == null)
                    {
                        Debug.WriteLine("OpenedHistoryWindow 못 열므");
                        return;
                    }
                    if (!WindowManager.TryGetViewModel<HistoryWindow, HistoryViewModel>(out HistoryViewModel? vm))
                    {
                        Debug.WriteLine("UpdateLgaProperty_vm 못가져옴 왠지모름 idk why tell me why~~~~~");
                        // return;
                    }

                    HistoryUpdateProperty<HistoryWindow, HistoryViewModel>(
                        vm,                        // ViewModel
                        propertyName,              // 처리할 property/command 이름
                        value,                     // 값 (예: moveTab → "RejectMark")
                        OpenedHistoryWindow    // Window
                    );
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"Property '{propertyName}' updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
        public async Task<ApiResult> UpdateGridBgaProperty(string propertyName, string value)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 열려있는 GridBgaTeachingWindow 찾기
                    var window = Application.Current.Windows
                        .OfType<GridBgaTeachingWindow>()
                        .FirstOrDefault();

                    // 창이 없으면 열기
                    if (window == null)
                    {
                        // 이미지 파일 선택 없이 빈 창 열기 (또는 기본 이미지 사용)
                        window = new GridBgaTeachingWindow();
                        window.Show();

                        // 창이 완전히 로드될 때까지 잠시 대기
                        System.Threading.Thread.Sleep(500);
                    }

                    // 창의 DataContext에서 실제 ViewModel 가져오기
                    if (window.DataContext is GridBgaTeachingViewModel viewModel)
                    {
                        viewModel.UpdateProperty(propertyName, value);

                        SystemInformationViewModel.Instance.Print($"GridBgaTeaching 속성 업데이트: {propertyName} = {value}");

                        return new ApiResult
                        {
                            Success = true,
                            Message = $"속성 '{propertyName}'이(가) '{value}'(으)로 업데이트되었습니다."
                        };
                    }
                    else
                    {
                        return new ApiResult
                        {
                            Success = false,
                            Error = "ViewModel을 찾을 수 없습니다."
                        };
                    }
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"속성 업데이트 실패: {ex.Message}");
                    return new ApiResult
                    {
                        Success = false,
                        Error = $"속성 업데이트 실패: {ex.Message}"
                    };
                }
            });
        }

        public async Task<ApiResult> AddRecipe(string newRecipeName)
        {
            // 기존: return await Application.Current.Dispatcher.InvokeAsync(async () =>
            // 수정: Dispatcher.InvokeAsync는 Task<ApiResult>를 반환해야 하므로, 내부 람다에서 await 사용하지 않고 Task<ApiResult> 반환
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {

                    WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
                    if (string.IsNullOrEmpty(newRecipeName))
                    {
                        return new ApiResult { Success = false, Error = "New recipe name cannot be empty." };
                    }

                    string newRecipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", newRecipeName);

                    if (Directory.Exists(newRecipePath))
                    {
                        return new ApiResult { Success = false, Error = "A recipe with that name already exists." };
                    }

                    Directory.CreateDirectory(newRecipePath);
                    SaveRecipe(newRecipePath);

                    SettingsViewModel.Instance.RefreshRecipeList();

                    SystemInformationViewModel.Instance.Print($"{newRecipeName} recipe created via API.");
                    return new ApiResult { Success = true, Message = $"{newRecipeName} recipe created successfully." };
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Failed to create recipe: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Failed to create recipe: {ex.Message}" };
                }
            });
        }

        private void SaveRecipe(string path)
        {
            WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
            Device defaultDevice = new Device();
            DeviceRecipeRepository.Instance.SaveRecipeByPath(defaultDevice, path);

            BgaRepository.Instance.SaveRecipeByPath(new BgaTeaching { IsTaught = false }, path);
            QfnRepository.Instance.SaveRecipeByPath(new QfnTeaching { IsTaught = false }, path);
            MoldRepository.Instance.SaveRecipeByPath(new MoldTeaching { IsTaught = false }, path);
            LgaRepository.Instance.SaveRecipeByPath(new LgaTeaching { IsTaught = false }, path);

            GridMoldRepository.Instance.SaveRecipeByPath(new GridMoldTeaching { IsTaught = false }, path);
            GridBgaRepository.Instance.SaveRecipeByPath(new GridBgaTeaching { IsTaught = false }, path);
            GridLgaRepository.Instance.SaveRecipeByPath(new GridLgaTeaching { IsTaught = false }, path);
            GridQfnRepository.Instance.SaveRecipeByPath(new GridQfnTeaching { IsTaught = false }, path);

            StripRepository.Instance.SaveRecipeByPath(new StripTeaching { IsTaught = false }, path);
            IlluminationRepository.Instance.SaveRecipeByPath(new IlluminationRecipe { IsTaught = false }, path);

            IlluminationRecipe illuminationRecipe = new IlluminationRecipe { IsTaught = false };
            foreach (KeyValuePair<ECamera, Dictionary<ELight, Light>> lightSettings in LightManager.Instance.Lights)
            {
                illuminationRecipe.Setting.Add(lightSettings.Key, new List<Dictionary<ELight, int>>());

                Dictionary<ELight, Light> lightByCamera = lightSettings.Value;
                List<Dictionary<ELight, int>> defaultLightValues = new List<Dictionary<ELight, int>>();

                foreach (KeyValuePair<ELight, Light> light in lightByCamera)
                {
                    if (illuminationRecipe.Setting[lightSettings.Key].Count == 0)
                    {
                        illuminationRecipe.Setting[lightSettings.Key].Add(new Dictionary<ELight, int>());
                    }
                    illuminationRecipe.Setting[lightSettings.Key][0].Add(light.Key, 0);
                }
            }

            IlluminationRepository.Instance.SaveRecipeByPath(illuminationRecipe, path);
        }

        public async Task<ApiResult> CopyRecipe(string sourceRecipeName, string newRecipeName)
        {

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {

                try
                {
                    WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
                    string sourceRecipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", sourceRecipeName);
                    string newRecipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", newRecipeName);

                    if (!Directory.Exists(sourceRecipePath))
                    {
                        return new ApiResult { Success = false, Error = "Source recipe not found." };
                    }

                    if (Directory.Exists(newRecipePath))
                    {
                        return new ApiResult { Success = false, Error = "A recipe with the new name already exists." };
                    }

                    CopyDirectory(sourceRecipePath, newRecipePath);

                    SettingsViewModel.Instance.RefreshRecipeList();

                    SystemInformationViewModel.Instance.Print($"Recipe {sourceRecipeName} copied to {newRecipeName} via API.");
                    return new ApiResult { Success = true, Message = $"Recipe copied successfully." };
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Failed to copy recipe: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Failed to copy recipe: {ex.Message}" };
                }
            });
        }

        private void CopyDirectory(string sourceFolder, string destFolder)
        {
            try
            {
                WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }

                string[] files = Directory.GetFiles(sourceFolder);
                string[] folders = Directory.GetDirectories(sourceFolder);

                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true);
                }

                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(destFolder, name);
                    CopyDirectory(folder, dest);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResult> RenameRecipe(string oldRecipeName, string newRecipeName)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
                    string oldRecipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", oldRecipeName);
                    string newRecipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", newRecipeName);

                    if (!Directory.Exists(oldRecipePath))
                    {
                        return new ApiResult { Success = false, Error = "Source recipe not found." };
                    }

                    if (Directory.Exists(newRecipePath))
                    {
                        return new ApiResult { Success = false, Error = "A recipe with the new name already exists." };
                    }

                    Directory.Move(oldRecipePath, newRecipePath);

                    if (GlobalSetting.Instance.DeviceInfo.RecipeName == oldRecipeName)
                    {
                        RecipeService.Instance.ChangeRecipe(newRecipePath, newRecipeName);
                    }

                    SettingsViewModel.Instance.RefreshRecipeList();

                    SystemInformationViewModel.Instance.Print($"Recipe {oldRecipeName} renamed to {newRecipeName} via API.");
                    return new ApiResult { Success = true, Message = $"Recipe renamed successfully." };
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Failed to rename recipe: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Failed to rename recipe: {ex.Message}" };
                }
            });
        }

        public async Task<ApiResult> DeleteRecipe(string recipeName)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
                    string recipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", recipeName);

                    if (!Directory.Exists(recipePath))
                    {
                        return new ApiResult { Success = false, Error = "Recipe not found." };
                    }

                    if (GlobalSetting.Instance.DeviceInfo.RecipeName == recipeName)
                    {
                        return new ApiResult { Success = false, Error = "Cannot delete the currently selected recipe." };
                    }

                    System.IO.Directory.Delete(recipePath, true);

                    SettingsViewModel.Instance.RefreshRecipeList();

                    SystemInformationViewModel.Instance.Print($"Recipe {recipeName} deleted via API.");
                    return new ApiResult { Success = true, Message = $"Recipe deleted successfully." };
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Failed to delete recipe: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Failed to delete recipe: {ex.Message}" };
                }
            });
        }

        public async Task<ApiResult> SelectRecipe(string recipeName)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    WindowManager.OpenOrActivateAsync<SettingsWindow>().Wait();
                    string recipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes", recipeName);

                    if (!Directory.Exists(recipePath))
                    {
                        return new ApiResult { Success = false, Error = "Recipe not found." };
                    }

                    RecipeService.Instance.ChangeRecipe(recipePath, recipeName);

                    SettingsViewModel.Instance.RefreshRecipeList();

                    SystemInformationViewModel.Instance.Print($"Recipe {recipeName} selected via API.");
                    return new ApiResult { Success = true, Message = $"Recipe selected successfully." };
                }
                catch (Exception ex)
                {
                    SystemInformationViewModel.Instance.Print($"Failed to select recipe: {ex.Message}");
                    return new ApiResult { Success = false, Error = $"Failed to select recipe: {ex.Message}" };
                }
            });
        }

       //나중을 위해 만든 레시피 목록 조회
        //public List<string> GetAllRecipes()
        //{
        //    var recipePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/Recipes");
        //    if (!Directory.Exists(recipePath))
        //    {
        //        return new List<string>();
        //    }
        //    return Directory.GetDirectories(recipePath).Select(Path.GetFileName).ToList()!;
        //}

        //private BgaTeachingViewModel? GetBgaTeachingViewModel()
        //{
        //    //var bgaWindow = GetChildWindow<BgaTeachingWindow>();
        //    if (bgaWindow == null)
        //    {
        //        return null;
        //    }

        //    return bgaWindow.DataContext as BgaTeachingViewModel;
        //}

        //bga roi 변경
        //public async Task<ApiResult> ExecuteBgaRoiOperation(string roiType, string operation, Dictionary<string, object> parameters)
        //{
        //    try
        //    {
        //        // 1) UI 스레드에서 현재 열린 BGA 티칭 뷰모델 가져오기
        //        var viewModel = await Application.Current.Dispatcher.InvokeAsync(() => GetBgaTeachingViewModel());

        //        // 2) 뷰모델이 없으면 창을 열도록 시도
        //        if (viewModel == null)
        //        {
        //            var openResult = await OpenBgaTeachingWindow();
        //            if (openResult == null || !openResult.Success)
        //            {
        //                return new ApiResult
        //                {
        //                    Success = false,
        //                    Error = $"BGA 티칭 창을 열 수 없습니다: {openResult?.Message ?? openResult?.Error ?? "Unknown error"}"
        //                };
        //            }

        //            // 창/뷰모델 초기화 시간 대기
        //            await Task.Delay(300);

        //            // 다시 뷰모델을 가져온다 (UI 스레드)
        //            viewModel = await Application.Current.Dispatcher.InvokeAsync(() => GetBgaTeachingViewModel());

        //            if (viewModel == null)
        //            {
        //                return new ApiResult
        //                {
        //                    Success = false,
        //                    Error = "BGA 티칭 뷰모델을 가져오지 못했습니다. 창이 정상적으로 열렸는지 확인하세요."
        //                };
        //            }
        //        }

        //        // 3) 뷰모델이 확보되었으므로 실제 ROI 작업을 UI 스레드에서 수행
        //        return await Application.Current.Dispatcher.InvokeAsync(() =>
        //        {
        //            try
        //            {
        //                var teaching = viewModel.Teaching;
        //                IList<Roi> roiList;

        //                switch (roiType.ToLower())
        //                {
        //                    case "pattern":
        //                        roiList = teaching.PatternRois;
        //                        break;
        //                    case "ball":
        //                        roiList = teaching.BallRois;
        //                        break;
        //                    case "surface":
        //                        roiList = teaching.SurfaceRois;
        //                        break;
        //                    case "dontcare":
        //                        roiList = teaching.DontCareRois;
        //                        break;
        //                    default:
        //                        return new ApiResult { Success = false, Error = $"알 수 없는 ROI 유형: {roiType}" };
        //                }

        //                switch (operation.ToLower())
        //                {
        //                    case "add":
        //                        string name = (string)parameters.GetValueOrDefault("name", "ROI");
        //                        double row = Convert.ToDouble(parameters.GetValueOrDefault("row", 100.0));
        //                        double col = Convert.ToDouble(parameters.GetValueOrDefault("col", 100.0));
        //                        double width = Convert.ToDouble(parameters.GetValueOrDefault("width", 100.0));
        //                        double height = Convert.ToDouble(parameters.GetValueOrDefault("height", 100.0));
        //                        roiList.Add(new Roi(name, row, col, row + height, col + width));
        //                        return new ApiResult { Success = true, Message = $"{roiType} ROI 추가 완료" };

        //                    case "update":
        //                        if (!parameters.ContainsKey("index"))
        //                            return new ApiResult { Success = false, Error = "update 작업에는 index 파라미터가 필요합니다." };

        //                        int indexToUpdate = Convert.ToInt32(parameters["index"]);
        //                        if (indexToUpdate < 0 || indexToUpdate >= roiList.Count)
        //                        {
        //                            return new ApiResult { Success = false, Error = "인덱스가 범위를 벗어났습니다." };
        //                        }
        //                        Roi roiToUpdate = roiList[indexToUpdate];
        //                        if (parameters.ContainsKey("name")) roiToUpdate.Name = (string)parameters["name"];
        //                        if (parameters.ContainsKey("row")) roiToUpdate.Row1 = Convert.ToDouble(parameters["row"]);
        //                        if (parameters.ContainsKey("col")) roiToUpdate.Col1 = Convert.ToDouble(parameters["col"]);
        //                        if (parameters.ContainsKey("height")) roiToUpdate.Row2 = roiToUpdate.Row1 + Convert.ToDouble(parameters["height"]);
        //                        if (parameters.ContainsKey("width")) roiToUpdate.Col2 = roiToUpdate.Col1 + Convert.ToDouble(parameters["width"]);
        //                        return new ApiResult { Success = true, Message = $"{roiType} ROI 업데이트 완료 (인덱스: {indexToUpdate})" };

        //                    case "delete":
        //                        if (!parameters.ContainsKey("index"))
        //                            return new ApiResult { Success = false, Error = "delete 작업에는 index 파라미터가 필요합니다." };

        //                        int indexToDelete = Convert.ToInt32(parameters["index"]);
        //                        if (indexToDelete < 0 || indexToDelete >= roiList.Count)
        //                        {
        //                            return new ApiResult { Success = false, Error = "인덱스가 범위를 벗어났습니다." };
        //                        }
        //                        roiList.RemoveAt(indexToDelete);
        //                        return new ApiResult { Success = true, Message = $"{roiType} ROI 삭제 완료 (인덱스: {indexToDelete})" };

        //                    case "reset":
        //                        roiList.Clear();
        //                        return new ApiResult { Success = true, Message = $"모든 {roiType} ROI가 리셋되었습니다." };

        //                    default:
        //                        return new ApiResult { Success = false, Error = $"알 수 없는 작업: {operation}" };
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                return new ApiResult { Success = false, Error = $"ROI 작업 실패: {ex.Message}" };
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResult { Success = false, Error = $"ExecutefRoiOperation 실패: {ex.Message}" };
        //    }
        //}
        //#endregion
    }



    /// <summary>
    /// Represents the result of an API operation
    /// </summary>
    public class ApiResult
    {
        // _userAgreed 유저의 실행여부 확인을 받기
        private bool? _userAgreed;
        public bool UserAgreed
        {
            get => _userAgreed ?? false;
            set => _userAgreed = value;
        }

        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
