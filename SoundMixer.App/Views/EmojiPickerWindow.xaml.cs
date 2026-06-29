using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SoundMixer.App.Views;

public partial class EmojiPickerWindow : Window
{
    public string? SelectedEmoji { get; private set; }
    private string _currentCategory = "Все";
    private readonly Dictionary<string, string[]> _categories;
    private readonly Dictionary<string, string> _emojiNames;

    public EmojiPickerWindow()
    {
        _categories = BuildEmojiCategories();
        _emojiNames = BuildEmojiNames();
        InitializeComponent();
        BuildCategoryButtons();
        ShowCategory("Звуки");
    }

    private void BuildCategoryButtons()
    {
        foreach (var category in _categories.Keys)
        {
            var btn = new Button
            {
                Content = category,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                Tag = category
            };
            btn.Click += (s, e) => { if (s is Button b && b.Tag is string cat) ShowCategory(cat); };
            CategoryPanel.Children.Add(btn);
        }
    }

    private void ShowCategory(string category)
    {
        _currentCategory = category;
        EmojiGrid.Items.Clear();

        string[] emojis = category == "Все"
            ? _categories.Values.SelectMany(v => v).Distinct().ToArray()
            : _categories.TryGetValue(category, out var c) ? c : Array.Empty<string>();

        string search = SearchBox.Text.Trim().ToLower();

        foreach (var emoji in emojis)
        {
            if (!string.IsNullOrEmpty(search))
            {
                bool match = emoji.Contains(search);
                if (!match && _emojiNames.TryGetValue(emoji, out var name))
                    match = name.ToLower().Contains(search);
                if (!match) continue;
            }

            var btn = new Button
            {
                Content = emoji,
                FontSize = 26,
                Width = 48,
                Height = 48,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                ToolTip = _emojiNames.TryGetValue(emoji, out var tip) ? tip : emoji
            };

            btn.Click += (s, e) => { SelectedEmoji = emoji; DialogResult = true; Close(); };
            btn.MouseEnter += (s, e) =>
            {
                SelectedPreview.Text = emoji;
                btn.Background = new SolidColorBrush(Color.FromArgb(40, 100, 100, 255));
            };
            btn.MouseLeave += (s, e) => { btn.Background = Brushes.Transparent; };

            EmojiGrid.Items.Add(btn);
        }

        foreach (Button catBtn in CategoryPanel.Children)
        {
            catBtn.Background = catBtn.Tag?.ToString() == category
                ? new SolidColorBrush(Color.FromRgb(108, 99, 255))
                : new SolidColorBrush(Color.FromRgb(45, 45, 68));
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ShowCategory(_currentCategory);
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private static Dictionary<string, string[]> BuildEmojiCategories()
    {
        return new Dictionary<string, string[]>
        {
            ["Звуки"] = new[] { "🔊", "🔉", "🔈", "🔇", "📢", "📣", "🔔", "🔕", "🎵", "🎶", "🎼", "🎧", "🎤", "🎙️", "🎺", "🎸", "🎹", "🥁", "🎷", "🎻", "🪗", "🪘", "📻" },
            ["Люди"] = new[] { "😀", "😂", "🤣", "😊", "😎", "🤔", "😱", "😡", "🥺", "😭", "🤯", "🥳", "😴", "🤢", "🤮", "💀", "👻", "🤡", "👽", "🤖", "💩", "👋", "👍", "👎", "👏", "🙌", "🤝", "🙏", "💪", "🦾", "🖕", "✌️" },
            ["Животные"] = new[] { "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", "🦁", "🐮", "🐷", "🐸", "🐵", "🐔", "🐧", "🐦", "🦆", "🦅", "🦉", "🐺", "🐗", "🐴", "🦄", "🐝", "🐛", "🦋", "🐌", "🐙", "🦑", "🐠" },
            ["Предметы"] = new[] { "💣", "🔫", "💰", "💎", "🏆", "🎯", "🎲", "🎮", "🕹️", "🎰", "🚗", "🚀", "✈️", "🛸", "⚡", "🔥", "💥", "💫", "⭐", "🌟", "✨", "🎆", "🎇", "🎉", "🎊", "🎈", "🎁", "🏅", "🥇", "🥈", "🥉", "⚽" },
            ["Еда"] = new[] { "🍎", "🍕", "🍔", "🌭", "🍟", "🌮", "🍿", "🍩", "🍪", "🎂", "🍰", "🍫", "🍬", "🍭", "🍺", "🍻", "🥂", "🍷", "☕", "🧃", "🥤", "🧁", "🍣", "🍜" },
            ["Символы"] = new[] { "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "💔", "❣️", "💕", "💗", "💖", "💘", "💝", "💟", "☮️", "✝️", "☪️", "♾️", "💲", "⚠️", "🚫", "❌", "✅", "❓", "❗", "💯", "🔴", "🟢", "🔵", "🟡" },
        };
    }

    private static Dictionary<string, string> BuildEmojiNames()
    {
        return new Dictionary<string, string>
        {
            ["🔊"] = "loud speaker",
            ["🔉"] = "speaker medium",
            ["🔇"] = "muted",
            ["📢"] = "loudspeaker",
            ["🔔"] = "bell",
            ["🎵"] = "music note",
            ["🎧"] = "headphones",
            ["🎤"] = "microphone",
            ["🎸"] = "guitar",
            ["🎹"] = "piano",
            ["🥁"] = "drum",
            ["😀"] = "happy",
            ["😂"] = "laugh",
            ["😎"] = "cool",
            ["😱"] = "scream",
            ["💀"] = "skull",
            ["👻"] = "ghost",
            ["🤖"] = "robot",
            ["💣"] = "bomb",
            ["🔫"] = "gun",
            ["💎"] = "diamond",
            ["🏆"] = "trophy",
            ["🎮"] = "game",
            ["🚀"] = "rocket",
            ["🔥"] = "fire",
            ["💥"] = "explosion",
            ["✨"] = "sparkles",
            ["🎉"] = "party",
        };
    }
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int length) return length == 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}