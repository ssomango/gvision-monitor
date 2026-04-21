using GVisionWpf.DSMMI.UI;
using GVisionWpf.Models.Dtos;
using GVisionWpf.UIs.ViewModels;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.DSMMI
{
    public class VinsCommunicator
    {
        private static readonly Lazy<VinsCommunicator> lazy = new Lazy<VinsCommunicator>(() => new VinsCommunicator());
        public static VinsCommunicator Instance => lazy.Value;

        private const int LISTENING_PORT = 1004;
        private const int SENDING_PORT = 3333;
        private readonly IPEndPoint endPoint;

        public string HandlerPcAddress { get; set; } = "127.0.0.1";
        private UdpClient? udpClient;

        private VinsCommunicator()
        {
            endPoint = new IPEndPoint(IPAddress.Any, LISTENING_PORT);
            //InitializeClient();
        }

        public void InitializeClient()
        {
            udpClient = new UdpClient();
            try
            {
                udpClient?.Client.Bind(endPoint);
                VinsFantasyViewModel.Instance.Print($"Listening on {IPAddress.Any}:{LISTENING_PORT}");
            }
            catch
            {
                VinsFantasyViewModel.Instance.Print($"Failed to bind EndPoint: {LISTENING_PORT}");
            }
        }

        public void ReleaseClient()
        {
            if (udpClient == null)
            {
                return;
            }

            udpClient.Close();
            udpClient.Dispose();
            udpClient = null;
        }

        public void Send(IBytesConvertible response)
        {
            byte[] bytes = response.ToBytes();
            Send(bytes);
        }

        public void Send(byte[] packet)
        {
            if (udpClient == null)
            {
                GVisionMessenger.Instance.UI.SendSystemInfoMessage("UDP Client is null.");
            }

            udpClient.BeginSend(packet, packet.Length, new IPEndPoint(IPAddress.Parse(HandlerPcAddress), SENDING_PORT), sendDone, null);
        }

        private void sendDone(IAsyncResult asyncResult)
        {
            try
            {
                int bytesSent = udpClient!.EndSend(asyncResult);
            }
            catch
            {
                VinsFantasyViewModel.Instance.Print("Failed to send the packet.");
            }
        }

        public Task StartReceivingAsync()
        {
            _ = Task.Run(receiveLoopAsync);
            return Task.CompletedTask;
        }

        private async Task receiveLoopAsync()
        {
            while (udpClient != null)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                new Thread(() => handleReceivedData(result.Buffer)).Start();
            }
        }

        private static void handleReceivedData(byte[] bytes) { }
    }
}
