// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI
{
    /// <summary>
    /// Toolstrip separator control.
    /// </summary>
    /// <seealso cref="FlaxEngine.GUI.Control" />
    public class ToolStripSeparator : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolStripSeparator"/> class.
        /// </summary>
        /// <param name="height">The height.</param>
        public ToolStripSeparator(float height)
        : base(0, 0, 1.5f, height)
        {
            AutoFocus = false;
        }

        /// <inheritdoc />
        public override void Draw()
        {
            base.Draw();

            // Draw the separator line
            Render2D.FillRectangle(new Rectangle(0, 0, Width, Height), Style.Current.BackgroundHighlighted);
        }
    }
}
