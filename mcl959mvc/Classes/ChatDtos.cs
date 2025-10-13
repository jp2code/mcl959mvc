namespace mcl959mvc.Classes;


public class ChatRequest
{
    public string Question { get; set; } = "";
}

public class ChatResponse
{
    public string Answer { get; set; } = "";
    public bool IsRegistrationHelp { get; set; }
    public string[] Suggestions { get; set; } = Array.Empty<string>();
    public bool IsHtml { get; set; } = false;
}