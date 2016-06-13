using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace VideoSocketApp
{
    public sealed partial class MainPage : Page
    {
        private Guid _guid = Guid.NewGuid();
        private MediaPlaybackList _playlist = null;
        private bool Buffering => _playlist.Items.Count == 0;

        public MainPage()
        {
            InitializeComponent();
            Media.MediaEnded += Media_MediaEnded;

            _playlist = new MediaPlaybackList();
            //remove played items from the list
            _playlist.CurrentItemChanged += (sender, args) => _playlist.Items.Remove(args.OldItem);
            _playlist.ItemOpened += Playlist_ItemOpened;
            _playlist.ItemFailed += Playlist_ItemFailed;
            Media.SetPlaybackSource(_playlist);
        }

        private void Playlist_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
        {
            Debug.WriteLine("New playlist item Opened");
        }

        private void Playlist_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
        {
            Debug.WriteLine("New playlist item Failed!");
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //when we're testing this in VS, using Multiple Startup Projects
            //pause for a few seconds to allow the server to start up.
            await Task.Delay(TimeSpan.FromSeconds(5));
            await Task.WhenAll(
                PlayNextVideo(),
                DownloadVideos());
        }

        private async Task DownloadVideos()
        {
            var socket = new StreamSocket();
            while (true)
            {
                try
                {
                    //if the server hasn't yet started up, or we're having transient 
                    //network issues, keep retrying until we can connect. Obviously
                    //we'd make this more robust in a non-testing scenario...
                    await socket.ConnectAsync(new HostName("192.168.1.112"), "13337");
                    break;
                }
                catch { }
            }

            //after connecting to the server, block on the input stream until we receive 
            //a response on it... the server will send a single byte to tell us it's ready
            byte[] inbuffer = new byte[1];
            IBuffer result = await socket.InputStream.ReadAsync(inbuffer.AsBuffer(), inbuffer.AsBuffer().Capacity, InputStreamOptions.None);

            //once we're connected, and the server's ready, go into a continuous looop 
            //of downloading bytes from the socket and reassemble to create MediaSource objects
            //In testing this is fine, in production this would need to be more robust
            while (true)
            {
                //in each packet of bytes, the first 16 are a Guid.
                //this Guid identifies that video segment. We pass this back
                //in on the next request to make sure we don't download the same segment twice
                Debug.WriteLine($"Requesting next download ({_guid})");
                IBuffer outbuffer = Encoding.UTF8.GetBytes($"{_guid}").AsBuffer();
                await socket.OutputStream.WriteAsync(outbuffer);
                inbuffer = new byte[10000000];

                //again, block on the input stream until we've received the full packet,
                //but use the Partial option so that we don't have to fill the entire buffer before we continue.
                //this is important, because the idea is to set the buffer big enough to handle any packet we'll receive,
                //meaning we'll never fill the entire buffer... and we don't want to block here indefinitely
                result = await socket.InputStream.ReadAsync(inbuffer.AsBuffer(), inbuffer.AsBuffer().Capacity, InputStreamOptions.Partial);
                Debug.WriteLine($"Download complete: {result.Length} bytes");
                
                //strip off the Guid, leaving just the video data
                byte[] guid = result.ToArray().Take(16).ToArray();
                byte[] data = result.ToArray().Skip(16).ToArray();
                _guid = new Guid(guid);

                //wrap the data in a stream, create a MediaSource from it,
                //then use that to create a MediaPlackbackItem which gets added 
                //to the back of the playlist...
                var stream = new MemoryStream(data);
                var source = MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/mp4");
                var item = new MediaPlaybackItem(source);
                _playlist.Items.Add(item);
                Debug.WriteLine($"New playlist item added to list: {data.Length} bytes");
                Debug.WriteLine($"Playlist now contains {_playlist.Items.Count} items");
                if (_playlist.Items.Count > 2)
                {
                    //this is a bug I haven't worked out yet..
                    //from time to time, the list seems to get stuck ?!?
                    Debug.WriteLine("Playlist stuck, moving next");
                    //we can get things moving again by forcing this.. 
                    //TODO: Find out why and implement a more robust solution
                    _playlist.MoveNext();
                }

                Debug.WriteLine($"Media state: {Media.CurrentState}");
                if (Media.CurrentState != MediaElementState.Playing)
                {
                    //if this is the first cycle/video and we've not yet started playing, or
                    //if the network is slow, the MediaElement may have reached the end of 
                    //the previous item and stopped, putting us into a state of "buffering"...
                    Debug.WriteLine("Playing...");
                    Media.Play();
                }

                //reset the buffer
                inbuffer = new byte[10000000];
            }
        }

        private async Task PlayNextVideo()
        {
            Debug.WriteLine($"Playing next video...");
            while (true)
            {
                if (!Buffering)
                {
                    //as long as there's at least one item in the
                    //playlist, start playing the MediaElement
                    BufferingLbl.Visibility = Visibility.Collapsed;
                    Media.Play();
                    break;
                }
                else
                {
                    //else go into a 'buffering' loop
                    Debug.WriteLine($"Buffering...");
                    BufferingLbl.Visibility = Visibility.Visible;
                    await Task.Delay(500);
                }
            }
        }

        private async void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            //ideally, the media never stops, as it downloads video 
            //segments faster than they're played back, but if it does, restart it...
            Debug.WriteLine($"Playback ended");
            await PlayNextVideo();
        }
    }
}
