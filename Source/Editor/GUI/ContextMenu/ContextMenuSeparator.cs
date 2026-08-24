// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.GUI.ContextMenu
{
    /// <summary>
    /// Context Menu separator control that visually separate chunks of the popup menu items.
    /// </summary>
    /// <seealso cref="ContextMenuItem" />
    [HideInEditor]
    public class ContextMenuSeparator : ContextMenuItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuSeparator"/> class.
        /// </summary>
        /// <param name="parent">The parent context menu.</param>
        public ContextMenuSeparator(ContextMenu parent)
        : base(parent, 8, 4)
        {
        }

        /// <inheritdoc />
        public override void Draw()
        {
            base.Draw();

            // Draw separator line. The separator control is offset by the items margin (left indent),
            // so extend the line back to the panel origin so left/right insets look symmetrical.
            const float inset = 6f;
            const float thickness = 2f;
            float leftOffset = -X + inset;
            float width = Width - leftOffset - inset;
            Render2D.FillRectangle(new Rectangle(leftOffset, 1, width, thickness), Style.Current.BackgroundHighlighted);
        }
    }
}
