using AndroidNas.Core;

namespace AndroidNas.Server.Files;

public interface IFileRootProvider
{
    string RootPath { get; }
    bool GlobalMode { get; }
    string Resolve(string? virtualPath);
    string ToVirtual(string physicalPath);
}

public sealed class PhysicalRootProvider : IFileRootProvider
{
    public string RootPath { get; }
    public bool GlobalMode { get; }
    public PhysicalRootProvider(string rootPath, bool globalMode)
    {
        RootPath = rootPath;
        GlobalMode = globalMode;
    }
    public string Resolve(string? virtualPath) => SafePath.Resolve(RootPath, virtualPath);
    public string ToVirtual(string physicalPath) => SafePath.ToVirtual(RootPath, physicalPath);
}
