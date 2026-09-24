// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using FlaxEngine;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Content item that contains <see cref="FlaxEngine.Scene"/> data.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.JsonAssetItem" />
    public sealed class SceneItem : JsonAssetItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneItem"/> class.
        /// </summary>
        /// <param name="path">The asset path.</param>
        /// <param name="id">The asset identifier.</param>
        public SceneItem(string path, Guid id)
        : base(path, id, Scene.EditorPickerTypename)
        {
        }

        /// <inheritdoc />
        public override ContentItemType ItemType => ContentItemType.Scene;

        /// <inheritdoc />
        public override ContentItemSearchFilter SearchFilter => ContentItemSearchFilter.Scene;

        /// <inheritdoc />
        public override string TypeDescription => "Scene";

        /// <inheritdoc />
        // Invalid (not Scene128) so the item has no DefaultThumbnail and falls through to
        // SceneProxy.GetGenericThumbnailIcon instead of ThumbnailsModule.RequestPreview short-circuiting on it.
        public override SpriteHandle DefaultThumbnail => SpriteHandle.Invalid;

        /// <inheritdoc />
        public override bool IsOfType(Type type)
        {
            return type.IsAssignableFrom(typeof(SceneAsset));
        }
    }
}
