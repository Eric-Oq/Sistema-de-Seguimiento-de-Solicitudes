using CurrieTechnologies.Razor.SweetAlert2;

namespace Sistema_de_Seguimiento_de_Solicitudes.Services;

public sealed class AppMessageService
{
    private readonly SweetAlertService _alerts;

    public AppMessageService(SweetAlertService alerts)
    {
        _alerts = alerts;
    }

    public Task SuccessAsync(string message) => ShowAsync("Operación completada", message, SweetAlertIcon.Success);

    public Task ErrorAsync(string message) => ShowAsync("Ocurrió un error", message, SweetAlertIcon.Error);

    public Task WarningAsync(string message) => ShowAsync("Aviso", message, SweetAlertIcon.Warning);

    public Task InfoAsync(string message) => ShowAsync("Información", message, SweetAlertIcon.Info);

    public async Task<bool> ConfirmAsync(string message, string confirmText = "Aceptar")
    {
        var result = await _alerts.FireAsync(new SweetAlertOptions
        {
            Title = "Confirmar acción",
            Text = message,
            Icon = SweetAlertIcon.Warning,
            ShowCancelButton = true,
            ConfirmButtonText = confirmText,
            CancelButtonText = "Cancelar"
        });

        return result.IsConfirmed;
    }

    private async Task ShowAsync(string title, string message, SweetAlertIcon icon)
    {
        await _alerts.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Text = message,
            Icon = icon,
            ConfirmButtonText = "Aceptar"
        });
    }
}
