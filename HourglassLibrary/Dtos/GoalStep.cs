using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HourglassLibrary.Dtos
{
    public class GoalStep : INotifyPropertyChanged
    {
        private int _stepId;
        private int _stepOrder;
        private string _text;

        public int StepId
        {
            get => _stepId;
            set
            {
                if (_stepId != value)
                {
                    _stepId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int StepOrder
        {
            get => _stepOrder;
            set
            {
                if (_stepOrder != value)
                {
                    _stepOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}