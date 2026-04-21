using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GVisionWpf.Events.Message.Inspection
{
    public class PrsInspectionTypeChangedMessage : ValueChangedMessage<EInspection>
    {
        public PrsInspectionTypeChangedMessage(EInspection value) : base(value) { }
    }
}
