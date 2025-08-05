using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using System.IO;

namespace mcl959mvc.Classes;

public static class Extensions {

public static void AddRange<T, S>(this ICollection<T> list, params S[] values) where S : T {
  foreach (S value in values) list.Add(value);
}

public static bool Between<T>(this T actual, T lower, T upper) where T : IComparable<T> {
  return 0 < actual.CompareTo(lower) && actual.CompareTo(upper) < 0;
}

public static string br2nl(this string str) {
  if (!String.IsNullOrEmpty(str)) {
    str = str.Replace("<br/>", "\r\n");
  }
  return str;
}

public static bool CoinToss(this Random rng) {
  return rng.Next(2) == 0;
}

public static int CompareQuick(this string text, string value) {
  if (!String.IsNullOrEmpty(text)) {
    if (!String.IsNullOrEmpty(value)) {
      return text.CompareTo(value);
    }
    return 1;
  } else if (!String.IsNullOrEmpty(value)) {
    return -1;
  }
  return 0;
}

public static string Format(this string s, params object[] args) {
  return string.Format(s, args);
}

public static bool HasValue(this object item) {
  string text = $"{item}".Trim();
  return !String.IsNullOrEmpty(text);
}

public static string HtmlSafe(this string text) {
  string inputString = $"{text}".Trim();
  if (!String.IsNullOrEmpty(inputString)) {
    string msg = inputString;
    msg = msg.Replace("\"", "\'");
    msg = msg.Replace("<", "&lt;");
    msg = msg.Replace(">", "&gt;");
    msg = msg.Replace("\r\n", "<br/>");
    msg = msg.Replace("&amp;", "and");
    return msg;
  }
  return "";
}

public static bool In<T>(this T source, params T[] list) {
  if (null == source) throw new ArgumentNullException("source");
  return list.Contains(source);
}

public static bool IsNumeric(this string inputString) {
  if (!String.IsNullOrEmpty(inputString)) {
    string input = inputString.Trim();
    if (String.IsNullOrEmpty(input)) return false;
    foreach (char c in input.ToCharArray()) {
      if (char.IsLetter(c)) return false;
    }
    return true;
  }
  return false;
}

public static DateTime NODATE { get { return DateTime.MinValue; } }

public static string nl2br(this string str) {
  if (!String.IsNullOrEmpty(str)) {
    str = str.Replace("\r\n", "<br/>");
    str = str.Replace("\n", "<br/>");
  }
  return str;
}

public static string NullTrim(this string inputString) {
  if (!String.IsNullOrEmpty(inputString)) return inputString.Trim();
  return "";
}

public static T OneOf<T>(this Random rng, params T[] things) {
  return things[rng.Next(things.Length)];
}

public static string TextFollowing(this string txt, string value) {
  if (!String.IsNullOrEmpty(txt) && !String.IsNullOrEmpty(value)) {
    int index = txt.IndexOf(value);
    if (-1 < index) {
      int start = index + value.Length;
      if (start <= txt.Length) {
        return txt.Substring(start);
      }
    }
  }
  return "";
}

public static bool ToBool(this object obj) {
  if (obj.HasValue()) {
    try {
      return Convert.ToBoolean(obj);
    } catch { }
    bool value;
    if (bool.TryParse($"{obj}".Trim(), out value)) {
      return value;
    }
  }
  return false;
}

public static DateTime ToDateOrNoDate(this object obj) {
  if (obj.HasValue()) {
    try {
      return Convert.ToDateTime(obj);
    } catch (Exception) { }
    DateTime value;
    if (DateTime.TryParse(obj.TrimString(), out value)) {
      return value;
    }
  }
  return NODATE;
}

public static double ToDouble(this object obj) {
  if (obj.HasValue()) {
    try {
      return Convert.ToDouble(obj);
    } catch { }
    double value;
    if (double.TryParse($"{obj}".Trim(), out value)) {
      return value;
    }
  }
  return 0;
}

public static string ToHtmlText(this string text) {
  string msg = text.TrimString();
  if (!String.IsNullOrEmpty(msg)) {
    msg = msg.Replace("&lt;", "<").Replace("&gt;", ">").Replace("\r\n", "<br/>");
  }
  return msg;
}

public static int ToInt32(this object obj) {
  if (obj.HasValue()) {
    try {
      return Convert.ToInt32(obj);
    } catch { }
    int value;
    if (int.TryParse($"{obj}".Trim(), out value)) {
      return value;
    }
  }
  return 0;
}

public static string TrimString(this object objString) {
  if (objString.HasValue()) {
    string text = $"{objString}";
    return text.Trim();
  }
  return "";
}
/// <summary>Deserializes an xml string in to an object of Type T</summary>
/// <typeparam name="T">Any class type</typeparam>
/// <param name="xml">Xml as string to deserialize from</param>
/// <returns>A new object of type T is successful, null if failed</returns>
public static T? XmlDeserialize<T>(this string xml) where T : class, new() {
  if (xml == null) throw new ArgumentNullException("xml");
  var serializer = new XmlSerializer(typeof(T));
  using (var reader = new StringReader(xml)) {
    try {
        return (T?)serializer.Deserialize(reader);
    } catch {
        return null;
    } // Could not be deserialized to this type.
  }
}
/// <summary>Serializes an object of type T in to an xml string</summary>
/// <typeparam name="T">Any class type</typeparam>
/// <param name="obj">Object to serialize</param>
/// <returns>A string that represents Xml, empty otherwise</returns>
public static string XmlSerialize<T>(this T obj) where T : class, new() {
  ArgumentNullException.ThrowIfNull(obj);
  var serializer = new XmlSerializer(typeof(T));
  using (var writer = new StringWriter()) {
    serializer.Serialize(writer, obj);
    return writer.ToString();
  }
}

}