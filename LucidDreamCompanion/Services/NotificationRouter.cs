namespace LucidDreamCompanion.Services;

public class NotificationRouter
{
    private string? _pending;
    public event Action<string>? RouteRequested;

    public void Request(string route)
    {
        _pending = route;
        RouteRequested?.Invoke(route);
    }

    public string? ConsumePending()
    {
        var p = _pending;
        _pending = null;
        return p;
    }
}
