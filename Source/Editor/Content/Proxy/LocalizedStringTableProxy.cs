// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEditor.Windows;
using FlaxEditor.Windows.Assets;
using FlaxEngine;

namespace FlaxEditor.Content
{
    /// <summary>
    /// <see cref="LocalizedStringTable"/> proxy.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.JsonAssetProxy" />
    public class LocalizedStringTableProxy : JsonAssetProxy
    {
        /// <inheritdoc />
        public override EditorWindow Open(Editor editor, ContentItem item)
        {
            return new LocalizedStringTableWindow(editor, (JsonAssetItem)item);
        }

        /// <inheritdoc />
        public override string TypeName => "FlaxEngine.LocalizedStringTable";

        /// <inheritdoc />
        public override AssetItem ConstructItem(string path, string typeName, ref Guid id)
        {
            // Invalid (not the base's Json128) so the item has no DefaultThumbnail and falls through to
            // GetGenericThumbnailIcon below, instead of ThumbnailsModule.RequestPreview short-circuiting on it.
            return new JsonAssetItem(path, id, typeName, SpriteHandle.Invalid);
        }

        /// <inheritdoc />
        public override string GetGenericThumbnailIcon(AssetItem item)
        {
            return EditorAssets.JsonAssetThumbIcon;
        }
    }
}
