using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace VideoSocketServer
{
    public sealed partial class MainPage : Page
    {
        MemoryController<Video> memCtlr;
        CameraController camCtlr;
        SocketController sckCtlr;
        static MemoryStream videoStream;

        public static MainPage Core = null;

        public MainPage()
        {
            Core = this;
            InitializeComponent();
            memCtlr = new MemoryController<Video>(); //shared video list, event raising
            camCtlr = new CameraController(memCtlr); //webcam interaction, video capture, save to file/memory
            sckCtlr = new SocketController(memCtlr); //client/server, event capture, send video to client

            videoStream = new MemoryStream();
            Core.Media.SetSource(videoStream.AsRandomAccessStream(), "video/mp4");
            Core.Media.Play();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await sckCtlr.InitialiseServer();
            await camCtlr.InitialiseWebCam();
        }

        internal static async void DisplayBuffer(IBuffer buffer)
        {
            await Core.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                for (uint i = 0; i < buffer.Length; i++)
                {
                    byte b = buffer.GetByte(i);
                    videoStream.WriteByte(b);
                }

                //videoStream.Flush();
            });
        }
    }
}
