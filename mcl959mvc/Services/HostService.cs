using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace mcl959mvc.Services;

public class HostService
{
    private readonly IWebHostEnvironment _env;

    public HostService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string MapPath(string relativePath)
    {
        // For files under wwwroot
        return Path.Combine(_env.WebRootPath, relativePath.TrimStart('/', '\\'));
        // For files under the project root (not wwwroot), use _env.ContentRootPath
        // return Path.Combine(_env.ContentRootPath, relativePath.TrimStart('/', '\\'));
    }
}