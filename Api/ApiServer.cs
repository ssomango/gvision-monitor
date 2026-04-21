using GVisionWpf.Illuminations;
using GVisionWpf.Models.Visions;
using GVisionWpf.Services;
using GVisionWpf.Types;
using GVisionWpf.UIs.Frames.Pages;
using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.UIs.ViewModels.Calibrations;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using static GVisionWpf.UIs.ViewModels.ChatWindowViewModel;

namespace GVisionWpf.Api
{
    /// HTTP Listener 기반 API 서버 구현

    public class ApiServer
    {
        private static ApiServer? _instance;
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly int _port = 3000;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _isRunning;
        private static readonly HttpClient client = new HttpClient();

        private ChatWindowViewModel? _chatWindowViewModel;

        private Dictionary<string, Type> tabMap = new()
        {
            { "SETTING", typeof(SettingJigCalibrationTabViewModel) },
            { "VISION", typeof(VisionTableCalibrationTabViewModel) },
            { "TRAY", typeof(TrayCalibrationTabViewModel) },
            { "BOTTOM", typeof(BottomJigCalibrationTabViewModel)},
            { "PAD", typeof(PadPitchCalibrationTabViewModel)}
        };

        public static ApiServer Instance => _instance ??= new ApiServer();
        private ApiServer()
        {
            //시간 지나도 오류 표시 불가능 할 시 타임아웃 설정
            //client.Timeout = TimeSpan.FromSeconds(180);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            _listener = new HttpListener();
            _listener.Prefixes.Clear();

            // ✅ 권장: 로컬 전용으로 명시 등록
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            if (_isRunning)
                return;

            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"API Server started on port {_port}");
            SystemInformationViewModel.Instance.Print($"REST API server started on http://localhost:{_port}");
            // Start processing requests in a background task
            _ = Task.Run(async () => await ProcessRequestsAsync(_cancellationTokenSource.Token));
        }
        public async Task<HttpResponseMessage> SwitchModelAsync(string modelFullName)
        {
            var url = "http://localhost:5000/models/switch";
            //var url = "http://100.112.233.107:5000/models/switch";
            var json = JsonSerializer.Serialize(new { model_name = modelFullName });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            return response;
        }

        private static async Task<string> ReadRequestBodyAsync(HttpListenerRequest request)
        {
            var enc = request.ContentEncoding ?? Encoding.UTF8;
            using var reader = new StreamReader(request.InputStream, enc);
            return await reader.ReadToEndAsync();
        }

