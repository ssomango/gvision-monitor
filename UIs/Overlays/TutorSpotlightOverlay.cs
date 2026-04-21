using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;
using WpfSize = System.Windows.Size;
using System.Diagnostics;

namespace GVisionWpf.UIs.Overlays
{
    /// <summary>
    /// 딤(전체 어둡게) + 구멍(스포트라이트) + 설명 카드
    /// Popup 기반 (XAML 수정 없이 오버레이 표시)
    /// </summary>
    public static class TutorSpotlightOverlay
    {
        public readonly struct DebugContext
        {
            public DebugContext(string? requestId, int? stepIndex, string? stepAnchorKey, string? source = null)
            {
                RequestId = requestId;
                StepIndex = stepIndex;
                StepAnchorKey = stepAnchorKey;
                Source = source;
            }

            public string? RequestId { get; }
            public int? StepIndex { get; }
            public string? StepAnchorKey { get; }
            public string? Source { get; }
        }

        private static Popup? _popup;
        private static DispatcherTimer? _closeTimer;

        private static Window? _hostWindow;
        private static FrameworkElement? _surface;

        private static EventHandler? _onLocationChanged;
        private static SizeChangedEventHandler? _onSizeChanged;
        private static EventHandler? _onLayoutUpdated;
        private static PreProcessInputEventHandler? _onPreProcessInput;

        private static bool _updatePending;
        private static bool _cardPlaceBelow = true;
        private static int _overlaySeq;
        private static int _overlayId;
        private static DebugContext? _currentDebugContext;

        public static event Action? NextRequested;
        public static event Action? CancelRequested;

        public static int ActiveOverlayId => Volatile.Read(ref _overlayId);

        public static void Hide(string reason = "Unspecified")
        {
            var disp = Application.Current?.Dispatcher;
            if (disp != null && !disp.CheckAccess())
            {
                disp.Invoke(() => Hide(reason));
                return;
            }

            var overlayId = _overlayId;
            var debugContext = _currentDebugContext;
            Debug.WriteLine($"[TutorOverlay] HIDE overlayId={overlayId} reason={reason} rid={Fmt(debugContext?.RequestId)} stepIndex={Fmt(debugContext?.StepIndex)} stepAnchorKey={Fmt(debugContext?.StepAnchorKey)} source={Fmt(debugContext?.Source)} phase=HIDE tid={Environment.CurrentManagedThreadId} ts={Ts()}");

            _closeTimer?.Stop();
            _closeTimer = null;

            if (_onPreProcessInput != null)
            {
                InputManager.Current.PreProcessInput -= _onPreProcessInput;
                _onPreProcessInput = null;
            }

            if (_hostWindow != null)
            {
                if (_onLocationChanged != null) _hostWindow.LocationChanged -= _onLocationChanged;
                if (_onSizeChanged != null) _hostWindow.SizeChanged -= _onSizeChanged;
            }

            if (_surface != null && _onLayoutUpdated != null)
                _surface.LayoutUpdated -= _onLayoutUpdated;

            _onLocationChanged = null;
            _onSizeChanged = null;
            _onLayoutUpdated = null;

            _hostWindow = null;
            _surface = null;

            if (_popup != null)
            {
                _popup.IsOpen = false;
                _popup.Child = null;
                _popup = null;
            }

            _updatePending = false;
            _overlayId = 0;
            _currentDebugContext = null;
        }

