// Copyright (c) Wojciech Figat. All rights reserved.

namespace FlaxEngine.GUI
{
    /// <summary>
    /// Which corners of a rectangle should be rounded when using <see cref="StyleRendering"/> helpers.
    /// </summary>
    [System.Flags]
    public enum RoundedCorners
    {
        /// <summary>No rounded corners.</summary>
        None = 0,

        /// <summary>The top-left corner.</summary>
        TopLeft = 1,

        /// <summary>The top-right corner.</summary>
        TopRight = 2,

        /// <summary>The bottom-left corner.</summary>
        BottomLeft = 4,

        /// <summary>The bottom-right corner.</summary>
        BottomRight = 8,

        /// <summary>The top-left and top-right corners.</summary>
        Top = TopLeft | TopRight,

        /// <summary>The bottom-left and bottom-right corners.</summary>
        Bottom = BottomLeft | BottomRight,

        /// <summary>The top-left and bottom-left corners.</summary>
        Left = TopLeft | BottomLeft,

        /// <summary>The top-right and bottom-right corners.</summary>
        Right = TopRight | BottomRight,

        /// <summary>All corners.</summary>
        All = Top | Bottom,
    }

    /// <summary>
    /// Shared rendering helpers for style-aware GUI chrome (rounded rectangles etc.).
    /// </summary>
    public static class StyleRendering
    {
        /// <summary>
        /// Draws a filled rectangle with rounded corners.
        /// </summary>
        public static void FillRoundedRectangle(Rectangle bounds, Color color, float radius)
        {
            FillRoundedRectangle(bounds, color, radius, RoundedCorners.All);
        }

        /// <summary>
        /// Draws a filled rectangle with rounded corners on selected sides.
        /// </summary>
        public static void FillRoundedRectangle(Rectangle bounds, Color color, float radius, RoundedCorners corners)
        {
            radius = Mathf.Min(radius, Mathf.Min(bounds.Width, bounds.Height) * 0.5f);
            if (radius < 0.5f || corners == RoundedCorners.None)
            {
                Render2D.FillRectangle(bounds, color);
                return;
            }

            int steps = Mathf.CeilToInt(radius);
            int topSteps = (corners & RoundedCorners.Top) != 0 ? steps : 0;
            int bottomSteps = (corners & RoundedCorners.Bottom) != 0 ? steps : 0;
            float middleHeight = bounds.Height - topSteps - bottomSteps;

            // Fill the un-rounded middle band in one call.
            if (middleHeight > 0.0f)
                Render2D.FillRectangle(new Rectangle(bounds.X, bounds.Y + topSteps, bounds.Width, middleHeight), color);

            // Rounded rows: draw full-opacity core + antialiased 1px edge column on each rounded side.
            for (int i = 0; i < topSteps; i++)
            {
                DrawRoundedRow(bounds, color, radius, i, bounds.Y + i,
                    roundLeft: (corners & RoundedCorners.TopLeft) != 0,
                    roundRight: (corners & RoundedCorners.TopRight) != 0);
            }

            for (int i = 0; i < bottomSteps; i++)
            {
                DrawRoundedRow(bounds, color, radius, i, bounds.Bottom - i - 1.0f,
                    roundLeft: (corners & RoundedCorners.BottomLeft) != 0,
                    roundRight: (corners & RoundedCorners.BottomRight) != 0);
            }
        }

        private static void DrawRoundedRow(Rectangle bounds, Color color, float radius, int row, float y, bool roundLeft, bool roundRight)
        {
            // Coverage of the pixel at column X in this row.
            // We sample the circle at the row center: dy = radius - row - 0.5f
            // The exact horizontal boundary is at inset = radius - sqrt(r^2 - dy^2).
            // The pixel at floor(inset) is partially covered by (1 - frac(inset)); the pixel at floor(inset) + 1 is fully covered.
            float inset = GetCornerInset(radius, row);
            int intInset = Mathf.FloorToInt(inset);
            float edgeAlpha = 1.0f - (inset - intInset);

            float leftFull = roundLeft ? intInset + 1.0f : 0.0f;
            float rightFull = roundRight ? intInset + 1.0f : 0.0f;
            float coreWidth = Mathf.Max(0.0f, bounds.Width - leftFull - rightFull);

            if (coreWidth > 0.0f)
                Render2D.FillRectangle(new Rectangle(bounds.X + leftFull, y, coreWidth, 1.0f), color);

            if (edgeAlpha > 0.0f)
            {
                var edgeColor = color;
                edgeColor.A *= edgeAlpha;
                if (roundLeft && intInset < bounds.Width)
                    Render2D.FillRectangle(new Rectangle(bounds.X + intInset, y, 1.0f, 1.0f), edgeColor);
                if (roundRight && intInset < bounds.Width)
                    Render2D.FillRectangle(new Rectangle(bounds.Right - intInset - 1.0f, y, 1.0f, 1.0f), edgeColor);
            }
        }

        private static float GetCornerInset(float radius, int row)
        {
            float dy = radius - row - 0.5f;
            return radius - Mathf.Sqrt(Mathf.Max(0.0f, radius * radius - dy * dy));
        }
    }
}
