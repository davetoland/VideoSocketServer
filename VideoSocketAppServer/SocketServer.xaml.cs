using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace VideoSocketAppServer
{
    public sealed partial class SocketAppServer : Page
    {
        private int _port = 13337;
        private MediaCapture _mediaCap;
        private StreamSocketListener _listener;
        private ManualResetEvent _signal = new ManualResetEvent(false);
        private List<Connection> _connections = new List<Connection>();
        internal CurrentVideo CurrentVideo = new CurrentVideo();

        public SocketAppServer()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await InitialiseVideo();
            await StartListener();
            await BeginRecording();
        }

        private async Task InitialiseVideo()
        {
            //to be refactored
            Debug.WriteLine($"Initialising video...");
            var settings = ApplicationData.Current.LocalSettings;
            string preferredDeviceName = $"{settings.Values["PreferredDeviceName"]}";
            if (string.IsNullOrWhiteSpace(preferredDeviceName))
                preferredDeviceName = "Microsoft® LifeCam HD-3000";

            //select webcam device
            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation device = videoDevices.FirstOrDefault(x => x.Name == preferredDeviceName);
            if (device == null)
                device = videoDevices.FirstOrDefault();

            if (device == null)
                throw new Exception("Cannot find a camera device");
            else
            {
                //initialise media capture
                _mediaCap = new MediaCapture();
                var initSettings = new MediaCaptureInitializationSettings { VideoDeviceId = device.Id };
                await _mediaCap.InitializeAsync(initSettings);
                _mediaCap.Failed += new MediaCaptureFailedEventHandler(MediaCaptureFailed);
            }

            Debug.WriteLine($"Video initialised");
        }

        private void MediaCaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.WriteLine($"Video capture failed: {errorEventArgs.Message}");
        }

        private async Task BeginRecording()
        {
            while (true)
            {
                try
                {
                    //record a 5 second video to stream
                    Debug.WriteLine($"Recording started");
                    var memoryStream = new InMemoryRandomAccessStream();
                    await _mediaCap.StartRecordToStreamAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Vga), memoryStream);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await _mediaCap.StopRecordAsync();
                    Debug.WriteLine($"Recording finished, {memoryStream.Size} bytes");

                    //create a CurrentVideo object to hold stream data and give it a unique id
                    //which the client app can use to ensure they only request each video once
                    memoryStream.Seek(0);
                    CurrentVideo.Id = Guid.NewGuid();
                    CurrentVideo.Data = new byte[memoryStream.Size];

                    //read the stream data into the CurrentVideo  
                    await memoryStream.ReadAsync(CurrentVideo.Data.AsBuffer(), (uint)memoryStream.Size, InputStreamOptions.None);
                    Debug.WriteLine($"Bytes written to stream");

                    //signal to waiting connections that there's a new video
                    _signal.Set();
                    _signal.Reset();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"StartRecording -> {ex.Message}");
                    break;
                }
            }
        }

        private async Task StartListener()
        {
            //listen for client socket connections by binding to a host and port, 
            //then wrap the socket in a Connection object and add it to the collection
            Debug.WriteLine($"Starting listener");
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += (sender, args) =>
            {
                Debug.WriteLine($"Connection received from {args.Socket.Information.RemoteAddress}");
                _connections.Add(new Connection(args.Socket, this));
            };

            HostName host = NetworkInformation.GetHostNames().FirstOrDefault(x => x.IPInformation != null && x.Type == HostNameType.Ipv4);
            await _listener.BindEndpointAsync(host, $"{_port}");
            Debug.WriteLine($"Listener started on {host.DisplayName}:{_listener.Information.LocalPort}");
        }

        internal byte[] GetCurrentVideoDataAsync(Guid guid)
        {
            //if this is the initial run, wait until the first video is available
            //or if this request is for the current video, wait for the next one
            if (CurrentVideo.Id == Guid.Empty || CurrentVideo.Id == guid)
                 _signal.WaitOne();

            //join the guid onto the start of the stream data
            return CurrentVideo.Id.ToByteArray().Concat(CurrentVideo.Data).ToArray();
        }
    }
}
