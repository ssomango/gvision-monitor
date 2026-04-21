using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Windows.Controls;


namespace GVisionWpf.UIs.Overlays
{
    public sealed class TutorFlowRunner : IDisposable
    {
        private readonly Window _hostWindow;
        private readonly IReadOnlyList<TutorStep> _steps;
        private readonly Dictionary<string, int> _stepIndexByKey;
        private readonly Action<string> _onStopped;
        private readonly CancellationTokenSource _cts = new();
        private readonly string _runId;

        private bool _isRunning;
        private bool _isDisposed;
        private int _currentStepIndex = -1;
        private string _currentStepKey = "-";
        private int _stepLatch;

        private PreProcessInputEventHandler? _preProcessInputHandler;


        public TutorFlowRunner(
            Window hostWindow,
            IReadOnlyList<TutorStep> steps,
            Action<string> onStopped,
            string runId)
        {
            _hostWindow = hostWindow ?? throw new ArgumentNullException(nameof(hostWindow));
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _onStopped = onStopped ?? throw new ArgumentNullException(nameof(onStopped));
            _runId = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N") : runId;
            _stepIndexByKey = BuildStepIndexMap(_steps);
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            HookGlobalEsc(); // ✅ 추가 (전역 ESC)
            _hostWindow.PreviewKeyDown += OnHostPreviewKeyDown; // (옵션) 남겨도 되고 제거해도 됨

            Log("Start", reason: $"stepCount={_steps.Count}");
            _ = RunAsync();
        }
        public void Stop(string reason)
        {
            if (_cts.IsCancellationRequested) return;
            Log("Stop", reason: reason);

            UnhookGlobalEsc();
            _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;

            _cts.Cancel();
            TutorSpotlightOverlay.Hide($"TutorFlowStop:{reason}");
        }


 


