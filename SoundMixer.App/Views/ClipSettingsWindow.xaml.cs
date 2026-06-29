using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SoundMixer.App.Helpers;
using SoundMixer.App.ViewModels;

namespace SoundMixer.App.Views;

public partial class ClipSettingsWindow : Window
{
    private readonly SoundClipViewModel _clipVm;
    private string? _newHotKey;
    private string? _newEmoji;

    public bool WasDeleted { get; private set; }
    public bool WasSaved { get; private set; }
    public bool WantsTrim { get; private set; }
    public bool WantsExport { get; private set; }

    public ClipSettingsWindow(SoundClipViewModel clipVm)
    {
        InitializeComponent();
        _clipVm = clipVm;

        _newEmoji = clipVm.Emoji;
        _newHotKey = clipVm.HotKeyRaw;

        EmojiButton.DataContext = clipVm;
        NameInput.Text = clipVm.Name;
        VolumeSlider.Value = clipVm.Volume;
        LoopCheck.IsChecked = clipVm.Loop;
        HotKeyBox.Text = clipVm.HotKeyDisplay;
        UpdateVolumeLabel();
        UpdateTrimInfo();

        VolumeSlider.ValueChanged += (s, e) => UpdateVolumeLabel();
    }

    private void UpdateVolumeLabel()
    {
        VolumeLabel.Text = $"{VolumeSlider.Value:P0}";
    }

    private void UpdateTrimInfo()
    {
        TrimInfoText.Text = _clipVm.IsTrimmed ? _clipVm.TrimInfo : "Без обрезки";
    }

    private void PickEmoji_Click(object sender, RoutedEventArgs e)
    {
        var picker = new EmojiPickerWindow { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedEmoji != null)
        {
            _newEmoji = picker.SelectedEmoji;
            _clipVm.Emoji = _newEmoji;
        }
    }

    private void HotKeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        HotKeyBox.Text = "Нажмите комбинацию...";
    }

    private void HotKeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin) return;
        if (key == Key.Escape) { HotKeyBox.Text = _clipVm.HotKeyDisplay; Keyboard.ClearFocus(); return; }

        string hotKeyString = GlobalHotKeyManager.KeyEventToString(e);
        if (!string.IsNullOrEmpty(hotKeyString))
        {
            _newHotKey = hotKeyString;
            HotKeyBox.Text = hotKeyString;
        }
        Keyboard.ClearFocus();
    }

    private void ClearHotKey_Click(object sender, RoutedEventArgs e)
    {
        _newHotKey = null;
        HotKeyBox.Text = "Нет";
    }

    private void Trim_Click(object sender, RoutedEventArgs e)
    {
        WantsTrim = true;
        WasSaved = true;
        ApplyValues();
        DialogResult = true;
        Close();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        WantsExport = true;
        WasSaved = true;
        ApplyValues();
        DialogResult = true;
        Close();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show($"Удалить \"{_clipVm.EffectiveName}\"?", "Удаление",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            WasDeleted = true;
            DialogResult = true;
            Close();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        WasSaved = true;
        ApplyValues();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ApplyValues()
    {
        _clipVm.Name = NameInput.Text;
        _clipVm.Volume = (float)VolumeSlider.Value;
        _clipVm.Loop = LoopCheck.IsChecked ?? false;
        _clipVm.HotKeyRaw = _newHotKey;
        if (_newEmoji != null) _clipVm.Emoji = _newEmoji;
    }
}