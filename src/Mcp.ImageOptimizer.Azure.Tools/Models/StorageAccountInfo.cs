

namespace Mcp.ImageOptimizer.Azure.Tools.Models;

public class StorageAccountInfo
{
    public StorageAccountInfo(string Name, string Id, string Location, string Kind, string Sku)
    {
        this.Name = Name;
        this.Id = Id;
        this.Location = Location;
        this.Kind = Kind;
        this.Sku = Sku;
    }

    public string Name { get; }
    public string Id { get; }
    public string Location { get; }
    public string Kind { get; }
    public string Sku { get; }

    public IEnumerable<ContainerInfo> Containers { get; set; } = Array.Empty<ContainerInfo>();

}

