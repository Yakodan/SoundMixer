using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SoundMixer.App.Helpers;

/// <summary>
/// Управляет глобальными горячими клавишами Windows.
/// Работает даже когда приложение не в фокусе.
/// </summary>
public class GlobalHotKeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    // Модификаторы
    public const uint MOD_NONE = 0x0000;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    private IntPtr _windowHandle;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _hotKeyActions = new();
    private readonly Dictionary<string, int> _hotKeyIds = new();
    private int _nextId = 9000;

    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;

        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);
    }

    /// <summary>
    /// Зарегистрировать горячую клавишу.
    /// hotKeyString формат: "Ctrl+Shift+F1", "Alt+Q", "F5" и т.д.
    /// </summary>
    public bool Register(string clipId, string hotKeyString, Action callback)
    {
        if (_windowHandle == IntPtr.Zero) return false;

        // Сначала удаляем старую привязку если была
        Unregister(clipId);

        // Парсим строку
        if (!TryParseHotKey(hotKeyString, out uint modifiers, out uint vk))
            return false;

        int id = _nextId++;

        if (RegisterHotKey(_windowHandle, id, modifiers | MOD_NOREPEAT, vk))
        {
            _hotKeyActions[id] = callback;
            _hotKeyIds[clipId] = id;

            System.Diagnostics.Debug.WriteLine(
                $"[HotKey] Зарегистрирован: {hotKeyString} (id={id}) для {clipId}");
            return true;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[HotKey] Не удалось зарегистрировать: {hotKeyString}");
        return false;
    }

    /// <summary>
    /// Снять регистрацию горячей клавиши.
    /// </summary>
    public void Unregister(string clipId)
    {
        if (_hotKeyIds.TryGetValue(clipId, out int id))
        {
            UnregisterHotKey(_windowHandle, id);
            _hotKeyActions.Remove(id);
            _hotKeyIds.Remove(clipId);
        }
    }

    /// <summary>
    /// Снять все горячие клавиши.
    /// </summary>
    public void UnregisterAll()
    {
        foreach (var kvp in _hotKeyIds)
        {
            UnregisterHotKey(_windowHandle, kvp.Value);
        }
        _hotKeyActions.Clear();
        _hotKeyIds.Clear();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotKeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Парсит строку вида "Ctrl+Shift+F1" в модификаторы и виртуальный код.
    /// </summary>
    public static bool TryParseHotKey(string hotKeyString, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(hotKeyString))
            return false;

        var parts = hotKeyString.Split('+').Select(p => p.Trim()).ToArray();

        foreach (var part in parts)
        {
            switch (part.ToUpper())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                case "WIN":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    // Это должна быть клавиша
                    if (Enum.TryParse<Key>(part, true, out var key))
                    {
                        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                    }
                    else if (uint.TryParse(part, out uint numericVk))
                    {
                        vk = numericVk;
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
        }

        return vk != 0;
    }

    /// <summary>
    /// Конвертировать нажатие клавиш в строку.
    /// </summary>
    public static string KeyEventToString(KeyEventArgs e)
    {
        var parts = new List<string>();

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Пропускаем сами модификаторы
        if (key != Key.LeftCtrl && key != Key.RightCtrl &&
            key != Key.LeftAlt && key != Key.RightAlt &&
            key != Key.LeftShift && key != Key.RightShift &&
            key != Key.LWin && key != Key.RWin)
        {
            parts.Add(key.ToString());
        }

        return string.Join("+", parts);
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(HwndHook);
    }
}