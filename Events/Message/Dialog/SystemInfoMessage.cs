namespace GVisionWpf.Events.Message.Dialog
{
    public class SystemInfoMessage
    {
        public string Message { get; set; } = string.Empty;

        public SystemInfoMessage(string message)
        {
            Message = message;
        }
    }
}
