// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI
{
    /// <summary>
    /// Status strip GUI control.
    /// </summary>
    /// <seealso cref="FlaxEngine.GUI.ContainerControl" />
    public class StatusBar : ContainerControl
    {
        private Color _statusLineColor;

        /// <summary>
        /// The default height.
        /// </summary>
        public const int DefaultHeight = 25;

        /// <summary>
        /// Gets or sets the color of the status strip.
        /// </summary>
        public Color StatusColor
        {
            get => _statusLineColor;
            set => _statusLineColor = value;
        }

        /// <summary>
        /// Gets or sets the status text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the status text color
        /// </summary>
        public Color TextColor { get; set; } = Style.Current.Foreground;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusBar"/> class.
        /// </summary>
        public StatusBar()
        {
            AutoFocus = false;
            AnchorPreset = AnchorPresets.HorizontalStretchBottom;
        }

        /// <inheritdoc />
        public override void Draw()
        {
            // 1. Draw the neutral background (matching the tab bar area)
            var style = Style.Current;
            Render2D.FillRectangle(new Rectangle(Float2.Zero, Size), style.SecondaryBackground);

            // 2. Draw the scenario line at the top (2 pixels high for a "fine line" look)
            Render2D.FillRectangle(new Rectangle(0, 0, Width, 2.0f), _statusLineColor);

            // 3. Draw the size grip (keep existing logic)
            if (Root is WindowRootControl window && !window.IsMaximized)
                Render2D.DrawSprite(style.StatusBarSizeGrip, new Rectangle(Width - 12, 10, 12, 12), style.Foreground);

            // 4. Draw status text (keep existing logic)
            Render2D.DrawText(style.FontSmall, Text, new Rectangle(4, 0, Width - 20, Height), TextColor, TextAlignment.Near, TextAlignment.Center);

            // 5. Draw child controls (buttons, widgets, etc.)
            base.Draw();
        }
    }
}
