namespace PhantomTvSales.Web.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class ToastMessage
{
    public ToastMessage(string message, ToastLevel level)
    {
        Id = Guid.NewGuid();
        Message = message;
        Level = level;
    }

    public Guid Id { get; }
    public string Message { get; }
    public ToastLevel Level { get; }
}

public class ToastService
{
    private readonly List<ToastMessage> _messages = new();
    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? OnChange;

    public void Show(string message, ToastLevel level = ToastLevel.Info, int timeoutMs = 3000)
    {
        var toast = new ToastMessage(message, level);
        _messages.Add(toast);
        NotifyStateChanged();

        _ = DismissLaterAsync(toast.Id, timeoutMs);
    }

    public void ShowSuccess(string message, int timeoutMs = 3000) =>
        Show(message, ToastLevel.Success, timeoutMs);

    public void ShowError(string message, int timeoutMs = 4000) =>
        Show(message, ToastLevel.Error, timeoutMs);

    public void Dismiss(Guid id)
    {
        var idx = _messages.FindIndex(m => m.Id == id);
        if (idx >= 0)
        {
            _messages.RemoveAt(idx);
            NotifyStateChanged();
        }
    }

    private async Task DismissLaterAsync(Guid id, int timeoutMs)
    {
        await Task.Delay(timeoutMs);
        Dismiss(id);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
