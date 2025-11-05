namespace Visitor.Web.Common;

public class ApplicationContext
{
    public List<string> Errors { get; set; } = new();
    public bool IsBusy { get; private set; }

    public void AddError(string error)
    {
        Errors.Add(error);
        OnError?.Invoke();
    }

    public void AddErrorOrErrors(List<ErrorOr.Error> errors)
    {
        Errors.AddRange(errors.Select(x => x.Description));
        OnError?.Invoke();
    }

    public Action? OnError { get; set; }
    public Action<bool>? IsBusyChanged { get; set; }

    public bool HasErrors() => Errors.Any();

    public void ClearErrors() => Errors.Clear();

    public void SetIsBusy()
    {
        IsBusy = true;
        IsBusyChanged?.Invoke(IsBusy);
    }

    public void SetIsNotBusy()
    {
        IsBusy = false;
        IsBusyChanged?.Invoke(IsBusy);
    }
}
