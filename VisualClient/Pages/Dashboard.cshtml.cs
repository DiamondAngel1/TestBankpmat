using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyClient.JSON_Converter;
using MyPrivate.JSON_Converter;

public class DashboardModel : PageModel
{
    private readonly AtmClientService _atm;
    public DashboardModel(AtmClientService atm) => _atm = atm;

    public string UserName { get; set; } = "�볺��";
    public string? BalanceMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (HttpContext.Session.GetString("Authorized") != "true")
            return RedirectToPage("LoginCard");

        UserName = HttpContext.Session.GetString("UserName") ?? "�볺��";

        var resp = await _atm.SendAsync(new RequestType5());

        if (resp?.PassCode == 1945)
            BalanceMessage = resp.Comment;
        else
            BalanceMessage = "������� ��������� �������";

        return Page();
    }
}