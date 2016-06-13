using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace VideoSocketServer
{
    public class Connection
    {
        public StreamSocket Socket { get; set; }
        public bool IsConnected { get; set; } = true;
    }
}
