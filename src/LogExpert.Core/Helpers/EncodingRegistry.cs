using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LogExpert.Core.Helpers;

/// <summary>
/// Resolves encoding names and code pages, including the legacy Windows code pages that .NET does not
/// ship with by default.
/// </summary>
/// <remarks>
/// .NET only knows Unicode, ASCII and latin1 out of the box; anything else — windows-1250,
/// windows-1252, … — requires <see cref="CodePagesEncodingProvider"/> to be registered first, and
/// <see cref="Encoding.GetEncoding(string)"/> throws until it is.
/// <para>
/// Registration used to happen as a side effect of constructing the Preferences dialog. Everything
/// that resolves an encoding name runs earlier than that or never opens the dialog at all — the
/// Preferences default encoding, the per-file encoding in a .lxp, the settings JSON — and every one of
/// those call sites swallows the exception and falls back to <see cref="Encoding.Default"/>. The result
/// was that a code page the user had picked was silently discarded on the next start.
/// </para>
/// <para>
/// Resolving through this class removes the ordering problem: every method here registers the provider
/// before it resolves, so no caller has to run after some other component. Callers should not use
/// <see cref="Encoding.GetEncoding(string)"/> or <see cref="Encoding.GetEncoding(int)"/> directly;
/// the <c>Encoding.Ascii</c>-style static properties are fine, since .NET always has those.
/// </para>
/// </remarks>
public static class EncodingRegistry
{
    /// <summary>
    /// Code page of GB2312, the simplified-Chinese code page (issue #688).
    /// </summary>
    public const int CODE_PAGE_GB2312 = 936;

    /// <summary>
    /// The encodings LogExpert offers the user to choose from, in the order they are presented.
    /// </summary>
    /// <remarks>
    /// The single list: the Preferences default-encoding combo box and the per-file View → Encoding
    /// menu both draw their rows from it, so they cannot drift apart and adding an encoding is a
    /// one-line change here. Three invariants, each pinned by a test:
    /// <list type="bullet">
    ///   <item>
    ///     Every entry resolves from its own <see cref="Encoding.HeaderName"/>, because that name is
    ///     what gets persisted (in the settings JSON and in a .lxp) and resolved again on the next
    ///     start. An entry that did not would silently degrade to a fallback.
    ///   </item>
    ///   <item>
    ///     No two entries share a code page. Both UIs label a row with its header name, so two entries
    ///     for one code page are two rows the user cannot tell apart — which is exactly what
    ///     <c>Encoding.Default</c> alongside <see cref="Encoding.UTF8"/> produced (both are code page
    ///     65001 on .NET, differing only in the BOM they emit, which a read-side encoding never uses).
    ///     Hence no <c>Encoding.Default</c> entry.
    ///   </item>
    ///   <item>
    ///     New entries are appended rather than sorted in, so an existing user's row does not move
    ///     under the cursor on upgrade.
    ///   </item>
    /// </list>
    /// </remarks>
    public static IReadOnlyList<Encoding> OfferedEncodings => _offeredEncodings.Value;

    /// <remarks>
    /// Built once: <see cref="Encoding"/> instances are immutable and the ones from
    /// <see cref="GetEncoding(int)"/> are cached by .NET anyway, so the list is safe to share. Lazy
    /// rather than a plain initialiser so the code pages resolve through
    /// <see cref="EnsureRegistered"/> rather than depending on when this class is first touched.
    /// </remarks>
    private static readonly Lazy<IReadOnlyList<Encoding>> _offeredEncodings = new(
        () =>
        [
            Encoding.ASCII,
            Encoding.Latin1,
            Encoding.UTF8,
            Encoding.Unicode,
            GetEncoding(1250),
            GetEncoding(1252),
            GetEncoding(CODE_PAGE_GB2312)
        ],
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Registers <see cref="CodePagesEncodingProvider"/> on first use.
    /// </summary>
    /// <remarks>
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> is the point: registration must have
    /// *completed* before any thread is allowed past, otherwise a second thread resolving concurrently
    /// would call <see cref="Encoding.GetEncoding(string)"/> too early, catch the
    /// <see cref="ArgumentException"/> and silently fall back — the exact bug this class exists to
    /// prevent. Files load under <c>Task.Run</c>, so concurrent first resolves do happen.
    /// </remarks>
    private static readonly Lazy<bool> _provider = new(
        () =>
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return true;
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static void EnsureRegistered ()
    {
        _ = _provider.Value;
    }

    /// <summary>
    /// Resolves a code page number.
    /// </summary>
    /// <param name="codePage">The code page number, e.g. 1252.</param>
    /// <returns>The <see cref="Encoding"/> for <paramref name="codePage"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="codePage"/> is not a supported code page. Intended for hard-coded code pages,
    /// where an unsupported value is a programming error rather than bad user input; use
    /// <see cref="TryGetEncoding(string, out Encoding)"/> for values that come from a file.
    /// </exception>
    public static Encoding GetEncoding (int codePage)
    {
        EnsureRegistered();
        return Encoding.GetEncoding(codePage);
    }

    /// <summary>
    /// Resolves an encoding name, falling back when it cannot be resolved.
    /// </summary>
    /// <param name="name">An encoding name such as "windows-1252", possibly null or empty.</param>
    /// <param name="fallback">The encoding to return when <paramref name="name"/> is unusable.</param>
    /// <returns>The resolved encoding, or <paramref name="fallback"/>.</returns>
    public static Encoding GetEncoding (string? name, Encoding fallback)
    {
        return TryGetEncoding(name, out var encoding) ? encoding : fallback;
    }

    /// <summary>
    /// Attempts to resolve an encoding name.
    /// </summary>
    /// <param name="name">An encoding name such as "windows-1252", possibly null or empty.</param>
    /// <param name="encoding">The resolved encoding, or null when the name is unusable.</param>
    /// <returns>
    /// <c>true</c> when <paramref name="name"/> names a supported encoding; <c>false</c> when it is
    /// null, blank or unknown.
    /// </returns>
    public static bool TryGetEncoding (string? name, [NotNullWhen(true)] out Encoding? encoding)
    {
        encoding = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        EnsureRegistered();

        try
        {
            encoding = Encoding.GetEncoding(name);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            // Thrown for code pages the provider knows of but cannot instantiate.
            return false;
        }
    }
}
