// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEditor.GUI.ContextMenu;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI
{
    /// <summary>
    /// Button control that shows a dropdown context menu when clicked.
    /// </summary>
    /// <seealso cref="FlaxEngine.GUI.Button" />
    [HideInEditor]
    public class DropdownButton : Button
    {
        private bool _blockPopup;
        private ContextMenu.ContextMenu _contextMenu;

        /// <summary>
        /// Gets or sets the button icon.
        /// </summary>
        public SpriteHandle Icon;

        /// <summary>
        /// Gets or sets the context menu associated with this button.
        /// </summary>
        public ContextMenu.ContextMenu ContextMenu
        {
            get => _contextMenu;
            set
            {
                if (_contextMenu != value)
                {
                    if (_contextMenu != null)
                    {
                        _contextMenu.VisibleChanged -= OnMenuVisibleChanged;
                    }

                    _contextMenu = value;

                    if (_contextMenu != null)
                    {
                        _contextMenu.VisibleChanged += OnMenuVisibleChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the dropdown popup is currently opened.
        /// </summary>
        public bool IsPopupOpened => _contextMenu != null && _contextMenu.IsOpened;

        /// <summary>
        /// Occurs when popup is showing (before show). Can be used to populate or customize the context menu.
        /// </summary>
        public event Action<DropdownButton> PopupShowing;

        /// <summary>
        /// Initializes a new instance of the <see cref="DropdownButton"/> class.
        /// </summary>
        public DropdownButton()
        : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DropdownButton"/> class.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public DropdownButton(float x, float y, float width = 120.0f, float height = DefaultHeight)
        : base(x, y, width, height)
        {
        }

        private void OnMenuVisibleChanged(Control control)
        {
            var win = Root;
            _blockPopup = win != null && new Rectangle(Float2.Zero, Size).Contains(PointFromWindow(win.MousePosition));
            if (!_blockPopup)
                Focus();
        }

        /// <summary>
        /// Shows the context menu.
        /// </summary>
        public void ShowPopup()
        {
            if (_contextMenu == null)
                return;

            if (_contextMenu.Visible)
            {
                _contextMenu.Hide();
                return;
            }

            PopupShowing?.Invoke(this);
            _contextMenu.MinimumWidth = Width;
            _contextMenu.Show(this, new Float2(0, Height));
        }

        /// <summary>
        /// Hides the context menu.
        /// </summary>
        public void HidePopup()
        {
            if (_contextMenu != null && _contextMenu.Visible)
            {
                _contextMenu.Hide();
            }
        }

        /// <inheritdoc />
        public override void Draw()
        {
            // Simulate button pressed state while dropdown popup is open to use native button pressed gradient
            var isPressed = _isPressed;
            if (IsPopupOpened)
                _isPressed = true;

            base.Draw();

            _isPressed = isPressed;

            if (Icon.IsValid)
            {
                var iconRect = new Rectangle(4, (Height - 14) * 0.5f, 14, 14);
                Render2D.DrawSprite(Icon, iconRect, EnabledInHierarchy ? (TextColor != Color.Transparent ? TextColor : Style.Current.Foreground) : Style.Current.ForegroundDisabled);
            }
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (!_blockPopup)
            {
                ShowPopup();
                base.OnClick();
            }
            else
            {
                _blockPopup = false;
            }
        }

        /// <inheritdoc />
        public override void OnLostFocus()
        {
            _blockPopup = false;

            base.OnLostFocus();
        }

        /// <inheritdoc />
        public override void OnMouseLeave()
        {
            _blockPopup = false;

            base.OnMouseLeave();
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            if (_contextMenu != null)
            {
                _contextMenu.VisibleChanged -= OnMenuVisibleChanged;
                _contextMenu.Dispose();
                _contextMenu = null;
            }

            base.OnDestroy();
        }
    }
}
