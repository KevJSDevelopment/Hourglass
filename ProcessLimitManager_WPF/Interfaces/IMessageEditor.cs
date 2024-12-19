namespace ProcessLimitManager.WPF.Interfaces
{
    public interface IMessageEditor
    {
        string UpdatedMessage { get; }
        bool? ShowDialog();
    }
}