        public static void Show(
            Window hostWindow,
            FrameworkElement target,
            string? title,
            string? body,
            FrameworkElement? cardTarget = null,
            int durationMs = 5000,
            double padding = 2,
            double dimOpacity = 0.62,
            bool manualAdvance = false,
            bool allowManualAdvanceInput = true,
            bool overlayHitTestVisible = true,
            string? cardImageUri = null,
            DebugContext? debugContext = null,
            int? overlayDebugId = null,
            bool nonBlockingCardOnly = false)   // ✅ walkthrough 모드용
        {

            if (hostWindow == null || target == null) return;
            var candidateOverlayId = overlayDebugId ?? Interlocked.Increment(ref _overlaySeq);

            if (!hostWindow.Dispatcher.CheckAccess())
            {
                Debug.WriteLine($"[TutorOverlay] SHOW_DEFER overlayId={candidateOverlayId} rid={Fmt(debugContext?.RequestId)} stepIndex={Fmt(debugContext?.StepIndex)} stepAnchorKey={Fmt(debugContext?.StepAnchorKey)} source={Fmt(debugContext?.Source)} phase=DEFER tid={Environment.CurrentManagedThreadId} ts={Ts()} reason=Dispatcher target={target.GetType().Name}");
                hostWindow.Dispatcher.Invoke(() =>
                    Show(hostWindow, target, title, body, cardTarget, durationMs, padding, dimOpacity, manualAdvance, allowManualAdvanceInput, overlayHitTestVisible, cardImageUri, debugContext, candidateOverlayId, nonBlockingCardOnly));
                return;
            }

            if (!target.IsLoaded)
            {
                Debug.WriteLine($"[TutorOverlay] SHOW_DEFER overlayId={candidateOverlayId} rid={Fmt(debugContext?.RequestId)} stepIndex={Fmt(debugContext?.StepIndex)} stepAnchorKey={Fmt(debugContext?.StepAnchorKey)} source={Fmt(debugContext?.Source)} phase=DEFER tid={Environment.CurrentManagedThreadId} ts={Ts()} reason=WaitLoaded target={target.GetType().Name}");
                RoutedEventHandler? loaded = null;
                loaded = (_, __) =>
                {
                    target.Loaded -= loaded!;
                    Show(hostWindow, target, title, body, cardTarget, durationMs, padding, dimOpacity, manualAdvance, allowManualAdvanceInput, overlayHitTestVisible, cardImageUri, debugContext, candidateOverlayId, nonBlockingCardOnly);
                };
                target.Loaded += loaded;
                return;
            }
            Debug.WriteLine($"[TutorOverlay] SHOW_ENTER overlayId={candidateOverlayId} rid={Fmt(debugContext?.RequestId)} stepIndex={Fmt(debugContext?.StepIndex)} stepAnchorKey={Fmt(debugContext?.StepAnchorKey)} source={Fmt(debugContext?.Source)} phase=SHOW tid={Environment.CurrentManagedThreadId} ts={Ts()} host={hostWindow.GetType().Name} target={target.GetType().Name} aw={target.ActualWidth:F1} ah={target.ActualHeight:F1}\n{Environment.StackTrace}");
            var replaceReason = _overlayId > 0 ? "NextStepStart" : "ShowReplace";
            Hide(replaceReason);
            _overlayId = candidateOverlayId;
            _currentDebugContext = debugContext;

            var surface = ResolveOverlaySurface(hostWindow);
            Debug.WriteLine($"[TutorOverlay] SURFACE overlayId={candidateOverlayId} rid={Fmt(debugContext?.RequestId)} stepIndex={Fmt(debugContext?.StepIndex)} stepAnchorKey={Fmt(debugContext?.StepAnchorKey)} source={Fmt(debugContext?.Source)} phase=SHOW tid={Environment.CurrentManagedThreadId} ts={Ts()} surface={surface.GetType().Name} sw={surface.ActualWidth:F1} sh={surface.ActualHeight:F1} hostW={hostWindow.ActualWidth:F1} hostH={hostWindow.ActualHeight:F1}");

            _hostWindow = hostWindow;
            _surface = surface;

            var card = BuildCard(title, body, cardImageUri);
            Debug.WriteLine($"[TutorOverlay] CARD_BIND overlayId={_overlayId} titleLen={(title?.Length ?? 0)} bodyLen={(body?.Length ?? 0)} imageUri={Fmt(cardImageUri)} mode={(nonBlockingCardOnly ? "CardOnly" : "FullOverlay")}");

            if (nonBlockingCardOnly)
            {
                _popup = new Popup
                {
                    PlacementTarget = surface,
                    Placement = PlacementMode.Relative,
                    AllowsTransparency = true,
                    StaysOpen = true,
                    PopupAnimation = PopupAnimation.None,
                    Child = card,
                    IsOpen = false
                };
                card.Focusable = true;
                Keyboard.ClearFocus();  

                if (!manualAdvance)
                {
                    AttachGlobalKeyClose();
                }
                else if (allowManualAdvanceInput)
                {
                    AttachGlobalKeyAdvance();
                }

                void RequestUpdateCardOnly()
                {
                    if (_popup == null) return;
                    if (_updatePending) return;

                    _updatePending = true;
                    hostWindow.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _updatePending = false;
                        UpdateCardOnly();
                    }), DispatcherPriority.Render);
                }

