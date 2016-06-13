using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace VideoSocketClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MemoryStream videoStream;
        private const string HOST = "192.168.1.112";
        
        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            videoStream = new MemoryStream();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var socket = new StreamSocket();

            while (true)
            {
                try
                {
                    Debug.WriteLine("Attempting connection..");
                    var host = new HostName(HOST);
                    await socket.ConnectAsync(host, "13337");
                    Debug.WriteLine("Connected!");
                    break;
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to connect, retrying...");
                }
            }

            //await Task.Run(() => PlayBuffer());


            //var buffer = new byte[1];
            //var reader = new DataReader(socket.InputStream.AsStreamForRead().AsRandomAccessStream());
            //reader.InputStreamOptions = InputStreamOptions.ReadAhead;
            while (true)
            {
                var mem = new MemoryStream();
                await RandomAccessStream.CopyAsync(socket.InputStream.AsStreamForRead().AsRandomAccessStream(), mem.AsOutputStream());
                //socket.InputStream.AsStreamForRead().CopyTo(mem);
                //await socket.InputStream.ReadAsync(buffer.AsBuffer(), 1, InputStreamOptions.Partial);
                if (mem.Length > 0)
                {
                    // Set inputstream options so that we don't have to know the data size
                    //await reader.LoadAsync(reader.UnconsumedBufferLength);
                    StorageFile file = await KnownFolders.VideosLibrary.CreateFileAsync("TestVid.mp4", CreationCollisionOption.ReplaceExisting);
                    //await FileIO.WriteBytesAsync(file, reader.DetachBuffer().ToArray());
                    await FileIO.WriteBytesAsync(file, mem.ToArray());
                }
            }

            //byte[] buffer = new byte[1024];
            //using (var ms = new MemoryStream())
            //{
            //    while (true)
            //    {
            //        await socket.InputStream.ReadAsync(buffer.AsBuffer(), 1024, InputStreamOptions.Partial);
            //        ms.Write(buffer, 0, buffer.Length);

            //        if (buffer.Length != 1024)
            //        {
            //            buffer = new byte[ms.Length];
            //            ms.Position = 0;
            //            ms.Read(buffer, 0, (int)ms.Length);
            //            StorageFile file = await KnownFolders.VideosLibrary.CreateFileAsync("TestVid.mp4", CreationCollisionOption.ReplaceExisting);
            //            await FileIO.WriteBytesAsync(file, buffer);

            //            Media.SetSource(await file.OpenAsync(FileAccessMode.Read), "");
            //            Media.Play();
            //            break;
            //        }
            //    }
            //}


            //var reader = new DataReader(socket.InputStream);
            //reader.InputStreamOptions = InputStreamOptions.Partial;

            //while (true)
            //{
            //    await reader.LoadAsync(999999);

            //    long length = reader.UnconsumedBufferLength;
            //    if (length > 0)
            //    {
            //        Debug.WriteLine($"socket buffer length: {length}");
            //        byte[] buffer = reader.DetachBuffer().ToArray();

            //        //SoftwareBitmap bitmap = SoftwareBitmap.CreateCopyFromBuffer(buffer.AsBuffer(), )

            //        //var stream = new MemoryStream(buffer);
            //        //stream.WriteTo(videoStream);
            //        //Debug.WriteLine($"stream buffer length: {videoStream.Length}");
            //    }
            //    else
            //    {

            //    }

            //    if (videoStream.Length > 5000)
            //    {
            //        //    Media.SetSource(videoStream.AsRandomAccessStream(), "video/mp4");
            //        //    //Media.Play();

            //        //    while (Media.CurrentState != MediaElementState.Playing)
            //        //    {
            //        //        Debug.WriteLine($"Media state: {Media.CurrentState}");
            //        //        await Task.Delay(TimeSpan.FromSeconds(1));
            //        //    }



            //        //Media.SetMediaStreamSource(source);
            //        //Media.Play();
            //    }

            await Task.Delay(TimeSpan.FromSeconds(1));
            


            ////for testing
            //await Task.Delay(TimeSpan.FromSeconds(3));
            //while (true)
            //{
            //    byte[] bytes = Encoding.UTF8.GetBytes(DateTime.Now.ToString("HHmmss"));
            //    Debug.WriteLine($"writing bytes: {Encoding.UTF8.GetString(bytes, 0, bytes.Length)}");
            //    videoStream.Write(bytes, 0, 1);
            //    Debug.WriteLine($"stream length: {videoStream.GetWindowsRuntimeBuffer().Length}");
            //    await Task.Delay(TimeSpan.FromSeconds(1));
            //}
        }
        
        private async void PlayBuffer()
        {
            uint currentbuffersize = 0;
            while (true)
            {
                uint buffersize = videoStream.GetWindowsRuntimeBuffer().Length;
                //byte[] buffer = new byte[buffersize];
                //await videoStream.ReadAsync(buffer, 0, (int)buffersize);
                //Debug.WriteLine($"reading: {Encoding.UTF8.GetString(buffer, 0, buffer.Length)}");

                if (buffersize > currentbuffersize)
                {
                    //if (buffersize > )

                    Debug.WriteLine($"buffer: {buffersize} - current: {currentbuffersize}");
                    currentbuffersize = buffersize;
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                    {
                        Debug.WriteLine($"old state: {Media.CurrentState}");
                        switch (Media.CurrentState)
                        {
                            case MediaElementState.Closed:
                                Media.SetSource(videoStream.AsRandomAccessStream(), "video/mp4");
                                Media.Play();
                                Debug.WriteLine($"new state: {Media.CurrentState}");
                                break;

                            case MediaElementState.Stopped:
                            case MediaElementState.Paused:
                                Media.Play();
                                break;
                        }
                    });
                }

                await Task.Delay(TimeSpan.FromMilliseconds(1));
            }
        }
    }
}
