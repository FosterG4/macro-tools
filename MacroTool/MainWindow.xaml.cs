using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace MacroTool
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<MacroTask> MacroTasks { get; set; } = new ObservableCollection<MacroTask>();
        private DispatcherTimer _timer;
        private bool _isRecordingKeys = false;
        private MacroTask? _recordingTask = null; // Track which task is being recorded
        
        // Temp storage for recording
        private Key _recordedKey;
        private ModifierKeys _recordedModifiers;
        private string _recordedFriendlyText = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            gridMacros.ItemsSource = MacroTasks;
            RefreshWindowList();
            LoadSettings();

            // Setup Timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200); // Check 5 times a second
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Key Preview for recording
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_recordingTask != null)
            {
                e.Handled = true;
                if (IsModifierKey(e.Key)) return;

                _recordingTask.Key = e.Key;
                _recordingTask.Modifiers = Keyboard.Modifiers;
                _recordingTask.FriendlyKeyText = GetFriendlyKeyText(e.Key, Keyboard.Modifiers);
                _recordingTask.IsRecording = false;
                _recordingTask = null;
                return;
            }

            if (_isRecordingKeys)
            {
                e.Handled = true;

                if (IsModifierKey(e.Key)) return;

                _recordedKey = e.Key;
                _recordedModifiers = Keyboard.Modifiers;
                _recordedFriendlyText = GetFriendlyKeyText(e.Key, Keyboard.Modifiers);

                txtKeys.Text = _recordedFriendlyText;

                // Stop recording
                _isRecordingKeys = false;
                btnRecordKeys.Content = "Set";
                btnRecordKeys.Background = Brushes.Transparent;
            }
        }

        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.System;
        }

        private string GetFriendlyKeyText(Key key, ModifierKeys modifiers)
        {
            StringBuilder sb = new StringBuilder();
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) sb.Append("Ctrl + ");
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) sb.Append("Alt + ");
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) sb.Append("Shift + ");
            
            sb.Append(key.ToString());
            return sb.ToString();
        }

        private void btnRecordKeys_Click(object sender, RoutedEventArgs e)
        {
            _isRecordingKeys = !_isRecordingKeys;
            
            if (_isRecordingKeys)
            {
                btnRecordKeys.Content = "...";
                btnRecordKeys.Background = Brushes.LightSalmon;
                txtKeys.Focus();
            }
            else
            {
                btnRecordKeys.Content = "Set";
                btnRecordKeys.Background = Brushes.Transparent;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                DateTime now = DateTime.Now;

                foreach (var task in MacroTasks)
                {
                    if (!task.IsRunning) 
                    {
                        task.Status = "Stopped";
                        continue;
                    }

                    // Background Execution: Find the target window handle
                    IntPtr targetHWnd = FindWindowByPartialTitle(task.TargetWindowTitle);

                    if (targetHWnd != IntPtr.Zero)
                    {
                        bool isMinimized = NativeMethods.IsIconic(targetHWnd);
                        task.Status = isMinimized ? "Running (Minimized)" : "Running (Visible)";
                        
                        if ((now - task.LastRunTime).TotalMilliseconds >= task.IntervalMs)
                        {
                            try 
                            {
                                if (task.UseHardwareInput)
                                {
                                    SendHardwareInput(targetHWnd, task.Key, task.Modifiers, task.FocusOnSend);
                                    task.Status = "Executed (Hardware)";
                                }
                                else
                                {
                                    if (task.FocusOnSend) EnsureForegroundForHardware(targetHWnd);
                                    SendBackgroundInput(targetHWnd, task.Key, task.Modifiers);
                                    task.Status = (isMinimized ? "Executed (Minimized)" : "Executed (Visible)");
                                }
                                
                                task.LastRunTime = now;
                            }
                            catch (Exception ex)
                            {
                                task.Status = $"Error: {ex.Message}";
                            }
                        }
                    }
                    else
                    {
                        task.Status = "Target not found";
                    }
                }

                // Update Status Bar (Still useful to see what's active)
                IntPtr activeWnd = NativeMethods.GetForegroundWindow();
                string activeTitle = GetWindowTitle(activeWnd);
                lblActiveWindow.Text = string.IsNullOrEmpty(activeTitle) ? "(Unknown)" : activeTitle;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error in loop: " + ex.Message;
            }
        }

        private IntPtr FindWindowByPartialTitle(string partialTitle)
        {
            IntPtr foundHWnd = IntPtr.Zero;
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    string title = GetWindowTitle(hWnd);
                    if (!string.IsNullOrEmpty(title) && title.Contains(partialTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        foundHWnd = hWnd;
                        return false; // Stop enumeration
                    }
                }
                return true; // Continue
            }, IntPtr.Zero);
            return foundHWnd;
        }

        private void SendBackgroundInput(IntPtr hWnd, Key key, ModifierKeys modifiers)
        {
            // Robust check: If minimized, sometimes we need to restore logic or just post message.
            // PostMessage is usually sufficient for minimized windows as it puts msg in queue.
            
            // Convert WPF Key to Virtual Key
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            
            // Handle Modifiers
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYDOWN, (IntPtr)0x11, (IntPtr)0x001D0001); // VK_CONTROL
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYDOWN, (IntPtr)0x12, (IntPtr)0x20380001); // VK_MENU
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYDOWN, (IntPtr)0x10, (IntPtr)0x002A0001); // VK_SHIFT

            // Send Key
            // lParam is crucial for some apps (ScanCode, RepeatCount, etc.)
            // Constructing a basic lParam: RepeatCount=1 (0-15), ScanCode (16-23), etc.
            // For now, passing 0 is often okay, but 1 (RepeatCount) is safer.
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYDOWN, (IntPtr)virtualKey, (IntPtr)0x00000001);
            unchecked 
            {
                NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYUP, (IntPtr)virtualKey, (IntPtr)(int)0xC0000001);
            }

            // Release Modifiers
            unchecked
            {
                if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYUP, (IntPtr)0x10, (IntPtr)(int)0xC02A0001);
                if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYUP, (IntPtr)0x12, (IntPtr)(int)0xE0380001);
                if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYUP, (IntPtr)0x11, (IntPtr)(int)0xC01D0001);
            }
        }

        private void SendHardwareInput(IntPtr targetHWnd, Key key, ModifierKeys modifiers, bool focusFirst)
        {
            ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(key);
            ushort sc = (ushort)NativeMethods.MapVirtualKey(vk, 0);

            if (focusFirst) EnsureForegroundForHardware(targetHWnd);

            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) SendKeyScan(0x1D, false, false);
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) SendKeyScan(0x38, false, true);
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) SendKeyScan(0x2A, false, false);
            Thread.Sleep(15);

            SendKeyScan(sc, false, false);
            Thread.Sleep(20);
            SendKeyScan(sc, true, false);
            Thread.Sleep(15);

            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) SendKeyScan(0x2A, true, false);
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) SendKeyScan(0x38, true, true);
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) SendKeyScan(0x1D, true, false);
            Thread.Sleep(10);
        }

        private void SendKeyScan(ushort scanCode, bool isKeyUp, bool extended)
        {
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = (extended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0) | NativeMethods.KEYEVENTF_SCANCODE | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private void EnsureForegroundForHardware(IntPtr targetHWnd)
        {
            if (targetHWnd == IntPtr.Zero)
            {
                return;
            }

            uint targetThread = NativeMethods.GetWindowThreadProcessId(targetHWnd, out _);
            uint currentThread = NativeMethods.GetCurrentThreadId();

            NativeMethods.ShowWindowAsync(targetHWnd, NativeMethods.SW_SHOW);
            NativeMethods.ShowWindow(targetHWnd, NativeMethods.SW_RESTORE);

            NativeMethods.AttachThreadInput(currentThread, targetThread, true);
            NativeMethods.SetForegroundWindow(targetHWnd);
            NativeMethods.BringWindowToTop(targetHWnd);
            NativeMethods.SetFocus(targetHWnd);
            NativeMethods.AttachThreadInput(currentThread, targetThread, false);
        }

        private string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            if (NativeMethods.GetWindowText(hWnd, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return string.Empty;
        }

        private void cmbWindows_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This method is intentionally left empty as we're using the text for filtering
            // The actual filtering is handled by the ComboBox's built-in search functionality
            // since we've set IsTextSearchEnabled="True"
        }

        private void RefreshWindowList()
        {
            var windows = new List<string>();
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    string title = GetWindowTitle(hWnd);
                    if (!string.IsNullOrWhiteSpace(title) && 
                        !title.Equals("Program Manager") && 
                        !title.Equals("Settings") &&
                        !title.Equals("Microsoft Text Input Application") &&
                        !title.StartsWith("MSCTFIME"))
                    {
                        if (!windows.Contains(title))
                        {
                            windows.Add(title);
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            // Update UI on the UI thread
            Dispatcher.Invoke(() =>
            {
                string currentText = cmbWindows.Text;
                cmbWindows.ItemsSource = windows
                    .OrderBy(w => w)
                    .Where(w => string.IsNullOrEmpty(currentText) || 
                              w.IndexOf(currentText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            });
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindowList();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string target = cmbWindows.Text;
            
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(_recordedFriendlyText))
            {
                MessageBox.Show("Please select a target window and set keys.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtInterval.Text, out int interval) || interval < 1)
            {
                MessageBox.Show("Please enter a valid interval (ms).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MacroTasks.Add(new MacroTask
            {
                TargetWindowTitle = target,
                Key = _recordedKey,
                Modifiers = _recordedModifiers,
                FriendlyKeyText = _recordedFriendlyText,
                IntervalMs = interval,
                IsRunning = false,
                FocusOnSend = true,
                UseHardwareInput = true,
                LastRunTime = DateTime.Now,
                Status = "Stopped"
            });

            // Clear inputs
            txtKeys.Text = "";
            _recordedFriendlyText = "";
            cmbWindows.Text = "";
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MacroTask task)
            {
                MacroTasks.Remove(task);
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MacroTask task)
            {
                try
                {
                     IntPtr targetHWnd = FindWindowByPartialTitle(task.TargetWindowTitle);
                     if (targetHWnd != IntPtr.Zero)
                     {
                         if (task.UseHardwareInput)
                         {
                             // Warn user: Focus will be stolen or they need to focus fast
                             // For test button, we just fire it.
                             SendHardwareInput(targetHWnd, task.Key, task.Modifiers, task.FocusOnSend);
                             MessageBox.Show("Hardware Input Sent! (Ensure target was focused if it failed)", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                         }
                         else
                         {
                             SendBackgroundInput(targetHWnd, task.Key, task.Modifiers);
                             MessageBox.Show("Background Input Sent!", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                         }
                     }
                     else
                     {
                         MessageBox.Show("Target window not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                     }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending input: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private static string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MacroTool");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "macros.json");
        }

        private void SaveSettings()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var data = MacroTasks.ToList();
            var path = GetSettingsPath();
            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
        }

        private void LoadSettings()
        {
            var path = GetSettingsPath();
            if (!File.Exists(path)) return;
            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };
                var json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<MacroTask>>(json, options);
                if (list != null)
                {
                    MacroTasks.Clear();
                    foreach (var t in list)
                    {
                        t.LastRunTime = DateTime.Now;
                        t.Status = "Ready";
                        MacroTasks.Add(t);
                    }
                }
            }
            catch
            {
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void txtKeys_GotFocus(object sender, RoutedEventArgs e)
        {
            // Removed auto-record to match mockup's explicit "Set" button logic
        }

        private void txtKeys_LostFocus(object sender, RoutedEventArgs e)
        {
            // We keep recording until a key is pressed or user manually stops it
            // but for UX, we might want to visual feedback
        }

        private void RowKeys_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MacroTask task)
            {
                // Toggle recording for this row
                if (_recordingTask == task)
                {
                    // Cancel recording
                    task.IsRecording = false;
                    _recordingTask = null;
                }
                else
                {
                    // Cancel any other recording
                    if (_recordingTask != null)
                        _recordingTask.IsRecording = false;

                    // Start recording for this task
                    _recordingTask = task;
                    task.IsRecording = true;
                    
                    // Force focus to main window to ensure we catch key events
                    this.Focus();
                }
            }
        }
    }
}
