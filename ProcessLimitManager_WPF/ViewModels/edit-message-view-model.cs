using System.Windows.Input;
using ProcessLimitManager.WPF.ViewModels;
using ProcessLimitManager.WPF.Commands;

namespace ProcessLimitManager.WPF.ViewModels
{
    public class EditMessageViewModel : ViewModelBase
    {
        private string _message;

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand SaveCommand { get; }

        public EditMessageViewModel(string currentMessage)
        {
            Message = currentMessage;
            SaveCommand = new RelayCommand(
                execute: _ => { /* Save action will be handled by the window */ },
                canExecute: _ => !string.IsNullOrWhiteSpace(Message)
            );
        }
    }
}