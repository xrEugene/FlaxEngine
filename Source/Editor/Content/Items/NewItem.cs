// Copyright (c) Wojciech Figat. All rights reserved.

using System.Text;
using FlaxEngine;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Helper content item used to mock UI during creating new assets by <see cref="FlaxEditor.Windows.ContentWindow"/>.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.ContentItem" />
    public sealed class NewItem : ContentItem
    {
        /// <summary>
        /// Gets the proxy object related to the created asset.
        /// </summary>
        public ContentProxy Proxy { get; }

        /// <summary>
        /// Gets the argument passed to the proxy for the item creation. In most cases it is null.
        /// </summary>
        public object Argument { get; }

        // The proxy that actually represents the item being created for preview purposes - usually just Proxy
        // itself, but a handful of "New Asset" shortcut proxies (eg. WidgetProxy for "New/Widget") only exist to
        // create a file of some OTHER type (a Prefab, in that case, via their own FileExtension/TypeName) and never
        // display themselves (IsProxyFor always false) - for those, this instead resolves to the proxy that will
        // actually be shown once the item exists, so eg. Widget previews with Prefab's real icon/accent color
        // rather than WidgetProxy's own (its AccentColor is Transparent, since it's never meant to be seen).
        private readonly ContentProxy _displayProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewItem"/> class.
        /// </summary>
        /// <param name="path">The path for the new item.</param>
        /// <param name="proxy">The content proxy object.</param>
        /// <param name="arg">The argument passed to the proxy for the item creation. In most cases it is null.</param>
        public NewItem(string path, ContentProxy proxy, object arg)
        : base(path)
        {
            Proxy = proxy;
            Argument = arg;

            _displayProxy = proxy;
            if (proxy is WidgetProxy)
            {
                // WidgetProxy never displays itself (IsProxyFor always false) - it just creates a Prefab file
                // under the hood via its own FileExtension/TypeName. Resolve to the actual PrefabProxy directly
                // (rather than a generic TypeName match against every registered proxy) so this only ever
                // redirects this one known shortcut proxy - a generic match previously also fired in reverse when
                // creating a Prefab directly, since WidgetProxy shares Prefab's TypeName too, incorrectly
                // redirecting a real PrefabProxy's own accent color to WidgetProxy's (Transparent).
                var prefabProxy = Editor.Instance.ContentDatabase.Proxy.Find(p => p is PrefabProxy);
                if (prefabProxy != null)
                    _displayProxy = prefabProxy;
            }

            if (proxy is WidgetProxy)
            {
                // The created item ends up a Prefab, shown with a real rendered preview of its UI control from
                // then on - but that doesn't exist yet during naming, so show this dedicated placeholder icon
                // (drawn the same way, and with the same background, as every other icon-based thumbnail - see
                // DrawGenericIconThumbnail) rather than an empty tile.
                GenericThumbnailIcon = EditorAssets.WidgetThumbIcon;
            }
            else if (proxy is SettingsProxy)
            {
                // The created item keeps its own real, baked icon (SettingsProxy.ConstructItem's _thumbnail)
                // from then on - but there's nothing to show yet during naming, so show this dedicated
                // placeholder icon instead of an empty tile.
                GenericThumbnailIcon = EditorAssets.SettingsThumbIcon;
            }
            else if (proxy is PrefabProxy)
            {
                // The created item is shown with a real rendered preview of its root actor from then on - but
                // that doesn't exist yet during naming, so show this dedicated placeholder icon instead of an
                // empty tile.
                GenericThumbnailIcon = EditorAssets.PrefabThumbIcon;
            }
            else if (proxy is ParticleEmitterProxy)
            {
                // The created item is shown with a real rendered preview of the emitter from then on - but that
                // doesn't exist yet during naming, so show this dedicated placeholder icon instead of an empty tile.
                GenericThumbnailIcon = EditorAssets.ParticleEmitterThumbIcon;
            }
            else if (proxy is ParticleSystemProxy)
            {
                // The created item is shown with a real rendered preview of the system from then on - but that
                // doesn't exist yet during naming, so show this dedicated placeholder icon instead of an empty tile.
                GenericThumbnailIcon = EditorAssets.ParticleSystemThumbIcon;
            }
            else if (proxy is MaterialProxy)
            {
                // The created item is shown with a real rendered preview of the material from then on - but that
                // doesn't exist yet during naming, so show this dedicated placeholder icon instead of an empty tile.
                GenericThumbnailIcon = EditorAssets.MaterialThumbIcon;
            }
            else if (proxy is MaterialInstanceProxy)
            {
                // The created item is shown with a real rendered preview of the material from then on - but that
                // doesn't exist yet during naming, so show this dedicated placeholder icon instead of an empty tile.
                GenericThumbnailIcon = EditorAssets.MaterialInstanceThumbIcon;
            }
            else if (_displayProxy is AssetProxy displayAssetProxy)
            {
                // Show this placeholder with the same generic icon/text the real item will use once created,
                // rather than a generic raw-file icon - this only ever reads the passed item for something like
                // its ShortName, never applicable here since nothing exists on disk yet for this placeholder, so
                // every implementation ignores it and passing null is safe.
                GenericThumbnailIcon = displayAssetProxy.GetGenericThumbnailIcon(null);
                if (GenericThumbnailIcon == null)
                    GenericThumbnailText = displayAssetProxy.GetGenericThumbnailText(null);
            }
        }

        /// <inheritdoc />
        protected override ContentProxy CachedAssetProxy => _displayProxy;

        /// <inheritdoc />
        public override ContentItemType ItemType => ContentItemType.Other;

        /// <inheritdoc />
        public override ContentItemSearchFilter SearchFilter => ContentItemSearchFilter.Other;

        /// <inheritdoc />
        public override string TypeDescription => "New";

        /// <inheritdoc />
        // Only reached when the type has neither a generic icon nor text (eg. Model/Prefab, which need a real
        // rendered preview - not something to show yet since there's no asset on disk during naming) - an empty
        // tile with just the accent bar, rather than a misleading raw-file icon.
        public override SpriteHandle DefaultThumbnail => SpriteHandle.Invalid;

        /// <inheritdoc />
        protected override bool DrawShadow => true;

        /// <inheritdoc />
        public override void UpdateTooltipText()
        {
            TooltipText = null;
        }

        /// <inheritdoc />
        // ContentItem overrides this to unconditionally true (it always has real tooltip text once it's an actual
        // asset), which for this placeholder - whose TooltipText is always null, see UpdateTooltipText above -
        // just shows an empty tooltip bubble on hover instead of none at all.
        protected override bool ShowTooltip => false;

        /// <inheritdoc />
        // The base implementation checks File.Exists(Path), which is false here since this placeholder's file
        // hasn't been created yet - and ContentDatabaseModule.LoadFolder treats any folder child that "doesn't
        // exist" as stale and deletes (Dispose()s) it, so an unrelated refresh of this folder (eg. deleting or
        // pasting some other item into it while this one is still waiting on a name or a create-settings dialog)
        // would silently destroy this placeholder. It isn't a real content-database entry at all, so "existing on
        // disk" doesn't apply to it the normal way - always true here keeps it out of that cleanup entirely.
        public override bool Exists => true;
    }
}
