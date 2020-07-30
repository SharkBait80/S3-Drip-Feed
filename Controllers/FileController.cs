using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace s3proxy.net.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;

        
        const int BufferSize = 1024 * 5; 

        Amazon.S3.AmazonS3Client s3Client;

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
            s3Client = new Amazon.S3.AmazonS3Client();
        }

        [HttpGet("{Key}")]
        public async Task<IActionResult> GetFile(string Key)
        {
            if (string.IsNullOrEmpty(Key))
                throw new Exception("No key defined.");

            var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET");

            if (string.IsNullOrEmpty(bucketName))
                throw new Exception("Bucket name not defined.");
           
            string fileName = Key;
            if (Key.Contains("/"))
                fileName = Key.Substring(Key.LastIndexOf("/")+1);

            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = Key
            };

            
            
            GetObjectResponse response =  await s3Client.GetObjectAsync(request);
            Stream responseStream = response.ResponseStream;
            BufferedStream buffered = new BufferedStream(responseStream, BufferSize);
           
            return File(buffered, response.Headers.ContentType);
           
        }
    }
}
