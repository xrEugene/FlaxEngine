// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI
{
    /// <summary>
    /// A <see cref="TextBox"/> used by <see cref="RenamePopup"/> for wrapped, multi-line renaming (eg. the Content
    /// window's grid tiles). The base <see cref="TextBox"/> draws every selected line except the last at an
    /// effectively-infinite width when a selection spans multiple lines - a reasonable shortcut for left-to-right,
    /// left-aligned text (where "select to end of line" naturally means "go to the far right"), but wrong for
    /// center-aligned wrapped text, where it paints over empty space around each shorter, centered line. This
    /// override measures each line's own actual text extent instead, so the highlight only ever covers real text.
    /// </summary>
    internal sealed class WrappingRenameTextBox : TextBox
    {
        /// <inheritdoc />
        public WrappingRenameTextBox(bool isMultiline, float x, float y, float width = 120)
        : base(isMultiline, x, y, width)
        {
        }

        /// <summary>
        /// The font scale used for text layout and drawing, matching the scale the finished (non-editing) label is
        /// drawn with (eg. <see cref="FlaxEditor.Content.ContentItem"/> tiles scale their label text by the
        /// content view's zoom level), so wrapping happens at the same point during editing as it does once finished.
        /// Named distinctly from the inherited <see cref="Control.Scale"/> (a <see cref="Float2"/> UI transform) to
        /// avoid hiding it.
        /// </summary>
        public float TextScale { get; set; } = 1.0f;

        /// <summary>
        /// Total horizontal margin (split evenly across both sides) to inset from the control's full width when
        /// laying out text - see <see cref="GetWrapBounds"/>. Matches
        /// <see cref="FlaxEditor.Content.ContentItem.GetWrapTextRectangle(Rectangle, Float2)"/>'s margin, so a name
        /// wraps at exactly the same width while being renamed as it does once finished: this control's own
        /// inherited <see cref="TextBoxBase.TextRectangle"/> insets by a fixed pixel amount meant for caret/padding
        /// breathing room, unrelated to and different from that proportional margin, and being a few pixels wider
        /// was enough to flip a borderline name's wrap point between the two.
        /// </summary>
        public float WrapWidthMargin { get; set; }

        private bool _hasCachedEffective;
        private string _cachedSourceText;
        private Rectangle _cachedBounds;
        private (string Text, TextWrapping Wrapping, Func<int, int> ToDraw, Func<int, int> ToOriginal, Float2[] Positions, (int Start, int End, Float2 Position)[] Lines) _cachedEffective;

        private Font GetActualFont()
        {
            if (Bold)
                return Italic ? Font.GetBold().GetItalic().GetFont() : Font.GetBold().GetFont();
            if (Italic)
                return Font.GetItalic().GetFont();
            return Font.GetFont();
        }

        /// <summary>
        /// The rectangle text is actually laid out/measured/drawn against: the control's full area, inset by
        /// <see cref="WrapWidthMargin"/> - see its remarks for why this replaces the inherited, differently-inset
        /// <see cref="TextBoxBase.TextRectangle"/> for layout purposes.
        /// </summary>
        private Rectangle GetWrapBounds()
        {
            return new Rectangle(WrapWidthMargin * 0.5f, 0, Size.X - WrapWidthMargin, Size.Y);
        }

        private TextLayoutOptions GetLayout()
        {
            var layout = TextLayoutOptions.Default;
            layout.HorizontalAlignment = HorizontalAlignment;
            layout.VerticalAlignment = VerticalAlignment;
            layout.TextWrapping = Wrapping;
            layout.Bounds = GetWrapBounds();
            layout.Scale = TextScale;
            return layout;
        }

        /// <summary>
        /// Computes the text to actually draw/measure and the wrapping mode to use for it: unchanged if not set to
        /// word-wrap, otherwise routed through <see cref="SingleWordWrap.ComputeRenameLayout"/>, which computes line
        /// breaks and every character's on-screen position directly - not through the native engine's own multi-line
        /// wrap/measure calls at all, so there's nothing left for a later, separate native call to disagree with.
        /// </summary>
        /// <remarks>
        /// Caches the result per (text, bounds) pair, and verifies it by recomputing up to a few times until two
        /// consecutive attempts agree before caching - even a single, simple <c>Font.MeasureText</c> call (all
        /// <see cref="SingleWordWrap.ComputeRenameLayout"/> uses) has been observed to occasionally disagree with
        /// itself for the same input during roughly the first second a given (text, font, scale) combination is
        /// measured, and caching a result from inside that window would otherwise lock in a wrong wrap for this
        /// control's entire lifetime.
        /// </remarks>
        private (string Text, TextWrapping Wrapping, Func<int, int> ToDraw, Func<int, int> ToOriginal, Float2[] Positions, (int Start, int End, Float2 Position)[] Lines) GetEffectiveText(Font font, Rectangle bounds)
        {
            var text = Text;
            if (Wrapping != TextWrapping.WrapWords)
                return (text, Wrapping, i => i, i => i, null, null);

            if (_hasCachedEffective && _cachedSourceText == text && _cachedBounds == bounds)
                return _cachedEffective;

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

            var result = SingleWordWrap.ComputeRenameLayout(text, font, TextScale, bounds, HorizontalAlignment, VerticalAlignment);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var verify = SingleWordWrap.ComputeRenameLayout(text, font, TextScale, bounds, HorizontalAlignment, VerticalAlignment);
                if (verify.Text == result.Text && PositionsMatch(verify.Positions, result.Positions))
                    break;
                result = verify;
            }

            _cachedEffective = (result.Text, TextWrapping.NoWrap, result.ToDraw, result.ToOriginal, result.Positions, result.Lines);
            _cachedSourceText = text;
            _cachedBounds = bounds;
            _hasCachedEffective = true;
            return _cachedEffective;
        }

        /// <inheritdoc />
        public override void ScrollToCaret()
        {
            // This box always shows its whole wrapped text within a fixed area (matching the tile's allocated
            // space) - it never needs to scroll. The base implementation assumes single-line horizontal scrolling:
            // it clamps the caret into view by shifting ViewOffset uniformly, which is wrong here since each
            // wrapped line is independently centered - a uniform horizontal shift crops every line's start
            // inconsistently instead of doing anything sensible for the one line that actually needs it.
        }

        /// <inheritdoc />
        public override Float2 GetTextSize()
        {
            var font = GetActualFont();
            if (!font)
                return Float2.Zero;
            var layout = GetLayout();
            var effective = GetEffectiveText(font, layout.Bounds);
            layout.TextWrapping = effective.Wrapping;
            return font.MeasureText(effective.Text, ref layout);
        }

        /// <inheritdoc />
        public override Float2 GetCharPosition(int index, out float height)
        {
            var font = GetActualFont();
            if (!font)
            {
                height = Height;
                return Float2.Zero;
            }

            // The base TextBox implementation measures through its own private, unscaled layout - fine for it since
            // it never wraps at a non-1 scale, but the caret (TextBoxBase.CaretBounds calls this virtually) and
            // keyboard/mouse navigation need to agree with what's actually drawn here, at TextScale.
            height = font.Height * TextScale / DpiScale;
            var layout = GetLayout();
            var effective = GetEffectiveText(font, layout.Bounds);
            var drawIndex = effective.ToDraw(index);
            if (effective.Positions != null)
                return effective.Positions[Math.Clamp(drawIndex, 0, effective.Positions.Length - 1)];

            layout.TextWrapping = effective.Wrapping;
            var charLayout = SingleWordWrap.CharMeasureLayout(TextScale);
            return SingleWordWrap.GetCharPositionSafe(font, effective.Text, drawIndex, layout, charLayout);
        }

        /// <inheritdoc />
        public override int HitTestText(Float2 location)
        {
            var font = GetActualFont();
            if (!font)
                return 0;

            var layout = GetLayout();
            var effective = GetEffectiveText(font, layout.Bounds);

            if (effective.Lines != null && effective.Positions != null)
            {
                // Hit-test against the exact same cached per-character positions DrawSelf renders from and
                // GetCharPosition reports, instead of asking the native engine to hit-test effective.Text fresh: that's
                // a third independent native wrap/measure call, and it's been observed to disagree with what's
                // actually drawn - eg. clicking a visibly-correct second or third line landing the caret back where
                // it already was, since the native call's own idea of where that line's characters sit doesn't match
                // the cached positions the click is really being compared against on screen.
                var lines = effective.Lines;
                var positions = effective.Positions;

                var lineIndex = 0;
                var bestLineDist = float.MaxValue;
                for (int i = 0; i < lines.Length; i++)
                {
                    var dist = Math.Abs(lines[i].Position.Y - location.Y);
                    if (dist < bestLineDist)
                    {
                        bestLineDist = dist;
                        lineIndex = i;
                    }
                }

                var line = lines[lineIndex];
                var bestIndex = line.Start;
                var bestCharDist = float.MaxValue;
                for (int i = line.Start; i <= line.End; i++)
                {
                    var pos = positions[Math.Clamp(i, 0, positions.Length - 1)];
                    var dist = Math.Abs(pos.X - location.X);
                    if (dist < bestCharDist)
                    {
                        bestCharDist = dist;
                        bestIndex = i;
                    }
                }

                return effective.ToOriginal(bestIndex);
            }

            layout.TextWrapping = effective.Wrapping;
            var drawIndex = font.HitTestText(effective.Text, location, ref layout);
            return effective.ToOriginal(drawIndex);
        }

        /// <inheritdoc />
        public override void DrawSelf()
        {
            // Cache data
            var rect = new Rectangle(Float2.Zero, Size);
            bool enabled = EnabledInHierarchy;
            var font = GetActualFont();
            if (!font)
                return;

            // Background (constant regardless of hover/focus state)
            Render2D.FillRectangle(rect, BackgroundColor);
            if (HasBorder)
            {
                if (IsFocused)
                {
                    // Focused state: accent line along the bottom edge only, at double thickness
                    var lineThickness = BorderThickness * 3.0f;
                    var lineOffsetY = 1.0f;
                    var bottomLine = new Rectangle(rect.Left, rect.Bottom - lineThickness + lineOffsetY, rect.Width, lineThickness);
                    Render2D.FillRectangle(bottomLine, BorderSelectedColor);
                }
                else
                {
                    Render2D.DrawRectangle(rect, BorderColor, BorderThickness);
                }
            }

            var layout = GetLayout();
            var text = Text;
            var effective = GetEffectiveText(font, layout.Bounds);
            var drawText = effective.Text;
            var toDraw = effective.ToDraw;
            layout.TextWrapping = effective.Wrapping;
            var charLayout = SingleWordWrap.CharMeasureLayout(TextScale);
            var positions = effective.Positions;
            Float2 GetPos(int i)
            {
                var drawIndex = toDraw(i);
                return positions != null
                       ? positions[Math.Clamp(drawIndex, 0, positions.Length - 1)]
                       : SingleWordWrap.GetCharPositionSafe(font, drawText, drawIndex, layout, charLayout);
            }

            // Apply view offset and clip mask
            if (ClipText)
                Render2D.PushClip(TextClipRectangle);
            bool useViewOffset = !ViewOffset.IsZero;
            if (useViewOffset)
                Render2D.PushTransform(Matrix3x3.Translation2D(-ViewOffset));

            // Check if sth is selected to draw selection
            if (HasSelection && IsFocused)
            {
                var leftEdge = GetPos(SelectionLeft);
                var rightEdge = GetPos(SelectionRight);
                float fontHeight = font.Height;
#if PLATFORM_MAC && !PLATFORM_SDL
                fontHeight /= (float)Platform.Dpi / 96.0f; // TODO: refactor DPI support on macOS to skip such hacks
#endif
                // GetCharPosition (above) reports positions in the scaled coordinate space of `layout` (Scale =
                // TextScale), so the line height used to size each highlight rect must be scaled the same way, or a
                // non-1 TextScale throws off the rect bounds.
                float textHeight = fontHeight * TextScale / DpiScale;

                // Draw selection background
                float alpha = Mathf.Min(1.0f, Mathf.Cos(_animateTime * BackgroundSelectedFlashSpeed) * 0.5f + 1.3f);
                alpha *= alpha;
                Color selectionColor = SelectionColor * alpha;

                // Whether the selection spans one line or several is decided by directly comparing the two edges'
                // actual Y positions, not by dividing their Y difference by textHeight: that division only works if
                // textHeight happens to exactly match the real native line spacing at this particular scale, and
                // when it doesn't, the ratio can floor to 0 - treating a genuinely multi-line selection as a single
                // line, which draws one rect straight from leftEdge to rightEdge and skips every line in between
                // entirely (rather than, say, just sizing that one rect slightly wrong).
                bool isSingleLine = Mathf.NearEqual(leftEdge.Y, rightEdge.Y);
                if (isSingleLine)
                {
                    // Selected is part of single line
                    Rectangle r1 = new Rectangle(leftEdge.X, leftEdge.Y, rightEdge.X - leftEdge.X, textHeight);
                    Render2D.FillRectangle(r1, selectionColor);
                }
                else
                {
                    // Selected is more than one line - walk every selected character and highlight each visual line
                    // tight to its actual text extent. GetCharPosition(i) returns the START of character i, so just
                    // tracking the highest start position seen on a line under-shoots by that character's own glyph
                    // width - clipping the last character of the line out of the highlight. And the character whose
                    // Y first differs (closing the line) isn't a reliable right edge for the line it's closing
                    // either: for a wrapped-away trailing space it can be measured as still on the old line or
                    // already on the new one, and even when it's unambiguously the new line's first character, that
                    // position is independently centered for that (differently sized) line and bears no relation to
                    // where the previous line ends. So each line is closed using its own last VISIBLE (non-
                    // whitespace) character's start position plus that one character's own measured width, which is
                    // accurate regardless of any of the above.
                    float lineY = leftEdge.Y;
                    float lineLeft = leftEdge.X;
                    float lineRight = leftEdge.X;
                    int lastVisibleIndex = SelectionLeft;
                    Float2 lastVisiblePos = leftEdge;
                    for (int i = SelectionLeft; i <= SelectionRight; i++)
                    {
                        var pos = GetPos(i);
                        if (!Mathf.NearEqual(pos.Y, lineY))
                        {
                            var closeRight = lineRight;
                            if (lastVisibleIndex < text.Length && Mathf.NearEqual(lastVisiblePos.Y, lineY))
                            {
                                var charWidth = font.MeasureText(text.Substring(lastVisibleIndex, 1), ref charLayout).X;
                                closeRight = Mathf.Max(closeRight, lastVisiblePos.X + charWidth);
                            }
                            Render2D.FillRectangle(new Rectangle(lineLeft, lineY, closeRight - lineLeft, textHeight), selectionColor);
                            lineY = pos.Y;
                            lineLeft = pos.X;
                            lineRight = pos.X;
                        }
                        else if (pos.X > lineRight)
                        {
                            lineRight = pos.X;
                        }

                        if (i < text.Length && !char.IsWhiteSpace(text[i]))
                        {
                            lastVisibleIndex = i;
                            lastVisiblePos = pos;
                        }
                    }
                    {
                        var closeRight = lineRight;
                        if (lastVisibleIndex < text.Length && Mathf.NearEqual(lastVisiblePos.Y, lineY))
                        {
                            var charWidth = font.MeasureText(text.Substring(lastVisibleIndex, 1), ref charLayout).X;
                            closeRight = Mathf.Max(closeRight, lastVisiblePos.X + charWidth);
                        }
                        Render2D.FillRectangle(new Rectangle(lineLeft, lineY, closeRight - lineLeft, textHeight), selectionColor);
                    }
                }
            }

            // Text or watermark
            if (text.Length > 0)
            {
                var color = TextColor;
                if (!enabled)
                    color *= 0.6f;
                else if (IsReadOnly)
                    color *= 0.85f;

                if (effective.Lines != null)
                {
                    // Draw each cached line explicitly at its cached position instead of handing the whole
                    // multi-line string to DrawText, which would re-run the native wrap from scratch - see the
                    // GetEffectiveText remarks for why that's unsafe to do every frame.
                    var lineLayout = TextLayoutOptions.Default;
                    lineLayout.Scale = TextScale;
                    lineLayout.TextWrapping = TextWrapping.NoWrap;
                    lineLayout.HorizontalAlignment = TextAlignment.Near;
                    lineLayout.VerticalAlignment = TextAlignment.Near;
                    foreach (var line in effective.Lines)
                    {
                        if (line.End <= line.Start)
                            continue;
                        // Strip any literal '\n' (from a hyphen-split insertion, see SingleWordWrap) - line breaks
                        // are handled by drawing each cached line separately already, so a leftover newline here
                        // would only be a stray, invisible-but-line-affecting character.
                        var lineText = drawText.Substring(line.Start, line.End - line.Start).Replace("\n", "");
                        if (lineText.Length == 0)
                            continue;
                        lineLayout.Bounds = new Rectangle(line.Position, new Float2(float.MaxValue, float.MaxValue));
                        Render2D.DrawText(font, lineText, color, ref lineLayout, TextMaterial);
                    }
                }
                else
                {
                    Render2D.DrawText(font, drawText, color, ref layout, TextMaterial);
                }
            }
            else
            {
                string watermark = WatermarkText;
                if (!string.IsNullOrEmpty(watermark))
                {
                    Render2D.DrawText(font, watermark, WatermarkTextColor, ref layout, TextMaterial);
                }
            }

            // Caret
            if (IsFocused && CaretPosition > -1)
            {
                float alpha = Mathf.Saturate(Mathf.Cos(_animateTime * CaretFlashSpeed) * 0.5f + 0.8f);
                alpha = alpha * alpha;
                Render2D.FillRectangle(CaretBounds, CaretColor * alpha);
            }

            // Restore rendering state
            if (useViewOffset)
                Render2D.PopTransform();
            if (ClipText)
                Render2D.PopClip();
        }
    }
}
