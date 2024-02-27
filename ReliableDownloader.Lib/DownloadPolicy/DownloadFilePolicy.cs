using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDownloader.Lib.DownloadPolicy
{
    public class DownloadFilePolicy
    {
        public int RetryCount { get; set; }
        public int DownloadTimeOut { get; set; }

        public int BufferSize { get; set; }
    }
}
