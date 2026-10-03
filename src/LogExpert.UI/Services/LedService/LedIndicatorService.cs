using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;

using NLog;

namespace LogExpert.UI.Services.LedService;

[SupportedOSPlatform("windows")]
internal sealed class LedIndicatorService : ILedIndicatorService, IDisposable
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    // Constants
    private const int DIFF_MAX = 100;
    private const int LED_DECAY_RATE = 10;
    private const int ANIMATION_INTERVAL_MS = 200;
    private const int ICON_SIZE = 16;
    private const int ICON_LEVELS = 6;        // Activity levels 0-5
    private const int ICON_DIRTY_STATES = 2;  // Clean/Dirty
    private const int ICON_TAIL_STATES = 4;   // On/Off/Paused/Hidden
    private const int ICON_SYNC_STATES = 2;   // Not synced/Synced

    // Icon cache: [level][dirty][tail][sync]
    private Icon[][][][] _iconCache;
    private Icon _deadIcon;

    // Drawing resources
    private readonly Rectangle[] _leds = new Rectangle[5];
    private Brush[] _ledBrushes;
    private Brush[] _tailLedBrush;
    private Brush _offLedBrush;
    private Brush _dirtyLedBrush;
    private Brush _syncLedBrush;

    // Window tracking
    private readonly Dictionary<LogWindow, LedState> _windowStates = [];
    private readonly Lock _stateLock = new();

    // Animation
    private System.Windows.Forms.Timer _animationTimer;
    private readonly SynchronizationContext _uiContext;
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;
    private bool _isInitialized;
    private bool _disposed;

    public event EventHandler<IconChangedEventArgs> IconChanged;

    /// <summary>
    /// Gets the current tail color used for LED indicators
    /// </summary>
    public Color CurrentTailColor
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(LedIndicatorService));

            return !_isInitialized
                ? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_Message_ServiceNotInitialized, nameof(LedIndicatorService)))
                : (field);
        }

        private set;
    }

    public LedIndicatorService ()
    {
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_Message_ServiceMustBeCreatedOnUIThread, nameof(LedIndicatorService)));

        InitializeLedRectangles();
    }

    /// <summary>
    /// Initializes LED rectangles for icon generation
    /// </summary>
    private void InitializeLedRectangles ()
    {
        _leds[0] = new Rectangle(0, 0, 3, 3);
        _leds[1] = new Rectangle(3, 0, 3, 3);
        _leds[2] = new Rectangle(6, 0, 3, 3);
        _leds[3] = new Rectangle(9, 0, 3, 3);
        _leds[4] = new Rectangle(12, 0, 3, 3);
    }

    /// <summary>
    /// Disposes all resources
    /// </summary>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopService();

        Thread.Sleep(ANIMATION_INTERVAL_MS * 2);

        _logger.Info("Disposing LedIndicatorService");

        lock (_stateLock)
        {
            DisposeBrushes();
            //DisposeIcons();
            _windowStates.Clear();
        }

        _disposed = true;
    }

    /// <summary>
    /// Disposes all brushes
    /// </summary>
    private void DisposeBrushes ()
    {
        if (_ledBrushes != null)
        {
            foreach (var brush in _ledBrushes.Where(b => b != null))
            {
                brush.Dispose();
            }

            _ledBrushes = null;
        }

        if (_tailLedBrush != null)
        {
            foreach (var brush in _tailLedBrush.Where(b => b != null))
            {
                brush.Dispose();
            }

            _tailLedBrush = null;
        }

        _offLedBrush?.Dispose();
        _offLedBrush = null;

        _dirtyLedBrush?.Dispose();
        _dirtyLedBrush = null;

        _syncLedBrush?.Dispose();
        _syncLedBrush = null;
    }

    /// <summary>
    /// Disposes all icons
    /// </summary>
    private void DisposeIcons ()
    {
        if (_iconCache != null)
        {
            for (int level = 0; level < ICON_LEVELS; level++)
            {
                if (_iconCache[level] == null)
                {
                    continue;
                }

                for (int dirty = 0; dirty < ICON_DIRTY_STATES; dirty++)
                {
                    if (_iconCache[level][dirty] == null)
                    {
                        continue;
                    }

                    for (int tail = 0; tail < ICON_TAIL_STATES; tail++)
                    {
                        if (_iconCache[level][dirty][tail] == null)
                        {
                            continue;
                        }

                        for (int sync = 0; sync < ICON_SYNC_STATES; sync++)
                        {
                            _iconCache[level][dirty][tail][sync]?.Dispose();
                            _iconCache[level][dirty][tail][sync] = null;
                        }
                    }
                }
            }

            _iconCache = null;
        }

        _deadIcon?.Dispose();
        _deadIcon = null;
    }

    /// <summary>
    /// Gets the dead icon
    /// </summary>
    public Icon GetDeadIcon ()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(LedIndicatorService));

        return !_isInitialized
            ? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_Message_ServiceNotInitialized, nameof(LedIndicatorService)))
            : (Icon)_deadIcon.Clone();
    }

    /// <summary>
    /// Gets the appropriate icon for the specified state
    /// </summary>
    public Icon GetIcon (int diffLevel, LedState state)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(LedIndicatorService));

        if (!_isInitialized)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_Message_ServiceNotInitialized, nameof(LedIndicatorService)));
        }

        int level = GetLevelFromDiff(diffLevel);
        int dirty = state.IsDirty ? 1 : 0;
        int tail = (int)state.TailState;
        int sync = (int)state.SyncState;

        return _iconCache[level][dirty][tail][sync];
    }

    public void Initialize (Color tailColor)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_Message_ServiceIsAlreadyInitialized, nameof(LedIndicatorService)));
        }

        _logger.Info("Initializing LedIndicatorService with tail color: {Color}", tailColor);

        CurrentTailColor = tailColor;

        // Create brushes
        CreateBrushes(tailColor);

        // Generate all icons
        GenerateIcons();

        // Create dead icon
        CreateDeadIcon();

        _isInitialized = true;
        _logger.Info("LedIndicatorService initialized successfully");
    }

    /// <summary>
    /// Creates all brushes needed for icon generation
    /// </summary>
    private void CreateBrushes (Color tailColor)
    {
        _ledBrushes = new Brush[5];
        _ledBrushes[0] = new SolidBrush(Color.FromArgb(255, 0, 0));     // Red (highest activity)
        _ledBrushes[1] = new SolidBrush(Color.FromArgb(255, 100, 0));   // Orange
        _ledBrushes[2] = new SolidBrush(Color.FromArgb(255, 190, 0));   // Yellow
        _ledBrushes[3] = new SolidBrush(Color.FromArgb(190, 255, 0));   // Yellow-green
        _ledBrushes[4] = new SolidBrush(Color.FromArgb(0, 255, 0));     // Green (lowest activity)

        _tailLedBrush = new Brush[3];
        _tailLedBrush[0] = new SolidBrush(tailColor);                    // Tail on
        _tailLedBrush[1] = new SolidBrush(Color.FromArgb(160, 160, 160)); // Tail off
        _tailLedBrush[2] = new SolidBrush(Color.FromArgb(220, 220, 0));  // Tail trigger

        _offLedBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
        _dirtyLedBrush = new SolidBrush(Color.FromArgb(220, 0, 220));  // Magenta for dirty
        _syncLedBrush = new SolidBrush(Color.FromArgb(0, 100, 220));   // Blue for synced
    }

    /// <summary>
    /// Generates all possible icon combinations
    /// </summary>
    private void GenerateIcons ()
    {
        _logger.Debug("Generating LED icon cache");

        // Initialize jagged array
        _iconCache = new Icon[ICON_LEVELS][][][];

        int iconCount = 0;
        for (int level = 0; level < ICON_LEVELS; level++)
        {
            _iconCache[level] = new Icon[ICON_DIRTY_STATES][][];

            for (int dirty = 0; dirty < ICON_DIRTY_STATES; dirty++)
            {
                _iconCache[level][dirty] = new Icon[ICON_TAIL_STATES][];

                for (int tail = 0; tail < ICON_TAIL_STATES; tail++)
                {
                    _iconCache[level][dirty][tail] = new Icon[ICON_SYNC_STATES];

                    for (int sync = 0; sync < ICON_SYNC_STATES; sync++)
                    {
                        _iconCache[level][dirty][tail][sync] = CreateLedIcon(
                            level,
                            dirty == 1,
                            (TailFollowState)tail,
                            (TimeSyncState)sync);
                        iconCount++;
                    }
                }
            }
        }

        _logger.Info("Generated {Count} LED icons", iconCount);
    }

    /// <summary>
    /// Creates a single LED icon with the specified state
    /// </summary>
    /// <param name="level">Activity level (0-5)</param>
    /// <param name="dirty">Whether content has changed</param>
    /// <param name="tailState">Tail follow state</param>
    /// <param name="syncState">Time synchronization state</param>
    /// <returns>The generated icon</returns>
    private Icon CreateLedIcon (int level, bool dirty, TailFollowState tailState, TimeSyncState syncState)
    {
        using var bmp = new Bitmap(ICON_SIZE, ICON_SIZE, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw 5 LED segments
            for (int i = 0; i < 5; i++)
            {
                Brush brush = i < level
                    ? _ledBrushes[i]
                    : _offLedBrush;
                g.FillRectangle(brush, _leds[i]);
            }

            // Draw dirty indicator (top-right pixel)
            if (dirty)
            {
                g.FillRectangle(_dirtyLedBrush, new Rectangle(13, 0, 3, 3));
            }

            // Draw tail indicator (bottom row) - hidden if state is Hidden
            if (tailState != TailFollowState.Hidden)
            {
                for (int i = 0; i < 5; i++)
                {
                    g.FillRectangle(_tailLedBrush[(int)tailState], new Rectangle(i * 3, 13, 3, 3));
                }
            }

            // Draw sync indicator (left side)
            if (syncState == TimeSyncState.Synced)
            {
                g.FillRectangle(_syncLedBrush, new Rectangle(0, 4, 2, 9));
            }
        }

        // Convert to icon
        IntPtr hIcon = bmp.GetHicon();
        try
        {
            var icon = (Icon)Icon.FromHandle(hIcon).Clone();
            return icon;
        }
        finally
        {
            // Destroy native icon handle
            _ = Vanara.PInvoke.User32.DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// Creates the "dead" icon for missing files
    /// </summary>
    private void CreateDeadIcon ()
    {
        using var bmp = new Bitmap(ICON_SIZE, ICON_SIZE, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw X in red
            using var pen = new Pen(Color.Red, 2);
            g.DrawLine(pen, 2, 2, 14, 14);
            g.DrawLine(pen, 14, 2, 2, 14);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            //using var bmp = Resources.Deceased;
            _deadIcon = (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            _ = Vanara.PInvoke.User32.DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// Calculates LED level from diff value
    /// </summary>
    private static int GetLevelFromDiff (int diff)
    {
        return diff < 1
            ? 0
            : diff < 10
                ? 1
                : diff < 20
                    ? 2
                    : diff < 40
                        ? 3
                        : diff < 80
                            ? 4
                            : 5;
    }

    /// <summary>
    /// Regenerates all icons with new color
    /// </summary>
    public void RegenerateIcons (Color tailColor)
    {
        _logger.Info("Regenerating icons with new tail color: {Color}", tailColor);

        bool wasRunning = _animationTimer != null && _animationTimer.Enabled;

        if (wasRunning)
        {
            StopService();
            Thread.Sleep(ANIMATION_INTERVAL_MS * 2);  // Wait for pending ticks
        }

        lock (_stateLock)
        {
            // Dispose old resources
            DisposeBrushes();
            //DisposeIcons();
            _iconCache = null;

            // Create new ones
            CurrentTailColor = tailColor;
            CreateBrushes(tailColor);
            GenerateIcons();
            CreateDeadIcon();

            // Update all windows
            var updates = new List<(LogWindow window, Icon icon)>();
            foreach (var kvp in _windowStates)
            {
                var icon = GetIcon(kvp.Value.DiffSum, kvp.Value);
                updates.Add((kvp.Key, icon));
            }

            // Raise events outside lock on UI thread
            foreach (var (window, icon) in updates)
            {
                OnIconChanged(window, icon);
            }
        }

        if (wasRunning)
        {
            StartService();
        }
    }

    /// <summary>
    /// Registers a window for tracking
    /// </summary>
    public void RegisterWindow (LogWindow window, LedState state)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(state);

        lock (_stateLock)
        {
            if (!_windowStates.ContainsKey(window))
            {
                _windowStates[window] = state;
                _logger.Debug("Registered window for LED tracking: {Window}", window.Text);
            }
        }
    }

    /// <summary>
    /// Starts the LED animation timer
    /// </summary>
    public void StartService ()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_Message_ServiceNotInitialized, nameof(LedIndicatorService)));
        }

        if (_animationTimer != null)
        {
            _logger.Warn("Animation timer already started");
            return;
        }

        _logger.Info("Starting LED animation timer");

        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = ANIMATION_INTERVAL_MS
        };

        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    /// <summary>
    /// Stops the LED animation timer
    /// </summary>
    public void StopService ()
    {
        if (_animationTimer == null)
        {
            return;
        }

        _logger.Info("Stopping LED animation timer");

        _animationTimer.Stop();
        _animationTimer.Tick -= OnAnimationTick;
        _animationTimer.Dispose();
        _animationTimer = null;
    }

    /// <summary>
    /// Animation tick handler - decrements activity levels
    /// </summary>
    private void OnAnimationTick (object sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        List<(LogWindow window, Icon icon)> updates = [];

        lock (_stateLock)
        {
            foreach (var kvp in _windowStates.ToList())
            {
                var window = kvp.Key;
                var state = kvp.Value;

                if (state.DiffSum > 0)
                {
                    state.DiffSum -= LED_DECAY_RATE;
                    if (state.DiffSum < 0)
                    {
                        state.DiffSum = 0;
                    }

                    var icon = GetIcon(state.DiffSum, state);
                    updates.Add((window, icon));
                }
            }
        }

        foreach (var (window, icon) in updates)
        {
            OnIconChanged(window, icon);
        }
    }

    /// <summary>
    /// Unregisters a window from tracking
    /// </summary>
    public void UnregisterWindow (LogWindow window)
    {
        if (window == null)
        {
            return;
        }

        lock (_stateLock)
        {
            if (_windowStates.Remove(window))
            {
                _logger.Debug("Unregistered window from LED tracking: {Window}", window.Text);
            }
        }
    }

    /// <summary>
    /// Updates window activity level
    /// </summary>
    public void UpdateWindowActivity (LogWindow window, int lineDiff, bool markDirty)
    {
        if (window == null || lineDiff < 0)
        {
            return;
        }

        Icon newIcon = null;

        lock (_stateLock)
        {
            if (_windowStates.TryGetValue(window, out var state))
            {
                state.DiffSum += lineDiff;
                if (state.DiffSum > DIFF_MAX)
                {
                    state.DiffSum = DIFF_MAX;
                }

                if (markDirty)
                {
                    state.IsDirty = true;
                }

                newIcon = GetIcon(state.DiffSum, state);
            }
        }

        if (newIcon != null)
        {
            OnIconChanged(window, newIcon);
        }
    }

    /// <summary>
    /// Clears the dirty state of a window
    /// </summary>
    public void ClearDirty (LogWindow window)
    {
        if (window == null)
        {
            return;
        }

        Icon newIcon = null;

        lock (_stateLock)
        {
            if (_windowStates.TryGetValue(window, out var state))
            {
                state.IsDirty = false;
                newIcon = GetIcon(state.DiffSum, state);
            }
        }

        if (newIcon != null)
        {
            OnIconChanged(window, newIcon);
        }
    }

    /// <summary>
    /// Raises the IconChanged event on the UI thread
    /// </summary>
    /// <param name="window">The window whose icon changed</param>
    /// <param name="icon">The new icon</param>
    private void OnIconChanged (LogWindow window, Icon icon)
    {
        if (_disposed || IconChanged == null)
        {
            return;
        }

        var args = new IconChangedEventArgs(window, icon);

        // Marshal to UI thread if needed
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            _uiContext.Post(_ => IconChanged?.Invoke(this, args), null);
        }
        else
        {
            // Already on UI thread
            IconChanged.Invoke(this, args);
        }
    }
}
