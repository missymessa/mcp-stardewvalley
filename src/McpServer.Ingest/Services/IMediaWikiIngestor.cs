using System.Threading;
using System.Threading.Tasks;
using McpServer.Core.Models;

namespace McpServer.Ingest.Services
{
    public interface IMediaWikiIngestor
    {
        Task<IngestResult> FetchPageAsync(string pageTitle, CancellationToken ct = default);
    }
}
