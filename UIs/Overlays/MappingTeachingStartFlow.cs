using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GVisionWpf.UIs.Frames.Windows.Teaching;

namespace GVisionWpf.UIs.Overlays
{
    public static class MappingTeachingStartFlow
    {
        public enum FileSelectionSignal
        {
            None,
            Top6Selected,
            CompletedWithoutTop6,
            Canceled,
            Failed,
        }

        private static readonly object Sync = new();

        private static int _runSequence;
        private static TutorFlowRunner? _activeRunner;
        private static string? _activeRunId;
        private static Window? _activeHost;
        private static Guid? _activeMappingTutorToken;

        private static bool _awaitSidebarMappingClick;
        private static bool _openRequestIssued;
        private static FileSelectionSignal _fileSelectionSignal = FileSelectionSignal.None;

        public static bool StartFromTraining(Guid mappingTutorToken)
        {
            var host = ResolveMainWindowHost();
            if (host == null)
            {
                Debug.WriteLine("[TutorFlow] runId=- stepIndex=- stepKey=- phase=Stop reason=MainWindowNotFound clickedAutomationId=-");
                return false;
            }

            StopActive("Restart");

            var runId = $"mapping-start-{host.GetHashCode()}-{Interlocked.Increment(ref _runSequence)}";

            lock (Sync)
            {
                _activeRunId = runId;
                _activeHost = host;
                _activeMappingTutorToken = mappingTutorToken;
                _awaitSidebarMappingClick = true;
                _openRequestIssued = false;
                _fileSelectionSignal = FileSelectionSignal.None;
            }

            var steps = new List<TutorStep>
            {
                new WaitClickStep(
                    key: "step2-click-mapping",
                    title: "사이드바 Mapping 클릭",
                    body:
                        "사이드바에서 Mapping 버튼을 클릭하세요.\n" +
                        "파일 탐색기가 열리면 top6.png를 선택하세요.",
                    anchorAutomationId: "MENU_MAPPING",
                    clickAutomationIds: ["MENU_MAPPING", "MAPPING"],
                    allowSkip: false,
                    consumeMatchingClick: false,
                    useSpotlight: true,
                    transitionDelayMs: 16),

                new WaitConditionStep(
                    key: "step5-wait-top6",
                    title: "top6 선택 대기",
                    body: "Mapping(앞면) 검사 예제로 top6.png하세요.",
                    anchorAutomationId: "MAIN_MAINFRAME",
                    condition: _ => IsTop6SelectedOrStop(runId),
                    pollIntervalMs: 100,
                    useSpotlight: false)
            };

            var runner = new TutorFlowRunner(host, steps, stopReason => OnRunnerStopped(runId, stopReason), runId);

            lock (Sync)
            {
                _activeRunner = runner;
            }

            Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=- stepKey=- phase=Start reason=MappingTeachingStartFlow token={mappingTutorToken} clickedAutomationId=-");
            TutorFlowStartHub.Start(runner, runId, "MappingTeachingStartFlow");
            return true;
        }

        private static Window? ResolveMainWindowHost()
        {
            var app = Application.Current;
            if (app == null) return null;

            var mainWindowInstance = app.Windows
                .OfType<Window>()
                .FirstOrDefault(w => string.Equals(w.GetType().Name, "MainWindow", StringComparison.Ordinal));

            if (mainWindowInstance != null) return mainWindowInstance;

            if (app.MainWindow != null &&
                string.Equals(app.MainWindow.GetType().Name, "MainWindow", StringComparison.Ordinal))
            {
                return app.MainWindow;
            }

            return null;
        }

        public static bool TryHandleSidebarMappingClick()
        {
            lock (Sync)
            {
                if (_activeRunner == null || !_awaitSidebarMappingClick) return false;
                _awaitSidebarMappingClick = false;
                return true;
            }
        }

        public static bool TryGetActiveMappingTutorToken(out Guid token)
        {
            lock (Sync)
            {
                if (_activeRunner != null && _activeMappingTutorToken.HasValue)
                {
                    token = _activeMappingTutorToken.Value;
                    return true;
                }
            }

            token = Guid.Empty;
            return false;
        }

        public static void NotifyFileSelectionSignal(FileSelectionSignal signal, int top6ShotIndex, IReadOnlyList<string> selectedFiles)
        {
            var files = string.Join(", ", selectedFiles.Select(Path.GetFileName));
            var runId = "-";

            lock (Sync)
            {
                if (_activeRunId != null)
                {
                    _fileSelectionSignal = signal;
                    runId = _activeRunId;
                }
            }

            Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=5 stepKey=step5-wait-top6 phase=Signal reason={signal},top6ShotIndex={top6ShotIndex},files=[{files}] clickedAutomationId=-");
        }

        public static void StopActive(string reason)
        {
            TutorFlowRunner? runner;
            string runId;

            lock (Sync)
            {
                runner = _activeRunner;
                runId = _activeRunId ?? "-";
                _activeRunner = null;
                _activeRunId = null;
                _activeHost = null;
                _activeMappingTutorToken = null;
                _awaitSidebarMappingClick = false;
                _openRequestIssued = false;
                _fileSelectionSignal = FileSelectionSignal.None;
            }

            Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=- stepKey=- phase=Stop reason={reason} clickedAutomationId=-");
            runner?.Stop(reason);
            runner?.Dispose();
        }

        private static async Task TriggerOpenMapTeachingWindowAsync(string runId, CancellationToken token)
        {
            lock (Sync)
            {
                if (_activeRunId != runId || _openRequestIssued)
                {
                    Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=4 stepKey=step4-open-map-dialog phase=Complete reason=OpenRequestIgnored clickedAutomationId=-");
                    return;
                }

                _openRequestIssued = true;
                _fileSelectionSignal = FileSelectionSignal.None;
            }

            token.ThrowIfCancellationRequested();
            Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=4 stepKey=step4-open-map-dialog phase=Start reason=CallOpenMapTeachingWindow clickedAutomationId=-");

            await WindowManager.OpenOrActivateAsync<GridMoldTeachingWindow>();
        }

        private static bool IsTop6SelectedOrStop(string runId)
        {
            FileSelectionSignal signal;
            lock (Sync)
            {
                if (_activeRunId != runId) return false;
                signal = _fileSelectionSignal;
            }

            if (signal == FileSelectionSignal.Top6Selected)
            {
                Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=5 stepKey=step5-wait-top6 phase=Complete reason=Top6Selected clickedAutomationId=-");
                return true;
            }

            if (signal == FileSelectionSignal.Canceled)
            {
                StopActive("ImageSelectionCanceled");
                return false;
            }

            if (signal == FileSelectionSignal.CompletedWithoutTop6)
            {
                StopActive("Top6NotSelected");
                return false;
            }

            if (signal == FileSelectionSignal.Failed)
            {
                StopActive("ImageSelectionFailed");
                return false;
            }

            return false;
        }

        private static void OnRunnerStopped(string runId, string reason)
        {
            lock (Sync)
            {
                if (_activeRunId != null && _activeRunId.Equals(runId, StringComparison.Ordinal))
                {
                    _activeRunner = null;
                    _activeRunId = null;
                    _activeHost = null;
                    _activeMappingTutorToken = null;
                    _awaitSidebarMappingClick = false;
                    _openRequestIssued = false;
                    _fileSelectionSignal = FileSelectionSignal.None;
                }
            }

            Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=- stepKey=- phase=Stop reason={reason} clickedAutomationId=-");
        }
    }
}
