using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
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

        int? _BufferSize = null;
        int BufferSize
        {
            get {
                if (_BufferSize != null)
                    return _BufferSize.Value;

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CHUNK_SIZE")))
                {
                    int parsedSize = 0;
                    if (int.TryParse(Environment.GetEnvironmentVariable("CHUNK_SIZE"), out parsedSize))
                        _BufferSize = parsedSize;
                }

                if (_BufferSize == null)
                    _BufferSize = 1024 * 4; // 4KB default

                return _BufferSize.Value;
            }
        }

        int? _WaitTimeBetweenChunks = null;
        int WaitTimeBetweenChunks
        {
            get
            {
                if (_WaitTimeBetweenChunks != null)
                    return _WaitTimeBetweenChunks.Value;

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TLS_WAIT_TIME")))
                {
                    int parsedSize = 0;
                    if (int.TryParse(Environment.GetEnvironmentVariable("TLS_WAIT_TIME"), out parsedSize))
                        _WaitTimeBetweenChunks = parsedSize;
                }

                if (_WaitTimeBetweenChunks == null)
                    _WaitTimeBetweenChunks = 500; // 500ms default

                return _WaitTimeBetweenChunks.Value;
            }
        }

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
          
        }

        [HttpGet("{Key}")]
        public async Task GetFile(string Key)
        {
            if (string.IsNullOrEmpty(Key))
                throw new Exception("No key defined.");

            var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET");

            if (string.IsNullOrEmpty(bucketName))
                throw new Exception("Bucket name not defined.");
           

            string fileName = Key;

            if (Key.Contains("/"))
                fileName = Key.Substring(Key.LastIndexOf("/")+1);

            var url = $"https://{bucketName}.s3.amazonaws.com/{Key}{Request.QueryString.ToString()}";

            _logger.LogInformation($"Downloading S3 object from {url}");

            long? firstbyte = null;
            long? lastbyte = null;
            bool validRangeBytes = false;

            if (Request.Headers.ContainsKey("Range"))
            {
                var strValues = Request.Headers["Range"].ToArray();


                if (strValues.Length > 0)
                {
                    // only concerned with the first item
                    var strValue = strValues[0];

                    Regex regex = new Regex(@"^\s*bytes\s*[=]{1}\s*(?<first>\d*)\s*[-]{1}\s*(?<last>\d*)\s*$",RegexOptions.IgnoreCase);

                    if (regex.IsMatch(strValue))
                    {
                        validRangeBytes = true;

                        var matchGroups = regex.Match(strValue).Groups;

                        if (matchGroups.ContainsKey("first"))
                        {
                            var strFirst = matchGroups["first"].Value.Trim();

                            if (!string.IsNullOrEmpty(strFirst))
                            {
                                try {
                                   firstbyte=long.Parse(strFirst);
                                }
                                catch { }
                            }
                        }

                        if (matchGroups.ContainsKey("last"))
                        {
                            var strLast = matchGroups["last"].Value.Trim();

                            if (!string.IsNullOrEmpty(strLast))
                            {
                                try
                                {
                                    lastbyte = long.Parse(strLast);
                                }
                                catch { }
                            }
                        }

                    }

                    
                }
            }

            using (HttpClient httpClient = new HttpClient())
            {
                if (validRangeBytes)
                    httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(firstbyte, lastbyte);

                var s3response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                
                    
                    var contentType = s3response.Content.Headers.ContentType.MediaType;
                    var contentLength= s3response.Content.Headers.ContentLength;
                    var contentRange = s3response.Content.Headers.ContentRange;
                    
                    _logger.LogInformation($"Content type {contentType} read with length {contentLength}");


                    if (s3response.Content is object)
                    {

                    if (validRangeBytes)
                    {
                        var strRange = contentRange.ToString();

                        Response.Headers.Add("Content-Range", strRange);
                        Response.StatusCode = 206;
                    }


                    Response.ContentLength = contentLength;

                    var responseStream = await s3response.Content.ReadAsStreamAsync();

                    

                    byte[] buffer = new byte[BufferSize];

                    int bytesRead;

                    long currentOffset = 0;

                    while ((bytesRead=responseStream.Read(buffer,0,buffer.Length))>0)
                    {
                        if (HttpContext.RequestAborted.IsCancellationRequested)
                            break;

                        currentOffset += bytesRead;

                        await Response.Body.WriteAsync(buffer, 0, bytesRead);
                        await Response.Body.FlushAsync();

                        await Task.Delay(WaitTimeBetweenChunks);
                    }

                  
                        
                    }
                    else
                        throw new Exception("Non-content type returned.");
                
            }
        }
    }
}