                WpfRect lastCardRectRaw = WpfRect.Empty;
                void UpdateCardOnly()
                {
                    if (_popup == null) return;

                    var cardAnchor = cardTarget ?? target;
                    var cardRectRaw = GetTargetRectInSurface(surface, cardAnchor);
                    if (!IsUsableTargetRect(cardRectRaw) && !ReferenceEquals(cardAnchor, target))
                    {
                        cardRectRaw = GetTargetRectInSurface(surface, target);
                    }

                    if (!IsUsableTargetRect(cardRectRaw))
                    {
                        if (_popup.IsOpen)
                        {
                            _popup.IsOpen = false;
                        }
                        return;
                    }

                    if (NearlyEqual(cardRectRaw, lastCardRectRaw)) return;
                    lastCardRectRaw = cardRectRaw;

                    PositionCard(surface, card, Inflate(cardRectRaw, padding));
                    var margin = card.Margin;
                    card.Margin = new Thickness(0);
                    _popup.HorizontalOffset = margin.Left;
                    _popup.VerticalOffset = margin.Top;

                    if (!_popup.IsOpen)
                    {
                        _popup.IsOpen = true;
                    }
                }

                hostWindow.UpdateLayout();

                _onLocationChanged = (_, __) => RequestUpdateCardOnly();
                _onSizeChanged = (_, __) => RequestUpdateCardOnly();
                hostWindow.LocationChanged += _onLocationChanged;
                hostWindow.SizeChanged += _onSizeChanged;
                _onLayoutUpdated = (_, __) => RequestUpdateCardOnly();
                surface.LayoutUpdated += _onLayoutUpdated;
                RequestUpdateCardOnly();

