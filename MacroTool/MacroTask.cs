using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MacroTool
{
    public class MacroTask : INotifyPropertyChanged
    {
        private string _targetWindowTitle = string.Empty;
        private Key _key;
        private ModifierKeys _modifiers;
        private string _friendlyKeyText = string.Empty;
        private int _intervalMs;
        private bool _isRunning;
        private bool _isRecording;
        private DateTime _lastRunTime;
        private string _status = string.Empty;
        private bool _useHardwareInput;
        private bool _focusOnSend;

        public string TargetWindowTitle
        {
            get => _targetWindowTitle;
            set { _targetWindowTitle = value; OnPropertyChanged(); }
        }

        public bool UseHardwareInput
        {
            get => _useHardwareInput;
            set { _useHardwareInput = value; OnPropertyChanged(); }
        }

        public bool FocusOnSend
        {
            get => _focusOnSend;
            set { _focusOnSend = value; OnPropertyChanged(); }
        }

        public Key Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public ModifierKeys Modifiers
        {
            get => _modifiers;
            set { _modifiers = value; OnPropertyChanged(); }
        }

        public string FriendlyKeyText
        {
            get => _friendlyKeyText;
            set { _friendlyKeyText = value; OnPropertyChanged(); }
        }

        public int IntervalMs
        {
            get => _intervalMs;
            set { _intervalMs = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set { _isRecording = value; OnPropertyChanged(); }
        }

        public DateTime LastRunTime
        {
            get => _lastRunTime;
            set { _lastRunTime = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
