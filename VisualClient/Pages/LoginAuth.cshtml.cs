using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MyClient.JSON_Converter;
using MyClient;

public class LoginAuthModel : PageModel
{
    private readonly AtmClientService _atm;
    public LoginAuthModel(AtmClientService atm) => _atm = atm;

    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string LastName { get; set; } = "";
    [BindProperty] public string FatherName { get; set; } = "";
    [BindProperty] public long PinCode { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var cardNumberStr = HttpContext.Session.GetString("CardNumber");

        if (string.IsNullOrEmpty(cardNumberStr) || !long.TryParse(cardNumberStr, out var cardNumber))
        {
            ErrorMessage = "Сеанс входу не знайдено. Спробуйте спочатку.";
            return RedirectToPage("/LoginCard");
        }

        var response = await _atm.SendAsync(new RequestAuthOrReg
        {
            FirstName = FirstName,
            LastName = LastName,
            FatherName = FatherName,
            PinCode = PinCode
        });

        if (response.PassCode == 1918)
        {
            ErrorMessage = "Вас заблоковано";
            return Page();
        }
        if (response == null || response.PassCode != 1945)
        {
            ErrorMessage = "Невірні дані. Спробуйте ще раз.";
            return Page();
        }
        

        HttpContext.Session.SetString("Authorized", "true");
        HttpContext.Session.SetString("UserName", FirstName);
        return RedirectToPage("/Dashboard");
    }
}
