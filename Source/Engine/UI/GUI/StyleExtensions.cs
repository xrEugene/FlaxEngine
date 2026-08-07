// Copyright (c) Wojciech Figat. All rights reserved.

namespace FlaxEngine.GUI
{
    /// <summary>
    /// Extension methods providing styled corner-radius/accent values without modifying the serialized <see cref="Style"/> fields.
    /// </summary>
    public static class StyleExtensions
    {
        /// <summary>
        /// Default corner radius (in pixels) used for docking tabs and rounded chrome elements.
        /// </summary>
        public const float DefaultTabCornerRadius = 4.0f;

        /// <summary>
        /// Gets the corner radius used for dock/window tab headers.
        /// </summary>
        public static float GetTabCornerRadius(this Style style)
        {
            return DefaultTabCornerRadius;
        }
    }
}
