using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDownloader.Lib.DownloadPolicy
{
    public interface IRefFileDataProvider
    {
        byte[] ExpectedMd5 { get; }

        long ExpectedFileSize { get;  }
    }
}
