using System;
using System.Globalization;

namespace LogExpert.Core.Classes.DateTimeParser;

internal class Tokenizer
{
    private readonly string formatString;

    public Tokenizer (string fmt)
    {
        formatString = fmt;
    }

    public int Position { get; private set; }

    public int Length => formatString.Length;

    public string Substring (int startIndex, int length)
    {
        return formatString.Substring(startIndex, length);
    }

    public int Peek (int offset = 0)
    {
        if (Position + offset >= formatString.Length)
        {
            return -1;
        }

        return formatString[Position + offset];
    }

    public int PeekUntil (int startOffset, int until)
    {
        var offset = startOffset;
        while (true)
        {
            var c = Peek(offset++);
            if (c == -1)
            {
                break;
            }

            if (c == until)
            {
                return offset - startOffset;
            }
        }
        return 0;
    }

    public bool PeekOneOf (int offset, string s)
    {
        foreach (var c in s)
        {
            if (Peek(offset) == c)
            {
                return true;
            }
        }
        return false;
    }

    public void Advance (int characters = 1)
    {
        Position = Math.Min(Position + characters, formatString.Length);
    }

    public bool ReadOneOrMore (int c)
    {
        if (Peek() != c)
        {
            return false;
        }

        while (Peek() == c)
        {
            Advance();
        }

        return true;
    }

    public bool ReadOneOf (string s)
    {
        if (PeekOneOf(0, s))
        {
            Advance();
            return true;
        }
        return false;
    }

    public bool ReadString (string str, bool ignoreCase = false)
    {
        if (Position + str.Length > formatString.Length)
        {
            return false;
        }

        for (var i = 0; i < str.Length; i++)
        {
            var c1 = str[i];
            var c2 = (char)Peek(i);

            if ((ignoreCase && char.ToUpperInvariant(c1) != char.ToUpperInvariant(c2)) || (!ignoreCase && c1 != c2))
            {
                return false;
            }
        }

        Advance(str.Length);
        return true;
    }

    public bool ReadEnclosed (char open, char close)
    {
        if (Peek() == open)
        {
            var length = PeekUntil(1, close);
            if (length > 0)
            {
                Advance(1 + length);
                return true;
            }
        }

        return false;
    }
}
