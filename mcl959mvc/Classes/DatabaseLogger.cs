using Microsoft.Extensions.Logging;
using mcl959mvc.Data;
using mcl959mvc.Models;
using System;

namespace mcl959mvc.Classes;

public class DatabaseLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Mcl959DbContext _context;

    public DatabaseLogger(string categoryName, Mcl959DbContext context)
    {
        _categoryName = categoryName;
        _context = context;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var log = new WebsiteLog
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel.ToString(),
            Category = _categoryName,
            Message = formatter(state, exception),
            Exception = exception?.ToString()
        };

        try
        {
            using var db = new Mcl959DbContext(_context.Options);
            db.WebsiteLogs.Add(log);
            db.SaveChanges();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            // Error 208: Invalid object name 'WebsiteLogs'
            if (ex.Number == 208)
            {
                // Table does not exist yet (e.g., during migration). Ignore.
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            // Optionally log to another provider or ignore
        }
    }
}