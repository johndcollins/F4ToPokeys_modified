using System.ComponentModel;

namespace F4ToPokeys
{
    public interface IDevice : INotifyPropertyChanged
    {
        string DisplayName { get; }
        string Error { get; }
    }
}