        public async Task<HttpResponseMessage> SendChatInputAsync(string chatInput, Dictionary<string, string> currentOpenedWindow, string modelName)
        {
            var url = "http://localhost:5000/instruct/";
            //var url = "http://100.112.233.107:5000/instruct/";
            var json = JsonSerializer.Serialize(new
            {
                text = chatInput,
                model_name = modelName,
                current_opened_window_and_tab = currentOpenedWindow
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            // 응답 본문 읽기
            string responseBody = await response.Content.ReadAsStringAsync();
            // 콘솔에 로그 출력 (Visual Studio 출력창, 콘솔창 확인)
            // Debug.WriteLine($"response는..."+response);
            // Debug.WriteLine($"클라이언트 응답 본문: " + responseBody);

            return response;
        }

        public async Task<HttpResponseMessage> SendChatInputWithContextAsync(List<ChatMessage> chatInput, Dictionary<string, string> currentOpenedWindow, string modelName)
        {
            var url = "http://localhost:5000/instruct/";
            //var url = "http://100.112.233.107:5000/instruct/";

            // Time 제외, Sender + Message만 전송
            string promptText = string.Join(", ", chatInput.Select(m => $"{m.Sender}: {m.Message}"));

            var json = JsonSerializer.Serialize(new
            {
                text = promptText,
                model_name = modelName,
                current_opened_window_and_tab = currentOpenedWindow
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            // 응답 본문 읽기
            string responseBody = await response.Content.ReadAsStringAsync();
     
            return response;
        }


        public async Task StopAsync()
        {
            if (!_isRunning)
                return;
            try
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();
                _isRunning = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping API server: {ex.Message}");
            }
        }

        private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get the next request
                    HttpListenerContext context = await _listener.GetContextAsync();

                    // Process the request in a separate task
                    _ = Task.Run(async () => await HandleRequestAsync(context), cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Error processing request: {ex.Message}");
                }
            }
        }

        // ChatWindowViewModel 설정 메서드
        public void SetChatWindowViewModel(ChatWindowViewModel viewModel)
        {
            _chatWindowViewModel = viewModel;
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            Debug.WriteLine($"HandleRequestAsync 시작");
            try
            {
                Debug.WriteLine($"try 진입");
                //string path = context.Request.Url?.AbsolutePath ?? "/";
                string path = (context.Request.Url?.AbsolutePath ?? "/").TrimEnd('/');
                var roiQuery = context.Request.QueryString;
                Debug.WriteLine($"{path} 경로입니다."); 
                SystemInformationViewModel.Instance.Print($"{path}");

                if (path.Equals("/tutor/mode1", StringComparison.OrdinalIgnoreCase))
                {
                    if (!context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = 405;
                        await SendJsonResponse(context, new { success = false, error = "Use POST" });
                        return;
                    }

                    var contentType = context.Request.ContentType ?? "";
                    if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Content-Type must be application/json" });
                        return;
                    }

                    string rawBody = await ReadRequestBodyAsync(context.Request);

                    // ✅ ApiController에 만들었던 핸들러로 넘김
                    var result = await ApiController.Instance.HandleTutorMode1Async(rawBody);

                    if (!result.Success) context.Response.StatusCode = 400;

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path == "/")
                {
                    await SendJsonResponse(context, new
                    {
                        name = "GVision API",
                        version = "1.0",
                        endpoints = ApiController.Instance.GetApiEndpoints()
                    });
                    return;
                }

                if (path == "/api/status")
                {
                    await SendJsonResponse(context, ApiController.Instance.GetApplicationStatus());
                    return;
                }

                // 모드 설정 엔드포인트
                if (path.StartsWith("/mode/set"))
                {
                    string mode = context.Request.QueryString["mode"]?.ToUpper() ?? "";
                    if (mode == "RUN" || mode == "SETUP")
                    {
                        var result = await ApiController.Instance.SetOperationMode(mode);
                        await SendJsonResponse(context, new
                        {
                            success = result.Success,
                            message = result.Message,
                            error = result.Error,
                            timestamp = result.Timestamp
                        });
                        return;
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new
                        {
                            success = false,
                            error = "Invalid mode. Use 'RUN' or 'SETUP'.",
                            timestamp = DateTime.Now
                        });
                        return;
                    }
                }

                // 라이브 토글 엔드포인트
                if (path == "/live/toggle")
                {
                    string switchValue = context.Request.QueryString["switch"]?.ToUpper() ?? "";  //라이브 킬지 말지
                    string cameraIndexStr = context.Request.QueryString["no"] ?? "";            //카메라 번호 0~5

                    if (!int.TryParse(cameraIndexStr, out int cameraIndex) || cameraIndex < 0 || cameraIndex > 5)
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new
                        {
                            success = false,
                            error = "Invalid camera index. Use a number between 0 and 5 in the 'no' parameter.",
                            timestamp = DateTime.Now
                        });
                        return;
                    }
                    else
                    {   // 정상 값이면 -1 처리
                        cameraIndex -= 1;  // １~６ → ０~５ 로 조정
                    }

