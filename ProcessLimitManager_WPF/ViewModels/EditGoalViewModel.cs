using AppLimiterLibrary.Dtos;
using ProcessLimitManager.WPF.Commands;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;


namespace ProcessLimitManager.WPF.ViewModels
{
    public class EditGoalViewModel : ViewModelBase
    {
        private string _goalText;
        private string _newStepText;
        private readonly MotivationalMessage _originalGoal;
        private ObservableCollection<GoalStep> _steps;
        private bool _isNewGoal;

        public string GoalText
        {
            get => _goalText;
            set
            {
                if (SetProperty(ref _goalText, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string NewStepText
        {
            get => _newStepText;
            set
            {
                if (SetProperty(ref _newStepText, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ObservableCollection<GoalStep> Steps
        {
            get => _steps;
            private set => SetProperty(ref _steps, value);
        }

        public bool IsNewGoal
        {
            get => _isNewGoal;
            private set => SetProperty(ref _isNewGoal, value);
        }

        // Commands
        public ICommand AddStepCommand { get; }
        public ICommand RemoveStepCommand { get; }
        public ICommand MoveStepUpCommand { get; }
        public ICommand MoveStepDownCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditGoalViewModel(MotivationalMessage goal = null)
        {
            _originalGoal = goal;
            IsNewGoal = goal == null;
            Steps = new ObservableCollection<GoalStep>();

            // Initialize commands
            AddStepCommand = new RelayCommand(_ => AddStep(), _ => CanAddStep());
            RemoveStepCommand = new RelayCommand(RemoveStep);
            MoveStepUpCommand = new RelayCommand(MoveStepUp, CanMoveStepUp);
            MoveStepDownCommand = new RelayCommand(MoveStepDown, CanMoveStepDown);
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());

            InitializeFromGoal();
        }

        private void InitializeFromGoal()
        {
            if (_originalGoal != null)
            {
                var (goal, steps) = ParseGoalMessage(_originalGoal.Message);
                GoalText = goal;
                foreach (var step in steps)
                {
                    Steps.Add(new GoalStep { Index = Steps.Count + 1, Text = step });
                }
            }
        }

        private (string goal, List<string> steps) ParseGoalMessage(string message)
        {
            var parts = message.Split(new[] { "\n\nSteps to achieve this goal:\n" },
                StringSplitOptions.RemoveEmptyEntries);

            string goal = parts[0].Replace("Goal: ", "").Trim();
            var steps = new List<string>();

            if (parts.Length > 1)
            {
                steps = parts[1]
                    .Split('\n')
                    .Select(s => s.Substring(s.IndexOf(". ") + 2))
                    .ToList();
            }

            return (goal, steps);
        }

        private bool CanAddStep()
        {
            return !string.IsNullOrWhiteSpace(NewStepText);
        }

        private void AddStep()
        {
            Steps.Add(new GoalStep
            {
                Index = Steps.Count + 1,
                Text = NewStepText.Trim()
            });
            NewStepText = string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveStep(object parameter)
        {
            if (parameter is GoalStep step)
            {
                Steps.Remove(step);
                UpdateStepIndices();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanMoveStepUp(object parameter)
        {
            return parameter is GoalStep step && Steps.IndexOf(step) > 0;
        }

        private void MoveStepUp(object parameter)
        {
            if (parameter is GoalStep step)
            {
                int index = Steps.IndexOf(step);
                if (index > 0)
                {
                    Steps.Move(index, index - 1);
                    UpdateStepIndices();
                }
            }
        }

        private bool CanMoveStepDown(object parameter)
        {
            return parameter is GoalStep step && Steps.IndexOf(step) < Steps.Count - 1;
        }

        private void MoveStepDown(object parameter)
        {
            if (parameter is GoalStep step)
            {
                int index = Steps.IndexOf(step);
                if (index < Steps.Count - 1)
                {
                    Steps.Move(index, index + 1);
                    UpdateStepIndices();
                }
            }
        }

        private void UpdateStepIndices()
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].Index = i + 1;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(GoalText) && Steps.Any();
        }

        private void Save()
        {
            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        public MotivationalMessage GetUpdatedGoal()
        {
            if (!CanSave()) return null;

            var formattedMessage = FormatGoalMessage();

            if (_originalGoal != null)
            {
                _originalGoal.Message = formattedMessage;
                return _originalGoal;
            }

            return new MotivationalMessage
            {
                TypeId = 3, // Goal type
                TypeDescription = "Goal",
                Message = formattedMessage
            };
        }

        private string FormatGoalMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Goal: {GoalText.Trim()}");

            if (Steps.Any())
            {
                sb.AppendLine("\nSteps to achieve this goal:");
                foreach (var step in Steps)
                {
                    sb.AppendLine($"{step.Index}. {step.Text}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        public event Action<bool> RequestClose;
    }
}