                if (!manualAdvance && durationMs > 0)
                {
                    _closeTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(durationMs)
                    };
                    _closeTimer.Tick += (_, __) => Hide("TimerElapsed");
                    _closeTimer.Start();
                }
                else
                {
                    _closeTimer = null;
                }

                return;
            }

            var root = new Grid
            {
                Width = Math.Max(1, surface.ActualWidth),
                Height = Math.Max(1, surface.ActualHeight),
                Background = Brushes.Transparent,
                IsHitTestVisible = overlayHitTestVisible,
                Visibility = Visibility.Collapsed
            };

            var spotlightLayer = new SpotlightLayer
            {
                DimOpacity = dimOpacity,
                CornerRadius = 10
            };
            root.Children.Add(spotlightLayer);
            root.Children.Add(card);
            card.Visibility = Visibility.Collapsed;

            // ❌ walkthrough 모드에서는 overlay가 클릭을 먹지 않음
            if (!manualAdvance)
            {
                root.MouseDown += (_, __) => Hide("MouseDownOutside");
            }
            else if (allowManualAdvanceInput && overlayHitTestVisible)
            {
                // ✅ 클릭하면 다음 스텝
                root.PreviewMouseDown += (_, e) =>
                {
                    if (e.ChangedButton != MouseButton.Left) return;
                    NextRequested?.Invoke();
                    e.Handled = true;
                };

            }

            _popup = new Popup
            {
                PlacementTarget = surface,
                Placement = PlacementMode.Relative,
                AllowsTransparency = true,
                StaysOpen = true,
                PopupAnimation = PopupAnimation.None,
                Child = root,
                IsOpen = false
            };


            // ❌ walkthrough 모드에서는 Enter/Esc 훅 비활성
            if (!manualAdvance)
            {
                AttachGlobalKeyClose();
            }
            else if (allowManualAdvanceInput)
            {
                AttachGlobalKeyAdvance();
            }


            void RequestUpdate()
            {
                if (_popup == null) return;
                if (_updatePending) return;

                _updatePending = true;
                hostWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _updatePending = false;
                    Update();
                }), DispatcherPriority.Render);
            }

            WpfRect lastRectRaw = WpfRect.Empty;
            var openedOnValidRect = false;
            void Update()
            {
                if (_popup == null) return;

                // root.Width  = Math.Max(1, Math.Max(surface.ActualWidth, _hostWindow?.ActualWidth  ?? 0));
                // root.Height = Math.Max(1, Math.Max(surface.ActualHeight, _hostWindow?.ActualHeight ?? 0));
                root.Width  = Math.Max(1, surface.ActualWidth);
                root.Height = Math.Max(1, surface.ActualHeight);



                var rectRaw = GetTargetRectInSurface(surface, target);
                var cardRectRaw = cardTarget != null ? GetTargetRectInSurface(surface, cardTarget) : rectRaw;

                if (!IsUsableTargetRect(rectRaw))
                {
                    root.Visibility = Visibility.Collapsed;
                    card.Visibility = Visibility.Collapsed;
                    spotlightLayer.SpotlightRect = WpfRect.Empty;
                    if (rectRaw != lastRectRaw)
                    {
                        Debug.WriteLine(
                            $"[TutorOverlay] POS_WAIT_STABLE overlayId={_overlayId} rid={Fmt(_currentDebugContext?.RequestId)} stepIndex={Fmt(_currentDebugContext?.StepIndex)} stepAnchorKey={Fmt(_currentDebugContext?.StepAnchorKey)} source={Fmt(_currentDebugContext?.Source)} phase=SHOW tid={Environment.CurrentManagedThreadId} ts={Ts()} rectRaw={rectRaw} aw={target.ActualWidth:F1} ah={target.ActualHeight:F1} rw={root.Width:F1} rh={root.Height:F1} sw={surface.ActualWidth:F1} sh={surface.ActualHeight:F1}");
                    }
                    lastRectRaw = rectRaw;
                    return;
                }

                if (!openedOnValidRect)
                {
                    openedOnValidRect = true;
                    root.Visibility = Visibility.Visible;
                    _popup.IsOpen = true;
                    root.Focusable = true;
                    root.Focus();
                    Keyboard.Focus(root);
                    Debug.WriteLine($"[TutorOverlay] OPEN_ON_VALID_RECT overlayId={_overlayId} rid={Fmt(_currentDebugContext?.RequestId)} stepIndex={Fmt(_currentDebugContext?.StepIndex)} stepAnchorKey={Fmt(_currentDebugContext?.StepAnchorKey)} source={Fmt(_currentDebugContext?.Source)} phase=SHOW tid={Environment.CurrentManagedThreadId} ts={Ts()} rectRaw={rectRaw}");
                }

                root.Visibility = Visibility.Visible;
                card.Visibility = Visibility.Visible;

                if (NearlyEqual(rectRaw, lastRectRaw)) return;
                lastRectRaw = rectRaw;

                Debug.WriteLine(
                    $"[TutorOverlay] POS_UPDATE overlayId={_overlayId} rid={Fmt(_currentDebugContext?.RequestId)} stepIndex={Fmt(_currentDebugContext?.StepIndex)} stepAnchorKey={Fmt(_currentDebugContext?.StepAnchorKey)} source={Fmt(_currentDebugContext?.Source)} phase=SHOW tid={Environment.CurrentManagedThreadId} ts={Ts()} rectRaw={rectRaw} aw={target.ActualWidth:F1} ah={target.ActualHeight:F1} rw={root.Width:F1} rh={root.Height:F1} sw={surface.ActualWidth:F1} sh={surface.ActualHeight:F1}");

                var rect = Inflate(rectRaw, padding);
                var cardRect = Inflate(cardRectRaw, padding);

                spotlightLayer.SpotlightRect = rect;
                PositionCard(surface, card, cardRect);

                root.InvalidateVisual();
            }

            
            hostWindow.UpdateLayout();

            _onLocationChanged = (_, __) => RequestUpdate();
            _onSizeChanged = (_, __) => RequestUpdate();
            hostWindow.LocationChanged += _onLocationChanged;
            hostWindow.SizeChanged += _onSizeChanged;
            _onLayoutUpdated = (_, __) => RequestUpdate();
            surface.LayoutUpdated += _onLayoutUpdated;  
            RequestUpdate();

            // ✅ 자동 닫기: walkthrough 아닐 때 + durationMs > 0 일 때만
            if (!manualAdvance && durationMs > 0)
            {
                _closeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(durationMs)
                };
                _closeTimer.Tick += (_, __) => Hide("TimerElapsed");
                _closeTimer.Start();
            }
            else
            {
                _closeTimer = null;
            }
            
        }

        private static void AttachGlobalKeyClose()
        {
            if (_onPreProcessInput != null)
            {
                InputManager.Current.PreProcessInput -= _onPreProcessInput;
                _onPreProcessInput = null;
            }

            _onPreProcessInput = (_, e) =>
            {
                if (_popup == null || !_popup.IsOpen) return;

                if (e.StagingItem.Input is KeyEventArgs ke &&
                    ke.RoutedEvent == Keyboard.PreviewKeyDownEvent)
                {
                    if (ke.Key == Key.Enter || ke.Key == Key.Escape)
                    {
                        Hide($"KeyClose:{ke.Key}");
                        ke.Handled = true;
                    }
                }
            };

            InputManager.Current.PreProcessInput += _onPreProcessInput;
        }

        private static string Ts() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        private static string Fmt(object? value) => value?.ToString() ?? "-";

        /*private static void AttachGlobalKeyAdvance()
        {
            if (_onPreProcessInput != null)
            {
                InputManager.Current.PreProcessInput -= _onPreProcessInput;
                _onPreProcessInput = null;
            }

            _onPreProcessInput = (_, e) =>
            {
                if (_popup == null || !_popup.IsOpen) return;

                if (e.StagingItem.Input is not KeyEventArgs ke)
                    return;

                // Esc: 전체 flow 취소
                if (ke.RoutedEvent == Keyboard.PreviewKeyDownEvent && ke.Key == Key.Escape)
                {
                    Debug.WriteLine($"[TutorOverlay] KEY_CANCEL key={ke.Key} overlayId={_overlayId}");
                    //CancelRequested?.Invoke();
                    ke.Handled = true;
                    return;
                }

                // Enter / Space: 현재 step 다음으로 진행
                if (ke.Key == Key.Enter || ke.Key == Key.Space)
                {
                    // KeyDown 에서 next 요청
                    if (ke.RoutedEvent == Keyboard.PreviewKeyDownEvent)
                    {
                        Debug.WriteLine($"[TutorOverlay] KEY_NEXT key={ke.Key} overlayId={_overlayId}");
                        NextRequested?.Invoke();
                        ke.Handled = true;
                        return;
                    }

                    // KeyUp 도 막아서 아래 컨트롤(Button 등)이 반응하지 않게 함
                    if (ke.RoutedEvent == Keyboard.PreviewKeyUpEvent)
                    {
                        ke.Handled = true;
                        return;
                    }
                }
            };

            InputManager.Current.PreProcessInput += _onPreProcessInput;
        }*/


        private static void AttachGlobalKeyAdvance()
        {
            if (_onPreProcessInput != null)
            {
                InputManager.Current.PreProcessInput -= _onPreProcessInput;
                _onPreProcessInput = null;
            }

            _onPreProcessInput = (_, e) =>
            {
                if (_popup == null || !_popup.IsOpen) return;
                if (e.StagingItem.Input is not KeyEventArgs ke) return;

                // Esc: cancel
                if (ke.RoutedEvent == Keyboard.PreviewKeyDownEvent && ke.Key == Key.Escape)
                {
                    Debug.WriteLine($"[TutorOverlay] KEY_CANCEL key={ke.Key} overlayId={_overlayId}");
                    CancelRequested?.Invoke();
                    ke.Handled = true;
                    return;
                }

                // Enter / Space: next
                if (ke.Key == Key.Enter || ke.Key == Key.Space)
                {
                    if (ke.RoutedEvent == Keyboard.PreviewKeyDownEvent)
                    {
                        Debug.WriteLine($"[TutorOverlay] KEY_NEXT key={ke.Key} overlayId={_overlayId}");
                        NextRequested?.Invoke();
                        ke.Handled = true;
                        return;
                    }

                    // KeyUp도 먹어서 아래 컨트롤이 Space를 클릭처럼 처리하지 못하게 막음
                    if (ke.RoutedEvent == Keyboard.PreviewKeyUpEvent)
                    {
                        ke.Handled = true;
                        return;
                    }
                }
            };

            InputManager.Current.PreProcessInput += _onPreProcessInput;
        }


        private static Border BuildCard(string? title, string? body, string? cardImageUri)
        {
            const double cardWidth = 320;
            const double cardMaxHeight = 420;
            const double bodyMaxHeight = 260;

            var titleText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(title) ? "" : title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Visibility = string.IsNullOrWhiteSpace(title) ? Visibility.Collapsed : Visibility.Visible
            };

            var bodyText = new TextBlock
            {
                Text = body ?? "",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                TextWrapping = TextWrapping.WrapWithOverflow,
                TextTrimming = TextTrimming.None,
                Visibility = Visibility.Visible
            };

            var bodyScroll = new ScrollViewer
            {
                Margin = new Thickness(0, 6, 0, 0),
                Content = bodyText,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = bodyMaxHeight,
                MinHeight = 48,
                Visibility = Visibility.Visible
            };

            var stack = new Grid();
            stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // image
            stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // title
            stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // body

            var image = CreateCardImage(cardImageUri);
            if (image != null)
            {
                Grid.SetRow(image, 0);
                stack.Children.Add(image);
            }
            Grid.SetRow(titleText, 1);
            stack.Children.Add(titleText);
            Grid.SetRow(bodyScroll, 2);
            stack.Children.Add(bodyScroll);

            Debug.WriteLine($"[TutorOverlay] CARD_BUILD titleLen={(title?.Length ?? 0)} bodyLen={(body?.Length ?? 0)} hasImage={(image != null)} bodyVisibility={bodyText.Visibility} bodyScrollMinHeight={bodyScroll.MinHeight}");

            var card = new Border
            {
                Width = cardWidth,
                MaxHeight = cardMaxHeight,
                Background = new SolidColorBrush(Color.FromArgb(235, 25, 25, 28)),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 22,
                    ShadowDepth = 6,
                    Opacity = 0.35
                },
                Child = stack,
                IsHitTestVisible = false
            };

            card.Loaded += (_, __) =>
            {
                Debug.WriteLine($"[TutorOverlay] CARD_LOADED cardH={card.ActualHeight:F1} bodyH={bodyText.ActualHeight:F1} bodyScrollH={bodyScroll.ActualHeight:F1} bodyTextLen={bodyText.Text.Length}");
            };

            return card;
        }

        private static FrameworkElement? CreateCardImage(string? cardImageUri)
        {
            if (string.IsNullOrWhiteSpace(cardImageUri))
            {
                return null;
            }

            try
            {
                var uri = Uri.TryCreate(cardImageUri, UriKind.Absolute, out var absoluteUri)
                    ? absoluteUri
                    : new Uri(cardImageUri, UriKind.RelativeOrAbsolute);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();

                return new Border
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    CornerRadius = new CornerRadius(10),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    Child = new Image
                    {
                        Source = bitmap,
                        Stretch = Stretch.Uniform,
                        MaxHeight = 170,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TutorOverlay] CARD_IMAGE_LOAD_FAIL uri={cardImageUri} reason={ex.Message}");
                return null;
            }
        }

        private static void PositionCard(FrameworkElement surface, Border card, WpfRect targetRect)
        {
            const double gap = 14;
            const double margin = 16;
            const double preferredMaxHeight = 420;

            var W = Math.Max(1, surface.ActualWidth);
            var H = Math.Max(1, surface.ActualHeight);

            var availableWidth = Math.Max(1, W - margin * 2);
            var availableHeight = Math.Max(1, H - margin * 2);
            var cardWidth = Math.Min(card.Width, availableWidth);
            var cardMaxHeight = Math.Min(preferredMaxHeight, availableHeight);

            card.Width = cardWidth;
            card.MaxHeight = cardMaxHeight;
            card.HorizontalAlignment = HorizontalAlignment.Left;
            card.VerticalAlignment = VerticalAlignment.Top;

            card.Measure(new WpfSize(cardWidth, cardMaxHeight));
            var cw = card.DesiredSize.Width;
            var ch = card.DesiredSize.Height;

            var belowY = targetRect.Bottom + gap;
            var aboveY = targetRect.Top - ch - gap;

            var canBelow = belowY + ch <= H - margin;
            var canAbove = aboveY >= margin;

            if (!canBelow && canAbove) _cardPlaceBelow = false;
            else if (canBelow && !canAbove) _cardPlaceBelow = true;

            var x = Math.Max(margin, Math.Min(targetRect.Left, W - cw - margin));
            var y = _cardPlaceBelow ? belowY : aboveY;
            y = Math.Max(margin, Math.Min(y, H - ch - margin));

            card.Margin = new Thickness(x, y, 0, 0);
        }

        private static FrameworkElement ResolveOverlaySurface(Window hostWindow)
        {
            // ✅ xMainWindowBorder가 있으면 최우선 (MainWindow 타입 체크 불필요)
            if (hostWindow.FindName("xMainWindowBorder") is FrameworkElement mainBorder &&
                mainBorder.ActualWidth > 1 && mainBorder.ActualHeight > 1)
                return mainBorder;

            // ✅ 모든 Window에서 가장 안전: HwndSource.RootVisual
            if (PresentationSource.FromVisual(hostWindow) is System.Windows.Interop.HwndSource hs &&
                hs.RootVisual is FrameworkElement rv &&
                rv.ActualWidth > 1 && rv.ActualHeight > 1)
                return rv;

            // fallback
            if (hostWindow.Content is FrameworkElement contentRoot &&
                contentRoot.ActualWidth > 1 && contentRoot.ActualHeight > 1)
                return contentRoot;

            return hostWindow.Content as FrameworkElement ?? hostWindow;
        }




        internal static bool TryGetStableTargetRectInSurface(
            FrameworkElement surface,
            FrameworkElement target,
            out WpfRect rect)
        {
            rect = GetTargetRectInSurface(surface, target);
            return IsUsableTargetRect(rect);
        }

        private static bool IsUsableTargetRect(WpfRect rect)
            => !rect.IsEmpty && rect.Width >= 1 && rect.Height >= 1;

        private static WpfRect GetTargetRectInSurface(FrameworkElement surface, FrameworkElement target)
        {
            try
            {
                var bounds = VisualTreeHelper.GetDescendantBounds(target);
                if (bounds.IsEmpty)
                    bounds = new WpfRect(0, 0, target.ActualWidth, target.ActualHeight);

                var t = target.TransformToVisual(surface);
                return t.TransformBounds(bounds);
            }
            catch
            {
                try
                {
                    var bounds = VisualTreeHelper.GetDescendantBounds(target);
                    if (bounds.IsEmpty)
                        bounds = new WpfRect(0, 0, target.ActualWidth, target.ActualHeight);

                    var topLeftOnScreen = target.PointToScreen(new System.Windows.Point(bounds.Left, bounds.Top));
                    var bottomRightOnScreen = target.PointToScreen(new System.Windows.Point(bounds.Right, bounds.Bottom));

                    var topLeftInSurface = surface.PointFromScreen(topLeftOnScreen);
                    var bottomRightInSurface = surface.PointFromScreen(bottomRightOnScreen);

                    return new WpfRect(topLeftInSurface, bottomRightInSurface);
                }
                catch
                {
                    return WpfRect.Empty;
                }
            }
        }

        private static WpfRect Inflate(WpfRect r, double pad)
            => new WpfRect(r.X - pad, r.Y - pad, r.Width + pad * 2, r.Height + pad * 2);

        private static bool NearlyEqual(WpfRect a, WpfRect b, double eps = 0.5)
        {
            return Math.Abs(a.X - b.X) < eps
                && Math.Abs(a.Y - b.Y) < eps
                && Math.Abs(a.Width - b.Width) < eps
                && Math.Abs(a.Height - b.Height) < eps;
        }


        private sealed class SpotlightLayer : FrameworkElement
        {
            public double DimOpacity { get; set; } = 0.62;
            public double CornerRadius { get; set; } = 10;

            private readonly List<WpfRect> _spotlightRects = new();
            private Geometry _overlayGeometry = Geometry.Empty;

            public WpfRect SpotlightRect
            {
                get => _spotlightRects.Count > 0 ? _spotlightRects[0] : WpfRect.Empty;
                set
                {
                    _spotlightRects.Clear();
                    if (IsUsableSpotlightRect(value))
                    {
                        _spotlightRects.Add(value);
                    }

                    UpdateOverlayGeometry();
                    InvalidateVisual();
                }
            }

            public IReadOnlyList<WpfRect> SpotlightRects => _spotlightRects;

            public void SetSpotlightRects(IEnumerable<WpfRect>? spotlightRects)
            {
                _spotlightRects.Clear();

                if (spotlightRects != null)
                {
                    foreach (var rect in spotlightRects)
                    {
                        if (IsUsableSpotlightRect(rect))
                        {
                            _spotlightRects.Add(rect);
                        }
                    }
                }

                UpdateOverlayGeometry();
                InvalidateVisual();
            }

            protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
            {
                base.OnRenderSizeChanged(sizeInfo);
                UpdateOverlayGeometry();
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);

                var dimBrush = new SolidColorBrush(Color.FromArgb((byte)(DimOpacity * 255), 0, 0, 0));
                dimBrush.Freeze();
                dc.DrawGeometry(dimBrush, null, _overlayGeometry);
            }

            private void UpdateOverlayGeometry()
            {
                var w = ActualWidth;
                var h = ActualHeight;
                if (w <= 0 || h <= 0)
                {
                    _overlayGeometry = Geometry.Empty;
                    return;
                }

                var full = new RectangleGeometry(new WpfRect(0, 0, w, h));
                Geometry overlayGeometry;

                if (_spotlightRects.Count == 0)
                {
                    overlayGeometry = full;
                }
                else
                {
                    var holes = new GeometryGroup();
                    foreach (var rect in _spotlightRects)
                    {
                        holes.Children.Add(new RectangleGeometry(rect, CornerRadius, CornerRadius));
                    }

                    overlayGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, full, holes);
                }

                if (overlayGeometry.CanFreeze)
                {
                    overlayGeometry.Freeze();
                }

                _overlayGeometry = overlayGeometry;
            }

            private static bool IsUsableSpotlightRect(WpfRect rect)
            {
                return !rect.IsEmpty && rect.Width > 0 && rect.Height > 0;
            }
        }
    }
}