                    if (switchValue != "ON" && switchValue != "OFF")
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new
                        {
                            success = false,
                            error = "Invalid switch value. Use 'ON' or 'OFF' in the 'switch' parameter.",
                            timestamp = DateTime.Now
                        });
                        return;
                    }
                    bool switchValue_bool;
                    if (switchValue == "ON")
                    {
                        switchValue_bool = true;
                    }
                    else
                    {
                        switchValue_bool = false;
                    }

                    // Debug.WriteLine($"ToggleCameraLive(cameraIndex, switchValue == {switchValue_bool} 함0");
                    var result = await ApiController.Instance.ToggleCameraLive(cameraIndex, switchValue_bool);
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });

                    return;
                }

                if (path == "/test/run/map")
                {
                    var result = await ApiController.Instance.RunTestMap();

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path == "/test/run/prs")
                {
                    var result = await ApiController.Instance.RunTestPrs();


                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }



                // 윈도우 열기 엔드포인트들
                if (path == "/windows/teaching/bga")
                {
                    var result = await ApiController.Instance.OpenBgaTeachingWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                // AS 윈도우 열기 엔드포인트 2025.11.17

                if (path == "/windows/as")
                {
                    var result = await ApiController.Instance.OpenAsWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path == "/windows/teaching/mapping")
                {
                    Debug.WriteLine("Api Server   /windows/teaching/mapping");
                    var result = await ApiController.Instance.OpenMapTeachingWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }
                if (path.StartsWith("/teaching/mapping/update"))
                {
                    String propertyName = context.Request.QueryString["propertyName"];
                    String value = context.Request.QueryString["value"]?.ToUpper() ?? "";

                    Debug.WriteLine("ApiServer_propertyName " + propertyName + " value " + value);

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    var result = default(ApiResult); // ApiResult는 UpdateLgaProperty 반환 타입 예시
                    result = await ApiController.Instance.UpdateMappingProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }

                if (path == "/windows/teaching/qfn")
                {
                    var result = await ApiController.Instance.OpenQfnTeachingWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path.StartsWith("/teaching/bga/update"))
                {
                    String propertyName = context.Request.QueryString["propertyName"];
                    String value = context.Request.QueryString["value"]?.ToUpper() ?? "";

                    Debug.WriteLine("ApiServer_propertyName " + propertyName + " value " + value);

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    var result = default(ApiResult); // ApiResult는 UpdateLgaProperty 반환 타입 예시

                    //if (propertyName == "simple")
                    //{
                    //    result = await ApiController.Instance.UpdateSimpleLgaProperty(propertyName, value);
                    //}
                    //else
                    //{
                    result = await ApiController.Instance.UpdateBgaProperty(propertyName, value);
                    //}

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }

                //if (path == "/windows/teaching/mapping")
                //{
                //    var result = await ApiController.Instance.OpenMapTeachingWindow();
                //    await SendJsonResponse(context, new
                //    {
                //        success = result.Success,
                //        message = result.Message,
                //        error = result.Error,
                //        timestamp = result.Timestamp
                //    });
                //    return;
                //}
                if (path.StartsWith("/teaching/qfn/update"))
                {
                    String propertyName = context.Request.QueryString["propertyName"];
                    String value = context.Request.QueryString["value"]?.ToUpper() ?? "";

                    Debug.WriteLine("ApiServer_propertyName " + propertyName + " value " + value);

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    var result = default(ApiResult); // ApiResult는 UpdateLgaProperty 반환 타입 예시
                    result = await ApiController.Instance.UpdateQfnProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }

                if (path == "/windows/teaching/lga")
                {
                    var result = await ApiController.Instance.OpenLgaTeachingWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path.StartsWith("/teaching/lga/update"))
                {
                    String propertyName = context.Request.QueryString["propertyName"];
                    String value = context.Request.QueryString["value"]?.ToUpper() ?? "";

                    Debug.WriteLine("ApiServer_propertyName " + propertyName + " value " + value);

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    var result = default(ApiResult); // ApiResult는 UpdateLgaProperty 반환 타입 예시
                    result = await ApiController.Instance.UpdateLgaProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }

                if (path == "/windows/teaching/strip")
                {
                    var result = await ApiController.Instance.OpenStripTeachingWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path.StartsWith("/teaching/strip/update"))
                {
                    String propertyName = context.Request.QueryString["propertyName"];
                    String value = context.Request.QueryString["value"]?.ToUpper() ?? "";

                    Debug.WriteLine("ApiServer_propertyName " + propertyName + " value " + value);

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    var result = default(ApiResult); // ApiResult는 UpdateLgaProperty 반환 타입 예시
                    result = await ApiController.Instance.UpdateStripProperty(propertyName, value);
                    

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }

                if (path == "/windows/teaching/qc")
                {
                    //var result = await ApiController.Instance.OpenQcTeachingWindow();
                    //await SendJsonResponse(context, new
                    //{
                    //    success = result.Success,
                    //    message = result.Message,
                    //    error = result.Error,
                    //    timestamp = result.Timestamp
                    //});
                    return;
                }

                if (path == "/windows/teaching/prs/reteach")
                {
                    //var result = await ApiController.Instance.OpenPrsReteachWindow();
                    //await SendJsonResponse(context, new
                    //{
                    //    success = result.Success,
                    //    message = result.Message,
                    //    error = result.Error,
                    //    timestamp = result.Timestamp
                    //});
                    return;
                }

                if (path == "/windows/teaching/mapping/reteach")
                {
                    //var result = await ApiController.Instance.OpenMapReteachWindow();
                    //await SendJsonResponse(context, new
                    //{
                    //    success = result.Success,
                    //    message = result.Message,
                    //    error = result.Error,
                    //    timestamp = result.Timestamp
                    //});
                    return;
                }


                //if (path == "/windows/history")
                //{

                //    var result = await ApiController.Instance.OpenWindow<HistoryWindow>("History"); ;
                //    await SendJsonResponse(context, new
                //    {
                //        success = result.Success,
                //        message = result.Message,
                //        error = result.Error,
                //        timestamp = result.Timestamp
                //    });
                //    return;
                //}
                if (path == "/windows/calibration")
                {
                    var result = await ApiController.Instance.OpenWindow<CalibrationWindow>("Calibration");
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }
                if (path.StartsWith("/calibration/update"))
                {
                    String propertyName = context.Request.QueryString["propertyName"]?.ToUpper() ?? "";
                    String value = context.Request.QueryString["value"]?.ToUpper() ?? "";
  
                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    // 속성 값 업데이트 + 만약 창이 안 켜져있으면 켜줌    
                    var result = await ApiController.Instance.UpdateCalibrationProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }
                
                if (path == "/windows/history")
                {
                    Debug.WriteLine("histroy창 호출 됨요!!!!");
                    var result = await ApiController.Instance.OpenWindow<HistoryWindow>("History");
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }


                if (path.StartsWith("/history/update"))  // 예시 "2025-09-11"
                { 
                    String propertyName = context.Request.QueryString["propertyName"]?.ToUpper() ?? "";
                    String value = context.Request.QueryString["value"];  //?.ToUpper() ?? "";

                    Debug.WriteLine("apiserver_ propertyName "+ propertyName+ " value "+ value);
                    // 속성 값 업데이트 + 만약 창이 안 켜져있으면 켜줌    
                    var result = await ApiController.Instance.UpdateHistoryProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;

                }



                //새 채팅 코드 추가
                if (path == "/chat/clear")
                {
                    var result = await ApiController.Instance.CallClearChat();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path == "/windows/light")
                {
                    var result = await ApiController.Instance.OpenWindow<LightWindow>("Light");
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if(path == "/exit")
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = Application.Current.MainWindow;
                        if (mainWindow?.DataContext is MainWindowViewModel vm)
                        {
                            vm.exit();  // public 메서드 호출
                        }
                    });

                    await SendJsonResponse(context, new { success = true, message = "Application exiting." });
                    return;
                }

                //light창 live 카메라 on
                if (path.StartsWith("/windows/light/live"))
                {
                    var cameraName = context.Request.QueryString["camera"] ?? "";
                    Debug.WriteLine("apiServer_ windows/light/live_ cameraName " + cameraName);

                    if (string.IsNullOrEmpty(cameraName))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new
                        {
                            success = false,
                            message = "Camera name parameter is missing",
                            timestamp = DateTime.UtcNow
                        });
                        return;
                    }

                    //var validCameras = new HashSet<string> { "PRS", "BarCode", "SettingX1", "SettingX2", "Mapping" };
                    //if (!validCameras.Contains(cameraName))
                    //{
                    //    context.Response.StatusCode = 400;
                    //    await SendJsonResponse(context, new
                    //    {
                    //        success = false,
                    //        message = $"Invalid camera name: {cameraName}",
                    //        timestamp = DateTime.UtcNow
                    //    });
                    //    return;
                    //}

                    var result = await ApiController.Instance.OpenLightLiveView(cameraName);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });

                    return;
                }

                if (path == "/windows/settings")
                {
                    var result = await ApiController.Instance.OpenWindow<SettingsWindow>("Setting");
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }


                if (path.StartsWith("/closeWindows"))
                {
                    String WindowName = context.Request.QueryString["window"] ?? "";
                    Debug.WriteLine("from apiServer WindowName " + WindowName);

                    var result = await ApiController.Instance.RunCloseAllWindows(WindowName);
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                // 창 킬까요 끌까요?_ 답변이 yes일 때
                if (path == "/execute/yes")
                {
                    var result = await ApiController.Instance.executeYes();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }// 창 킬까요 끌까요?_ 답변이 no일 때
                if (path == "/execute/no")
                {
                    var result = await ApiController.Instance.executeNo();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                //모델 변경 요청 시 (새로 추가한 부분) 
                if (path == "/change/model")
                {
                    await ApiController.Instance.NotifyTestStatus("모델을 변경하겠습니다.");
                    await SendJsonResponse(context, new
                    {
                        success = true,
                        message = "Changing Model",
                        error = "false",
                        timestamp = true
                    });
                    return;
                }

                if (path == "/windows/monitor")
                {
                    var result = await ApiController.Instance.OpenMonitorWindow();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                if (path == "/windows/lot")
                {
                    var result = await ApiController.Instance.OpenWindow<LotWindow>("Lot");
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }

                //if (path == "/recipes")
                //{
                //    var recipes = ApiController.Instance.GetAllRecipes();
                //    await SendJsonResponse(context, new { success = true, data = recipes });
                //    return;
                //}

                if (path == "/recipes/add")
                {
                    string? recipeName = context.Request.QueryString["name"];
                    if (string.IsNullOrEmpty(recipeName))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing 'name' parameter." });
                        return;
                    }
                    var result = await ApiController.Instance.AddRecipe(recipeName);
                    await SendJsonResponse(context, result);
                    return;
                }

                if (path == "/recipes/copy")
                {
                    string? sourceName = context.Request.QueryString["source"];
                    string? destName = context.Request.QueryString["dest"];
                    if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(destName))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing 'source' or 'dest' parameter." });
                        return;
                    }
                    var result = await ApiController.Instance.CopyRecipe(sourceName, destName);
                    await SendJsonResponse(context, result);
                    return;
                }

                if (path == "/recipes/rename")
                {
                    string? oldName = context.Request.QueryString["old"];
                    string? newName = context.Request.QueryString["new"];
                    if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing 'old' or 'new' parameter." });
                        return;
                    }
                    var result = await ApiController.Instance.RenameRecipe(oldName, newName);
                    await SendJsonResponse(context, result);
                    return;
                }

                if (path == "/recipes/delete")
                {
                    string? recipeName = context.Request.QueryString["name"];
                    if (string.IsNullOrEmpty(recipeName))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing 'name' parameter." });
                        return;
                    }
                    var result = await ApiController.Instance.DeleteRecipe(recipeName);
                    await SendJsonResponse(context, result);
                    return;
                }

                if (path == "/recipes/select")
                {
                    string? recipeName = context.Request.QueryString["name"];
                    if (string.IsNullOrEmpty(recipeName))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing 'name' parameter." });
                        return;
                    }
                    var result = await ApiController.Instance.SelectRecipe(recipeName);
                    await SendJsonResponse(context, result);
                    return;
                }

                //if (path == "/main/foo")
                //{
                //    //bool called = ApiController.Instance.CallMainFoo();

                //    await SendJsonResponse(context, new
                //    {
                //        success = called,
                //        message = called ? "foo() called successfully" : "MainViewModel is not assigned",
                //        timestamp = DateTime.Now
                //    });
                //    return;
                //}
                if (path == "/settings/update")
                {
                    var query = context.Request.QueryString;

                    string propertyName = query["propertyName"] ?? "";
                    string value = query["value"] ?? "";

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new { success = false, error = "Missing propertyName or value in query string" });
                        return;
                    }

                    var result = await ApiController.Instance.UpdateSettingsProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = DateTime.Now
                    });
                    return;
                }
                if (path == "/chat/clear")
                {
                    var result = await ApiController.Instance.CallClearChat();
                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }
                // RoiDataListPanel 작업 실행 (통합 엔드포인트)
                if (path == "/roi/operation")
                {
                    string operationName = roiQuery["operationName"] ?? "";

                    if (string.IsNullOrEmpty(operationName))
                    {
                        await SendJsonResponse(context, new ApiResult
                        {
                            Success = false,
                            Error = "operationName 파라미터가 필요합니다."
                        });
                        return;
                    }

                    // 파라미터 파싱
                    var parameters = new Dictionary<string, object>();

                    // AddRoiOperation 파라미터
                    if (operationName == "AddRoiOperation")
                    {
                        //parameters["row"] = double.Parse(roiQuery["row"] ?? "500");
                        //parameters["col"] = double.Parse(roiQuery["col"] ?? "500");
                        //parameters["height"] = double.Parse(roiQuery["height"] ?? "1000");
                        //parameters["width"] = double.Parse(roiQuery["width"] ?? "1000");
                        parameters["row"] = ParseDoubleOrDefault(roiQuery["row"], 500);
                        parameters["col"] = ParseDoubleOrDefault(roiQuery["col"], 500);
                        parameters["height"] = ParseDoubleOrDefault(roiQuery["height"], 1000);
                        parameters["width"] = ParseDoubleOrDefault(roiQuery["width"], 1000);
                    }
                    // DeleteRoiOperation 파라미터
                    else if (operationName == "DeleteRoiOperation")
                    {
                        if (int.TryParse(roiQuery["index"], out int index))
                        {
                            parameters["index"] = index;
                        }
                    }
                    else if (operationName == "UpdateRoiOperation")
                    {
                        if (int.TryParse(roiQuery["index"], out int index))
                        {
                            parameters["index"] = index;
                        }
                        else
                        {
                            await SendJsonResponse(context, new ApiResult
                            {
                                Success = false,
                                Error = "필수 파라미터 'index'가 누락되었거나 잘못된 형식입니다."
                            });
                            return;
                        }

                        if (double.TryParse(roiQuery["row"], out double row))
                        {
                            parameters["row"] = row;
                        }
                        if (double.TryParse(roiQuery["col"], out double col))
                        {
                            parameters["col"] = col;
                        }
                        if (double.TryParse(roiQuery["height"], out double height))
                        {
                            parameters["height"] = height;
                        }
                        if (double.TryParse(roiQuery["width"], out double width))
                        {
                            parameters["width"] = width;
                        }
                        if (roiQuery["roiName"] != null)
                        {
                            parameters["roiName"] = roiQuery["roiName"];
                        }
                    }
                    // ResetRoisOperation은 파라미터 없음

                    return;
                }

                //if (path.StartsWith("/bga/roi/"))
                //{
                //    var pathSegments = path.Trim('/').Split('/');
                //    if (pathSegments.Length == 4)
                //    {
                //        string roiType = pathSegments[2];
                //        string operation = pathSegments[3];

                //        var parameters = new Dictionary<string, object>();
                //        foreach (var key in context.Request.QueryString.AllKeys)
                //        {
                //            if (key != null)
                //            {
                //                string? value = context.Request.QueryString[key];
                //                if (int.TryParse(value, out int intValue))
                //                {
                //                    parameters[key] = intValue;
                //                }
                //                else if (double.TryParse(value, out double doubleValue))
                //                {
                //                    parameters[key] = doubleValue;
                //                }
                //                else
                //                {
                //                    parameters[key] = value ?? "";
                //                }
                //            }
                //        }

                //        var result = await ApiController.Instance.ExecuteBgaRoiOperation(roiType, operation, parameters);
                //        await SendJsonResponse(context, result);
                //        return;
                //    }
                //}

                // GridBgaTeachingViewModel 속성 업데이트 엔드포인트
                if (path.StartsWith("/teaching/gridbga/update"))
                {
                    string propertyName = context.Request.QueryString["propertyName"];
                    string value = context.Request.QueryString["value"];

                    Debug.WriteLine($"ApiServer - GridBga Update: propertyName={propertyName}, value={value}");

                    if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(value))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(context, new
                        {
                            success = false,
                            error = "Missing propertyName or value in query string",
                            timestamp = DateTime.Now
                        });
                        return;
                    }

                    // 속성 값 업데이트
                    var result = await ApiController.Instance.UpdateGridBgaProperty(propertyName, value);

                    await SendJsonResponse(context, new
                    {
                        success = result.Success,
                        message = result.Message,
                        error = result.Error,
                        timestamp = result.Timestamp
                    });
                    return;
                }


                // 모호 프롬트 요청시 출력
                if (path.StartsWith("/vague"))
                {
                    string encodedUrl = context.Request.QueryString["response"];
                    Debug.WriteLine("vague 들어옴, encodedUrl: " + encodedUrl);
                    if (encodedUrl == null)
                    {
                        string raw = context.Request.RawUrl;
                        Debug.WriteLine("raw" + raw);
                        await ApiController.Instance.NotifyTestStatus
                            ("모호한 프롬프트. 다시 작성해주세요.");
                    }
                    else
                    {
                        string decoded = Uri.UnescapeDataString(encodedUrl);
                        await ApiController.Instance.NotifyTestStatus(decoded);
                    }
                    await SendJsonResponse(context, new
                    {
                        success = true,
                        message = "NO_FUNCTION STATUS",
                        error = "false",
                        timestamp = true
                    });
                    return;
                }

                //없는 기능 요청 시 출력
                if (path == "/NO_FUNCTION")
                {
                    await ApiController.Instance.NotifyTestStatus("없는 기능입니다.");
                    await SendJsonResponse(context, new
                    {
                        success = true,
                        message = "NO_FUNCTION STATUS",
                        error = "false",
                        timestamp = true
                    });
                    return;
                }

                else   //일치하는 path가 없다면 잘못된 path이니 나중에 고칠 때 참고용으로 출력하기
                {
                    string raw = context.Request.RawUrl;
                    await ApiController.Instance.NotifyTestStatus("잘못된 api를 llm이 전송함, path: " + raw);
                    await SendJsonResponse(context, new
                    {
                        success = true,
                        message = "wrong api STATUS",
                        error = "false",
                        timestamp = true
                    });
                    return;
                }

                // 404 Not Found
                context.Response.StatusCode = 404;
                await SendJsonResponse(context, new
                {
                    success = false,
                    error = $"Unknown path: {path}",
                    timestamp = DateTime.Now
                });

                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    await SendJsonResponse(context, new
                    {
                        success = false,
                        error = "Internal server error",
                        timestamp = DateTime.Now
                    });
                }
                catch
                {
                    // Ignore errors when sending error response
                }
            }
        }
        //문자열 파싱 헬퍼 메서드
        private static double ParseDoubleOrDefault(object? value, double defaultValue)
        {
            if (value == null)
                return defaultValue;

            string strVal = value.ToString()?.Trim() ?? "";
            if (double.TryParse(strVal, out double result))
                return result;

            return defaultValue;
        }

        private async Task SendJsonResponse(HttpListenerContext context, object data)
        {
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();
        }


    }
} 