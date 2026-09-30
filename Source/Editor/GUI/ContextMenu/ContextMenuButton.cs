// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI.ContextMenu
{
    /// <summary>
    /// Context Menu button control.
    /// </summary>
    /// <seealso cref="ContextMenuItem" />
    [HideInEditor]
    public class ContextMenuButton : ContextMenuItem
    {
        private bool _isMouseDown;
        
        /// <summary>
        /// The amount to adjust the short keys and arrow image by in x coordinates.
        /// </summary>
        public float ExtraAdjustmentAmount = 0;

        /// <summary>
        /// Event fired when user clicks on the button.
        /// </summary>
        public event Action Clicked;

        /// <summary>
        /// Event fired when user clicks on the button.
        /// </summary>
        public event Action<ContextMenuButton> ButtonClicked;

        /// <summary>
        /// The button text.
        /// </summary>
        public string Text;

        /// <summary>
        /// The button short keys information (eg. 'Ctrl+C').
        /// </summary>
        public string ShortKeys;

        /// <summary>
        /// Item icon (best is 16x16).
        /// </summary>
        public SpriteHandle Icon;

        /// <summary>
        /// Item icon brush, drawn instead of <see cref="Icon"/> when set (and the button is not checked).
        /// </summary>
        public IBrush IconBrush;

        /// <summary>
        /// The checked state.
        /// </summary>
        public bool Checked;

        /// <summary>
        /// The automatic check mode.
        /// </summary>
        public bool AutoCheck;

        /// <summary>
        /// If true, this button visually reserves space for a check mark even when not currently checked.
        /// Used to keep alignment consistent within a menu that supports checks.
        /// </summary>
        public bool SupportsCheck;

        /// <summary>
        /// Closes the context menu after clicking the button, otherwise menu will stay open.
        /// </summary>
        public bool CloseMenuOnClick = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuButton"/> class.
        /// </summary>
        /// <param name="parent">The parent context menu.</param>
        /// <param name="text">The text.</param>
        /// <param name="shortKeys">The short keys tip.</param>
        public ContextMenuButton(ContextMenu parent, string text, string shortKeys = "")
        : base(parent, 8, 22)
        {
            Text = text;
            ShortKeys = shortKeys;
        }

        /// <summary>
        /// Sets the automatic check mode. In auto check mode the button sets the check sprite as an icon when user clicks it.
        /// </summary>
        /// <param name="value">True if use auto check, otherwise false.</param>
        /// <returns>This button.</returns>
        public ContextMenuButton SetAutoCheck(bool value)
        {
            AutoCheck = value;
            return this;
        }

        /// <summary>
        /// Sets the checked state.
        /// </summary>
        /// <param name="value">True if check it, otherwise false.</param>
        /// <returns>This button.</returns>
        public ContextMenuButton SetChecked(bool value)
        {
            Checked = value;
            return this;
        }

        /// <summary>
        /// Clicks this button.
        /// </summary>
        public void Click()
        {
            if (CloseMenuOnClick)
            {
                // Close topmost context menu
                ParentContextMenu?.TopmostCM.Hide();
            }

            // Auto check logic
            if (AutoCheck)
                Checked = !Checked;

            // Fire event
            Clicked?.Invoke();
            ButtonClicked?.Invoke(this);
            ParentContextMenu?.OnButtonClicked(this);
        }

        /// <inheritdoc />
        public override void Draw()
        {
            var style = Style.Current;
            var backgroundRect = new Rectangle(-X + 3, 0, Parent.Width - 6, Height);

            // If this menu has any icon or check (any sibling button uses an icon or check state), reserve a
            // consistent icon/check gutter with a 1px margin on each side - same position and spacing either way.
            bool hasAnyIconOrCheck = false;
            if (Parent != null)
            {
                bool anyChecked = false;
                for (int i = 0; i < Parent.ChildrenCount; i++)
                {
                    if (Parent.Children[i] is ContextMenuButton b)
                    {
                        if (b.AutoCheck || b.SupportsCheck || b.IconBrush != null || b.Icon.IsValid)
                            hasAnyIconOrCheck = true;
                        if (b.Checked)
                        {
                            hasAnyIconOrCheck = true;
                            anyChecked = true;
                        }
                    }
                }
                // Sticky: once any sibling was checked, keep the check-oriented placement in this menu.
                if (anyChecked)
                {
                    for (int i = 0; i < Parent.ChildrenCount; i++)
                    {
                        if (Parent.Children[i] is ContextMenuButton b)
                            b.SupportsCheck = true;
                    }
                }
            }

            const float iconSize = 14;
            const float iconLeftMargin = 5.0f;
            const float iconRightMargin = 5.0f;
            float iconGutterEnd = backgroundRect.X + iconLeftMargin + iconSize + iconRightMargin;
            float textOffset = hasAnyIconOrCheck ? iconGutterEnd : 0.0f;

            var textRect = new Rectangle(textOffset, 0, Width - 8 - textOffset, Height);
            var textColor = Enabled ? style.Foreground : style.ForegroundDisabled;

            // Draw background
            if (IsMouseOver && Enabled)
                Render2D.FillRectangle(backgroundRect, style.LightBackground);
            else if (IsFocused)
                Render2D.FillRectangle(backgroundRect, style.LightBackground);
            else
            {
                // Compute zebra index by counting only visible ContextMenuButton siblings (skip separators/other controls)
                int zebraIndex = 0;
                if (HasParent)
                {
                    for (int i = 0; i < Parent.ChildrenCount; i++)
                    {
                        var child = Parent.Children[i];
                        if (child == this)
                            break;
                        if (child is ContextMenuButton && child.Visible)
                            zebraIndex++;
                    }
                }
                if ((zebraIndex & 1) != 0)
                    Render2D.FillRectangle(backgroundRect, style.TreeAlternateRowBackground);
            }

            base.Draw();

            // Draw text
            Render2D.DrawText(style.FontMedium, Text, textRect, textColor, TextAlignment.Near, TextAlignment.Center);

            if (!string.IsNullOrEmpty(ShortKeys))
            {
                // Draw short keys
                Render2D.DrawText(style.FontMedium, ShortKeys, new Rectangle(textRect.X + ExtraAdjustmentAmount, textRect.Y, textRect.Width, textRect.Height), textColor, TextAlignment.Far, TextAlignment.Center);
            }

            // Draw icon/check - same rect (size, left offset) regardless of which one this button shows
            var iconRect = new Rectangle(backgroundRect.X + iconLeftMargin, (Height - iconSize) / 2, iconSize, iconSize);
            if (Checked)
            {
                var checkBrush = (Editor.Instance.Icons as CustomEditorIcons)?.ContextMenuCheckBrush;
                if (checkBrush != null)
                    checkBrush.Draw(iconRect, textColor);
                else if (style.CheckBoxTick.IsValid)
                    Render2D.DrawSprite(style.CheckBoxTick, iconRect, textColor);
            }
            else if (IconBrush != null)
                IconBrush.Draw(iconRect, textColor);
            else if (Icon.IsValid)
                Render2D.DrawSprite(Icon, iconRect, textColor);
        }

        /// <inheritdoc />
        public override void OnMouseLeave()
        {
            _isMouseDown = false;

            base.OnMouseLeave();
        }

        /// <inheritdoc />
        public override bool OnMouseDown(Float2 location, MouseButton button)
        {
            if (base.OnMouseDown(location, button))
                return true;

            _isMouseDown = true;
            return true;
        }

        /// <inheritdoc />
        public override bool OnMouseUp(Float2 location, MouseButton button)
        {
            if (base.OnMouseUp(location, button))
                return true;

            if (_isMouseDown)
            {
                _isMouseDown = false;
                Click();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool OnKeyDown(KeyboardKeys key)
        {
            if (base.OnKeyDown(key))
                return true;

            switch (key)
            {
            case KeyboardKeys.ArrowUp:
                for (int i = IndexInParent - 1; i >= 0; i--)
                {
                    if (ParentContextMenu.ItemsContainer.Children[i] is ContextMenuButton item && item.Visible && item.Enabled)
                    {
                        item.Focus();
                        ParentContextMenu.ItemsContainer.ScrollViewTo(item);
                        return true;
                    }
                }
                break;
            case KeyboardKeys.ArrowDown:
                for (int i = IndexInParent + 1; i < ParentContextMenu.ItemsContainer.Children.Count; i++)
                {
                    if (ParentContextMenu.ItemsContainer.Children[i] is ContextMenuButton item && item.Visible && item.Enabled)
                    {
                        item.Focus();
                        ParentContextMenu.ItemsContainer.ScrollViewTo(item);
                        return true;
                    }
                }
                break;
            case KeyboardKeys.Return:
                Click();
                return true;
            case KeyboardKeys.Escape:
                ParentContextMenu.Hide();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override void OnLostFocus()
        {
            _isMouseDown = false;

            base.OnLostFocus();
        }

        /// <inheritdoc />
        public override float MinimumWidth
        {
            get
            {
                var style = Style.Current;
                float width = 20;
                if (style.FontMedium)
                {
                    width += style.FontMedium.MeasureText(Text).X;
                    if (!string.IsNullOrEmpty(ShortKeys))
                        width += 40 + style.FontMedium.MeasureText(ShortKeys).X;
                }

                return Mathf.Max(width, base.MinimumWidth);
            }
        }
    }
}
