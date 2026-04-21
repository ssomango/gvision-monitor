using GVisionWpf.Api;
using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatWindow()
        {
            InitializeComponent();
            // 창이 열릴 때 위치를 수동 지정
            // 창 위치를 수동으로 지정하도록 설정
            var apiServer = ApiServer.Instance; // 또는 new ApiServer();
            var viewModel = new ChatWindowViewModel(apiServer);
            DataContext = viewModel;
            viewModel.RequestFocus += () => FocusInputBox();

            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // 창이 다 로드된 후에 위치 지정
            this.Loaded += (s, e) =>
            {
                this.Left = 1700;  // X좌표
                this.Top = 300;   // Y좌표

                

                if (DataContext is ChatWindowViewModel vm)
                {
                    vm.ChatLog.CollectionChanged += (s2, e2) =>
                    {
                        if (ChatListBox.Items.Count > 0)
                            ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
                    };
                }

                if (apiServer != null)
                {
                    apiServer.SetChatWindowViewModel(viewModel);
                }
                // 창이 처음 열릴 때도 포커스
                FocusInputBox();
            };
        }

        /// <summary>
        /// 채팅 입력창에 포커스를 맞추는 함수
        /// </summary>
        public void FocusInputBox()
        {
            ChatTextBox.Focus();
            Keyboard.Focus(ChatTextBox);
        }

        private void TrainingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.ContextMenu == null) return;

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}


//private void ChatListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
//{

//}
//private void ChatTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
//{
//    var textBox = sender as TextBox;
//    if (textBox == null) return;

//    textBox.Height = Double.NaN; // 높이 자동 계산을 위해 일단 해제
//    textBox.UpdateLayout();      // 새 텍스트 레이아웃 반영
//    var formattedText = new FormattedText(
//        textBox.Text + " ", // 마지막 줄 계산 보정
//        System.Globalization.CultureInfo.CurrentCulture,
//        FlowDirection.LeftToRight,
//        new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
//        textBox.FontSize,
//        Brushes.Black,
//        new NumberSubstitution(),
//        1);

//    // 최소 높이 지정 가능
//    double minHeight = 30;
//    textBox.Height = Math.Max(minHeight, formattedText.Height + 10);
//}

