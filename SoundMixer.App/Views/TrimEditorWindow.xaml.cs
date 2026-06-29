using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SoundMixer.Core.Soundboard;

namespace SoundMixer.App.Views;

public partial class TrimEditorWindow : Window
{
    private readonly SoundClip _clip;
    private double _trimStartSeconds;
    private double _trimEndSeconds;
    private double _fullDurationSeconds;
    private bool _updatingInputs = false;

    private double _viewStartSeconds;
    private double _viewEndSeconds;

    private float[]? _waveformData;

    // Зум по таймеру
    private readonly DispatcherTimer _zoomTimer;
    private bool _isDragging = false;
    private double _currentMarkerPosition;
    private DateTime _lastBigMoveTime;
    private double _accumulatedDelta;

    public bool Applied { get; private set; }
    public double ResultTrimStart => _trimStartSeconds;
    public double ResultTrimEnd => _trimEndSeconds;

    public TrimEditorWindow(SoundClip clip)
    {
        InitializeComponent();
        _clip = clip;

        _fullDurationSeconds = clip.FullDuration.TotalSeconds;
        _trimStartSeconds = clip.Info.TrimStart;
        _trimEndSeconds = clip.Info.TrimEnd <= 0 ? _fullDurationSeconds : clip.Info.TrimEnd;

        _viewStartSeconds = 0;
        _viewEndSeconds = _fullDurationSeconds;

        TitleText.Text = $"Обрезка: {clip.Info.EffectiveName}";
        DurationText.Text = $"Полная длительность: {FormatTime(_fullDurationSeconds)}";

        _waveformData = GetNormalizedWaveform(800);

        // Таймер зума — проверяет каждые 100мс
        _zoomTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _zoomTimer.Tick += ZoomTimer_Tick;

        Loaded += OnLoaded;
        SizeChanged += (s, e) => RedrawAll();

        LeftThumb.DragStarted += Thumb_DragStarted;
        RightThumb.DragStarted += Thumb_DragStarted;
        LeftThumb.DragCompleted += Thumb_DragCompleted;
        RightThumb.DragCompleted += Thumb_DragCompleted;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RedrawAll();
    }

    // ==================== WAVEFORM ====================

    private float[] GetNormalizedWaveform(int points)
    {
        var raw = _clip.GetWaveformData(points);
        if (raw.Length == 0) return raw;

        float maxVal = 0;
        for (int i = 0; i < raw.Length; i++)
            if (raw[i] > maxVal) maxVal = raw[i];

        if (maxVal < 0.0001f) return raw;

        float[] result = new float[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            float normalized = raw[i] / maxVal;
            result[i] = (float)Math.Sqrt(normalized);
        }
        return result;
    }

