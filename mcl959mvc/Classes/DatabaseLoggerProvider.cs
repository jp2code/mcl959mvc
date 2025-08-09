using mcl959mvc.Models;

namespace mcl959mvc.Classes;

public class DatabaseLoggerProvider : ILoggerProvider
{
    private readonly Mcl959DbContext _context;

    public DatabaseLoggerProvider(Mcl959DbContext context)
    {
        _context = context;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DatabaseLogger(categoryName, _context);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
