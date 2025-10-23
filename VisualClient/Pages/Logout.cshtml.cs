using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VisualClient.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.Clear(); // повне очищення сесії
            return RedirectToPage("/Index"); // перенаправлення на авторизацію

        }
    }
}
