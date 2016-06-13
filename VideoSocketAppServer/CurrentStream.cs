using System;

namespace VideoSocketAppServer
{
    internal class CurrentVideo
    {
        public Guid Id { get; set; }
        public byte[] Data { get; set; }
    }
}