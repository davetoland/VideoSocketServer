using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace VideoSocketServer
{
    public class Video
    {
        public StorageFile File { get; set; }
        public IInputStream Stream { get; set; }
        public byte[] Bytes { get; set; }
        public IBuffer Buffer { get; set; } = new Windows.Storage.Streams.Buffer(4096);

        public async Task<IBuffer> GetFileBuffer()
        {
            IBuffer buffer = new byte[0].AsBuffer();

            try
            {
                IRandomAccessStreamWithContentType stream = await File.OpenReadAsync();
                buffer = new byte[stream.Size].AsBuffer();
                await stream.ReadAsync(buffer, (uint)stream.Size, InputStreamOptions.ReadAhead);
            }
            catch (Exception ex) { }

            return buffer;
        }

        public IBuffer GetStreamBuffer()
        {
            var reader = new DataReader(Stream);
            IBuffer buffer = reader.DetachBuffer();
            byte[] bytes = buffer.ToArray();
            return buffer;
        }
    }
}