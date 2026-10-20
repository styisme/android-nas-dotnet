using System.Security;
using System.IO.Compression;
using AndroidNas.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AndroidNas.Server.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/list", (string? path, IFileRootProvider root) =>
        {
            string dir;
            try { dir = root.Resolve(path); }
            catch (SecurityException) { return Results.BadRequest("Invalid path"); }
            if (!Directory.Exists(dir)) return Results.NotFound();

            var entries = new DirectoryInfo(dir)
                .EnumerateFileSystemInfos()
                .OrderBy(e => e is not DirectoryInfo)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Select(e =>
                {
                    var isDir = e is DirectoryInfo;
                    long size = 0;
                    if (!isDir && e is FileInfo fi) try { size = fi.Length; } catch { }
                    return new FileEntryDto(e.Name, isDir, size,
                        !isDir && MediaTypeHelper.IsTxt(e.Name),
                        !isDir && MediaTypeHelper.IsMedia(e.Name),
                        !isDir && MediaTypeHelper.IsZip(e.Name));
                })
                .ToArray();
            return Results.Ok(new ListResponse(root.ToVirtual(dir), entries));
        });

        app.MapGet("/api/download", (string? path, IFileRootProvider root) =>
        {
            string full;
            try { full = root.Resolve(path); }
            catch (SecurityException) { return Results.BadRequest("Invalid path"); }
            if (!File.Exists(full)) return Results.NotFound();
            return Results.File(full, "application/octet-stream", Path.GetFileName(full), enableRangeProcessing: true);
        });

        app.MapGet("/api/preview", (string? path, IFileRootProvider root) =>
        {
            string full;
            try { full = root.Resolve(path); }
            catch (SecurityException) { return Results.BadRequest(); }
            if (!File.Exists(full)) return Results.NotFound();
            if (new FileInfo(full).Length > 2 * 1024 * 1024) return Results.BadRequest("Too large");
            return Results.Text(File.ReadAllText(full));
        });

        app.MapPost("/api/upload", async (HttpRequest req, IFileRootProvider root) =>
        {
            if (!req.HasFormContentType) return Results.BadRequest("Not multipart");
            var form = await req.ReadFormAsync();
            var targetPath = form["path"].ToString();
            string dir;
            try { dir = root.Resolve(targetPath); }
            catch (SecurityException) { return Results.BadRequest("Invalid path"); }
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var names = new List<string>();
            foreach (var file in form.Files)
            {
                var dest = Path.Combine(dir, Path.GetFileName(file.FileName));
                await using var fs = File.Create(dest);
                await file.CopyToAsync(fs);
                names.Add(file.FileName);
            }
            return Results.Text("OK: " + string.Join(",", names));
        });

        app.MapPost("/api/mkdir", async (HttpRequest req, IFileRootProvider root) =>
        {
            var form = await req.ReadFormAsync();
            var name = form["name"].ToString();
            if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\') || name == "..")
                return Results.BadRequest();
            string dir;
            try { dir = root.Resolve(form["path"].ToString()); }
            catch (SecurityException) { return Results.BadRequest(); }
            Directory.CreateDirectory(Path.Combine(dir, name));
            return Results.Text("OK");
        });

        app.MapPost("/api/rename", async (HttpRequest req, IFileRootProvider root) =>
        {
            var form = await req.ReadFormAsync();
            var newName = form["newName"].ToString();
            if (string.IsNullOrWhiteSpace(newName) || newName.Contains('/') || newName.Contains('\\') || newName == "..")
                return Results.BadRequest();
            string oldFull;
            try { oldFull = root.Resolve(form["path"].ToString()); }
            catch (SecurityException) { return Results.BadRequest(); }
            if (!File.Exists(oldFull) && !Directory.Exists(oldFull)) return Results.NotFound();
            var newFull = Path.Combine(Path.GetDirectoryName(oldFull)!, newName);
            if (File.Exists(oldFull)) File.Move(oldFull, newFull);
            else Directory.Move(oldFull, newFull);
            return Results.Text("OK");
        });

        app.MapPost("/api/delete", async (HttpRequest req, IFileRootProvider root) =>
        {
            var form = await req.ReadFormAsync();
            string full;
            try { full = root.Resolve(form["path"].ToString()); }
            catch (SecurityException) { return Results.BadRequest(); }
            if (full == root.RootPath) return Results.BadRequest("Cannot delete root");
            if (Directory.Exists(full)) Directory.Delete(full, true);
            else if (File.Exists(full)) File.Delete(full);
            else return Results.NotFound();
            return Results.Text("OK");
        });

        app.MapPost("/api/save", async (HttpRequest req, IFileRootProvider root) =>
        {
            var form = await req.ReadFormAsync();
            string full;
            try { full = root.Resolve(form["path"].ToString()); }
            catch (SecurityException) { return Results.BadRequest(); }
            await File.WriteAllTextAsync(full, form["content"].ToString());
            return Results.Text("OK");
        });

        app.MapPost("/api/unzip", async (HttpRequest req, IFileRootProvider root) =>
        {
            var form = await req.ReadFormAsync();
            string zipPath;
            try { zipPath = root.Resolve(form["path"].ToString()); }
            catch (SecurityException) { return Results.BadRequest(); }
            if (!File.Exists(zipPath)) return Results.NotFound();

            var destDir = Path.Combine(Path.GetDirectoryName(zipPath)!, Path.GetFileNameWithoutExtension(zipPath));
            Directory.CreateDirectory(destDir);
            var destFull = Path.GetFullPath(destDir);

            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (var entry in archive.Entries)
                {
                    var destPath = Path.GetFullPath(Path.Combine(destDir, entry.FullName));
                    if (!destPath.StartsWith(destFull, StringComparison.Ordinal)) continue;
                    if (string.IsNullOrEmpty(entry.Name))
                        Directory.CreateDirectory(destPath);
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        entry.ExtractToFile(destPath, true);
                    }
                }
            });
            return Results.Text("OK");
        });

        return app;
    }
}
