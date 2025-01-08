using System.Windows.Input;
using HourglassManager.WPF.ViewModels;
using HourglassManager.WPF.Commands;

namespace HourglassManager.WPF.ViewModels
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