// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Content item that contains video media file.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.JsonAssetItem" />
    public sealed class VideoItem : FileItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoItem"/> class.
        /// </summary>
        /// <param name="path">The file path.</param>
        public VideoItem(string path)
        : base(path)
        {
            // Overrides the generic icon the FileItem base constructor just set - Video is its own distinct
            // content item type (own TypeDescription, own VideoProxy/AccentColor) and gets its own icon rather
            // than inheriting File's just because it happens to derive from FileItem for its file-handling behavior.
            GenericThumbnailIcon = EditorAssets.VideoThumbIcon;
        }

        /// <inheritdoc />
        public override string TypeDescription => "Video";

        /// <inheritdoc />
        public override SpriteHandle DefaultThumbnail => Editor.Instance.Icons.Document128;
    }
}