    private void DrawWaveform()
    {
        WaveformCanvas.Children.Clear();
        double width = WaveformGrid.ActualWidth;
        double height = WaveformGrid.ActualHeight;

        if (width <= 0 || height <= 0 || _waveformData == null || _waveformData.Length == 0) return;

        double viewDuration = _viewEndSeconds - _viewStartSeconds;
        if (viewDuration <= 0) return;

        int totalPoints = _waveformData.Length;
        int startIdx = (int)(_viewStartSeconds / _fullDurationSeconds * totalPoints);
        int endIdx = (int)(_viewEndSeconds / _fullDurationSeconds * totalPoints);
        startIdx = Math.Clamp(startIdx, 0, totalPoints - 1);
        endIdx = Math.Clamp(endIdx, startIdx + 1, totalPoints);

        int visiblePoints = endIdx - startIdx;
        double barWidth = width / visiblePoints;
        double centerY = height / 2;

        var brush = FindResource("PrimaryBrush") as SolidColorBrush ?? Brushes.Green;

        for (int i = 0; i < visiblePoints; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx >= _waveformData.Length) break;

            double barHeight = _waveformData[dataIdx] * height * 0.9;
            if (barHeight < 1) barHeight = 1;

            var rect = new Rectangle
            {
                Width = Math.Max(barWidth - 0.3, 0.5),
                Height = barHeight,
                Fill = brush,
                Opacity = 0.8
            };

            Canvas.SetLeft(rect, i * barWidth);
            Canvas.SetTop(rect, centerY - barHeight / 2);
            WaveformCanvas.Children.Add(rect);
        }
    }

    // ==================== MARKERS ====================

    private void UpdateMarkers()
    {
        double width = WaveformGrid.ActualWidth;
        if (width <= 24) return;

        double viewDuration = _viewEndSeconds - _viewStartSeconds;
        if (viewDuration <= 0) return;

        double leftRatio = Math.Clamp((_trimStartSeconds - _viewStartSeconds) / viewDuration, 0, 1);
        double rightRatio = Math.Clamp((_trimEndSeconds - _viewStartSeconds) / viewDuration, 0, 1);

        double usableWidth = width - 24;
        LeftThumb.Margin = new Thickness(leftRatio * usableWidth, 0, 0, 0);
        RightThumb.Margin = new Thickness(0, 0, usableWidth - rightRatio * usableWidth, 0);

        UpdateOverlay();
        UpdateLabels();
        UpdateInputs();
    }

    private void UpdateOverlay()
    {
        TrimOverlayCanvas.Children.Clear();
        double width = WaveformGrid.ActualWidth;
        double height = WaveformGrid.ActualHeight;
        if (width <= 0 || height <= 0) return;

        double viewDuration = _viewEndSeconds - _viewStartSeconds;
        if (viewDuration <= 0) return;

        double leftX = Math.Max(0, (_trimStartSeconds - _viewStartSeconds) / viewDuration * width);
        double rightX = Math.Min(width, (_trimEndSeconds - _viewStartSeconds) / viewDuration * width);

        if (leftX > 0)
        {
            var r = new Rectangle { Width = leftX, Height = height, Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)) };
            Canvas.SetLeft(r, 0);
            TrimOverlayCanvas.Children.Add(r);
        }
        if (rightX < width)
        {
            var r = new Rectangle { Width = width - rightX, Height = height, Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)) };
            Canvas.SetLeft(r, rightX);
            TrimOverlayCanvas.Children.Add(r);
        }
    }

    private void UpdateLabels()
    {
        StartTimeText.Text = $"Начало: {FormatTime(_trimStartSeconds)}";
        EndTimeText.Text = $"Конец: {FormatTime(_trimEndSeconds)}";
        TrimmedDurationText.Text = $"Выбрано: {FormatTime(_trimEndSeconds - _trimStartSeconds)}";
    }

    private void UpdateInputs()
    {
        _updatingInputs = true;
        StartInput.Text = _trimStartSeconds.ToString("F2");
        EndInput.Text = _trimEndSeconds.ToString("F2");
        _updatingInputs = false;
    }

    private void RedrawAll()
    {
        DrawWaveform();
        UpdateMarkers();
    }

    // ==================== TIMER ZOOM ====================

    private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        _isDragging = true;
        _accumulatedDelta = 0;
        _lastBigMoveTime = DateTime.Now;
        _zoomTimer.Start();
    }

    private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _isDragging = false;
        _zoomTimer.Stop();

        // Возвращаем масштаб к полному виду
        _viewStartSeconds = 0;
        _viewEndSeconds = _fullDurationSeconds;
        RedrawAll();
    }

    private void ZoomTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isDragging) return;

        double timeSinceLastBigMove = (DateTime.Now - _lastBigMoveTime).TotalSeconds;

        // Если 2 секунды почти не двигали — зумим на одну ступень
        if (timeSinceLastBigMove >= 2.0)
        {
            ZoomOneStep(_currentMarkerPosition);
            _lastBigMoveTime = DateTime.Now; // Сбрасываем таймер для следующей ступени
        }
    }

    private void ZoomOneStep(double markerPosition)
    {
        double viewDuration = _viewEndSeconds - _viewStartSeconds;
        double minViewDuration = Math.Max(0.3, _fullDurationSeconds * 0.02); // минимум 2% от полной длины или 0.3с

        if (viewDuration <= minViewDuration) return;

        // Уменьшаем обзор на 40% за ступень
        double newDuration = viewDuration * 0.6;
        if (newDuration < minViewDuration) newDuration = minViewDuration;

        // Центрируем на маркере с запасом 30% по краям
        double padding = newDuration * 0.3;
        double newStart = markerPosition - padding;
        double newEnd = newStart + newDuration;

        // Клампим в пределах полной длины
        if (newStart < 0)
        {
            newEnd -= newStart;
            newStart = 0;
        }
        if (newEnd > _fullDurationSeconds)
        {
            newStart -= (newEnd - _fullDurationSeconds);
            newEnd = _fullDurationSeconds;
        }
        newStart = Math.Max(0, newStart);

        _viewStartSeconds = newStart;
        _viewEndSeconds = newEnd;

        RedrawAll();
    }

    private void TrackDragMovement(double horizontalChange)
    {
        _accumulatedDelta += Math.Abs(horizontalChange);

        // Если за один тик сдвинули больше 5 пикселей — считаем "большим" движением
        if (Math.Abs(horizontalChange) > 5.0)
        {
            _lastBigMoveTime = DateTime.Now;
            _accumulatedDelta = 0;
        }
    }

    // ==================== DRAG HANDLERS ====================

    private void LeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double width = WaveformGrid.ActualWidth - 24;
        if (width <= 0) return;

        double viewDuration = _viewEndSeconds - _viewStartSeconds;
        double deltaSeconds = (e.HorizontalChange / width) * viewDuration;
        _trimStartSeconds = Math.Clamp(_trimStartSeconds + deltaSeconds, 0, _trimEndSeconds - 0.05);

        _currentMarkerPosition = _trimStartSeconds;
        TrackDragMovement(e.HorizontalChange);
        UpdateMarkers();
    }

    private void RightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double width = WaveformGrid.ActualWidth - 24;
        if (width <= 0) return;

        double viewDuration = _viewEndSeconds - _viewStartSeconds;
        double deltaSeconds = (e.HorizontalChange / width) * viewDuration;
        _trimEndSeconds = Math.Clamp(_trimEndSeconds + deltaSeconds, _trimStartSeconds + 0.05, _fullDurationSeconds);

        _currentMarkerPosition = _trimEndSeconds;
        TrackDragMovement(e.HorizontalChange);
        UpdateMarkers();
    }

    // ==================== TEXT INPUT ====================

    private void StartInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingInputs) return;
        if (double.TryParse(StartInput.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            _trimStartSeconds = Math.Clamp(val, 0, _trimEndSeconds - 0.05);
            ResetZoom();
        }
    }

    private void EndInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingInputs) return;
        if (double.TryParse(EndInput.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            _trimEndSeconds = Math.Clamp(val, _trimStartSeconds + 0.05, _fullDurationSeconds);
            ResetZoom();
        }
    }

    private void ResetZoom()
    {
        _viewStartSeconds = 0;
        _viewEndSeconds = _fullDurationSeconds;
        RedrawAll();
    }

    // ==================== BUTTONS ====================

    private void Apply_Click(object sender, RoutedEventArgs e) { Applied = true; DialogResult = true; Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    private void Reset_Click(object sender, RoutedEventArgs e) { _trimStartSeconds = 0; _trimEndSeconds = _fullDurationSeconds; ResetZoom(); }

    private static string FormatTime(double seconds)
    {
        if (seconds < 0) seconds = 0;
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalMinutes >= 1
            ? $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}"
            : $"{ts.Seconds}.{ts.Milliseconds / 10:D2}с";
    }
}