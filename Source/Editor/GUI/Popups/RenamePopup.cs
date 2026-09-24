// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEditor.GUI.ContextMenu;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI
{
    /// <summary>
    /// Popup menu useful for renaming objects via UI. Displays text box for renaming.
    /// </summary>
    /// <seealso cref="ContextMenuBase" />
    public class RenamePopup : ContextMenuBase
    {
        /// <summary>
        /// Extra height (in UI units) added on top of the text box's default height for single-line input boxes, so
        /// descenders (eg. on "g", "y", "p", "q", "j") aren't clipped by the box's bottom edge.
        /// </summary>
        private const float DescenderPadding = 3.0f;

        private string _startValue;
        private TextBox _inputField;
        private bool _fitToContent;
        private float _maxWidth;

        /// <summary>
        /// Occurs when renaming is done.
        /// </summary>
        public event Action<RenamePopup> Renamed;

        /// <summary>
        /// Occurs when popup is closing (after renaming done or not).
        /// </summary>
        public event Action<RenamePopup> Closed;

        /// <summary>
        /// Input value validation delegate.
        /// </summary>
        /// <param name="popup">The popup reference.</param>
        /// <param name="value">The input text value.</param>
        /// <returns>True if text is valid, otherwise false.</returns>
        public delegate bool ValidateDelegate(RenamePopup popup, string value);

        /// <summary>
        /// Occurs when input text validation should be performed.
        /// </summary>
        public ValidateDelegate Validate;

        /// <summary>
        /// Gets or sets the initial value.
        /// </summary>
        public string InitialValue
        {
            get => _startValue;
            set => _startValue = value;
        }

        /// <summary>
        /// Gets or sets the input field text.
        /// </summary>
        public string Text
        {
            get => _inputField.Text;
            set => _inputField.Text = value;
        }

        /// <summary>
        /// Gets the text input field control.
        /// </summary>
        public TextBox InputField => _inputField;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenamePopup"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="size">The size.</param>
        /// <param name="isMultiline">Enable/disable multiline text input support</param>
        /// <param name="horizontalAlignment">The horizontal alignment of the input text.</param>
        /// <param name="fitToContent">
        /// True to size the input box width to fit the text (up to <paramref name="size"/>'s width) and keep resizing it
        /// live as the user types, instead of always stretching to fill the whole given size. Ignored when <paramref name="isMultiline"/> is true.
        /// </param>
        /// <param name="wrapWords">
        /// True to word-wrap the text and use <see cref="WrappingRenameTextBox"/> instead of the plain <see cref="TextBox"/>,
        /// so multi-line selection highlighting stays accurate with non-left alignments (eg. Center, as used by the
        /// Content window's grid tiles). Only meaningful when <paramref name="isMultiline"/> is true.
        /// </param>
        /// <param name="textScale">
        /// The font scale to draw the text with when <paramref name="wrapWords"/> is used, so wrapping happens at the
        /// same point as the finished (non-editing) label it's replacing. Only meaningful when <paramref name="wrapWords"/>
        /// and <paramref name="isMultiline"/> are both true.
        /// </param>
        /// <param name="wrapWidthMargin">
        /// Total horizontal margin (split evenly across both sides) to inset from the full width when wrapping text,
        /// when <paramref name="wrapWords"/> is used - see <see cref="WrappingRenameTextBox.WrapWidthMargin"/>. Only
        /// meaningful when <paramref name="wrapWords"/> and <paramref name="isMultiline"/> are both true.
        /// </param>
        public RenamePopup(string value, Float2 size, bool isMultiline, TextAlignment horizontalAlignment = TextAlignment.Center, bool fitToContent = false, bool wrapWords = false, float textScale = 1.0f, float wrapWidthMargin = 0.0f)
        {
            if (!isMultiline)
                size.Y = TextBox.DefaultHeight + DescenderPadding;

            _fitToContent = fitToContent && !isMultiline;
            _maxWidth = size.X;

            // The popup itself (and its opaque background fill from ContextMenuBase.Draw) always covers the full
            // originally available width, regardless of fitToContent. It's a separate overlay window on top of the
            // main editor window - the row underneath keeps rendering itself (highlight + label) completely normally,
            // clipped only by its own host panel, for as long as renaming is in progress. Only actually covering that
            // same full extent hides it; a popup that shrinks to the typed text would leave a gap showing through.
            Size = size;

            _startValue = value;

            // Give the field its full generous width up front (not a narrow guess) - GetFitToContentWidth() below
            // measures through this same field, and starting it too narrow can throw that measurement off.
            _inputField = wrapWords && isMultiline
                          ? new WrappingRenameTextBox(isMultiline, 0, 0, size.X) { Wrapping = TextWrapping.WrapWords, TextScale = textScale, WrapWidthMargin = wrapWidthMargin }
                          : new TextBox(isMultiline, 0, 0, size.X);
            _inputField.TextChanged += OnTextChanged;
            _inputField.Text = _startValue;
            _inputField.HorizontalAlignment = horizontalAlignment;
            _inputField.VerticalAlignment = TextAlignment.Center;

            if (_fitToContent)
            {
                // Only the visible input box narrows to fit the text; the popup around it stays full-width (see above).
                _inputField.AnchorPreset = AnchorPresets.TopLeft;
                _inputField.Bounds = new Rectangle(Float2.Zero, new Float2(GetFitToContentWidth(), size.Y));
            }
            else
            {
                _inputField.AnchorPreset = AnchorPresets.StretchAll;
                _inputField.Offsets = Margin.Zero;
            }

            _inputField.Parent = this;
        }

        private bool IsInputValid => !string.IsNullOrWhiteSpace(_inputField.Text) && (_inputField.Text == _startValue || Validate == null || Validate(this, _inputField.Text));

        /// <inheritdoc />
        public override void Update(float deltaTime)
        {
            var mouseLocation = Root.MousePosition;
            if (!ContainsPoint(ref mouseLocation) && RootWindow.ContainsFocus && Text != _startValue)
            {
                // rename item before closing if left mouse button in clicked
                if (FlaxEngine.Input.GetMouseButtonDown(MouseButton.Left))
                    OnEnd();
            }

            base.Update(deltaTime);
        }

        private void OnTextChanged()
        {
            if (_fitToContent)
            {
                var newWidth = GetFitToContentWidth();
                if (!Mathf.NearEqual(newWidth, _inputField.Width))
                    _inputField.Width = newWidth;
            }

            if (Validate == null)
                return;

            var valid = IsInputValid;
            var style = Style.Current;
            if (valid)
            {
                _inputField.BorderColor = Color.Transparent;
                _inputField.BorderSelectedColor = style.BackgroundSelected;
            }
            else
            {
                var color = new Color(1.0f, 0.0f, 0.02745f, 1.0f);
                _inputField.BorderColor = Color.Lerp(color, style.TextBoxBackground, 0.6f);
                _inputField.BorderSelectedColor = color;
            }
        }

        /// <summary>
        /// The minimum width (in UI units) given to a rename input box when <c>fitToContent</c> shrinks it to the text.
        /// </summary>
        private const float MinFitToContentWidth = 60.0f;

        /// <summary>
        /// The multiplier applied to the measured text width when <c>fitToContent</c> is used, so the box has some
        /// breathing room around the text (1.2 = 20% wider than the text itself).
        /// </summary>
        private const float FitToContentWidthMultiplier = 1.2f;

        /// <summary>
        /// Computes the input box width for the input field's current text: its own <see cref="TextBox.GetTextSize"/>
        /// measurement scaled by <see cref="FitToContentWidthMultiplier"/>, clamped between
        /// <see cref="MinFitToContentWidth"/> and <see cref="_maxWidth"/> (the box never grows past the space that was
        /// originally available to it).
        /// </summary>
        private float GetFitToContentWidth()
        {
            var textWidth = _inputField.GetTextSize().X;
            return Mathf.Clamp(textWidth * FitToContentWidthMultiplier, MinFitToContentWidth, _maxWidth);
        }

        /// <summary>
        /// Shows the rename popup.
        /// </summary>
        /// <param name="control">The target control.</param>
        /// <param name="area">The target control area to cover.</param>
        /// <param name="value">The initial value.</param>
        /// <param name="isMultiline">Enable/disable multiline text input support</param>
        /// <param name="horizontalAlignment">The horizontal alignment of the input text.</param>
        /// <param name="fitToContent">
        /// True to size the input box width to fit <paramref name="value"/> (up to the width of <paramref name="area"/>),
        /// resizing it live as the user types, instead of always stretching to fill the whole area. Ignored when
        /// <paramref name="isMultiline"/> is true.
        /// </param>
        /// <param name="wrapWords">
        /// True to word-wrap the text with accurate multi-line selection highlighting for non-left alignments (see
        /// <see cref="WrappingRenameTextBox"/>). Only meaningful when <paramref name="isMultiline"/> is true.
        /// </param>
        /// <param name="textScale">
        /// The font scale to draw the text with when <paramref name="wrapWords"/> is used, so wrapping happens at the
        /// same point as the finished (non-editing) label it's replacing.
        /// </param>
        /// <param name="wrapWidthMargin">
        /// Total horizontal margin (split evenly across both sides) to inset from the full width when wrapping text,
        /// when <paramref name="wrapWords"/> is used - see <see cref="WrappingRenameTextBox.WrapWidthMargin"/>.
        /// </param>
        /// <returns>Created popup.</returns>
        public static RenamePopup Show(Control control, Rectangle area, string value, bool isMultiline, TextAlignment horizontalAlignment = TextAlignment.Center, bool fitToContent = false, bool wrapWords = false, float textScale = 1.0f, float wrapWidthMargin = 0.0f)
        {
            // hardcoded flushing layout for tree controls
            if (control is Tree.TreeNode treeNode && treeNode.ParentTree != null)
                treeNode.ParentTree.FlushPendingPerformLayout();

            // Calculate the control size in the window space to handle scaled controls
            var upperLeft = control.PointToWindow(area.UpperLeft);
            var bottomRight = control.PointToWindow(area.BottomRight);
            var size = bottomRight - upperLeft;

            if (fitToContent && !isMultiline)
            {
                // The area passed in (eg. a tree row's TextRect) doesn't always match what's actually visible on screen:
                // a Tree control widens itself (and every TreeNode row cascades that width) to fit its widest node, for
                // horizontal-scroll support, so tree rows report that inflated width rather than their host Panel's real,
                // anchor-constrained viewport width. Walk up to the nearest Panel ancestor (the actual scroll viewport)
                // and cap against its visible right edge instead; fall back to the enclosing window's right edge if none.
                Panel clipPanel = null;
                var clipAncestor = control.Parent;
                while (clipAncestor != null)
                {
                    if (clipAncestor is Panel panel)
                    {
                        clipPanel = panel;
                        break;
                    }
                    clipAncestor = clipAncestor.Parent;
                }
                float? clipRightX = null;
                if (clipPanel != null)
                {
                    // Exclude the vertical scrollbar (if visible) from the available width, otherwise the box can
                    // overlap it.
                    clipPanel.GetDesireClientArea(out var clientArea);
                    clipRightX = clipPanel.PointToWindow(new Float2(clientArea.Right, 0)).X;
                }

                var rootWindow = control.RootWindow;
                var windowRightX = rootWindow != null ? rootWindow.Width : (float?)null;

                // This is the hard ceiling GetFitToContentWidth() clamps against: the box can grow up to here (never
                // just the row's own, possibly too-narrow, reported TextRect width) before it stops expanding.
                var availableRightX = Mathf.Min(clipRightX ?? float.MaxValue, windowRightX ?? float.MaxValue);
                if (availableRightX < float.MaxValue)
                {
                    // A hair past the computed edge on purpose: better to slightly overshoot than leave any gap
                    // that lets what's underneath show through, but not so much it visibly overlaps the scrollbar.
                    var available = availableRightX - upperLeft.X + 1.0f;
                    if (available > 0.0f)
                        size.X = available;
                }
            }

            var rename = new RenamePopup(value, size, isMultiline, horizontalAlignment, fitToContent, wrapWords, textScale, wrapWidthMargin);
            rename.Show(control, area.Location + new Float2(0, (size.Y - rename.Height) * 0.5f));
            return rename;
        }

        private void OnEnd()
        {
            var text = Text;
            if (text != _startValue && IsInputValid)
            {
                Renamed?.Invoke(this);
            }

            Hide();
        }

        /// <inheritdoc />
        public override void Draw()
        {
            var style = Style.Current;
            // fitToContent is used for tree row renames: the popup covers a wider, deliberately invisible area than
            // the visible input box, so its fill should match the row's own selected-but-unfocused background
            // (TreeNode.BackgroundColorSelectedUnfocused) instead of the generic content background, or the two
            // would visibly mismatch right where the popup's cover picks up past the row's own rendering.
            var backgroundColor = _fitToContent ? Color.Lerp(style.ContentBackground, style.BackgroundHighlighted, 0.5f) : style.ContentBackground;
            Render2D.FillRectangle(new Rectangle(Float2.Zero, Size), backgroundColor);
            if (!_fitToContent)
            {
                // Matches ContextMenuBase.Draw()'s own outline - skipped for fitToContent, where the popup's bounds
                // are a deliberately invisible cover for whatever's underneath (wider than the visible input box),
                // so tracing a border around the full popup would show as a stray outline past the box's real edge.
                Render2D.DrawRectangle(new Rectangle(Float2.Zero, Size), Color.Lerp(style.BackgroundSelected, style.Background, 0.6f));
            }
            DrawChildren();
        }

        /// <inheritdoc />
        protected override bool UseAutomaticDirectionFix => false;

        /// <inheritdoc />
        public override bool OnKeyDown(KeyboardKeys key)
        {
            // Enter
            if (key == KeyboardKeys.Return)
            {
                OnEnd();
                return true;
            }
            // Esc
            if (key == KeyboardKeys.Escape)
            {
                Hide();
                return true;
            }

            // Base
            return base.OnKeyDown(key);
        }

        /// <inheritdoc />
        protected override void OnShow()
        {
            _inputField.EndEditOnClick = false; // Ending edit is handled through popup
            _inputField.Focus();
            _inputField.SelectAll();

            base.OnShow();
        }

        /// <inheritdoc />
        protected override void OnHide()
        {
            Closed?.Invoke(this);
            Closed = null;

            base.OnHide();

            // Remove itself
            Dispose();
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            Renamed = null;
            Closed = null;
            Validate = null;
            _inputField = null;

            base.OnDestroy();
        }
    }
}
