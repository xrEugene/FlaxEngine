// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using FlaxEditor.Content.Thumbnails;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Base class for all asset proxy objects used to manage <see cref="AssetItem"/>.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.ContentProxy" />
    [HideInEditor]
    public abstract class AssetProxy : ContentProxy
    {
        private static readonly Dictionary<Type, bool> _hasGenericTextThumbnailCache = new Dictionary<Type, bool>();

        /// <summary>
        /// True if this proxy doesn't override <see cref="OnThumbnailDrawBegin"/> and so falls back to the base
        /// implementation's plain <see cref="Label"/> showing <see cref="ContentProxy.Name"/> (eg. "Material
        /// Function", "Collision Data") rather than a real rendered asset preview. Determined via reflection, once
        /// per proxy type (then cached): checks whether <see cref="OnThumbnailDrawBegin"/> is still literally the
        /// base <see cref="AssetProxy"/> implementation for this proxy's concrete type.
        /// </summary>
        /// <remarks>
        /// Only decides the DEFAULT of <see cref="GetGenericThumbnailText"/> - a proxy that overrides
        /// <see cref="OnThumbnailDrawBegin"/> for reasons other than a real preview (eg. to show a different
        /// string) should override <see cref="GetGenericThumbnailText"/> directly instead of relying on this.
        /// </remarks>
        public bool HasGenericTextThumbnail
        {
            get
            {
                var type = GetType();
                if (_hasGenericTextThumbnailCache.TryGetValue(type, out var cached))
                    return cached;
                var method = type.GetMethod(nameof(OnThumbnailDrawBegin), BindingFlags.Public | BindingFlags.Instance);
                var result = method != null && method.DeclaringType == typeof(AssetProxy);
                _hasGenericTextThumbnailCache[type] = result;
                return result;
            }
        }

        /// <summary>
        /// The text to draw live, at the tile's actual current resolution, in place of this item's thumbnail - or
        /// null to draw the normal (real, rendered-preview) thumbnail. Defaults to <see cref="ContentProxy.Name"/>
        /// when <see cref="HasGenericTextThumbnail"/> is true (the base <see cref="OnThumbnailDrawBegin"/> fallback);
        /// a proxy that overrides <see cref="OnThumbnailDrawBegin"/> purely to show a different string instead of a
        /// real preview (eg. <see cref="AnimationProxy"/>/<see cref="BehaviorTreeProxy"/> showing the actual
        /// per-asset name, or <see cref="FontProxy"/> showing the font family) should override this too, with that
        /// same string - see <see cref="FlaxEditor.Content.ContentItem.Draw"/> and <c>DrawGenericThumbnailText</c>
        /// for why this is drawn live instead of baked into the (small, fixed-resolution) thumbnail sprite like a
        /// real preview: that fallback Label ends up rasterized at that same low resolution and then stretched
        /// across the tile, visibly blurring (or, if its own font size doesn't scale with that resolution, shrinking)
        /// at any tile size other than the exact one it was baked at.
        /// </summary>
        /// <param name="item">The asset item whose thumbnail is being resolved.</param>
        public virtual string GetGenericThumbnailText(AssetItem item)
        {
            return HasGenericTextThumbnail ? Name : null;
        }

        /// <summary>
        /// The content path of a standalone icon texture to draw live, stretched to fill the tile (same look as a
        /// Settings-type item's <see cref="ContentItem.DefaultThumbnail"/> icon - no extra background fill, no
        /// accent bar), in place of this item's thumbnail. Checked before, and taking priority over,
        /// <see cref="GetGenericThumbnailText"/>. Null (the default) means no icon.
        /// </summary>
        /// <remarks>
        /// Unlike a Settings item, this can't just use <see cref="ContentItem.DefaultThumbnail"/> directly: that's a
        /// <see cref="SpriteHandle"/>, which only wraps a sprite already packed into a <see cref="SpriteAtlas"/> (eg.
        /// the shared editor icons atlas) - it can't wrap an arbitrary standalone <see cref="Texture"/> asset. This
        /// draws that texture live instead, at the tile's actual current resolution, styled to look the same.
        /// </remarks>
        /// <param name="item">The asset item whose thumbnail is being resolved.</param>
        public virtual string GetGenericThumbnailIcon(AssetItem item)
        {
            return null;
        }

        /// <inheritdoc />
        public override bool IsAsset => true;

        /// <summary>
        /// Gets the full name of the asset type (stored data format).
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is virtual Proxy not linked to any asset.
        /// </summary>
        protected virtual bool IsVirtual { get; }

        /// <summary>
        /// Determines whether [is virtual proxy].
        /// </summary>
        /// <returns><c>true</c> if [is virtual proxy]; otherwise, <c>false</c>.</returns>
        public bool IsVirtualProxy()
        {
            return IsVirtual && CanExport == false;
        }

        /// <summary>
        /// Checks if this proxy supports the given asset type id at the given path.
        /// </summary>
        /// <param name="typeName">The asset type identifier.</param>
        /// <param name="path">The asset path.</param>
        /// <returns>True if proxy supports assets of the given type id and path.</returns>
        public virtual bool AcceptsAsset(string typeName, string path)
        {
            return typeName == TypeName && path.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Constructs the item for the asset.
        /// </summary>
        /// <param name="path">The asset path.</param>
        /// <param name="typeName">The asset type name identifier.</param>
        /// <param name="id">The asset identifier.</param>
        /// <returns>Created item.</returns>
        public abstract AssetItem ConstructItem(string path, string typeName, ref Guid id);

        /// <summary>
        /// Called when thumbnail request gets prepared for drawing.
        /// </summary>
        /// <param name="request">The request.</param>
        public virtual void OnThumbnailDrawPrepare(ThumbnailRequest request)
        {
        }

        /// <summary>
        /// Determines whether thumbnail can be drawn for the specified item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if this thumbnail can be drawn for the specified item; otherwise, <c>false</c>.</returns>
        public virtual bool CanDrawThumbnail(ThumbnailRequest request)
        {
            return true;
        }

        /// <summary>
        /// Called when thumbnail drawing begins. Proxy should setup scene GUI for guiRoot.
        /// </summary>
        /// <param name="request">The request to render thumbnail.</param>
        /// <param name="guiRoot">The GUI root container control.</param>
        /// <param name="context">GPU context.</param>
        public virtual void OnThumbnailDrawBegin(ThumbnailRequest request, ContainerControl guiRoot, GPUContext context)
        {
            guiRoot.AddChild(new Label
            {
                Text = Name,
                AnchorPreset = AnchorPresets.StretchAll,
                Offsets = Margin.Zero,
                Wrapping = TextWrapping.WrapWords
            });
        }

        /// <summary>
        /// Called when thumbnail drawing ends. Proxy should clear custom GUI from guiRoot from that should be not destroyed.
        /// </summary>
        /// <param name="request">The request to render thumbnail.</param>
        /// <param name="guiRoot">The GUI root container control.</param>
        public virtual void OnThumbnailDrawEnd(ThumbnailRequest request, ContainerControl guiRoot)
        {
        }

        /// <summary>
        /// Called when thumbnail requests cleans data after drawing.
        /// </summary>
        /// <param name="request">The request.</param>
        public virtual void OnThumbnailDrawCleanup(ThumbnailRequest request)
        {
        }

        /// <summary>
        /// Initializes rendering settings for asset preview drawing for a thumbnail.
        /// </summary>
        /// <param name="preview">The asset preview.</param>
        protected void InitAssetPreview(Viewport.Previews.AssetPreview preview)
        {
            preview.RenderOnlyWithWindow = false;
            preview.UseAutomaticTaskManagement = false;
            preview.AnchorPreset = AnchorPresets.StretchAll;
            preview.Offsets = Margin.Zero;

            var task = preview.Task;
            task.Enabled = false;

            var view = task.View;
            view.IsSingleFrame = true; // Disable LOD transitions
            task.View = view;

            var eyeAdaptation = preview.PostFxVolume.EyeAdaptation;
            eyeAdaptation.Mode = EyeAdaptationMode.None;
            eyeAdaptation.OverrideFlags |= EyeAdaptationSettingsOverride.Mode;
            preview.PostFxVolume.EyeAdaptation = eyeAdaptation;

            var antiAliasing = preview.PostFxVolume.AntiAliasing;
            antiAliasing.Mode = AntialiasingMode.FastApproximateAntialiasing;
            antiAliasing.OverrideFlags |= AntiAliasingSettingsOverride.Mode;
            preview.PostFxVolume.AntiAliasing = antiAliasing;
        }
    }
}
