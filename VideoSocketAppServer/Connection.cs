using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace VideoSocketAppServer
{
    internal class Connection
    {
        private StreamSocket _socket;
        private SocketAppServer _server;

        public Connection(StreamSocket socket, SocketAppServer server)
        {
            _socket = socket;
            _server = server;

            //spin up a thread from the pool
            //to listen for client app comms
            Task.Run(() => Listen());
        }

        private async Task Listen()
        {
            //send an acknowledgement byte to the client app 
            //to signal that we're ready to receive requests
            Debug.WriteLine($"Sending connection acknowledgement");
            await _socket.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("0").AsBuffer());

            while (true)
            {
                //the client app is expected to request a new video, passing in a guid
                //as the "command", read 36 bytes from the buffer and try to parse it.
                Debug.WriteLine($"Listening for socket command...");
                IBuffer inbuffer = new Windows.Storage.Streams.Buffer(36);
                await _socket.InputStream.ReadAsync(inbuffer, 36, InputStreamOptions.Partial);
                string command = Encoding.UTF8.GetString(inbuffer.ToArray());
                Debug.WriteLine($"Command received: {command}");

                //use the guid to either get the current video, or wait for the 
                //next new one that's added by the server
                Guid guid = Guid.Empty;
                Guid.TryParse(command, out guid);
                byte[] data = _server.GetCurrentVideoDataAsync(guid);
                if (data != null)
                    await _socket.OutputStream.WriteAsync(data.AsBuffer());
                else
                    Debug.WriteLine($"Could not intialise, video does not exist");

                //add a brief delay to reduce system load
                await Task.Delay(50);
            }
        }
    }
}