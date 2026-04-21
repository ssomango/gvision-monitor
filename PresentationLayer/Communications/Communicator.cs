using GVisionWpf.Exceptions;
using GVisionWpf.Models.Dtos;
using GVisionWpf.UIs.UiUpdaters;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GVisionWpf.PresentationLayer.Communications
{
    public class Communicator
    {
        private static readonly Lazy<Communicator> lazy = new Lazy<Communicator>(() => new Communicator());
        public static Communicator Instance => lazy.Value;

        private UdpClient? udpClient;
        private readonly IPEndPoint endPoint;

        private const string HANDLER_PC_ADDRESS = "192.168.10.100";
        private const int LISTENING_PORT = 3333;
        private const int SENDING_PORT = 1004;

        private Communicator()
        {
            this.endPoint = new IPEndPoint(IPAddress.Any, LISTENING_PORT);
        }

        public void InitializeClient()
        {
            this.udpClient = new UdpClient();
            try
            {
                this.udpClient?.Client.Bind(this.endPoint);
            }
            catch
            {
                throw new UdpBindingException();
            }
        }

        public void ReleaseClient()
        {
            if (this.udpClient == null)
            {
                return;
            }

            Heart.Instance.HeartBeatMode = EHeartBeatMode.None;

            this.udpClient.Close();
            this.udpClient.Dispose();
            this.udpClient = null;
        }

        public void Connect()
        {
            if (this.udpClient != null)
            {
                return;
            }

            Instance.InitializeClient();
            Heart.Instance.HeartBeatMode = EHeartBeatMode.Ready;

            _ = Task.Run(() => { Instance.StartReceivingAsync(); });
        }

        public void Send(IBytesConvertible response)
        {
            byte[] bytes = response.ToBytes();
            Send(bytes);
        }

        public void Send(byte[] packet)
        {
            if (this.udpClient == null)
            {
                throw new UdpBindingException();
            }

            this.udpClient.BeginSend(packet, packet.Length, new IPEndPoint(IPAddress.Parse(HANDLER_PC_ADDRESS), SENDING_PORT), sendDone, null);
        }

        private void sendDone(IAsyncResult asyncResult)
        {
            try
            {
                //int bytesSent = this.udpClient!.EndSend(asyncResult);
            }
            catch
            {
                throw new UdpSendException();
            }
        }

        public Task StartReceivingAsync()
        {
            _ = Task.Run(receiveLoopAsync);
            return Task.CompletedTask;
        }

        private async Task receiveLoopAsync()
        {
            while (this.udpClient != null)
            {
                try
                {
                    UdpReceiveResult result = await this.udpClient.ReceiveAsync();
                    handleReceivedData(result.Buffer);
                }
                catch (SocketException)
                {
                    // Ignore this exception
                }
                catch (Exception? ex)
                {
                    GlobalErrorHandler.HandleException(ex);
                }
            }
        }

        private void handleReceivedData(byte[] bytes)
        {
            try
            {
                Task.Run(() => Router.Instance.RouteToController(bytes));
            }
            catch (Exception? ex)
            {
                GlobalErrorHandler.HandleException(ex);
            }
        }
    }
}