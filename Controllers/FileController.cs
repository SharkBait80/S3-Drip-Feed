using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
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

        

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
          
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

            var url = $"https://{bucketName}.s3.amazonaws.com/{Key}{HttpUtility.UrlDecode(Request.QueryString.ToString())}";

            _logger.LogInformation($"Downloading S3 object from {url}");

            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                

                    var contentType = response.Content.Headers.ContentType.MediaType;
                    var contentLength=response.Content.Headers.ContentLength;

                    _logger.LogInformation($"Content type {contentType} read with length {contentLength}");


                    if (response.Content is object)
                    {


                    var responseStream = await response.Content.ReadAsStreamAsync();

                    BufferedStream buffered = new BufferedStream(responseStream, BufferSize);
                            

                    return File(buffered, contentType);
                            
                        
                    }
                    else
                        throw new Exception("Non-content type returned.");
                
            }
        }
    }
}
