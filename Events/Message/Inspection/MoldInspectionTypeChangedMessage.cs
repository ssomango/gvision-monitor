using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GVisionWpf.Events.Message.Inspection
{
    public class MoldInspectionTypeChangedMessage : ValueChangedMessage<EInspection>
    {
        public MoldInspectionTypeChangedMessage(EInspection value) : base(value) { }
    }
}
