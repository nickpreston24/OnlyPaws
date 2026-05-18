using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OnlyPaws.Pages.Pets;

public class Profile : PageModel
{
    public void OnGet()
    {
    }

    public Pet Pet { get; set; } = new Pet("", "");
}
