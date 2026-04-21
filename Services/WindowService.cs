
using GVisionWpf.Api;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.ViewModels;
using System.Windows;

namespace GVisionWpf.Services
{
    public class WindowService : IWindowService
    {
        private Window? _chatWindowInstance;

        // 인터페이스의 ShowChatWindow 메서드를 구현
        public void ShowChatWindow()
        {
            // 채팅창이 없거나 닫혔으면 새로 생성해서 연다(싱글톤처럼 동작)
            if (_chatWindowInstance == null || !_chatWindowInstance.IsLoaded)
            {
                _chatWindowInstance = new ChatWindow();  // View를 직접 생성
                _chatWindowInstance.Owner = Application.Current.MainWindow;
                _chatWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _chatWindowInstance.LocationChanged += childWindow_LocationChanged;
                _chatWindowInstance.Show();
            }
            else
            {
                // 이미 열려있으면 최소화 
                if (_chatWindowInstance.WindowState == WindowState.Minimized)
                {
                    _chatWindowInstance.WindowState = WindowState.Normal;
                    _chatWindowInstance.Activate(); // 창을 최상위로
                }
                else
                {
                    _chatWindowInstance.WindowState = WindowState.Minimized; // 최소화
                }
            }
        }



        private void childWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (sender is not Window child) return;

            // 메인 윈도우 가져오기
            var parent = Application.Current.MainWindow;
            if (parent == null) return;

            // 부모 창 위치/크기
            var parentLeft = parent.Left;
            var parentTop = parent.Top;
            var parentRight = parent.Left + parent.Width;
            var parentBottom = parent.Top + parent.Height;

            // 자식 창 위치/크기
            var childLeft = child.Left;
            var childTop = child.Top;
            var childRight = child.Left + child.Width;
            var childBottom = child.Top + child.Height;

            // 왼쪽/위쪽 제한
            if (childLeft < parentLeft) child.Left = parentLeft;
            if (childTop < parentTop) child.Top = parentTop;

            // 오른쪽/아래쪽 제한
            if (childRight > parentRight) child.Left = parentRight - child.Width;
            if (childBottom > parentBottom) child.Top = parentBottom - child.Height;
        }

    }
}

