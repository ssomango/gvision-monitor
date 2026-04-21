// CommunityToolkit.Mvvm 라이브러리를 사용하여 MVVM 패턴을 쉽게 구현
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Api;
using GVisionWpf.Cameras;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using GVisionWpf.UIs.Overlays;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;



namespace GVisionWpf.UIs.ViewModels
{
    public partial class ChatWindowViewModel : ViewModelBase
    {
        private SystemUsageMonitorWindow? _systemWindow;
        private const int ChatLogMax = 200;
        private int _mainGuideInProgress;
        private int _mainGuideInvokeSeq;
        
        //circular progress indicator
        [ObservableProperty]
        private bool _isLoading = false;

        private ChatMessage _loadingMessage;

        //dot3개
        private void AddLoadingMessage()
        {
            _loadingMessage = new ChatMessage
            {
                Sender = "System",
                Message = string.Empty,
                Time = string.Empty,
                IsLoading = true
            }; 
            ChatLog.Add(_loadingMessage);
        }

        private readonly ApiServer _apiServer;

        [ObservableProperty]
        private string _chatInput = string.Empty;

        [ObservableProperty]
        private string _statusText = string.Empty;

        //콤보박스 바인딩용
        public ObservableCollection<string> ModelList { get; } = new ObservableCollection<string>
        {
            "Exaone-2.4B",
            "Exaone-7.8B",
            "Trillion-7B",
            "Qwen3-0.6B"
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedModelFullName))]
        private string _selectedModel = "Exaone-2.4B";

        // 모델 풀네임 매핑 딕셔너리
        private readonly Dictionary<string, string> modelNameMap = new Dictionary<string, string>
        {
            { "Exaone-2.4B", "LGAI-EXAONE/EXAONE-3.5-2.4B-Instruct" },
            { "Exaone-7.8B", "LGAI-EXAONE/EXAONE-3.5-7.8B-Instruct" },
            { "Trillion-7B", "trillionlabs/Trillion-7B-preview" },
            { "Qwen3-0.6B", "Qwen/Qwen3-0.6B" }
        };

        // 풀네임 반환 프로퍼티
        public string SelectedModelFullName => modelNameMap.ContainsKey(SelectedModel) ? modelNameMap[SelectedModel] : null;


        // 채팅 메시지를 나타내는 클래스, 필요시 확장 가능
        public class ChatMessage : ObservableObject
        {
            private string _sender;
            public string Sender
            {
                get => _sender;
                set => SetProperty(ref _sender, value);
            }

            private string _time;
            public string Time
            {
                get => _time;
                set => SetProperty(ref _time, value);
            }

            private string _message;
            public string Message
            {
                get => _message;
                set => SetProperty(ref _message, value);
            }

            private bool _isLoading;
            public bool IsLoading
            {
                get => _isLoading;
                set => SetProperty(ref _isLoading, value);
            }
        }



        // 채팅 로그를 담는 컬렉션 ObservableCollection은 아이템이 추가/삭제될 때 UI가 자동으로 갱신된다
        public ObservableCollection<ChatMessage> ChatLog { get; } = new();

        // 채팅의 버튼에서 사용할 Command들... xaml에서 바인딩 가능! mvvm 패턴? 유지보수성을 지켜주는 친구들! 
        public ICommand SendChatCommand { get; }
        public ICommand StartMainWindowGuideCommand { get; }
        public IAsyncRelayCommand StartMappingTeachingPracticeCommand { get; }
        public IAsyncRelayCommand StartBgaTeachingPracticeCommand { get; }
        public IAsyncRelayCommand StartSettingsGuideCommand { get; }
        public ICommand NewChatCommand { get; }  // 삭제  
        public ICommand PreviousChatsCommand { get; } // 삭제
        //private string ChatInput { get; set; }
        public ChatWindowViewModel(ApiServer apiServer)
        {
            LogSystem("환영합니다! 반도체 공정 프로그램 Gvision입니다.\r\n처음 사용하시는 분도 쉽게 따라올 수 있도록 검사 설정과 티칭 과정을 안내합니다.");
            LogSystem("'BGA teaching 실습' 등 원하는 실습을 입력하거나 버튼을 눌러 실행하세요");

            //SendChatCommand = new RelayCommand(ExecuteChatInput);
            _apiServer = apiServer;
            SendChatCommand = new RelayCommand(async () => await ExecuteChatInputAsync());
            StartMainWindowGuideCommand = new AsyncRelayCommand(StartMainWindowGuideAsync);
            StartMappingTeachingPracticeCommand = new AsyncRelayCommand(StartMappingTeachingPracticeAsync);
            StartBgaTeachingPracticeCommand = new AsyncRelayCommand(StartBgaTeachingPracticeAsync);
            StartSettingsGuideCommand = new AsyncRelayCommand(StartSettingsGuideAsync);

            // ModelList가 비어 있으면 기본값 "Exaone-2.4B" 설정
            SelectedModel = ModelList.FirstOrDefault() ?? "Exaone-2.4B";

            // NewChatCommand 로직을 수정하여 확인 창을 띄우도록 변경
            NewChatCommand = new RelayCommand(() =>
            {
                // 확인 창을 띄우고 사용자의 응답을 받습니다.
                bool? result = new AlertWindow(
                    "새 채팅 시작",
                    "정말 채팅 기록을 삭제하시겠습니까?",
                    AlertWindow.EAlert.YESNO
                ).ShowDialog();

                // 사용자가 '예'를 선택했는지 확인합니다.
                if (result.GetValueOrDefault())
                {
                    // 응답이 '예'인 경우에만 ChatLog 컬렉션을 비웁니다.
                    ChatLog.Clear();
                    LogSystem("새 채팅이 시작되었습니다.");
                }
                else
                {
                    // 응답이 '아니오'이거나 창이 닫힌 경우
                    LogSystem("새 채팅 시작이 취소되었습니다.");
                }
            });
            // 1) ModelList 내용이 바뀔 때도 동기화하고 싶다면 구독
            ModelList.CollectionChanged += async (s, e) =>
            {
                // 선택이 목록에 없으면 가장 앞 항목으로 재설정
                if (!ModelList.Contains(SelectedModel))
                    SelectedModel = ModelList.FirstOrDefault() ?? SelectedModel;

                // 목록 변화 후 현재 선택 모델로 /switch 동기화
                await SendSwitchAsync();
            };
        }

        // 2) SelectedModel 필드에 대응하는 변경 훅 구현
        partial void OnSelectedModelChanged(string value)
        {
            // fire-and-forget로 실행(예외는 내부 처리)
            _ = SendSwitchAsync();
        }

        // 3) 실제 전송 로직(공용)
        private async Task SendSwitchAsync()
        {
            try
            {
                var full = SelectedModelFullName;
                if (string.IsNullOrWhiteSpace(full))
                    return;

                var resp = await _apiServer.SwitchModelAsync(full);
                if (resp.IsSuccessStatusCode)
                {
                    StatusText = $"모델 전환: {full}";
                    LogSystem($"모델 전환 완료: {full}");
                }
                else
                {
                    StatusText = $"모델 전환 실패: {(int)resp.StatusCode}";
                    LogSystem($"모델 전환 실패: {full} (HTTP {(int)resp.StatusCode})");
                }
            }
            catch (Exception ex)
            {
                StatusText = $"모델 전환 오류: {ex.Message}";
                LogSystem($"모델 전환 오류: {ex.Message}");
            }
        }



        // 사용자가 입력한 메시지를 로그에 추가하는 메서드
        private void LogUser(string message)
        {
            var chat = new ChatMessage
            {
                Sender = "User",
                Time = DateTime.Now.ToString("HH:mm"),
                Message = message
            };
            ChatLog.Add(chat);
            TrimChatLogIfNeeded();
        }

        // 시스템 메시지를 로그에 추가하는 메서드
        private void LogSystem(string message)
        {
            var chat = new ChatMessage
            {
                Sender = "System",
                Time = DateTime.Now.ToString("HH:mm"),
                Message = message
            };
            ChatLog.Add(chat);
            TrimChatLogIfNeeded();
        }

        //채팅 로그가 최대 개수(200)를 넘지 않도록 가장 오래된 로그를 삭제
        private void TrimChatLogIfNeeded()
        {
            if (ChatLog.Count > ChatLogMax)
            {
                ChatLog.RemoveAt(ChatLog.Count - 1);
            }
        }

        bool askBeforeOpenWindow_in = false;
        private bool _shouldAskUser = true;

        public void askBeforeOpenWindow(string? cmd)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                StatusText = "해당 명령을 실행할까요?";
                LogSystem($"해당 명령을 실행할까요?");
            }
            else
            {
                StatusText = $"{cmd}를 실행할까요?";
                LogSystem($"{cmd}를 실행할까요?");
            }
            askBeforeOpenWindow_in = true;
        }

        // SendChatCommand가 실행될 때 호출되는 핵심 로직
        public event Action RequestFocus;
        private async Task ExecuteChatInputAsync()
        {
            string raw = ChatInput ?? string.Empty;  // null이면 empty 시키기 
            string norm = Normalize(raw);
            //var cmd = MatchIntent(norm);

            //입력값이 없으면 종료
            if (string.IsNullOrWhiteSpace(norm))
            {
                StatusText = "명령을 입력하세요.";
                LogSystem("명령이 비어 있습니다.");
                return;
            }

            LogUser(raw);
            ChatInput = string.Empty;

            if (IsMappingTeachingAlias(norm))
            {
                await StartMappingTeachingPracticeAsync();
                return;
            }

            AddLoadingMessage();

            try
            {
                RequestFocus?.Invoke();
                IsLoading = true; //로딩표시 켬
                ChatInput = string.Empty;

                // ✅ NEW: 서버 보내기 전에 "Mode1 오버레이"를 먼저 시도한다.
                if (TryRouteTutorMode1(norm, out var anchorKey, out var durationMs) && !string.IsNullOrWhiteSpace(anchorKey))
                {
                    bool ok = await CallTutorMode1Async(anchorKey!, durationMs);

                    if (_loadingMessage != null)
                    {
                        _loadingMessage.Message = ok ? "실행완료" : "실행할 수 없습니다.";
                        _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                        _loadingMessage.IsLoading = false;
                    }

                    LogSystemMessage(ok ? "실행완료" : "실행할 수 없습니다.");

                    // ✅ 핵심: Mode1을 처리했으면 서버(_apiServer) 호출은 생략
                    return;
                }

                // 서버로 보내기
                var openedWindow = new Dictionary<string, string>();
                //var openedWindow = WindowManager.GetOpenWindowsWithTabsFilte red();
                HttpResponseMessage response;

                // 최근 채팅 메시지 확인
                var chatCount = ChatLog.Count;
                List<ChatMessage> messagesToSend;
                if (chatCount >= 2 &&
                   (ChatLog[chatCount - 2].Message.Contains("해당 명령을 실행할까요") ||
                    ChatLog[chatCount - 2].Message.Contains("바꾸시겠습니까?")))

                {
                    messagesToSend = new List<ChatMessage>
                    {
                        ChatLog[chatCount - 3], // 원래 유저 명령
                        ChatLog[chatCount - 2], //해당명령을 실행할까요
                        ChatLog[chatCount-1]   // yes이 든 no
                    };
                    response = await _apiServer.SendChatInputWithContextAsync(messagesToSend, openedWindow, SelectedModelFullName);
                }
                else
                {
                    response = await _apiServer.SendChatInputAsync(raw, openedWindow, SelectedModelFullName);
                }
                
                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (_loadingMessage != null)
                    {
                        _loadingMessage.Message = "실행완료";
                        _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                        _loadingMessage.IsLoading = false;                     // 도트 끄기
                    }
                    LogSystemMessage("실행완료");//상태바 텍스트
                }
                else if ((int)response.StatusCode == 500)
                {
                    if (_loadingMessage != null)
                    {
                        _loadingMessage.Message = "실행할 수 없습니다.";
                        _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                        _loadingMessage.IsLoading = false;
                    }

                    StatusText = "실행할 수 없습니다.";
                    LogSystem($"서버 에러로 실행 실패: ");
                }
                else
                {
                    if (_loadingMessage != null)
                    {
                        _loadingMessage.Message = $"예상치 못한 상태: {(int)response.StatusCode}";
                        _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                        _loadingMessage.IsLoading = false;
                    }

                    StatusText = $"예상치 못한 상태: {(int)response.StatusCode}";
                    LogSystem($"예상치 못한 상태 발생:  상태:{(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                if (_loadingMessage != null)
                {
                    _loadingMessage.Message = $"실행 중 오류: {ex.Message}";
                    _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                    _loadingMessage.IsLoading = false;
                }

                StatusText = $"실행 중 오류: {ex.Message}";
                LogSystem($"실행 오류: | {ex.Message}");
            }
            finally 
            {
                IsLoading = false;         // 처리 끝: 로딩 표시 끔
            }

            if (!string.IsNullOrEmpty(ChatInput))
            {
                RequestFocus?.Invoke();
                ChatInput = string.Empty;
            }
        }

        private async Task StartMappingTeachingPracticeAsync()
        {
            try
            {
                var mappingTutorToken = TutorGate.Arm("mapping");
                var started = MappingTeachingStartFlow.StartFromTraining(mappingTutorToken);
                if (!started)
                {
                    TutorGate.Disarm("mapping", "MainWindowNotFound");
                    StatusText = "Mapping Teaching 시작 실패: MainWindow를 찾을 수 없습니다.";
                    LogSystem("Mapping Teaching 시작 실패: MainWindow를 찾을 수 없습니다.");
                    return;
                }

                StatusText = "Mapping Teaching 시작 플로우를 시작했습니다. 사이드바 Mapping을 클릭하세요.";
                LogSystem("Mapping Teaching 시작 플로우를 시작했습니다. 사이드바 Mapping을 클릭하세요.");
            }
            catch (Exception ex)
            {
                StatusText = $"Mapping Teaching 시작 실패: {ex.Message}";
                LogSystem($"Mapping Teaching 시작 실패: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private async Task StartBgaTeachingPracticeAsync()
        {
            try
            {
                await WindowManager.OpenOrActivateAsync<GridBgaTeachingWindow>();
                StatusText = "BGA Teaching을 시작했습니다.";
                LogSystem("BGA Teaching을 시작했습니다.");
            }
            catch (Exception ex)
            {
                StatusText = $"BGA Teaching 시작 실패: {ex.Message}";
                LogSystem($"BGA Teaching 시작 실패: {ex.Message}");
            }
        }

        private async Task StartSettingsGuideAsync()
        {
            try
            {
                await WindowManager.OpenOrActivateAsync<SettingsWindow>();
                StatusText = "Settings Guide를 시작했습니다.";
                LogSystem("Settings Guide를 시작했습니다.");
            }
            catch (Exception ex)
            {
                StatusText = $"Settings Guide 시작 실패: {ex.Message}";
                LogSystem($"Settings Guide 시작 실패: {ex.Message}");
            }
        }
        // ✅ NEW: 채팅 입력을 임시로 Mode1(anchor_key)로 라우팅해보는 함수
        private bool TryRouteTutorMode1(string norm, out string? anchorKey, out int durationMs)
        {
            anchorKey = null;
            durationMs = 4500;

            if (string.IsNullOrWhiteSpace(norm))
                return false;

            // specific UI anchors first
            if (norm.Contains("타이틀바") || norm.Contains("타이틀") || norm.Contains("mode info") || norm.Contains("title bar"))
            {
                anchorKey = "MAIN_TITLE_BAR";
                return true;
            }

            if (norm.Contains("로고") || norm.Contains("logo"))
            {
                anchorKey = "MAIN_LOGO_BUTTON";
                return true;
            }

            if (norm.Contains("operation") || norm.Contains("setup/run") || norm.Contains("셋업런") ||
                norm.Contains("run") || norm.Contains("setup") || norm.Contains("오퍼레이션"))
            {
                anchorKey = "MAIN_OPERATION_BUTTON";
                return true;
            }

            if (norm.Contains("사이드바") || norm.Contains("메뉴") || norm.Contains("sidebar"))
            {
                anchorKey = "MAIN_SIDEBAR_ITEMS";
                return true;
            }

            if (norm.Contains("연결") || norm.Contains("커넥션") || norm.Contains("connected") || norm.Contains("disconnected"))
            {
                anchorKey = "MAIN_CONNECTION_BUTTON";
                return true;
            }

            if (norm.Contains("lot") || norm.Contains("레시피"))
            {
                anchorKey = "MAIN_LOT_INFO_PANEL";
                return true;
            }

            if (norm.Contains("통계") || norm.Contains("yield") || norm.Contains("reject") || norm.Contains("good") || norm.Contains("total"))
            {
                anchorKey = "MAIN_STATISTICS_PANEL";
                return true;
            }

            if (norm.Contains("시스템 정보") || norm.Contains("로그") || norm.Contains("system info"))
            {
                anchorKey = "MAIN_SYSTEM_INFORMATION_PANEL";
                return true;
            }

            if (norm.Contains("메인 프레임") || norm.Contains("화면") || norm.Contains("페이지") || norm.Contains("main frame"))
            {
                anchorKey = "MAIN_MAINFRAME";
                return true;
            }

            // walkthrough keywords fallback
            if (norm.Contains("전체 가이드") || norm.Contains("메인 화면 설명") || norm.Contains("main window walkthrough") ||
                norm.Contains("메인") || norm.Contains("main window"))
            {
                anchorKey = "MAIN_WINDOW_WALKTHROUGH";
                durationMs = 4500;
                return true;
            }

            // ---- 매우 단순한 키워드 기반(임시). 나중에 서버/KB 매칭으로 대체하면 됨.
            // RUN/SETUP 모드 전환 관련
            if (norm.Contains("run") || norm.Contains("setup") || norm.Contains("셋업") ||
                norm.Contains("런") || norm.Contains("모드 전환") || norm.Contains("전환"))
            {
                anchorKey = "RUN_SETUP_TOGGLE";
                return true;
            }

            // 티칭 메뉴 관련
            if (norm.Contains("티칭") || norm.Contains("teaching") || norm.Contains("teach"))
            {
                anchorKey = "MENU_TEACHING";
                return true;
            }

            return false;
        }

        private async Task StartMainWindowGuideAsync()
        {
            var seq = Interlocked.Increment(ref _mainGuideInvokeSeq);
            if (Interlocked.CompareExchange(ref _mainGuideInProgress, 1, 0) != 0)
            {
                Debug.WriteLine($"[Tutor/Chat] START_IGNORED_DUPLICATE seq={seq} t={DateTime.Now:HH:mm:ss.fff}");
                return;
            }

            AddLoadingMessage();

            try
            {
                var requestId = $"chat-main-walk-{seq}";
                Debug.WriteLine($"[Tutor/Chat] START_CLICK seq={seq} rid={requestId} t={DateTime.Now:HH:mm:ss.fff}\n{Environment.StackTrace}");
                bool ok = await CallTutorMode1Async("MAIN_WINDOW_WALKTHROUGH", 4500, requestId);

                if (_loadingMessage != null)
                {
                    _loadingMessage.Message = ok ? "실행완료" : "실행할 수 없습니다.";
                    _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                    _loadingMessage.IsLoading = false;
                }

                LogSystemMessage(ok ? "실행완료" : "실행할 수 없습니다.");
            }
            catch (Exception ex)
            {
                if (_loadingMessage != null)
                {
                    _loadingMessage.Message = $"실행 중 오류: {ex.Message}";
                    _loadingMessage.Time = DateTime.Now.ToString("HH:mm");
                    _loadingMessage.IsLoading = false;
                }

                LogSystem($"실행 오류: | {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _mainGuideInProgress, 0);
            }
        }

        // ✅ 내부 Tutor Mode1 경로 호출 (ApiController -> 기존 flow)
        private async Task<bool> CallTutorMode1Async(string anchorKey, int durationMs = 5000)
            => await CallTutorMode1Async(anchorKey, durationMs, "chat-mode1");

        private async Task<bool> CallTutorMode1Async(string anchorKey, int durationMs, string requestId)
        {
            try
            {
                var payload = new
                {
                    request_id = requestId,
                    anchor_key = anchorKey,
                    options = new { spotlight = true, duration_ms = durationMs }
                };

                string json = JsonSerializer.Serialize(payload);
                var result = await ApiController.Instance.HandleTutorMode1Async(json);

                if (!result.Success)
                {
                    LogSystem($"[Tutor] 실패: {result.Error ?? result.Message}");
                    return false;
                }

                if ($"{anchorKey}" == "MAIN_WINDOW_WALKTHROUGH")
                    LogSystem($"Enter/Space to next step, Esc to interrupt");
                //LogSystem($"[Tutor] spotlight: {anchorKey}");
                return true;
            }
            catch (Exception ex)
            {
                LogSystem($"[Tutor] 오류: {ex.Message}");
                return false;
            }
        }


        private static string Normalize(string s)
        {
            s = s.Trim().ToLowerInvariant();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s;
        }

        private static bool IsMappingTeachingAlias(string norm)
        {
            if (string.IsNullOrWhiteSpace(norm)) return false;

            return norm == "mapping"
                || norm.Contains("mapping 티칭")
                || norm.Contains("매핑 티칭");
        }

        //새 채팅 기능
        public void ClearChat ()
        {
            ChatLog.Clear();
        }

        // ApiController에서 호출할 수 있도록 public 메서드 추가
        public void LogSystemMessage(string message)
        {
            // 상태바만 갱신
            StatusText = message;
            Debug.WriteLine("chatWindowviewModel_message " + message);
            RequestFocus?.Invoke();
            ChatInput = string.Empty;
        }

    }
}
