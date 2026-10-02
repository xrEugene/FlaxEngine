// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEngine;

namespace FlaxEditor.GUI
{
    /// <summary>
    /// Helper for fitting a tile/asset name into a fixed two-line area: wraps a single "word" that the native
    /// word-wrap engine has no break opportunity for (see Font::ProcessText's isWrapChar in the native engine, which
    /// only treats whitespace, '_' and an uppercase letter after the first character as wrap points - notably not
    /// '-'), which would otherwise render unwrapped, overflowing past the available width (eg. an all-lowercase,
    /// hyphenated asset name like "harshbricks-albedo"); and, separately, shortens with a trailing "..." any text
    /// that would still need more than two lines even once wrapped (eg. a name with several native wrap points that
    /// are just too far apart to fit, like "FlaxTechDemo2022_72"), since the tile/edit box only ever has room for
    /// two. Used by both the Content window's tile labels (<see cref="FlaxEditor.Content.ContentItem"/>) and
    /// <see cref="WrappingRenameTextBox"/>, so a name is fit the same way while being renamed as it is once finished.
    /// </summary>
    internal static class SingleWordWrap
    {
        private const string Ellipsis = "...";

        /// <summary>
        /// True if the native word-wrap engine already has at least one break opportunity in <paramref name="text"/>.
        /// </summary>
        public static bool HasNativeWrapPoint(string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '_' || char.IsWhiteSpace(c) || char.IsUpper(c))
                    return true;
            }
            return false;
        }

        public static bool HasWhitespace(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                    return true;
            }
            return false;
        }

        private static float MeasureWidth(Font font, string s, ref TextLayoutOptions layout)
        {
            return font.MeasureText(s, ref layout).X;
        }

        private static TextLayoutOptions UnboundedLayout(float scale)
        {
            var layout = TextLayoutOptions.Default;
            layout.Scale = scale;
            layout.TextWrapping = TextWrapping.NoWrap;
            layout.HorizontalAlignment = TextAlignment.Near;
            layout.VerticalAlignment = TextAlignment.Near;
            layout.Bounds = new Rectangle(Float2.Zero, new Float2(float.MaxValue, float.MaxValue));
            return layout;
        }

        /// <summary>
        /// A layout for measuring a single character in isolation (no wrapping, unbounded space), independent of
        /// whatever bounds the real text is laid out in - see <see cref="GetCharPositionSafe"/> and
        /// <see cref="ComputeLines"/>, both of which need a lone glyph's true width, not one skewed by the
        /// surrounding wrap box.
        /// </summary>
        public static TextLayoutOptions CharMeasureLayout(float scale)
        {
            var layout = TextLayoutOptions.Default;
            layout.Scale = scale;
            layout.TextWrapping = TextWrapping.NoWrap;
            layout.HorizontalAlignment = TextAlignment.Near;
            layout.VerticalAlignment = TextAlignment.Near;
            layout.Bounds = new Rectangle(Float2.Zero, new Float2(float.MaxValue, float.MaxValue));
            return layout;
        }

        /// <summary>
        /// Like <see cref="Font.GetCharPosition(string, int, ref TextLayoutOptions)"/>, but works around a native
        /// engine quirk: querying the position exactly at <c>s.Length</c> (one past the last character - what a
        /// caret at the end of the text, or a selection reaching the end, asks for) can, for text wrapped onto three
        /// or more lines, land on an earlier line instead of the true last one, because that index never falls
        /// inside any line's own [FirstCharIndex, LastCharIndex] range and the position-after-the-last-line fallback
        /// doesn't always land where expected. Querying the position of the actual last character (which *does*
        /// fall inside the last line's range) and adding that character's own measured width sidesteps the fallback
        /// entirely and always lands on the true last line.
        /// </summary>
        /// <remarks>
        /// Takes <paramref name="layout"/>/<paramref name="charLayout"/> BY VALUE (not by ref), so the extra native
        /// call this makes for the end-of-text case only ever mutates its own local copy - never the caller's
        /// struct. That copy turned out to matter: sharing the caller's <c>layout</c> by ref here left it silently
        /// altered afterward, corrupting every subsequent position query made through it in the same frame.
        /// </remarks>
        public static Float2 GetCharPositionSafe(Font font, string s, int index, TextLayoutOptions layout, TextLayoutOptions charLayout)
        {
            if (s.Length > 0 && index >= s.Length)
            {
                var lastIndex = s.Length - 1;
                var lastPos = font.GetCharPosition(s, lastIndex, ref layout);
                var lastWidth = font.MeasureText(s.Substring(lastIndex, 1), ref charLayout).X;
                return new Float2(lastPos.X + lastWidth, lastPos.Y);
            }
            return font.GetCharPosition(s, index, ref layout);
        }

        /// <summary>
        /// Computes every character's position in <paramref name="text"/> (indices 0 through
        /// <c>text.Length</c> inclusive, via <see cref="GetCharPositionSafe"/>) under <paramref name="layout"/>, and
        /// groups them into visual lines by shared Y position.
        /// </summary>
        /// <remarks>
        /// Same reliability concern as <see cref="WrapToFit"/> (see its remarks): character position queries for a
        /// (text, layout) combination that hasn't been measured before have been observed to disagree with an
        /// immediately-following, identical burst of the same queries - eg. the very first character's own position
        /// reported one line, but a handful of characters later, characters supposedly still on that same first
        /// line reported a different one. Retries here until two consecutive attempts agree (or a bounded number of
        /// attempts is exhausted), same as <see cref="WrapToFit"/>, so a caller that only computes this once (eg.
        /// because it caches the result) doesn't get to lock in a result from that unreliable window.
        /// </remarks>
        public static (Float2[] Positions, (int Start, int End, Float2 Position)[] Lines) ComputeLines(string text, Font font, TextLayoutOptions layout)
        {
            var charLayout = CharMeasureLayout(layout.Scale);

            (Float2[] Positions, (int, int, Float2)[] Lines) ComputeOnce()
            {
                var positions = new Float2[text.Length + 1];
                for (int i = 0; i <= text.Length; i++)
                    positions[i] = GetCharPositionSafe(font, text, i, layout, charLayout);

                var lines = new System.Collections.Generic.List<(int, int, Float2)>();
                var lineStart = 0;
                for (int i = 1; i <= text.Length; i++)
                {
                    if (!Mathf.NearEqual(positions[i].Y, positions[lineStart].Y))
                    {
                        lines.Add((lineStart, i, positions[lineStart]));
                        lineStart = i;
                    }
                }
                lines.Add((lineStart, text.Length, positions[lineStart]));
                return (positions, lines.ToArray());
            }

            // Compares every character's position, not just each line's boundaries/first-character position: a
            // (text, layout) combination whose line structure has already settled can still have an individual
            // *interior* character position still drifting between calls, and that's exactly what the selection
            // highlight's own line-detection (in DrawSelf) walks character by character to find - a positions array
            // that looks stable at the line-boundary level but isn't at every character can get cached anyway,
            // silently corrupting the highlight for that character's line without ever fully self-correcting, since
            // once cached it's never recomputed again for the rest of that rename.
            bool PositionsMatch(Float2[] a, Float2[] b)
            {
                if (a.Length != b.Length)
                    return false;
                for (int i = 0; i < a.Length; i++)
                {
                    if (!Mathf.NearEqual(a[i].X, b[i].X) || !Mathf.NearEqual(a[i].Y, b[i].Y))
                        return false;
                }
                return true;
            }

            var computed = ComputeOnce();
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var verify = ComputeOnce();
                if (PositionsMatch(computed.Positions, verify.Positions))
                    break;
                computed = verify;
            }
            return computed;
        }

        /// <summary>
        /// Whether <paramref name="text"/>, laid out under <paramref name="wrapping"/> within
        /// <paramref name="boundsWidth"/>, fits within (<paramref name="boundsWidth"/>, <paramref name="boundsHeight"/>)
        /// - measured via <see cref="ComputeLines"/>, the exact same per-character position grouping that
        /// <see cref="WrappingRenameTextBox"/> uses to decide what to actually draw, rather than
        /// <c>Font.MeasureText</c>'s own reported height (a separate native code path whose result has been
        /// observed to permanently disagree with how the text actually groups into lines for some inputs - not a
        /// timing flake that self-corrects, but a reproducible mismatch for a given (text, width): a name with
        /// several wrap points spaced just right could measure as fitting in two lines by height while its own
        /// character positions genuinely group into three, silently clipping that third line with no ellipsis ever
        /// applied, since the code thought no shortening was needed).
        /// </summary>
        /// <remarks>
        /// Also verifies every individual line's own rendered width against <paramref name="boundsWidth"/>, not
        /// just the overall line count against <paramref name="boundsHeight"/>: native <see cref="TextWrapping.WrapWords"/>
        /// only ever breaks at whitespace/'_'/an uppercase letter (see <see cref="HasNativeWrapPoint"/>), so a single
        /// long word with no such break anywhere inside it (eg. "Collisiongggggggggg" in "Wall Collisiongggggggggg")
        /// simply overflows its own line's width instead of forcing an extra line - the line *count* still looked
        /// like it fit, so this "fits" check used to say yes despite that line visibly overflowing/clipping with no
        /// ellipsis once actually drawn.
        /// <para/>
        /// Deliberately doesn't convert <paramref name="boundsHeight"/> into an expected line count via a font
        /// metric like <c>Font.Height</c> either: that metric isn't guaranteed to equal the real distance between
        /// one wrapped line's position and the next (eg. it can represent a taller full em-square than the tighter
        /// pitch the native renderer actually lays lines out at), which would silently under- or over-estimate how
        /// many lines actually fit and truncate text that didn't need it (or the reverse). Instead, when the text
        /// wraps onto two or more lines, the real pitch is read directly off their own computed Y positions - the
        /// same native output being fit-checked - so there's no separate metric that can disagree with it.
        /// </remarks>
        private static bool FitsHeight(string text, Font font, float scale, TextWrapping wrapping, float boundsWidth, float boundsHeight)
        {
            var layout = TextLayoutOptions.Default;
            layout.Scale = scale;
            layout.TextWrapping = wrapping;
            layout.HorizontalAlignment = TextAlignment.Near;
            layout.VerticalAlignment = TextAlignment.Near;
            layout.Bounds = new Rectangle(Float2.Zero, new Float2(boundsWidth, float.MaxValue));
            var lines = ComputeLines(text, font, layout).Lines;
            if (lines.Length == 0)
                return true;

            var measureLayout = UnboundedLayout(scale);
            for (int i = 0; i < lines.Length; i++)
            {
                var (start, end, _) = lines[i];
                if (end <= start)
                    continue;
                var lineText = text.Substring(start, end - start);
                var lineWidth = MeasureWidth(font, lineText, ref measureLayout);
                // Only a true floating-point tolerance here, not a visual fudge factor: the actual draw/clip rect
                // has no such slack, so anything bigger than rounding error (eg. the half-pixel tolerance this used
                // to carry) let lines that were genuinely, reproducibly too wide - not just noisy - report as
                // fitting, leaving them undrawn with no ellipsis and their trailing edge silently clipped.
                if (lineWidth > boundsWidth + 0.05f)
                    return false;
            }

            if (lines.Length <= 1)
                return true;

            var pitch = lines[1].Position.Y - lines[0].Position.Y;
            if (pitch <= 0)
                pitch = font.Height * scale;
            var usedHeight = lines[lines.Length - 1].Position.Y - lines[0].Position.Y + pitch;
            return usedHeight <= boundsHeight + 0.5f;
        }

        /// <summary>
        /// Shortens <paramref name="text"/> with a trailing "..." to fit within a single line of
        /// <paramref name="boundsWidth"/>, if it doesn't already fit unchanged. Used for the tile label outside
        /// rename mode: a whitespace-free name is never wrapped there (see <see cref="FlaxEditor.Content.ContentItem"/>'s
        /// own remarks on why), so an overlong one needs this instead to signal it was cut short, rather than being
        /// silently clipped by the tile's own clip rect with no visual indication anything is missing.
        /// </summary>
        /// <param name="text">The original text.</param>
        /// <param name="font">The font it will be drawn with.</param>
        /// <param name="scale">The font scale it will be drawn with.</param>
        /// <param name="boundsWidth">The available width for the line.</param>
        /// <param name="toWrapped">Maps an index into <paramref name="text"/> to the corresponding index in the returned string (clamped to the nearest visible position if that part of the text was truncated away).</param>
        /// <returns>The text to actually draw/measure.</returns>
        public static string TruncateSingleLine(string text, Font font, float scale, float boundsWidth, out Func<int, int> toWrapped)
        {
            if (font == null || text.Length == 0)
            {
                toWrapped = i => i;
                return text;
            }

            var measureLayout = UnboundedLayout(scale);
            if (MeasureWidth(font, text, ref measureLayout) <= boundsWidth)
            {
                toWrapped = i => i;
                return text;
            }

            // Binary search the longest original-text prefix (before the ellipsis) that still fits.
            int lo = 0, hi = text.Length;
            var best = Ellipsis;
            var bestPrefixLength = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var candidate = text.Substring(0, mid).TrimEnd() + Ellipsis;
                if (MeasureWidth(font, candidate, ref measureLayout) <= boundsWidth)
                {
                    best = candidate;
                    bestPrefixLength = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            toWrapped = i => Math.Min(i, bestPrefixLength);
            return best;
        }

        /// <summary>
        /// Decides how to draw <paramref name="text"/> with no truncation: unchanged (native WrapWords) if it
        /// already has a wrap opportunity, otherwise a hyphen split (see the type docs) or, failing that, plain
        /// character wrapping.
        /// </summary>
        private static (string Text, TextWrapping Wrapping, Func<int, int> ToWrapped, Func<int, int> ToOriginal) Decide(string text, Font font, float scale, float boundsWidth)
        {
            if (font == null || text.Length == 0 || HasNativeWrapPoint(text))
                return (text, TextWrapping.WrapWords, i => i, i => i);

            var measureLayout = UnboundedLayout(scale);
            if (MeasureWidth(font, text, ref measureLayout) <= boundsWidth)
                return (text, TextWrapping.WrapWords, i => i, i => i);

            // Last '-' whose preceding text (hyphen included) still fits on one line - further-right hyphens only
            // have more text before them, so once one doesn't fit, none after it will either.
            int splitAt = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '-')
                    continue;
                if (MeasureWidth(font, text.Substring(0, i + 1), ref measureLayout) <= boundsWidth)
                    splitAt = i + 1;
                else
                    break;
            }

            if (splitAt > 0 && splitAt < text.Length)
            {
                var wrapped = text.Substring(0, splitAt) + "\n" + text.Substring(splitAt);
                var cut = splitAt;
                return (wrapped, TextWrapping.NoWrap, i => i >= cut ? i + 1 : i, i => i > cut ? i - 1 : i);
            }

            // No usable hyphen - fall back to plain character wrapping; the native engine handles this using the
            // original text/indices directly, so no text changes or index mapping are needed.
            return (text, TextWrapping.WrapChars, i => i, i => i);
        }

        /// <summary>
        /// One attempt at fitting <paramref name="text"/> within (<paramref name="boundsWidth"/>,
        /// <paramref name="boundsHeight"/>): applies <see cref="Decide"/> first, then - if the result would still
        /// need more lines than <paramref name="boundsHeight"/> allows - progressively shortens the text with a
        /// trailing "..." until it fits. See <see cref="WrapToFit"/>, which calls this repeatedly to guard against
        /// this attempt alone being unreliable.
        /// </summary>
        private static string WrapToFitOnce(string text, Font font, float scale, float boundsWidth, float boundsHeight, out Func<int, int> toWrapped, out Func<int, int> toOriginal, out TextWrapping wrapping)
        {
            var decision = Decide(text, font, scale, boundsWidth);
            if (font == null || text.Length == 0)
            {
                toWrapped = decision.ToWrapped;
                toOriginal = decision.ToOriginal;
                wrapping = decision.Wrapping;
                return decision.Text;
            }
            if (FitsHeight(decision.Text, font, scale, decision.Wrapping, boundsWidth, boundsHeight))
            {
                toWrapped = decision.ToWrapped;
                toOriginal = decision.ToOriginal;
                wrapping = decision.Wrapping;
                return decision.Text;
            }

            // The word-based decision (native WrapWords, since decision.Text is unchanged - a hyphen split or the
            // char-wrap fallback in Decide() would already have picked something else) needs more lines than fit.
            // For a single "word" with no whitespace at all, try character-wrapping the same, untouched text before
            // giving up and truncating: it isn't constrained to break only at whitespace/'_'/an uppercase letter,
            // so it can pack more characters per line and may fit within the same line budget as-is, preserving the
            // full name instead of shortening it.
            if (decision.Wrapping == TextWrapping.WrapWords && decision.Text == text && !HasWhitespace(text) &&
                FitsHeight(text, font, scale, TextWrapping.WrapChars, boundsWidth, boundsHeight))
            {
                toWrapped = i => i;
                toOriginal = i => i;
                wrapping = TextWrapping.WrapChars;
                return text;
            }

            // Doesn't fit even once wrapped - shorten with a trailing ellipsis. Find the longest original-text
            // prefix (before the ellipsis) whose own wrap decision fits within boundsHeight, trying longest-first
            // rather than a binary search: Decide() can switch wrapping strategy (native word-wrap, a hyphen split,
            // or character wrap) at different prefix lengths, which can make "fits" non-monotonic in that length -
            // a classic binary search can walk straight past the one prefix length that actually fits and land on
            // none at all, silently leaving the full, overflowing text undrawn with no ellipsis ever applied. A
            // plain linear scan can't miss it like that, and these are short filenames, so the extra iterations cost
            // nothing worth optimizing away.
            var best = decision;
            var bestPrefixLength = 0;
            for (int mid = text.Length; mid >= 0; mid--)
            {
                var candidateSource = text.Substring(0, mid).TrimEnd() + Ellipsis;
                var candidateDecision = Decide(candidateSource, font, scale, boundsWidth);
                if (FitsHeight(candidateDecision.Text, font, scale, candidateDecision.Wrapping, boundsWidth, boundsHeight))
                {
                    best = candidateDecision;
                    bestPrefixLength = mid;
                    break;
                }
            }

            var innerToWrapped = best.ToWrapped;
            // Anything past the kept prefix was truncated away - there's no real position for it, so clamp to the
            // truncation point (the start of the ellipsis) rather than mapping into unrelated ellipsis characters.
            toWrapped = i => innerToWrapped(Math.Min(i, bestPrefixLength));
            toOriginal = best.ToOriginal;
            wrapping = best.Wrapping;
            return best.Text;
        }

        /// <summary>
        /// Fits <paramref name="text"/> within (<paramref name="boundsWidth"/>, <paramref name="boundsHeight"/>) -
        /// see <see cref="WrapToFitOnce"/>.
        /// </summary>
        /// <remarks>
        /// The underlying font measurement has been observed to be unreliable for roughly the first second a given
        /// (text, font, scale) combination is measured - eg. a name that ultimately needs three lines can measure
        /// as fitting on two for a short window right after a project opens, before settling into the correct,
        /// stable answer on every later measurement. <see cref="ComputeLines"/> (used by <see cref="FitsHeight"/>)
        /// already defends against per-call noise by retrying within one attempt, but that's not enough during this
        /// window: a caller with
        /// no cache (eg. the tile label, redrawn every frame) just self-heals once the window passes, but a caller
        /// that caches the first result it sees (eg. the rename edit box - see its own remarks) can lock in a wrong
        /// answer for its entire lifetime if that first computation happens to land inside the window. Calling
        /// <see cref="WrapToFitOnce"/> repeatedly here, until two consecutive attempts agree (or a bounded number of
        /// attempts is exhausted), keeps that unreliable window from ever being observed by either kind of caller.
        /// </remarks>
        /// <param name="text">The original text.</param>
        /// <param name="font">The font it will be drawn with.</param>
        /// <param name="scale">The font scale it will be drawn with.</param>
        /// <param name="boundsWidth">The available width for one line.</param>
        /// <param name="boundsHeight">The available height (eg. for exactly two lines).</param>
        /// <param name="toWrapped">Maps an index into <paramref name="text"/> to the corresponding index in the returned string (clamped to the nearest visible position if that part of the text was truncated away).</param>
        /// <param name="toOriginal">Maps an index into the returned string back to the corresponding index in <paramref name="text"/>.</param>
        /// <param name="wrapping">The wrapping mode to draw the returned text with.</param>
        /// <returns>The text to actually draw/measure.</returns>
        public static string WrapToFit(string text, Font font, float scale, float boundsWidth, float boundsHeight, out Func<int, int> toWrapped, out Func<int, int> toOriginal, out TextWrapping wrapping)
        {
            var result = WrapToFitOnce(text, font, scale, boundsWidth, boundsHeight, out toWrapped, out toOriginal, out wrapping);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var verify = WrapToFitOnce(text, font, scale, boundsWidth, boundsHeight, out var toWrapped2, out var toOriginal2, out var wrapping2);
                if (verify == result && wrapping2 == wrapping)
                    break;
                result = verify;
                toWrapped = toWrapped2;
                toOriginal = toOriginal2;
                wrapping = wrapping2;
            }
            return result;
        }

        /// <summary>
        /// Finds the end index of the whitespace/non-whitespace run in <paramref name="text"/> starting at
        /// <paramref name="start"/> (eg. a run of letters, or a run of spaces) - the unit <see cref="ComputeRenameLayout"/>
        /// packs onto lines whole, only breaking inside one when it doesn't fit any line by itself.
        /// </summary>
        private static int RunEnd(string text, int start)
        {
            if (start >= text.Length)
                return start;
            var isWhitespace = char.IsWhiteSpace(text[start]);
            var j = start + 1;
            while (j < text.Length && char.IsWhiteSpace(text[j]) == isWhitespace)
                j++;
            return j;
        }

        /// <summary>
        /// The real pixel distance between one line and the next for <paramref name="font"/> at <paramref name="scale"/>
        /// - measured empirically (the height of two explicitly-newline-separated lines minus the height of one),
        /// rather than assumed from a font metric like <c>Font.Height</c>: that metric represents a full line "cell"
        /// (eg. including extra leading beyond the glyphs themselves) and has been observed to be noticeably taller
        /// than the native renderer's real, tighter line-to-line spacing - using it directly as the pitch
        /// underestimated how many lines actually fit a given height, both truncating text that had room to wrap
        /// further and throwing off vertical centering (which sizes the whole text block off the same pitch).
        /// </summary>
        private static float LinePitch(Font font, float scale)
        {
            var layout = TextLayoutOptions.Default;
            layout.Scale = scale;
            layout.TextWrapping = TextWrapping.NoWrap;
            layout.HorizontalAlignment = TextAlignment.Near;
            layout.VerticalAlignment = TextAlignment.Near;
            layout.Bounds = new Rectangle(Float2.Zero, new Float2(float.MaxValue, float.MaxValue));
            var oneLine = font.MeasureText("Ag", ref layout).Y;
            var twoLines = font.MeasureText("Ag\nAg", ref layout).Y;
            var pitch = twoLines - oneLine;
            return pitch > 0.0f ? pitch : Math.Max(1.0f, font.Height * scale);
        }

        private static float AlignOffset(TextAlignment alignment, float available, float content)
        {
            switch (alignment)
            {
            case TextAlignment.Center: return (available - content) * 0.5f;
            case TextAlignment.Far: return available - content;
            default: return 0.0f;
            }
        }

        /// <summary>
        /// Rename-mode wrapping, rewritten from scratch to no longer depend on the native engine's own multi-line
        /// wrap/measure calls at all (the source of every wrap-versus-render mismatch this control has hit - a
        /// caret landing on the wrong line, a click not moving it, a third line silently clipped away). Line breaks
        /// and every character's on-screen position are computed directly here, in plain C#, from nothing but
        /// individual substring width measurements (<c>Font.MeasureText</c> in single-line, unbounded mode - a
        /// simple, self-contained query, not the native engine's own multi-line layout decision) - so what this
        /// function decides "fits" and what <see cref="WrappingRenameTextBox"/> actually draws are, by construction,
        /// always the exact same thing.
        /// </summary>
        /// <remarks>
        /// Greedily packs whole whitespace/non-whitespace runs (see <see cref="RunEnd"/>) onto each line - so
        /// wrapping still prefers to break between words, exactly as before. A run only gets split mid-run, by raw
        /// character width, when it's wider than the line all by itself - and even then, character width is *all*
        /// that decides where: '-', '_' and uppercase letters are no longer treated as preferred break points the
        /// way <see cref="WrapToFit"/> (used by the finished, non-editing label) treats them. That special-casing
        /// was a repeated source of its own bugs (an unreliable native wrap decision, a caret that could land
        /// somewhere unrelated to the actual text) without being needed here: this control just needs to reliably
        /// wrap wherever it runs out of horizontal room, not construct grammatically sensible line breaks.
        /// </remarks>
        /// <param name="text">The original text.</param>
        /// <param name="font">The font it will be drawn with.</param>
        /// <param name="scale">The font scale it will be drawn with.</param>
        /// <param name="bounds">The area text is laid out within (its X/Y is the top-left origin every returned position is relative to).</param>
        /// <param name="horizontalAlignment">How each line is aligned within <paramref name="bounds"/>'s width.</param>
        /// <param name="verticalAlignment">How the whole block of lines is aligned within <paramref name="bounds"/>'s height.</param>
        /// <returns>
        /// The text to actually draw (<paramref name="text"/> unchanged, unless it needed shortening with a
        /// trailing "..." to fit), every character's position (indices 0 through the returned text's length,
        /// inclusive), and each line's (Start, End, Position) span into that same returned text.
        /// </returns>
        public static (string Text, Float2[] Positions, (int Start, int End, Float2 Position)[] Lines, Func<int, int> ToDraw, Func<int, int> ToOriginal) ComputeRenameLayout(string text, Font font, float scale, Rectangle bounds, TextAlignment horizontalAlignment, TextAlignment verticalAlignment)
        {
            if (font == null)
                return (text, new[] { bounds.Location }, new[] { (0, 0, bounds.Location) }, i => i, i => i);

            var measureLayout = UnboundedLayout(scale);
            float Width(string s) => s.Length == 0 ? 0f : font.MeasureText(s, ref measureLayout).X;

            var lineHeight = LinePitch(font, scale);
            var maxLines = Math.Max(1, Mathf.FloorToInt((bounds.Height + 0.5f) / lineHeight));

            // Pack whole runs onto each line greedily; a run that doesn't fit an empty line gets character-wrapped
            // (ignoring '-'/'_'/case entirely - see the class remarks above).
            var lineSpans = new System.Collections.Generic.List<(int Start, int End)>();
            var cursor = 0;
            while (cursor < text.Length && lineSpans.Count < maxLines)
            {
                var lineStart = cursor;
                while (cursor < text.Length)
                {
                    var runEnd = RunEnd(text, cursor);
                    var runWidth = Width(text.Substring(cursor, runEnd - cursor));
                    var usedWidth = cursor > lineStart ? Width(text.Substring(lineStart, cursor - lineStart)) : 0f;

                    if (usedWidth + runWidth <= bounds.Width)
                    {
                        cursor = runEnd;
                        continue;
                    }

                    if (cursor == lineStart)
                    {
                        // Even a whole, empty line can't fit this run - character-wrap within it alone.
                        var c = cursor;
                        while (c < runEnd)
                        {
                            if (Width(text.Substring(cursor, c + 1 - cursor)) > bounds.Width && c > cursor)
                                break;
                            c++;
                        }
                        if (c == cursor)
                            c = cursor + 1; // Always consume at least one character, or the outer loop never ends.
                        cursor = c;
                    }
                    break;
                }
                lineSpans.Add((lineStart, cursor));
            }
            if (lineSpans.Count == 0)
            {
                // Empty text (or nothing fit at all) - still lay out one empty line, aligned the same way any other
                // line would be, so an emptied-out box centers its caret instead of leaving it pinned at the
                // unaligned top-left corner of the wrap bounds.
                lineSpans.Add((0, 0));
            }

            string displayText;
            var toDraw = (Func<int, int>)(i => i);
            var toOriginal = (Func<int, int>)(i => i);
            if (cursor < text.Length)
            {
                // More text remains than maxLines has room for - replace the last line with as much as fits
                // followed by "...", ignoring run boundaries entirely (this is a hard cutoff, not a wrap point).
                var lastIndex = lineSpans.Count - 1;
                var lineStart = lineSpans[lastIndex].Start;
                var keptLength = 0;
                for (var c = lineStart; c <= text.Length; c++)
                {
                    var candidate = text.Substring(lineStart, c - lineStart).TrimEnd() + Ellipsis;
                    if (Width(candidate) <= bounds.Width)
                        keptLength = c - lineStart;
                    else if (c > lineStart)
                        break;
                }
                var kept = text.Substring(lineStart, keptLength).TrimEnd();
                displayText = text.Substring(0, lineStart) + kept + Ellipsis;
                lineSpans[lastIndex] = (lineStart, displayText.Length);

                var truncateAt = lineStart + kept.Length;
                toDraw = i => i <= truncateAt ? i : truncateAt;
                toOriginal = i => Math.Min(i, truncateAt);
            }
            else
            {
                displayText = text;
            }

            // Compute every line's own width/position, and every character's position within it.
            var totalHeight = lineSpans.Count * lineHeight;
            var startY = bounds.Y + AlignOffset(verticalAlignment, bounds.Height, totalHeight);
            var positions = new Float2[displayText.Length + 1];
            var lines = new (int, int, Float2)[lineSpans.Count];
            for (var li = 0; li < lineSpans.Count; li++)
            {
                var (start, end) = lineSpans[li];
                var lineWidth = Width(displayText.Substring(start, end - start));
                var x = bounds.X + AlignOffset(horizontalAlignment, bounds.Width, lineWidth);
                var y = startY + li * lineHeight;
                lines[li] = (start, end, new Float2(x, y));
                for (var i = start; i <= end; i++)
                    positions[i] = new Float2(x + Width(displayText.Substring(start, i - start)), y);
            }

            return (displayText, positions, lines, toDraw, toOriginal);
        }
    }
}
