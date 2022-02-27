using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetReleaser.Helpers;

internal sealed class GzipCompressingHandler : DelegatingHandler
{
    public GzipCompressingHandler(HttpMessageHandler innerHandler)
    {
        InnerHandler = innerHandler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = request.Content;

        if (request.Method == HttpMethod.Post && content is not null)
        {
            request.Content = new GzipContent(content);
        }

        return base.SendAsync(request, cancellationToken);
    }

    internal sealed class GzipContent : HttpContent
    {
        private readonly HttpContent _content;

        public GzipContent(HttpContent content)
        {
            this._content = content;
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Add("gzip");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await using var gzip = new GZipStream(stream, CompressionMode.Compress, true);
            await _content.CopyToAsync(gzip);
        }
    }
}