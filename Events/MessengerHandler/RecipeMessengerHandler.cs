using GVisionWpf.Events.Message.Inspection;

namespace GVisionWpf.Events.MessengerHandler
{
    public sealed class RecipeMessengerHandler
    {
        private readonly GVisionMessenger _messenger;

        public RecipeMessengerHandler(GVisionMessenger messenger)
        {
            _messenger = messenger;
        }

        public void SendChangedPrsType(EInspection inspection)
        {
            _messenger.Send(new PrsInspectionTypeChangedMessage(inspection));
        }

        public void SendChangedMappingType(EInspection inspection)
        {
            _messenger.Send(new MoldInspectionTypeChangedMessage(inspection));
        }
    }
}
