using GVisionWpf.Events.Message.Packet;

namespace GVisionWpf.Events.MessengerHandler
{
    public sealed class PacketMessengerHandler
    {
        private readonly GVisionMessenger _messenger;

        public PacketMessengerHandler(GVisionMessenger messenger)
        {
            _messenger = messenger;
        }

        public void SendPacketHandlingMessage(PacketMessage.EPacketMessageAction action)
        {
            _messenger.Send(new PacketMessage { Action = action });
        }
    }
};
