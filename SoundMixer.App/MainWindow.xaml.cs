using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using SoundMixer.App.Helpers;
using SoundMixer.App.ViewModels;
using SoundMixer.App.Views;
using SoundMixer.Core.Audio;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SoundMixer.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly GlobalHotKeyManager _hotKeyManager;
    private TaskbarIcon? _trayIcon;
    private bool _isExiting = false;
    private AudioEngine _engine => _viewModel.Engine;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        _hotKeyManager = new GlobalHotKeyManager();
        DataContext = _viewModel;

        _viewModel.TrimClipRequested += OnTrimClipRequested;

        Loaded += OnWindowLoaded;
        StateChanged += OnStateChanged;
        Closing += OnWindowClosing;
    }

    private void OpenClipSettings_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        if (el.Tag is not SoundClipViewModel clipVm) return;

        var settingsWin = new ClipSettingsWindow(clipVm) { Owner = this };
        if (settingsWin.ShowDialog() == true)
        {
            if (settingsWin.WasDeleted)
            {
                _viewModel.RemoveSoundClipCommand.Execute(clipVm.Id);
                return;
            }

            if (settingsWin.WasSaved)
            {
                // Перерегистрируем хоткей
                _hotKeyManager.Unregister(clipVm.Id);
                if (!string.IsNullOrEmpty(clipVm.HotKeyRaw))
                {
                    var clipId = clipVm.Id;
                    _hotKeyManager.Register(clipId, clipVm.HotKeyRaw, () =>
                    {
                        Dispatcher.BeginInvoke(() => _viewModel.Engine.Soundboard.ToggleClip(clipId));
                    });
                }

                _viewModel.ForceSave();
            }

            if (settingsWin.WantsTrim)
            {
                OnTrimClipRequested(null, clipVm);
            }

            if (settingsWin.WantsExport)
            {
                _viewModel.ExportClipCommand.Execute(clipVm.Id);
            }
        }
    }

    // ==================== DRAG & DROP ====================

    private SoundClipViewModel? _draggedClip;

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;
        if (border.Tag is not SoundClipViewModel clipVm) return;

        _draggedClip = clipVm;
        var data = new DataObject("SoundClipVM", clipVm);
        DragDrop.DoDragDrop(border, data, DragDropEffects.Move);
    }

    private void SoundboardPanel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("SoundClipVM"))
            e.Effects = DragDropEffects.Move;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private void SoundboardPanel_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("SoundClipVM")) return;
        if (_draggedClip == null) return;

        var pos = e.GetPosition(SoundClipsList);
        int targetIndex = GetDropIndex(pos);

        int sourceIndex = _viewModel.SoundClips.IndexOf(_draggedClip);
        if (sourceIndex < 0 || sourceIndex == targetIndex) return;

        _viewModel.SoundClips.Move(sourceIndex, Math.Clamp(targetIndex, 0, _viewModel.SoundClips.Count - 1));
        _viewModel.ForceSave();
        _draggedClip = null;
    }

    private int GetDropIndex(System.Windows.Point position)
    {
        // Простой расчёт на основе позиции
        int columns = Math.Max(1, (int)(SoundClipsList.ActualWidth / 138));
        int col = (int)(position.X / 138);
        int row = (int)(position.Y / 168);
        int index = row * columns + col;
        return Math.Clamp(index, 0, _viewModel.SoundClips.Count);
    }

    private void OnTrimClipRequested(object? sender, SoundClipViewModel clipVm)
    {
        var editor = new TrimEditorWindow(clipVm.Clip) { Owner = this };
        if (editor.ShowDialog() == true && editor.Applied)
        {
            clipVm.Clip.SetTrim(editor.ResultTrimStart, editor.ResultTrimEnd);
            clipVm.RefreshTrimInfo();
            _viewModel.ForceSave();
            _viewModel.StatusMessage = $"Trim applied: {clipVm.EffectiveName}";
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _hotKeyManager.Initialize(this);
        RegisterAllHotKeys();
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        var contextMenu = new ContextMenu();

        var showItem = new MenuItem { Header = "Показать окно" };
        showItem.Click += (s, e) => ShowFromTray();

        var startStopItem = new MenuItem { Header = "Запустить/Остановить" };
        startStopItem.Click += (s, e) => _viewModel.StartStopCommand.Execute(null);

        var autoStartItem = new MenuItem
        {
            Header = "Автозагрузка",
            IsCheckable = true,
            IsChecked = IsInStartup()
        };
        autoStartItem.Click += (s, e) => ToggleAutoStart(autoStartItem.IsChecked);

        var exitItem = new MenuItem { Header = "Выход" };
        exitItem.Click += (s, e) => ExitApplication();

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(startStopItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "SoundMixer Pro",
            ContextMenu = contextMenu,
            Visibility = Visibility.Visible
        };

        try
        {
            var iconUri = new Uri("pack://application:,,,/Resources/Icons/app.ico");
            var iconStream = Application.GetResourceStream(iconUri)?.Stream;
            if (iconStream != null)
                _trayIcon.Icon = new Icon(iconStream);
        }
        catch
        {
            _trayIcon.Icon = SystemIcons.Application;
        }

        _trayIcon.TrayMouseDoubleClick += (s, e) => ShowFromTray();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Сворачиваем в трей вместо закрытия
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    private void ExitApplication()
    {
        _isExiting = true;

        // 1. Сначала сохраняем
        try
        {
            _viewModel.ForceSave();
            System.Diagnostics.Debug.WriteLine("[Exit] Settings saved");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Exit] Save error: {ex.Message}");
        }

        // 2. Потом освобождаем ресурсы
        _hotKeyManager.Dispose();
        _trayIcon?.Dispose();
        _viewModel.Dispose();

        // 3. Закрываем
        Application.Current.Shutdown();
    }

    // ==================== AUTO START ====================

    private const string AppRegistryName = "SoundMixerPro";

    private static bool IsInStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppRegistryName) != null;
        }
        catch { return false; }
    }

    private static void ToggleAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppRegistryName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(AppRegistryName, false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}");
        }
    }

    // ==================== HOT KEYS ====================

    private void RegisterAllHotKeys()
    {
        _hotKeyManager.UnregisterAll();
        foreach (var clipVm in _viewModel.SoundClips)
        {
            if (!string.IsNullOrEmpty(clipVm.HotKeyRaw))
            {
                var clipId = clipVm.Id;
                _hotKeyManager.Register(clipId, clipVm.HotKeyRaw, () =>
                {
                    Dispatcher.BeginInvoke(() => _viewModel.Engine.Soundboard.ToggleClip(clipId));
                });
            }
        }
    }

    private void HotKeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            textBox.Text = "Нажмите комбинацию...";
    }

    private void HotKeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (textBox.Tag is not SoundClipViewModel clipVm) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin) return;

        if (key == Key.Escape) { textBox.Text = clipVm.HotKeyDisplay; Keyboard.ClearFocus(); return; }

        string hotKeyString = GlobalHotKeyManager.KeyEventToString(e);
        if (!string.IsNullOrEmpty(hotKeyString))
        {
            _hotKeyManager.Unregister(clipVm.Id);
            clipVm.HotKeyRaw = hotKeyString;
            textBox.Text = hotKeyString;
            var clipId = clipVm.Id;
            bool success = _hotKeyManager.Register(clipId, hotKeyString, () =>
            {
                Dispatcher.BeginInvoke(() => _viewModel.Engine.Soundboard.ToggleClip(clipId));
            });
            _viewModel.StatusMessage = success ? $"Hotkey: {hotKeyString}" : $"Failed: {hotKeyString}";
        }
        Keyboard.ClearFocus();
    }

    private void ClearHotKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not SoundClipViewModel clipVm) return;
        _hotKeyManager.Unregister(clipVm.Id);
        clipVm.HotKeyRaw = null;
    }

    private void PickEmoji_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not SoundClipViewModel clipVm) return;
        var picker = new EmojiPickerWindow { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedEmoji != null)
            clipVm.Emoji = picker.SelectedEmoji;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_isExiting)
        {
            // Если OnClosed вызвался без ExitApplication (например Alt+F4 дважды)
            try { _viewModel.ForceSave(); } catch { }
        }
        _hotKeyManager.Dispose();
        _trayIcon?.Dispose();
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}