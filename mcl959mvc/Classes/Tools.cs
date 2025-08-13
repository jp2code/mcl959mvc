using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace mcl959mvc.Classes;

public class Tools
{

    public static DateTime NODATE { get { return DateTime.MinValue; } }

    /// <summary>
    /// Allows only basic HTML tags for inline formatting and line breaks.
    /// </summary>
    private static string AllowBasicHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // Allow <b>, <strong>, <i>, <em>, <a>, <br>, <ul>, <ol>, <li>, <img>
        // Remove all other tags for safety
        var allowedTags = new[] { "b", "strong", "i", "em", "a", "br", "ul", "ol", "li", "img" };
        // Use Regex to remove disallowed tags (simple approach)
        string pattern = $@"</?(?!({string.Join("|", allowedTags)})\b)[^>]*>";
        return System.Text.RegularExpressions.Regex.Replace(input, pattern, string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
    public static string CreateDirectoryWithPermission(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(relativePath));
        }
        // get absolute path under wwwroot
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var fullPath = Path.Combine(wwwroot, relativePath.TrimStart(Path.DirectorySeparatorChar));
        // Create directory if it does not exist
        if (!Directory.Exists(fullPath))
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            // Grant read/write permissions to the directory for the current user (for development purposes)
            try
            {
                var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
                if (currentUser != null)
                {
                    var directorySecurity = directoryInfo.GetAccessControl();
                    directorySecurity.AddAccessRule(new FileSystemAccessRule(
                        currentUser.Name,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow));
                    directoryInfo.SetAccessControl(directorySecurity);
                }
            }
            catch (PlatformNotSupportedException)
            {
                // On Linux or non-Windows, permissions may not be set this way; ignore
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                throw new InvalidOperationException("Failed to set directory permissions.", ex);
            }
        }
        return fullPath;
    }

    /// <summary>
    /// Deserializes an object of type T from a JSON file at the specified path.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="path">The file path.</param>
    /// <returns>The deserialized object, or default(T) if not found or on error.</returns>
    public static async Task<T?> DeserializeFromJsonFileAsync<T>(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return default;
        try
        {
            using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (JsonException)
        {
            // Handle invalid JSON
            // Optionally log or rethrow
            return default;
        }
        catch (Exception)
        {
            // Handle other exceptions (e.g., IO)
            // Optionally log or rethrow
            return default;
        }
    }

    public static string FolderBuilder(string fileAndPath)
    {
        var fa = FileAttributes.Hidden;
        try
        {
            fa = File.GetAttributes(fileAndPath);
        }
        catch (DirectoryNotFoundException)
        {
            fa = FileAttributes.Normal;
        }
        catch (FileNotFoundException)
        {
            fa = FileAttributes.Normal;
        }
        var split = fileAndPath.Split(Path.DirectorySeparatorChar);
        string path = null;
        int len = ((fa & FileAttributes.Directory) != 0) ? split.Length : split.Length - 1;
        int last1 = split.Length - 1;
        string part = null;
        for (int i = 0; i < split.Length; i++)
        {
            part = split[i];
            if (!String.IsNullOrEmpty(part))
            {
                if ((i == last1) && (-1 < part.IndexOf('.')))
                {
                }
                else
                {
                    if (!String.IsNullOrEmpty(path))
                    {
                        path += string.Format("{0}{1}", Path.DirectorySeparatorChar, part);
                        if (!Directory.Exists(path))
                        {
                            CreateDirectoryWithPermission(path);
                        }
                    }
                    else
                    {
                        path = part;
                    }
                }
            }
        }
        if ((fa & FileAttributes.Directory) == 0)
        {
            return string.Format("{0}{1}{2}", path, Path.DirectorySeparatorChar, part);
        }
        else
        {
            return path;
        }
    }

    public static string GetText(object o)
    {
        return ((o != null) && (o != DBNull.Value)) ? o.ToString().Trim() : "";
    }

    public static bool HasValue(object o)
    {
        return !String.IsNullOrEmpty(GetText(o));
    }
    /// <summary>
    /// Tests if the input String has any Letters in the Character Array
    /// </summary>
    /// <param name="input">String Value to convert to a Character Array</param>
    /// <returns>True if no characters in the Character Array is a Letter</returns>
    public static bool IsNumeric(string input)
    {
        if (String.IsNullOrEmpty(input)) return false;
        foreach (char c in input.ToCharArray())
        {
            // ','=44; '-'=45; '.'=46; '0'=48; '9'=57
            if (c < ',') return false; // '0'=Decimal 48
            if ('9' < c) return false; // '9'=Decimal 57
            //if (char.IsLetter(c)) return false;
        }
        return true;
    }
    /// <summary>
    /// Converts the most common Editor.js blocks (paragraph, header, list, image) from the Editor.js JSON output to HTML. This is a basic converter and can be extended to support more block types as needed.
    /// </summary>
    /// <param name="description">actual text from Editor.js</param>
    /// <returns>HTML representation of the Editor.js content</returns>
    public static string JsonToHtml(string description)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(description))
        {
            var doc = JsonNode.Parse(description);
            var blocks = doc?["blocks"]?.AsArray();
            if (blocks != null)
            {
                try
                {
                    foreach (var block in blocks)
                    {
                        var type = block?["type"]?.ToString();
                        var data = block?["data"];
                        switch (type)
                        {
                            case "paragraph":
                                sb.Append("<p>");
                                sb.Append(AllowBasicHtml(data?["text"]?.ToString() ?? ""));
                                sb.AppendLine("</p>");
                                break;
                            case "header":
                                var level = data?["level"]?.GetValue<int>() ?? 2;
                                sb.Append($"<h{level}>");
                                sb.Append(AllowBasicHtml(data?["text"]?.ToString() ?? ""));
                                sb.AppendLine($"</h{level}>");
                                break;
                            case "list":
                                var style = data?["style"]?.ToString() == "ordered" ? "ol" : "ul";
                                sb.Append($"<{style}>");
                                foreach (var item in data?["items"]?.AsArray() ?? new JsonArray())
                                {
                                    sb.Append("<li>");
                                    sb.Append(AllowBasicHtml(item?.ToString() ?? ""));
                                    sb.Append("</li>");
                                }
                                sb.AppendLine($"</{style}>");
                                break;
                            case "image":
                                var url = data?["file"]?["url"]?.ToString();
                                var caption = data?["caption"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(url))
                                {
                                    sb.Append($"<figure><img src=\"{System.Net.WebUtility.HtmlEncode(url)}\" alt=\"{System.Net.WebUtility.HtmlEncode(caption)}\" style=\"max-width:100%\" />");
                                    if (!string.IsNullOrEmpty(caption))
                                        sb.Append($"<figcaption>{System.Net.WebUtility.HtmlEncode(caption)}</figcaption>");
                                    sb.AppendLine("</figure>");
                                }
                                break;
                            // Add more block types as needed
                            default:
                                Console.WriteLine($"Unknown block type: {type}");
                                // Optionally handle unknown block types
                                break;
                        }
                    }
                }
                catch (JsonException err)
                {
                    Console.WriteLine(err);
                    // Handle JSON parsing errors
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Extracts the DateTime value from a string that includes a Julian Date (i.e. LT2009127B)
    /// </summary>
    /// <param name="value">string that includes the Julain Date</param>
    /// <returns>Converted DateTime using Days in Year or NODATE if any of the format is incorrect</returns>
    public static DateTime JulianGetDate(string value)
    {
        if (!String.IsNullOrEmpty(value))
        {
            int index = -1;
            char[] buf = value.ToCharArray();
            for (int i = 0; (i < buf.Length) && (index == -1); i++)
            {
                if (char.IsDigit(buf[i]))
                {
                    index = i;
                }
            }
            //foreach (char c in value.ToCharArray()) { (needs to be tested first)
            //  index++;
            //  if ((',' < c) && (c < '9')) {
            //    break;
            //  }
            //}
            if (-1 < index)
            {
                string text = value.Substring(index);
                if (6 < text.Length)
                {
                    string strYear = text.Substring(0, 4);
                    string strDate = text.Substring(4, 3);
                    try
                    {
                        int year = ToInt32(strYear);
                        int days = ToInt32(strDate);
                        if ((1980 < year) && (year < 2050) && (0 < days) & (days < 367))
                        { // leap year has 366 days
                            DateTime date = new DateTime(year, 1, 1);
                            date = date.AddDays(days - 1); // subtract 1 because I had to start with Jan 1 above
                            return date;
                        }
                    }
                    catch (Exception) { } // discard exception. Date was in invalid format
                }
            }
        }
        return NODATE;
    }
    /// <summary>
    /// Creates the Julian Date (Military style) from a DateTime value. [Returned date does not include time.]
    /// </summary>
    /// <param name="value">DateTime to convert</param>
    /// <returns>string formatted as YYYYDDD, where YYYY is the Year and DDD is the Day of the Year (1 - 366)</returns>
    public static string JulianGetString(DateTime value)
    {
        int year = value.Year;
        int days = value.DayOfYear;
        string julianString = string.Format("{0:0000}{1:000}", year, days);
        return julianString;
    }
    public static async Task SerializeToJsonFileAsync<T>(T obj, string path)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize the object to the file asynchronously
            using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, obj, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception)
        {
            // Optionally log or rethrow
            throw;
        }
    }
    /// <summary>
    /// Converts a String Value to a character
    /// </summary>
    /// <param name="value">String Value to convert</param>
    /// <returns>0 if null; -1 if not numeric or if Convert fails</returns>
    public static Int32 ToInt32(object item)
    {
        string value = HasValue(item) ? item.ToString().Trim() : null;
        if (String.IsNullOrEmpty(value))
        {
            return 0;
        }
        if (IsNumeric(value))
        {
            try
            {
                return Convert.ToInt32(value, 10);
            }
            catch (FormatException e)
            {
                Console.WriteLine("Format: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        return -1;
    }

}