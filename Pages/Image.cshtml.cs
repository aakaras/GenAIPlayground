using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyChatApp.Pages;

public class ImageModel : PageModel
{
    private readonly ILogger<ImageModel> _logger;

    public ImageModel(ILogger<ImageModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}

