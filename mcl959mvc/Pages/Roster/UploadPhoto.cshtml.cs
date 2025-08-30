using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace mcl959mvc.Pages.Roster;

public class UploadPhotoModel : PageModel
{
    private readonly ILogger<UploadPhotoModel> _logger;
    private readonly IWebHostEnvironment _environment;

    public UploadPhotoModel(ILogger<UploadPhotoModel> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public IFormFile? Photo { get; set; }

    public string? Message { get; set; }

    public void OnGet(int id)
    {
        Id = id;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Photo != null && 0 < Photo.Length)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "photos");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            var filePath = Path.Combine(uploadsFolder, $"{Id}.jpg");
            try
            {
                using (var image = Image.Load(Photo.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(600, 600) // Adjust size as needed
                    }));
                    await image.SaveAsJpegAsync(filePath);
                }
                _logger.LogInformation($"New photo: {filePath}");
                Message = "Photo uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for ID {Id}", Id);
                Message = "Error uploading photo. Please try again.";
            }
        }
        else
        {
            Message = "No file selected.";
        }
        return Content(Message, "text/plain");
    }
}