        private async Task RunAsync()
        {
            var stopReason = "Completed";

            try
            {
                var i = 0;
                while (i < _steps.Count)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var step = _steps[i];
                    _currentStepIndex = i;
                    _currentStepKey = step.Key;
                    Interlocked.Exchange(ref _stepLatch, 0);

                    Log("Enter", i, step.Key, reason: step.GetType().Name);
                    var executionResult = await ExecuteStepAsync(i, step, _cts.Token);
                    Log("Complete", i, step.Key, reason: "StepCompleted");

                    // One transition per dispatcher frame to avoid chained completion on same UI action.
                    await _hostWindow.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render, _cts.Token);
                    if (step.TransitionDelayMs > 0)
                    {
                        await Task.Delay(step.TransitionDelayMs, _cts.Token);
                    }

                    if (executionResult.HasJumpTarget)
                    {
                        i = ResolveStepIndex(executionResult.TargetStepKey!);
                        Log("Exit", i, step.Key, reason: $"JumpTo:{executionResult.TargetStepKey}");
                        continue;
                    }

                    Log("Exit", i, step.Key, reason: "Transitioned");
                    i++;
                }
            }
            catch (OperationCanceledException)
            {
                stopReason = "Canceled";
                Log("Stop", _currentStepIndex, _currentStepKey, reason: "OperationCanceled");
            }
            catch (Exception ex)
            {
                stopReason = "Faulted";
                Log("Stop", _currentStepIndex, _currentStepKey, reason: ex.Message);
            }
            finally
            {
                UnhookGlobalEsc();
                _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
                TutorSpotlightOverlay.Hide("TutorFlowFinally");
                _onStopped(stopReason);
            }
        }

        private async Task<StepExecutionResult> ExecuteStepAsync(int index, TutorStep step, CancellationToken token)
        {
            var anchor = await ResolveAnchorAsync(index, step, token);

            Debug.WriteLine($"[TutorFlow] Show step={step.Key} titleLen={step.Title?.Length} bodyLen={step.Body?.Length} body=[{step.Body}] img={step.CardImageUri}");

            switch (step)
            {
                case InfoStep info:
                    await ShowStepOverlayAndWaitNextAsync(index, info, anchor, token);
                    return StepExecutionResult.Continue;
                case WaitClickStep click:
                    await ShowStepOverlayAsync(index, click, anchor, token);
                    await WaitForClickAsync(index, click, token);
                    return StepExecutionResult.Continue;
                case WaitConditionStep condition:
                    await ShowStepOverlayAsync(index, condition, anchor, token);
                    await WaitUntilAsync(index, condition.Key, condition.Condition, condition.PollIntervalMs, token);
                    return StepExecutionResult.Continue;
                case WaitValueStep value:
                    await ShowStepOverlayAsync(index, value, anchor, token);
                    await WaitUntilAsync(index, value.Key, value.IsChanged, value.PollIntervalMs, token);
                    return StepExecutionResult.Continue;
                case ActionStep action:
                    await action.Action(_hostWindow, token);
                    return StepExecutionResult.Continue;
                case BranchStep branch:
                    return await branch.Branch(_hostWindow, token);
                default:
                    throw new InvalidOperationException($"Unknown step type: {step.GetType().Name}");
            }
        }

        private async Task ShowStepOverlayAndWaitNextAsync(int index, TutorStep step, FrameworkElement anchor, CancellationToken token)
        {
            await ShowStepOverlayAsync(index, step, anchor, token, allowManualAdvanceInput: true, overlayHitTestVisible: true);
            await WaitForOverlayNextAsync(index, step.Key, token);
        }

        private async Task ShowStepOverlayAsync(
            int index,
            TutorStep step,
            FrameworkElement anchor,
            CancellationToken token,
            bool allowManualAdvanceInput = false,
            bool overlayHitTestVisible = false)
        {
            var bodyText = step.Body ?? string.Empty;
            var bodySnippet = bodyText.Length > 120 ? $"{bodyText[..120]}..." : bodyText;
            var effectiveOverlayHitTestVisible = overlayHitTestVisible;
            var nonBlockingCardOnly = false;
            if (string.Equals(step.Key, "step-c3-gridroi-adjust", StringComparison.Ordinal) ||
                string.Equals(step.Key, "step-c3-gridroi-adjust-and-auto", StringComparison.Ordinal) ||
                string.Equals(step.Key, "retry-r2-gridroi-adjust", StringComparison.Ordinal))
            {
                // step-c3 uses a non-blocking card-only popup so ROI drag/zoom still reaches the vision UI
                // while Enter/Space advances through the overlay key hook.
                effectiveOverlayHitTestVisible = false;
                nonBlockingCardOnly = false;
            }
            Log(
                "OverlayPayload",
                index,
                step.Key,
                reason: $"titleLen={step.Title?.Length ?? 0},bodyLen={bodyText.Length},overlayHitTestVisible={effectiveOverlayHitTestVisible},nonBlockingCardOnly={nonBlockingCardOnly},bodySnippet={bodySnippet.Replace('\n', ' ')}");

            await _hostWindow.Dispatcher.InvokeAsync(() =>
            {
                var target = step.UseSpotlight ? anchor : (FrameworkElement)_hostWindow;
                var dimOpacity = step.UseSpotlight ? 0.62 : 0.0;
                FrameworkElement? cardTarget = null;
                if (!string.IsNullOrWhiteSpace(step.CardAnchorAutomationId))
                {
                    cardTarget = FindByAutomationId(_hostWindow, step.CardAnchorAutomationId!);
                    Log(
                        "CardAnchor",
                        index,
                        step.Key,
                        cardTarget != null ? AutomationProperties.GetAutomationId(cardTarget) : "-",
                        reason: cardTarget != null ? "Resolved" : $"NotFound:{step.CardAnchorAutomationId}");
                }

                TutorSpotlightOverlay.Show(
                    hostWindow: _hostWindow,
                    target: target,
                    title: $"Step {index + 1}: {step.Title}",
                    body: step.Body,
                    cardTarget: cardTarget,
                    durationMs: 0,
                    manualAdvance: allowManualAdvanceInput,
                    allowManualAdvanceInput: allowManualAdvanceInput,
                    overlayHitTestVisible: effectiveOverlayHitTestVisible,
                    cardImageUri: step.CardImageUri,
                    dimOpacity: dimOpacity,
                    debugContext: new TutorSpotlightOverlay.DebugContext(_runId, index, step.Key, "TutorFlow"),
                    nonBlockingCardOnly: nonBlockingCardOnly);
            }, DispatcherPriority.Render, token);
        }

        private async Task<FrameworkElement> ResolveAnchorAsync(int stepIndex, TutorStep step, CancellationToken token)
        {
            const int timeoutMs = 2000;
            const int pollMs = 50;
            var started = DateTime.UtcNow;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var resolved = await _hostWindow.Dispatcher.InvokeAsync(() =>
                {
                    var byAnchor = FindByAutomationId(_hostWindow, step.AnchorAutomationId);
                    if (byAnchor != null) return byAnchor;

                    if (step is WaitClickStep clickStep)
                    {
                        foreach (var id in clickStep.ClickAutomationIds)
                        {
                            var byClickId = FindByAutomationId(_hostWindow, id);
                            if (byClickId != null) return byClickId;
                        }
                    }

                    return null;
                }, DispatcherPriority.Loaded, token);

                if (resolved != null)
                {
                    Log("AnchorResolved", stepIndex, step.Key, AutomationProperties.GetAutomationId(resolved), reason: "ResolvedWithRetry");
                    return resolved;
                }

                if ((DateTime.UtcNow - started).TotalMilliseconds >= timeoutMs)
                {
                    Log("AnchorFallback", stepIndex, step.Key, reason: $"AnchorNotFound timeoutMs={timeoutMs} anchor={step.AnchorAutomationId}");
                    return _hostWindow;
                }

                await Task.Delay(pollMs, token);
            }
        }

        private async Task WaitUntilAsync(
            int stepIndex,
            string stepKey,
            Func<Window, bool> predicate,
            int pollMs,
            CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var satisfied = await _hostWindow.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        return predicate(_hostWindow);
                    }
                    catch (Exception ex)
                    {
                        Log("ConditionError", stepIndex, stepKey, reason: ex.Message);
                        return false;
                    }
                }, DispatcherPriority.Background, token);

                if (satisfied)
                {
                    TryLatchStepCompletion(stepIndex, stepKey, "ConditionSatisfied");
                    return;
                }

                await Task.Delay(pollMs, token);
            }
        }

        private Task WaitForOverlayNextAsync(int stepIndex, string stepKey, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            Log("WaitOverlayNext", stepIndex, stepKey, reason: "Subscribed");            

            void Cleanup()
            {
                TutorSpotlightOverlay.NextRequested -= OnNext;
                Log("WaitOverlayNext", stepIndex, stepKey, reason: "Unsubscribed");
            }

            void OnNext()
            {
                Log("OverlayNextReceived", stepIndex, stepKey, reason: "NextRequested");

                if (!TryLatchStepCompletion(stepIndex, stepKey, "OverlayNext"))
                {
                    Log("CompleteIgnored", stepIndex, stepKey, reason: "ignored due to latch");
                    return;
                }

                Cleanup();
                tcs.TrySetResult(null);
            }

            TutorSpotlightOverlay.NextRequested += OnNext;

            token.Register(() =>
            {
                Log("WaitOverlayNextCanceled", stepIndex, stepKey, reason: "TokenCanceled");
                Cleanup();
                tcs.TrySetCanceled(token);
            });

            return tcs.Task;
        }

        private Task WaitForClickAsync(int stepIndex, WaitClickStep step, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var wantedSet = new HashSet<string>(step.ClickAutomationIds, StringComparer.OrdinalIgnoreCase);
            var clickLatch = 0;

            RoutedEventHandler? onAnyButtonClick = null;
            onAnyButtonClick = (_, e) =>
            {
                if (token.IsCancellationRequested) return;
                if (stepIndex != _currentStepIndex) return;

                var clickedId = e.OriginalSource is DependencyObject src
                    ? FindAutomationIdFromAncestors(src)
                    : null;

                Log("Click", stepIndex, step.Key, clickedId, reason: "Detected");

                if (string.IsNullOrWhiteSpace(clickedId) || !wantedSet.Contains(clickedId))
                {
                    return;
                }

                Log(
                    "ClickMatch",
                    stepIndex,
                    step.Key,
                    clickedId,
                    reason: $"consume={step.ConsumeMatchingClick},handledBefore={e.Handled}");

                if (Interlocked.Exchange(ref clickLatch, 1) == 1)
                {
                    Log("ClickIgnored", stepIndex, step.Key, clickedId, reason: "ignored due to latch");
                    return;
                }

                if (!TryLatchStepCompletion(stepIndex, step.Key, $"Click:{clickedId}"))
                {
                    Log("CompleteIgnored", stepIndex, step.Key, clickedId, reason: "ignored due to latch");
                    return;
                }

                if (step.ConsumeMatchingClick)
                {
                    e.Handled = true;
                }

                Log(
                    "ClickHandled",
                    stepIndex,
                    step.Key,
                    clickedId,
                    reason: $"consume={step.ConsumeMatchingClick},handledAfter={e.Handled}");

                Cleanup();
                tcs.TrySetResult(null);
            };

            void Cleanup()
            {
                _hostWindow.RemoveHandler(ButtonBase.ClickEvent, onAnyButtonClick);
                _hostWindow.RemoveHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent, onAnyButtonClick);
            }

            _hostWindow.AddHandler(ButtonBase.ClickEvent, onAnyButtonClick, true);
            // RadioButton(=ToggleButton)의 체크 이벤트도 
            _hostWindow.AddHandler(
                System.Windows.Controls.Primitives.ToggleButton.CheckedEvent,
                onAnyButtonClick,
                true);

            token.Register(() =>
            {
                Cleanup();
                tcs.TrySetCanceled(token);
            });

            return tcs.Task;
        }

        private bool TryLatchStepCompletion(int stepIndex, string stepKey, string reason)
        {
            if (_currentStepIndex != stepIndex)
            {
                Log("CompleteIgnored", stepIndex, stepKey, reason: $"StaleStep reason={reason}");
                return false;
            }

            if (Interlocked.Exchange(ref _stepLatch, 1) == 1)
            {
                return false;
            }

            Log("StepTransition", stepIndex, stepKey, reason: reason);
            return true;
        }

        private static string? FindAutomationIdFromAncestors(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (TryGetAutomationId(current, out var id))
                {
                    return id;
                }

                current = GetParentObject(current);
            }

            return null;
        }

        private static bool TryGetAutomationId(DependencyObject current, out string automationId)
        {
            automationId = string.Empty;
            if (current is FrameworkElement fe)
            {
                automationId = AutomationProperties.GetAutomationId(fe);
                if (!string.IsNullOrWhiteSpace(automationId))
                {
                    return true;
                }
            }

            if (current is FrameworkContentElement fce)
            {
                automationId = AutomationProperties.GetAutomationId(fce);
                if (!string.IsNullOrWhiteSpace(automationId))
                {
                    return true;
                }
            }

            return false;
        }

        private static DependencyObject? GetParentObject(DependencyObject child)
        {
            if (child is Visual || child is Visual3D)
            {
                var visualParent = VisualTreeHelper.GetParent(child);
                if (visualParent != null)
                {
                    return visualParent;
                }
            }

            if (child is FrameworkElement fe)
            {
                if (fe.Parent != null) return fe.Parent;
                return LogicalTreeHelper.GetParent(fe);
            }

            if (child is FrameworkContentElement fce)
            {
                if (fce.Parent != null) return fce.Parent;
                return LogicalTreeHelper.GetParent(fce);
            }

            return LogicalTreeHelper.GetParent(child);
        }

        private static FrameworkElement? FindByAutomationId(DependencyObject root, string automationId)
        {
            if (root == null || string.IsNullOrWhiteSpace(automationId)) return null;

            // DFS stack (non-recursive)
            var stack = new Stack<DependencyObject>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var cur = stack.Pop();

                // 1) 현재 노드 체크 (FrameworkElement/FrameworkContentElement 모두)
                if (cur is FrameworkElement fe)
                {
                    var id = AutomationProperties.GetAutomationId(fe);
                    if (string.Equals(id, automationId, StringComparison.OrdinalIgnoreCase))
                        return fe;
                }
                else if (cur is FrameworkContentElement fce)
                {
                    var id = AutomationProperties.GetAutomationId(fce);
                    if (string.Equals(id, automationId, StringComparison.OrdinalIgnoreCase))
                    {
                        // FCE는 FrameworkElement가 아니라 spotlight 타겟으로 직접 쓰기 애매함.
                        // 가장 가까운 FrameworkElement 부모를 찾거나 null 처리 전략 필요.
                        // 여기선 "부모 FrameworkElement 찾기"로 처리하는 것을 권장.
                        var parentFe = FindNearestFrameworkElementParent(fce);
                        if (parentFe != null) return parentFe;
                    }
                }

                // 2) Visual children (Visual/Visual3D일 때만)  ✅ 여기서 100% 안전
                if (cur is Visual || cur is Visual3D)
                {
                    var vCount = VisualTreeHelper.GetChildrenCount(cur);
                    for (int i = 0; i < vCount; i++)
                    {
                        var child = VisualTreeHelper.GetChild(cur, i);
                        if (child != null) stack.Push(child);
                    }
                }

                // 3) (선택) Logical children은 정말 필요할 때만, 그리고 FE/FCE만
                //    대부분은 꺼도 됩니다. 켜려면 아래 주석 해제.
                /*
                foreach (var child in LogicalTreeHelper.GetChildren(cur))
                {
                    if (child is FrameworkElement || child is FrameworkContentElement)
                    {
                        stack.Push((DependencyObject)child);
                    }
                }
                */
            }

            return null;
        }

        private static FrameworkElement? FindNearestFrameworkElementParent(DependencyObject child)
        {
            var cur = child;
            while (cur != null)
            {
                if (cur is FrameworkElement fe) return fe;
                cur = GetParentObject(cur);
            }
            return null;
        }


        private void OnHostPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;

            e.Handled = true;
            Stop("Esc");
        }

        private void HookGlobalEsc()
        {
            if (_preProcessInputHandler != null) return;

            _preProcessInputHandler = (s, e) =>
            {
                if (!_isRunning) return;
                if (_cts.IsCancellationRequested) return;

                if (e.StagingItem.Input is KeyEventArgs ke &&
                    ke.RoutedEvent == Keyboard.PreviewKeyDownEvent &&
                    ke.Key == Key.Escape)
                {
                    ke.Handled = true; // ESC로 앱이 다른 동작 하는 거 방지(원하면 제거 가능)
                    Stop("Esc");
                }
            };

            InputManager.Current.PreProcessInput += _preProcessInputHandler;
        }

        private void UnhookGlobalEsc()
        {
            if (_preProcessInputHandler == null) return;
            InputManager.Current.PreProcessInput -= _preProcessInputHandler;
            _preProcessInputHandler = null;
        }

        private void Log(string phase, int? stepIndex = null, string? stepKey = null, string? clickedAutomationId = null, string? reason = null)
        {
            Debug.WriteLine(
                $"[TutorFlow] runId={_runId} stepIndex={stepIndex?.ToString() ?? _currentStepIndex.ToString()} " +
                $"stepKey={stepKey ?? _currentStepKey} phase={phase} reason={reason ?? "-"} clickedAutomationId={clickedAutomationId ?? "-"}");
        }

        private static Dictionary<string, int> BuildStepIndexMap(IReadOnlyList<TutorStep> steps)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var index = 0; index < steps.Count; index++)
            {
                var key = steps[index].Key;
                if (map.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate tutor step key: {key}");
                }

                map[key] = index;
            }

            return map;
        }

        private int ResolveStepIndex(string stepKey)
        {
            if (_stepIndexByKey.TryGetValue(stepKey, out var index))
            {
                return index;
            }

            throw new InvalidOperationException($"Tutor step key not found: {stepKey}");
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Stop("Dispose");
            _cts.Dispose();
        }
    }
}
