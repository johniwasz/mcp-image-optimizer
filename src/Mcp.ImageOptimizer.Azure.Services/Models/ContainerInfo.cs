
namespace Mcp.ImageOptimizer.Azure.Services.Models;

public class ContainerInfo
{
    public ContainerInfo(string name, string etag, DateTimeOffset lastModified)
    {
        this.Name = name;
        this.Etag = etag;
        LastModified = lastModified;
    }

    public string Name { get; }
    public string Etag { get; }

    public DateTimeOffset LastModified { get; }

    public IEnumerable<string> Blobs { get; set; } = Array.Empty<string>();

}

public record BlobInfo(string Name, string Etag, DateTimeOffset LastModified, long Size);
