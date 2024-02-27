using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDownloader.Lib.DownloadPolicy
{
    
    public class RefFileDataProvider : IRefFileDataProvider
    {
        private readonly string _refFilePath;
        private readonly ILogger<RefFileDataProvider> _logger;
        public RefFileDataProvider(string refFilePath, ILogger<RefFileDataProvider> logger) 
        {
            _refFilePath = refFilePath;
            _logger = logger;
            LoadRefData();
        }

        public byte[] ExpectedMd5
        {
            get;
            private set;
        }

        public long ExpectedFileSize
        {
            get;
            private set;
        }

        public void LoadRefData()
        {
            if (File.Exists(_refFilePath))
            {
                var fileInfo = new FileInfo(_refFilePath);
                ExpectedFileSize = fileInfo.Length;

                using var md5 = MD5.Create();
                using var stream = File.OpenRead(_refFilePath);
                ExpectedMd5 = md5.ComputeHash(stream);
            }
        }
       
    }
}
