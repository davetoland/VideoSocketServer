using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace VideoSocketServer
{
    public class CameraController
    {
        private Timer _timer;
        private MediaCapture _mediaCap;
        private MemoryController<Video> _memCtlr;
        private bool _isInitialised;

        public CameraController(MemoryController<Video> memoryController)
        {
            _memCtlr = memoryController;
        }

        public async Task InitialiseWebCam()
        {
            if (!_isInitialised)
            {
                var settings = ApplicationData.Current.LocalSettings;
                string preferredDeviceName = $"{settings.Values["PreferredDeviceName"]}";
                if (string.IsNullOrWhiteSpace(preferredDeviceName))
                    preferredDeviceName = "Microsoft® LifeCam HD-3000";

                var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                DeviceInformation device = videoDevices.FirstOrDefault(x => x.Name == preferredDeviceName);
                if (device == null)
                    device = videoDevices.FirstOrDefault();

                if (device == null)
                    throw new Exception("Cannot find a camera device");
                else
                {
                    //initialize the WebCam via MediaCapture object
                    _mediaCap = new MediaCapture();
                    var initSettings = new MediaCaptureInitializationSettings { VideoDeviceId = device.Id };
                    await _mediaCap.InitializeAsync(initSettings);
                    _mediaCap.Failed += new MediaCaptureFailedEventHandler(MediaCaptureFailed);
                    
                    _isInitialised = true;
                    VideoLoop();
                }
            }
        }

        private void MediaCaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            //throw new NotImplementedException();
        }

        private async void VideoLoop()
        {
            for (int i=0; i<5; i++)
            //while (true)
            {
                StorageFile videoFile = await KnownFolders.VideosLibrary.CreateFileAsync(
                    $"video_{DateTime.Now.ToString("yyyyMMddHHmmss")}.mp4", CreationCollisionOption.GenerateUniqueName);

                var mediaEncoding = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Vga);
                //Debug.WriteLine($"Bitrate: {mediaEncoding.Video.Bitrate}");
                var memoryStream = new InMemoryRandomAccessStream();
                //VideoFrame frame = await _mediaCap.GetPreviewFrameAsync();
                await _mediaCap.StartRecordToStreamAsync(mediaEncoding, memoryStream);
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _mediaCap.StopRecordAsync();
                //_mediaCap.GetPreviewFrameAsync(new VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat.Yuy2,0,0).SoftwareBitmap.)

                var video = new Video();
                //frame.SoftwareBitmap.CopyToBuffer(video.Buffer);
                //await memoryStream.ReadAsync(video.Buffer, video.Buffer.Length, InputStreamOptions.Partial);
                memoryStream.Seek(0);

                byte[] bytes = new byte[memoryStream.Size];
                var dr = new DataReader(memoryStream);
                await dr.LoadAsync((uint)memoryStream.Size);
                dr.ReadBytes(bytes);

                //video.Buffer = ob.AsBuffer();
                //_memCtlr.AddItem(video);

                video.Buffer = bytes.AsBuffer();
                _memCtlr.AddItem(video);
            }
        }
    }
}