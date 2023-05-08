namespace Group17PortalWasm.Services;

using Group17.Auth;

public class AppState
{
    public string? ErrorMessage;
    public string? SuccessMessage;
    public string? WarningMessage;

    public event Action? StateChanged;

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    public void DisplayError(APIException ex)
    {
        ErrorMessage = string.IsNullOrEmpty(ex.Message) ? $"An error occured: {ex.StatusCode}" : ex.Message;
        NotifyStateChanged();
    }

    public void DisplayError(string message)
    {
        ErrorMessage = message;
        NotifyStateChanged();
    }

    public void DisplayWarning(string message)
    {
        WarningMessage = message;
        NotifyStateChanged();
    }

    public void DisplaySuccess(string message)
    {
        SuccessMessage = message;
        NotifyStateChanged();
    }
}