using System;

namespace mcl959mvc.Classes;

public class WebsiteLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string LogLevel { get; set; }
    public string Category { get; set; }
    public string Message { get; set; }
    public string Exception { get; set; }
}