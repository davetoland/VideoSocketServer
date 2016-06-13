using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace VideoSocketServer
{
    public class SocketController
    {
        private uint _port = 13337;
        private List<Connection> _connections;
        private StreamSocketListener _listener;
        private MemoryController<Video> _memCtlr;

        public SocketController(MemoryController<Video> memoryController)
        {
            _memCtlr = memoryController;
            _memCtlr.OnNewItem += HandleNewVideo;
            _connections = new List<Connection>();
        }

        public async Task InitialiseServer()
        {
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += ConnectionReceived;

            HostName host = null;
            foreach (HostName localhost in NetworkInformation.GetHostNames())
                if (localhost.IPInformation != null)
                    if (localhost.Type == HostNameType.Ipv4)
                    {
                        host = localhost;
                        break;
                    }

            await _listener.BindEndpointAsync(host, $"{_port}");
        }

        private async void ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Run(() => _connections.Add(new Connection { Socket = args.Socket }));
        }

        private async void HandleNewVideo(object sender, uint e)
        {
            Video video = _memCtlr.GetItem(e);
            var markedForDeletion = new List<StreamSocket>();
            var tasks = new List<Task>();
            //TODO: Periodically remove dead connections from the pool
            foreach (Connection connection in _connections.Where(c => c.IsConnected))
            try
            {
                    var writer = new DataWriter(connection.Socket.OutputStream);
                    writer.WriteBuffer(video.Buffer);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                    writer.Dispose();
                    //await connection.Socket.OutputStream.WriteAsync(video.Buffer);
                    //await connection.Socket.OutputStream.FlushAsync();
                    Debug.WriteLine($"Sent {video.Buffer.Length} bytes");
            }
            catch
            {
                connection.IsConnected = false;
            }

            //MainPage.DisplayBuffer(buffer);
        }
    }
}