using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.MessengerHandler;

namespace GVisionWpf.Events
{
    public partial class GVisionMessenger
    {
        public static GVisionMessenger Instance = new GVisionMessenger();
        public UIMessengerHandler UI { get; }
        public RecipeMessengerHandler Recipe { get; }
        public PacketMessengerHandler Packet { get; }

        private GVisionMessenger()
        {
            UI = new UIMessengerHandler(this);
            Recipe = new RecipeMessengerHandler(this);
            Packet = new PacketMessengerHandler(this);
        }

        public void Register<TMessage>(IRecipient<TMessage> message) where TMessage : class
            => WeakReferenceMessenger.Default.Register(message);

        public void RegisterAll(object recipent)
            => WeakReferenceMessenger.Default.RegisterAll(recipent);

        public void RegisterAll<TToken>(object recipent, TToken token) where TToken : IEquatable<TToken>
            => WeakReferenceMessenger.Default.RegisterAll(recipent, token);

        public void Send<TMessage>() where TMessage : class, new()
            => WeakReferenceMessenger.Default.Send<TMessage>();

        public void Send<TMessage>(TMessage message) where TMessage : class
            => WeakReferenceMessenger.Default.Send<TMessage>(message);
    }
}
