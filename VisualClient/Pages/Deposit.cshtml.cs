using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyClient.JSON_Converter;

public class DepositModel : PageModel
{
    private readonly AtmClientService _atm;
    public DepositModel(AtmClientService atm) => _atm = atm;

    [BindProperty] public decimal Sum { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var resp = await _atm.SendAsync(new RequestDeposit { Sum = Sum });

        if (resp?.PassCode == 1945)
        {
            return RedirectToPage("Success");
        }
        else if (resp?.PassCode == 1111)
        {
            ErrorMessage = "Cума поповнення перевищує ліміт 100 000 грн";
            return Page();
        }
        else
        {
            ErrorMessage = "Операція не пройшла.";
            return Page();
        }
            
    }
}