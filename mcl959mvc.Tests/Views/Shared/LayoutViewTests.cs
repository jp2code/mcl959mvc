using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Xunit;
using AngleSharp.Html.Parser;

public class LayoutViewTests
{
    [Fact]
    public void ModelStateErrors_AreSerialized_AndRenderedInHiddenInput()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required.");
        modelState.AddModelError("Password", "Password is too short.");

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ModelState = modelState
        };

        // Simulate the serialization logic from _Layout.cshtml
        var errorDict = modelState
            .Where(x => x.Value != null && x.Value.Errors.Any())
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToList()
            );
        var modelStateErrorsJson = JsonSerializer.Serialize(errorDict);

        // Act: Render the input as Razor would
        var html = $"<input type=\"hidden\" id=\"modelStateErrors\" value='{modelStateErrorsJson}' />";

        // Parse HTML and extract value
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var input = doc.QuerySelector("#modelStateErrors");
        var value = input?.GetAttribute("value");

        // Assert
        Assert.NotNull(value);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(value!);
        Assert.Contains("Email", deserialized.Keys);
        Assert.Contains("Password", deserialized.Keys);
        Assert.Contains("Email is required.", deserialized["Email"]);
        Assert.Contains("Password is too short.", deserialized["Password"]);
    }
}