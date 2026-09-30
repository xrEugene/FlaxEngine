// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FlaxEditor.Content.GUI;
using FlaxEditor.GUI;
using FlaxEditor.GUI.Drag;
using FlaxEditor.Utilities;
using FlaxEngine;
using FlaxEngine.Assertions;
using FlaxEngine.GUI;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Content item types.
    /// </summary>
    [HideInEditor]
    public enum ContentItemType
    {
        /// <summary>
        /// The binary or text asset.
        /// </summary>
        Asset,

        /// <summary>
        /// The directory.
        /// </summary>
        Folder,

        /// <summary>
        /// The script file.
        /// </summary>
        Script,

        /// <summary>
        /// The scene file.
        /// </summary>
        Scene,

        /// <summary>
        /// The other type.
        /// </summary>
        Other,
    }

    /// <summary>
    /// Content item filter types used for searching.
    /// </summary>
    [HideInEditor]
    public enum ContentItemSearchFilter
    {
        /// <summary>
        /// The model.
        /// </summary>
        Model,

        /// <summary>
        /// The skinned model.
        /// </summary>
        SkinnedModel,

        /// <summary>
        /// The material.
        /// </summary>
        Material,

        /// <summary>
        /// The texture.
        /// </summary>
        Texture,

        /// <summary>
        /// The scene.
        /// </summary>
        Scene,

        /// <summary>
        /// The prefab.
        /// </summary>
        Prefab,

        /// <summary>
        /// The script.
        /// </summary>
        Script,

        /// <summary>
        /// The audio.
        /// </summary>
        Audio,

        /// <summary>
        /// The animation.
        /// </summary>
        Animation,

        /// <summary>
        /// The json.
        /// </summary>
        Json,

        /// <summary>
        /// The particles.
        /// </summary>
        Particles,

        /// <summary>
        /// The shader source files.
        /// </summary>
        Shader,

        /// <summary>
        /// The other.
        /// </summary>
        Other,
    }

    /// <summary>
    /// Interface for objects that can reference the content items in order to receive events from them.
    /// </summary>
    [HideInEditor]
    public interface IContentItemOwner
    {
        /// <summary>
        /// Called when referenced item gets deleted (asset unloaded, file deleted, etc.).
        /// Item should not be used after that.
        /// </summary>
        /// <param name="item">The item.</param>
        void OnItemDeleted(ContentItem item);

        /// <summary>
        /// Called when referenced item gets renamed (filename change, path change, etc.)
        /// </summary>
        /// <param name="item">The item.</param>
        void OnItemRenamed(ContentItem item);

        /// <summary>
        /// Called when item gets reimported or reloaded.
        /// </summary>
        /// <param name="item">The item.</param>
        void OnItemReimported(ContentItem item);

        /// <summary>
        /// Called when referenced item gets disposed (editor closing, database internal changes, etc.).
        /// Item should not be used after that.
        /// </summary>
        /// <param name="item">The item.</param>
        void OnItemDispose(ContentItem item);
    }

    /// <summary>
    /// Base class for all content items.
    /// Item parent GUI control is always <see cref="ContentView"/> or null if not in a view.
    /// </summary>
    /// <seealso cref="FlaxEngine.GUI.Control" />
    [HideInEditor]
    public abstract class ContentItem : Control
    {
        /// <summary>
        /// The default margin size.
        /// </summary>
        public const int DefaultMarginSize = 4;

        /// <summary>
        /// The default text height.
        /// </summary>
        public const int DefaultTextHeight = 42;

        /// <summary>
        /// The default thumbnail size - the tile grid's own on-screen sizing reference, at 100% zoom/interface scale.
        /// Deliberately a fixed value, independent of <see cref="PreviewsCache.AssetIconSize"/> (the resolution
        /// thumbnails are actually rendered/stored at): that resolution was bumped well above this size specifically
        /// so a thumbnail still has real detail left to show once drawn larger than this reference (a bigger zoom
        /// level, or a bigger interface scale) - tying this on-screen sizing constant to that same value would have
        /// made every tile balloon to the new, much larger baked resolution instead, which is a display-size change
        /// nobody asked for, not a quality one.
        /// </summary>
        public const int DefaultThumbnailSize = 64;

        /// <summary>
        /// The default width.
        /// </summary>
        public const int DefaultWidth = (DefaultThumbnailSize + 2 * DefaultMarginSize);

        /// <summary>
        /// The default height.
        /// </summary>
        public const int DefaultHeight = (DefaultThumbnailSize + 2 * DefaultMarginSize + DefaultTextHeight);

        /// <summary>
        /// Whether the item is being but.
        /// </summary>
        public bool IsBeingCut;

        /// <summary>
        /// Whether a <see cref="FlaxEditor.GUI.RenamePopup"/> is currently open over this item. While true, the
        /// item's own bottom selection accent bar is skipped (see <see cref="Draw"/>): the rename popup is an
        /// opaque overlay covering roughly the same area, and if its own rect doesn't land pixel-for-pixel flush
        /// with this one - eg. because one goes through a window-space round-trip (<c>Control.PointToWindow</c>) and
        /// the other doesn't - a thin sliver of this item's own background could otherwise show between the popup's
        /// own bottom border and this accent bar. Not drawing the accent bar at all while renaming sidesteps needing
        /// that alignment to be pixel-perfect in the first place.
        /// </summary>
        public bool IsBeingRenamed;

        private ContentFolder _parentFolder;

        private bool _isMouseDown;
        private Float2 _mouseDownStartPos;
        private float _lastClickDownTime = -1f;
        private ClickGesture _pendingClickGesture = ClickGesture.Select;

        /// <summary>
        /// What a left-click press should do once it's released, decided up front (at press time) from how long
        /// ago the previous press was - see <see cref="RegisterClickDown"/>.
        /// </summary>
        private enum ClickGesture
        {
            Select,
            Open,
            Rename,
        }

        // Custom, OS-independent click-gesture timing: a second left-click within OpenMaxGap of the first opens
        // the item (matches a fast double-click, which every OS would already treat as one - but relying on the
        // OS's own double-click speed setting would make this vary per platform and per-user setting, so it's
        // reimplemented here instead); one landing between OpenMaxGap and RenameMaxGap instead starts a rename,
        // like Windows Explorer's "click, pause, click" gesture; anything slower is just two unrelated clicks.
        private const float OpenMaxGap = 0.25f;
        private const float RenameMaxGap = 1.0f;

        private readonly List<IContentItemOwner> _references = new List<IContentItemOwner>(4);

        private SpriteHandle _thumbnail;
        private SpriteHandle _shadowIcon;
        private bool _assetProxyResolved;
        private ContentProxy _cachedAssetProxy;

        /// <summary>
        /// Set by <see cref="FlaxEditor.Content.Thumbnails.ThumbnailsModule"/> whenever this item's thumbnail is
        /// resolved (whether freshly rendered or found already cached), to its proxy's
        /// <see cref="AssetProxy.GetGenericThumbnailText"/> result. Non-null here means <see cref="Draw"/> draws
        /// this text live instead of the small, fixed-resolution baked sprite every other thumbnail uses - see
        /// <see cref="DrawGenericTextThumbnail"/>. Checked only when <see cref="GenericThumbnailIcon"/> is null - an
        /// icon takes priority over text.
        /// </summary>
        public string GenericThumbnailText;

        /// <summary>
        /// For an <see cref="AssetItem"/>, set by <see cref="FlaxEditor.Content.Thumbnails.ThumbnailsModule"/>
        /// whenever this item's thumbnail is resolved (whether freshly rendered or found already cached), to its
        /// proxy's <see cref="AssetProxy.GetGenericThumbnailIcon"/> result (a content path). A non-asset item with no
        /// thumbnail request cycle of its own (eg. <see cref="FileItem"/>) can instead just set this directly, since
        /// there's nothing to wait on. Non-null here means <see cref="Draw"/> draws that icon texture live, stretched
        /// to fill the tile, instead of the small, fixed-resolution baked sprite every other thumbnail uses - see
        /// <see cref="DrawGenericIconThumbnail"/>.
        /// </summary>
        public string GenericThumbnailIcon;

        /// <summary>
        /// This item's <see cref="ContentProxy"/> (for <see cref="ContentProxy.AccentColor"/>), resolved lazily and
        /// cached per item instance the first time it's drawn, rather than every frame: <see cref="Editor.ContentDatabase"/>'s
        /// own proxy lookup walks every registered proxy checking each one's <c>IsProxyFor</c>, too much to redo on
        /// every single <see cref="Draw"/> call for every visible tile. Typed as the base <see cref="ContentProxy"/>
        /// (not just <see cref="AssetProxy"/>) so a non-asset item with its own proxy and accent color (eg.
        /// <see cref="FileItem"/>/<see cref="FileProxy"/>) still gets its accent bar drawn - every actual usage here
        /// only reads <see cref="ContentProxy.AccentColor"/>, which doesn't need the narrower type. Null for anything
        /// without any proxy at all (eg. folders).
        /// </summary>
        protected virtual ContentProxy CachedAssetProxy
        {
            get
            {
                if (!_assetProxyResolved)
                {
                    _cachedAssetProxy = Editor.Instance.ContentDatabase.GetProxy(this);
                    _assetProxyResolved = true;
                }
                return _cachedAssetProxy;
            }
        }

        /// <summary>
        /// The fraction of the tile's own size a file-icon item's <see cref="DefaultThumbnail"/> is drawn at (see
        /// <see cref="Draw"/>'s file-icon branch) - centered, rather than stretched to fill the tile.
        /// </summary>
        private const float FileIconScale = 0.75f;

        /// <summary>
        /// A copy of <paramref name="rectangle"/> shrunk to <paramref name="scale"/> of its own size and re-centered
        /// within it - used to draw a file icon (or a generic thumbnail icon) smaller than the tile it's in, rather
        /// than stretched to fill it.
        /// </summary>
        private static Rectangle GetCenteredRect(Rectangle rectangle, float scale)
        {
            var size = rectangle.Size * scale;
            return new Rectangle(rectangle.Center - size * 0.5f, size);
        }

        /// <summary>
        /// The size the generic text thumbnail's font scale and the accent bar's thickness (see
        /// <see cref="DrawGenericTextThumbnail"/>/<see cref="DrawAccentBar"/>) are proportioned against. Deliberately
        /// independent of <see cref="PreviewsCache.AssetIconSize"/> (the actual baked-preview resolution, which can
        /// change on its own) - these two need their own fixed design reference so their on-screen proportions never
        /// shift just because that unrelated constant does.
        /// </summary>
        private const float GenericThumbnailReferenceSize = 64.0f;

        /// <summary>
        /// Shared cache of loaded generic-thumbnail icon textures, keyed by content path - many items of the same
        /// asset type share the exact same icon, so it's loaded once rather than per item.
        /// </summary>
        private static readonly Dictionary<string, Texture> _genericThumbnailIconCache = new Dictionary<string, Texture>();

        /// <summary>
        /// The fraction of the tile's own size <see cref="DrawGenericIconThumbnail"/> draws its icon at.
        /// </summary>
        private const float GenericThumbnailIconScale = 0.7f;

        /// <summary>
        /// Draws this item's thumbnail as a live icon texture at the tile's actual resolution instead of the small,
        /// fixed-resolution baked sprite every other thumbnail uses - see <see cref="GenericThumbnailIcon"/>.
        /// Centered at <see cref="GenericThumbnailIconScale"/> of the tile's own size, over the same black
        /// background and live accent bar the text thumbnail uses (see <see cref="DrawGenericTextThumbnail"/>) -
        /// the icon itself doesn't cover either, being smaller than the tile.
        /// </summary>
        private void DrawGenericIconThumbnail(ref Rectangle rectangle)
        {
            // SecondaryBackground - same near-black used by an unselected dock tab's header row.
            Render2D.FillRectangle(rectangle, Style.Current.SecondaryBackground);

            if (!_genericThumbnailIconCache.TryGetValue(GenericThumbnailIcon, out var texture))
            {
                texture = FlaxEngine.Content.LoadAsyncInternal<Texture>(GenericThumbnailIcon);
                _genericThumbnailIconCache[GenericThumbnailIcon] = texture;
            }

            if (texture != null && !texture.WaitForLoaded())
            {
                var iconSize = rectangle.Size * GenericThumbnailIconScale;
                var iconRect = new Rectangle(rectangle.Center - iconSize * 0.5f, iconSize);
                Render2D.DrawTexture(texture, iconRect, Color.White);
            }

            var proxy = CachedAssetProxy;
            if (proxy != null)
                DrawAccentBar(ref rectangle, proxy.AccentColor);
        }

        /// <summary>
        /// Draws this item's thumbnail as live text at the tile's actual resolution instead of the small, fixed-
        /// resolution baked sprite every other thumbnail uses - see <see cref="GenericThumbnailText"/> for why.
        /// Mirrors the styling of the <see cref="FlaxEngine.GUI.Label"/> that used to get baked into that sprite for
        /// these items: a black background, <see cref="Style.FontMedium"/>, <see cref="Style.Foreground"/>, centered,
        /// word-wrapped. The text scales with the thumbnail's own size (relative to
        /// <see cref="GenericThumbnailReferenceSize"/>) so it reads at roughly the same size the baked version was
        /// designed to, rather than shrinking to a corner or overflowing at a different tile size.
        /// </summary>
        private void DrawGenericTextThumbnail(ref Rectangle rectangle)
        {
            var style = Style.Current;
            Render2D.FillRectangle(rectangle, Color.Black);
            var scale = rectangle.Width / GenericThumbnailReferenceSize;
            Render2D.DrawText(style.FontMedium, GenericThumbnailText, rectangle, style.Foreground, TextAlignment.Center, TextAlignment.Center, TextWrapping.WrapWords, 1.0f, scale);
            var proxy = CachedAssetProxy;
            if (proxy != null)
                DrawAccentBar(ref rectangle, proxy.AccentColor);
        }

        /// <summary>
        /// Draws the per-asset-type <see cref="ContentProxy.AccentColor"/> strip along a thumbnail's bottom edge,
        /// live at the thumbnail's actual current size - for every thumbnail, not just the generic text one, since
        /// that strip used to be baked into the (fixed, low-resolution) preview sprite itself for every asset type
        /// alike (<c>ThumbnailsModule.PreviewRoot</c> used to draw it there), which blurred it under the same
        /// upscaling as everything else in that sprite once stretched to a larger tile.
        /// </summary>
        private void DrawAccentBar(ref Rectangle rectangle, Color accentColor)
        {
            const float accentHeightAtReferenceSize = 2.0f;
            var accentHeight = accentHeightAtReferenceSize * (rectangle.Width / GenericThumbnailReferenceSize);
            var accentRect = new Rectangle(rectangle.X, rectangle.Bottom - accentHeight, rectangle.Width, accentHeight);
            Render2D.FillRectangle(accentRect, accentColor);
        }

        /// <summary>
        /// Gets the type of the item.
        /// </summary>
        public abstract ContentItemType ItemType { get; }

        /// <summary>
        /// Gets the type of the item searching filter to use.
        /// </summary>
        public abstract ContentItemSearchFilter SearchFilter { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is asset.
        /// </summary>
        public bool IsAsset => ItemType == ContentItemType.Asset;

        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        public bool IsFolder => ItemType == ContentItemType.Folder;

        /// <summary>
        /// Gets a value indicating whether this instance can have children.
        /// </summary>
        public bool CanHaveChildren => ItemType == ContentItemType.Folder;

        /// <summary>
        /// Determines whether this item can be renamed.
        /// </summary>
        public virtual bool CanRename => true;

        /// <summary>
        /// Gets a value indicating whether this item can be dragged and dropped.
        /// </summary>
        public virtual bool CanDrag => Root != null;

        /// <summary>
        /// Gets a value indicating whether this <see cref="ContentItem"/> exists on drive.
        /// </summary>
        public virtual bool Exists => System.IO.File.Exists(Path);

        /// <summary>
        /// Gets the parent folder.
        /// </summary>
        public ContentFolder ParentFolder
        {
            get => _parentFolder;
            set
            {
                if (_parentFolder == value)
                    return;

                // Remove from old
                _parentFolder?.Children.Remove(this);

                // Link
                _parentFolder = value;

                // Add to new
                _parentFolder?.Children.Add(this);

                OnParentFolderChanged();
            }
        }

        /// <summary>
        /// Gets the path to the item.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the item file name (filename with extension).
        /// </summary>
        public string FileName { get; internal set; }

        /// <summary>
        /// Gets the item short name (filename without extension).
        /// </summary>
        public string ShortName { get; internal set; }

        /// <summary>
        /// Gets the asset name relative to the project root folder (without asset file extension)
        /// </summary>
        public string NamePath => FlaxEditor.Utilities.Utils.GetAssetNamePath(Path);

        /// <summary>
        /// Gets the content item type description (for UI).
        /// </summary>
        public abstract string TypeDescription { get; }

        /// <summary>
        /// Gets the default name of the content item thumbnail. Returns null if not used.
        /// </summary>
        public virtual SpriteHandle DefaultThumbnail => SpriteHandle.Invalid;

        /// <summary>
        /// Gets a value indicating whether this item has a default/generic thumbnail rather than a real rendered
        /// preview - either the older baked-sprite <see cref="DefaultThumbnail"/>, or the newer per-type
        /// <see cref="GenericThumbnailIcon"/>/<see cref="GenericThumbnailText"/>.
        /// </summary>
        public bool HasDefaultThumbnail => DefaultThumbnail.IsValid || GenericThumbnailIcon != null || GenericThumbnailText != null;

        /// <summary>
        /// Gets or sets the item thumbnail. Warning, thumbnail may not be available if item has no references (<see cref="ReferencesCount"/>).
        /// </summary>
        public SpriteHandle Thumbnail
        {
            get => _thumbnail;
            set => _thumbnail = value;
        }

        /// <summary>
        /// True if force show file extension.
        /// </summary>
        public bool ShowFileExtension;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItem"/> class.
        /// </summary>
        /// <param name="path">The path to the item.</param>
        protected ContentItem(string path)
        : base(0, 0, DefaultWidth, DefaultHeight)
        {
            // Set path
            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            ShortName = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Updates the item path. Use with caution or even don't use it. It's dangerous.
        /// </summary>
        /// <param name="value">The new path.</param>
        internal virtual void UpdatePath(string value)
        {
            // Set path
            Path = StringUtils.NormalizePath(value);
            FileName = System.IO.Path.GetFileName(value);
            ShortName = System.IO.Path.GetFileNameWithoutExtension(value);

            // Fire event
            OnPathChanged();
            for (int i = 0; i < _references.Count; i++)
            {
                _references[i].OnItemRenamed(this);
            }
        }

        /// <summary>
        /// Refreshes the item thumbnail.
        /// </summary>
        public virtual void RefreshThumbnail()
        {
            // Skip if item has default thumbnail
            if (HasDefaultThumbnail)
                return;

            var thumbnails = Editor.Instance.Thumbnails;

            // Delete old thumbnail and remove it from the cache
            thumbnails.DeletePreview(this);

            // Request new one (if need to)
            if (_references.Count > 0)
            {
                thumbnails.RequestPreview(this);
            }
        }

        /// <summary>
        /// Updates the tooltip text text.
        /// </summary>
        public virtual void UpdateTooltipText()
        {
            var sb = new StringBuilder();
            OnBuildTooltipText(sb);
            if (sb.Length != 0 && sb[sb.Length - 1] == '\n')
            {
                // Remove new-line from end
                int sub = 1;
                if (sb.Length != 1 && sb[sb.Length - 2] == '\r')
                    sub = 2;
                sb.Length -= sub;
            }
            TooltipText = sb.ToString();
        }

        /// <summary>
        /// Called when building tooltip text.
        /// </summary>
        /// <param name="sb">The output string builder.</param>
        protected virtual void OnBuildTooltipText(StringBuilder sb)
        {
            sb.Append("Type: ").Append(TypeDescription).AppendLine();
            if (File.Exists(Path))
                sb.Append("Size: ").Append(Utilities.Utils.FormatBytesCount((ulong)new FileInfo(Path).Length)).AppendLine();
            sb.Append("Path: ").Append(Utilities.Utils.GetAssetNamePathWithExt(Path)).AppendLine();
        }

        /// <summary>
        /// Tries to find the item at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Found item or null if missing.</returns>
        public virtual ContentItem Find(string path)
        {
            return Path == path ? this : null;
        }

        /// <summary>
        /// Tries to find a specified item in the assets tree.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if has been found, otherwise false.</returns>
        public virtual bool Find(ContentItem item)
        {
            return this == item;
        }

        /// <summary>
        /// Tries to find the item with the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Found item or null if missing.</returns>
        public virtual ContentItem Find(Guid id)
        {
            return null;
        }

        /// <summary>
        /// Tries to find script with the given name.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <returns>Found script or null if missing.</returns>
        public virtual ScriptItem FindScriptWitScriptName(string scriptName)
        {
            return null;
        }

        /// <summary>
        /// Gets a value indicating whether draw item shadow.
        /// </summary>
        protected virtual bool DrawShadow => false;

        /// <summary>
        /// Gets the local space rectangle for element name text area.
        /// </summary>
        public Rectangle TextRectangle
        {
            get
            {
                // Skip when hidden
                if (!Visible)
                    return Rectangle.Empty;
                var view = Parent as ContentView;
                var size = Size;
                switch (view?.ViewType ?? ContentViewType.Tiles)
                {
                case ContentViewType.Tiles:
                {
                    var textHeight = DefaultTextHeight * size.X / DefaultWidth;
                    return new Rectangle(0, size.Y - textHeight, size.X, textHeight);
                }
                case ContentViewType.List:
                {
                    var thumbnailSize = size.Y - 2 * DefaultMarginSize;
                    var textHeight = Mathf.Min(size.Y, 24.0f);
                    return new Rectangle(thumbnailSize + DefaultMarginSize * 2, (size.Y - textHeight) * 0.5f, size.X - textHeight - DefaultMarginSize * 3.0f, textHeight);
                }
                default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Gets the rectangle text should actually be wrapped/measured against: <see cref="TextRectangle"/>, inset
        /// by a small margin that scales with the item's size, so names that sit right at the wrap boundary (eg. a
        /// name just barely narrow enough to fit on one line) get some breathing room instead of nearly touching the
        /// edge. Also used by <see cref="FlaxEditor.Windows.ContentWindow"/> when sizing the rename edit box, so a
        /// name wraps at exactly the same width while being renamed as it does once finished - a fixed-size margin
        /// there instead of this same proportional one was enough of a width mismatch to flip a borderline name's
        /// wrap point between the two.
        /// </summary>
        public Rectangle GetWrapTextRectangle(Rectangle textRect, Float2 size)
        {
            var wrapWidthMargin = 10.0f * size.X / DefaultWidth;
            return new Rectangle(textRect.X + wrapWidthMargin * 0.5f, textRect.Y, textRect.Width - wrapWidthMargin, textRect.Height);
        }

        /// <inheritdoc cref="GetWrapTextRectangle(Rectangle, Float2)"/>
        public Rectangle GetWrapTextRectangle()
        {
            return GetWrapTextRectangle(TextRectangle, Size);
        }

        /// <summary>
        /// Draws the item thumbnail.
        /// </summary>
        /// <param name="rectangle">The thumbnail rectangle.</param>
        public void DrawThumbnail(ref Rectangle rectangle)
        {
            // Draw shadow
            if (DrawShadow)
            {
                const float thumbnailInShadowSize = 50.0f;
                var shadowRect = rectangle.MakeExpanded((DefaultThumbnailSize - thumbnailInShadowSize) * rectangle.Width / DefaultThumbnailSize * 1.3f);
                if (!_shadowIcon.IsValid)
                    _shadowIcon = Editor.Instance.Icons.AssetShadow128;
                Render2D.DrawSprite(_shadowIcon, shadowRect);
            }

            // Draw thumbnail
            if (_thumbnail.IsValid)
                Render2D.DrawSprite(_thumbnail, rectangle);
            else
                Render2D.FillRectangle(rectangle, Color.Black);
        }

        /// <summary>
        /// Draws the item thumbnail.
        /// </summary>
        /// <param name="rectangle">The thumbnail rectangle.</param>
        /// /// <param name="shadow">Whether or not to draw the shadow. Overrides DrawShadow.</param>
        public void DrawThumbnail(ref Rectangle rectangle, bool shadow)
        {
            // Draw shadow
            if (shadow)
            {
                const float thumbnailInShadowSize = 50.0f;
                var shadowRect = rectangle.MakeExpanded((DefaultThumbnailSize - thumbnailInShadowSize) * rectangle.Width / DefaultThumbnailSize * 1.3f);
                if (!_shadowIcon.IsValid)
                    _shadowIcon = Editor.Instance.Icons.AssetShadow128;
                Render2D.DrawSprite(_shadowIcon, shadowRect);
            }

            // Draw thumbnail
            if (_thumbnail.IsValid)
                Render2D.DrawSprite(_thumbnail, rectangle);
            else
                Render2D.FillRectangle(rectangle, Color.Black);
        }

        /// <summary>
        /// Gets the amount of references to that item.
        /// </summary>
        public int ReferencesCount => _references.Count;

        /// <summary>
        /// Adds the reference to the item.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void AddReference(IContentItemOwner obj)
        {
            Assert.IsNotNull(obj);
            Assert.IsFalse(_references.Contains(obj));

            _references.Add(obj);

            // Check if need to generate preview
            if (_references.Count == 1 && !_thumbnail.IsValid)
            {
                RequestThumbnail();
            }
        }

        /// <summary>
        /// Removes the reference from the item.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void RemoveReference(IContentItemOwner obj)
        {
            if (_references.Remove(obj))
            {
                // Check if need to release the preview
                if (_references.Count == 0 && _thumbnail.IsValid)
                {
                    ReleaseThumbnail();
                }
            }
        }

        /// <summary>
        /// Called when context menu is being prepared to show. Can be used to add custom options.
        /// </summary>
        /// <param name="menu">The menu.</param>
        public virtual void OnContextMenu(FlaxEditor.GUI.ContextMenu.ContextMenu menu)
        {
        }

        /// <summary>
        /// Called when item gets renamed or location gets changed (path modification).
        /// </summary>
        public virtual void OnPathChanged()
        {
        }

        /// <summary>
        /// Called when content item gets removed (by the user or externally).
        /// </summary>
        public virtual void OnDelete()
        {
            // Fire event
            while (_references.Count > 0)
            {
                var reference = _references[0];
                reference.OnItemDeleted(this);
                RemoveReference(reference);
            }

            // Release thumbnail
            if (_thumbnail.IsValid)
            {
                ReleaseThumbnail();
            }
        }

        /// <summary>
        /// Called when item parent folder gets changed.
        /// </summary>
        protected virtual void OnParentFolderChanged()
        {
        }

        /// <summary>
        /// Requests the thumbnail.
        /// </summary>
        protected void RequestThumbnail()
        {
            Editor.Instance.Thumbnails.RequestPreview(this);
        }

        /// <summary>
        /// Releases the thumbnail.
        /// </summary>
        protected void ReleaseThumbnail()
        {
            // Simply unlink sprite
            _thumbnail = SpriteHandle.Invalid;
        }

        /// <summary>
        /// Called when item gets reimported or reloaded.
        /// </summary>
        protected virtual void OnReimport()
        {
            for (int i = 0; i < _references.Count; i++)
                _references[i].OnItemReimported(this);
            RefreshThumbnail();
        }

        /// <summary>
        /// Does the drag and drop operation with this asset.
        /// </summary>
        protected virtual void DoDrag()
        {
            if (!CanDrag)
                return;

            DragData data;

            // Check if is selected
            if (Parent is ContentView view && view.IsSelected(this))
            {
                // Drag selected item
                data = DragItems.GetDragData(view.Selection);
            }
            else
            {
                // Drag single item
                data = DragItems.GetDragData(this);
            }

            // Start drag operation
            DoDragDrop(data);
        }

        /// <inheritdoc />
        protected override bool ShowTooltip => true;

        /// <inheritdoc />
        public override bool OnShowTooltip(out string text, out Float2 location, out Rectangle area)
        {
            UpdateTooltipText();
            var result = base.OnShowTooltip(out text, out _, out area);
            location = Size * new Float2(0.9f, 0.5f);
            return result;
        }

        /// <inheritdoc />
        public override void NavigationFocus()
        {
            base.NavigationFocus();

            if (IsFocused)
                (Parent as ContentView)?.Select(this);
        }

        /// <inheritdoc />
        public override void Draw()
        {
            var size = Size;
            var style = Style.Current;
            var view = Parent as ContentView;
            var isSelected = view.IsSelected(this);
            var clientRect = new Rectangle(Float2.Zero, size);
            var textRect = TextRectangle;
            Rectangle thumbnailRect;
            TextAlignment nameAlignment;
            switch (view.ViewType)
            {
            case ContentViewType.Tiles:
            {
                var thumbnailSize = size.X;
                thumbnailRect = new Rectangle(0, 0, thumbnailSize, thumbnailSize);
                nameAlignment = TextAlignment.Center;

                if (this is ContentFolder)
                {
                    // Small shadow
                    var shadowRect = new Rectangle(2, 2, clientRect.Width + 1, clientRect.Height + 1);
                    var color = Color.Black.AlphaMultiplied(0.2f);
                    Render2D.FillRectangle(shadowRect, color);
                    Render2D.FillRectangle(clientRect, Color.Lerp(style.ContentBackground, style.BackgroundHighlighted, 0.5f));

                    if (isSelected && !IsBeingRenamed)
                    {
                        // Keep the unselected background and show a blue accent line at the bottom (persist even when unfocused)
                        var accentColor = style.BackgroundSelected;
                        var accentRect = new Rectangle(0, clientRect.Height - 3.0f, clientRect.Width, 3.0f);
                        Render2D.FillRectangle(accentRect, accentColor);
                    }
                    else if (IsMouseOver)
                        Render2D.FillRectangle(clientRect, style.BackgroundHighlighted);

                    DrawThumbnail(ref thumbnailRect, false);
                }
                else
                {
                    // Small shadow
                    var shadowRect = new Rectangle(2, 2, clientRect.Width + 1, clientRect.Height + 1);
                    var color = Color.Black.AlphaMultiplied(0.2f);
                    Render2D.FillRectangle(shadowRect, color);

                    // Don't apply the hover highlight over an already-selected item - its own selected-state
                    // background/accent bar already communicates state, and re-highlighting it on hover reads as a
                    // spurious flicker rather than useful feedback.
                    var baseColor = Color.Lerp(style.ContentBackground, style.BackgroundHighlighted, 0.5f);
                    var isHighlighted = IsMouseOver && !isSelected;
                    Render2D.FillRectangle(clientRect, isHighlighted ? style.BackgroundHighlighted : baseColor);
                    Render2D.FillRectangle(TextRectangle, isHighlighted ? style.BackgroundHighlighted : baseColor);

                    var accentHeight = 2 * view.ViewScale;
                    var barRect = new Rectangle(0, thumbnailRect.Height - accentHeight, clientRect.Width, accentHeight);
                    Render2D.FillRectangle(barRect, Color.DimGray);

                    if (GenericThumbnailIcon != null)
                        DrawGenericIconThumbnail(ref thumbnailRect);
                    else if (GenericThumbnailText != null)
                        DrawGenericTextThumbnail(ref thumbnailRect);
                    else
                    {
                        // Same background as the generic icon/text thumbnails, but only for a file-icon item (its
                        // thumbnail short-circuits to a static DefaultThumbnail sprite, eg. JsonAsset/VisualScript) -
                        // a real rendered preview (a material/model/etc) already fills the whole rect itself.
                        if (DefaultThumbnail.IsValid)
                        {
                            Render2D.FillRectangle(thumbnailRect, style.SecondaryBackground);
                            var iconRect = GetCenteredRect(thumbnailRect, FileIconScale);
                            DrawThumbnail(ref iconRect, false);
                        }
                        else
                        {
                            DrawThumbnail(ref thumbnailRect, false);
                        }
                        var proxy = CachedAssetProxy;
                        if (proxy != null)
                            DrawAccentBar(ref thumbnailRect, proxy.AccentColor);
                    }
                    if (isSelected && !IsBeingRenamed)
                    {
                        // Blue accent line at the bottom instead of coloring the whole tile (persist even when unfocused)
                        var accentColor = style.BackgroundSelected;
                        var accentRect = new Rectangle(0, clientRect.Height - 3.0f, clientRect.Width, 3.0f);
                        Render2D.FillRectangle(accentRect, accentColor);
                    }
                }
                break;
            }
            case ContentViewType.List:
            {
                var thumbnailSize = size.Y - 2 * DefaultMarginSize;
                thumbnailRect = new Rectangle(DefaultMarginSize, DefaultMarginSize, thumbnailSize, thumbnailSize);
                nameAlignment = TextAlignment.Near;

                if (isSelected && !IsBeingRenamed)
                {
                    var accentColor = style.BackgroundSelected;
                    var accentRect = new Rectangle(0, clientRect.Height - 3.0f, clientRect.Width, 3.0f);
                    Render2D.FillRectangle(accentRect, accentColor);
                }
                else if (IsMouseOver)
                    Render2D.FillRectangle(clientRect, style.BackgroundHighlighted);

                if (GenericThumbnailIcon != null)
                    DrawGenericIconThumbnail(ref thumbnailRect);
                else if (GenericThumbnailText != null)
                    DrawGenericTextThumbnail(ref thumbnailRect);
                else
                {
                    // Same background as the generic icon/text thumbnails, but only for a file-icon item (its
                    // thumbnail short-circuits to a static DefaultThumbnail sprite, eg. JsonAsset/VisualScript) -
                    // not for a folder (also DefaultThumbnail-based here) or a real rendered preview.
                    if (!IsFolder && DefaultThumbnail.IsValid)
                    {
                        Render2D.FillRectangle(thumbnailRect, style.SecondaryBackground);
                        var iconRect = GetCenteredRect(thumbnailRect, FileIconScale);
                        DrawThumbnail(ref iconRect);
                    }
                    else
                    {
                        DrawThumbnail(ref thumbnailRect);
                    }
                    var proxy = CachedAssetProxy;
                    if (proxy != null)
                        DrawAccentBar(ref thumbnailRect, proxy.AccentColor);
                }
                break;
            }
            default: throw new ArgumentOutOfRangeException();
            }

            // Draw short name
            var displayName = ShowFileExtension || view.ShowFileExtensions ? FileName : ShortName;
            var scale = 0.95f * view.ViewScale;
            var wrapRect = GetWrapTextRectangle(textRect, size);
            // Clip to wrapRect, not the full textRect: wrapRect is already inset from textRect by the same margin
            // on both sides, so clipping there keeps that margin on the right when text overflows and gets cropped,
            // matching the margin the left/start side always has instead of cropping flush against the tile border.
            Render2D.PushClip(ref wrapRect);

            var font = style.FontMedium;
            // Only wrap a name that actually contains whitespace - a whitespace-free name (eg. "harshbricks-albedo"
            // or "FlaxTechDemo2022_720p") is left completely unwrapped here, with no attempt to break at '-'/'_'
            // either, since that special handling has caused more problems (an unreliable native wrap decision, and
            // a caret that can land somewhere unrelated to the actual text once wrapped) than it's worth for a
            // static, non-interactive label.
            var drawName = displayName;
            // NoWrap, not WrapWords: native WrapWords wraps at whitespace but *also* at every '_' and every
            // uppercase letter after the first character (see Font::ProcessText's isWrapChar in the native engine)
            // - so leaving WrapWords here would still wrap "MetalPlates_Normal_13" via the engine's own built-in
            // break points, completely bypassing this gate.
            var nameWrapping = TextWrapping.NoWrap;
            var nameToWrapped = (Func<int, int>)(i => i);
            if (SingleWordWrap.HasWhitespace(displayName))
                drawName = SingleWordWrap.WrapToFit(displayName, font, scale, wrapRect.Width, wrapRect.Height, out nameToWrapped, out _, out nameWrapping);
            else
                drawName = SingleWordWrap.TruncateSingleLine(displayName, font, scale, wrapRect.Width, out nameToWrapped);
            var drawNameAlignment = nameAlignment;

            // Highlight matched search substrings
            if (view.IsSearching
                && !string.IsNullOrEmpty(view.SearchFilterText) &&
                QueryFilterHelper.Match(view.SearchFilterText, displayName, out var highlightRanges))
            {
                var layout = new TextLayoutOptions
                {
                    Bounds = wrapRect,
                    HorizontalAlignment = drawNameAlignment,
                    VerticalAlignment = TextAlignment.Center,
                    TextWrapping = nameWrapping,
                    Scale = scale,
                    BaseLinesGapScale = 1.0f,
                };

                var lineHeight = font.Height * scale;
                // font.Height is the font's full line-box metric, taller than what a wrapped 2-line name actually
                // needs per line - a box this tall drawn down from the line's top bleeds into the next line's row.
                // Trim off the bottom only (top already lines up with the text) so it stops short of the next line.
                var highlightHeight = lineHeight * 0.55f;
                var highlightColor = style.ProgressNormal * 0.6f;

                for (int r = 0; r < highlightRanges.Length; r++)
                {
                    int i = highlightRanges[r].StartIndex;
                    int end = highlightRanges[r].EndIndex;

                    while (i < end)
                    {
                        var s = font.GetCharPosition(drawName, nameToWrapped(i), ref layout);
                        int j = i + 1;

                        var e = font.GetCharPosition(drawName, nameToWrapped(j), ref layout);
                        while (j < end)
                        {
                            var next = font.GetCharPosition(drawName, nameToWrapped(j + 1), ref layout);
                            if (!Mathf.NearEqual(next.Y, s.Y)) break;

                            e = next;
                            j++;
                        }

                        if (e.X > s.X) Render2D.FillRectangle(new Rectangle(s.X, s.Y, e.X - s.X, highlightHeight), highlightColor);
                        i = j;
                    }
                }
            }

            Render2D.DrawText(font, drawName, wrapRect, style.Foreground, drawNameAlignment, TextAlignment.Center, nameWrapping, 1f, scale);
            Render2D.PopClip();

            if (IsBeingCut)
            {
                var color = style.LightBackground.AlphaMultiplied(0.5f);
                Render2D.FillRectangle(clientRect, color);
            }
        }

        /// <summary>
        /// Classifies a just-started left-click press against the previous one's timestamp (see
        /// <see cref="OpenMaxGap"/>/<see cref="RenameMaxGap"/>) and records it as the one to act on once this
        /// press is released. Called from both <see cref="OnMouseDown"/> and <see cref="OnMouseDoubleClick"/> -
        /// a platform may substitute the latter for the former's second call when its own double-click speed
        /// setting is satisfied, but either way this is "a press just happened", so both feed the same timer.
        /// </summary>
        private void RegisterClickDown()
        {
            var now = Time.UnscaledGameTime;
            var sinceLastClick = _lastClickDownTime < 0f ? float.MaxValue : now - _lastClickDownTime;
            _lastClickDownTime = now;
            if (sinceLastClick <= OpenMaxGap)
                _pendingClickGesture = ClickGesture.Open;
            else if (sinceLastClick <= RenameMaxGap)
                _pendingClickGesture = ClickGesture.Rename;
            else
                _pendingClickGesture = ClickGesture.Select;
        }

        /// <inheritdoc />
        public override bool OnMouseDown(Float2 location, MouseButton button)
        {
            Focus();

            if (button == MouseButton.Left)
            {
                // Cache data
                _isMouseDown = true;
                _mouseDownStartPos = location;
                RegisterClickDown();
            }

            return true;
        }

        /// <inheritdoc />
        public override bool OnMouseUp(Float2 location, MouseButton button)
        {
            if (button == MouseButton.Left && _isMouseDown)
            {
                // Clear flag
                _isMouseDown = false;

                switch (_pendingClickGesture)
                {
                case ClickGesture.Open:
                    _lastClickDownTime = -1f;
                    (Parent as ContentView).OnItemDoubleClick(this);
                    break;
                case ClickGesture.Rename:
                    _lastClickDownTime = -1f;
                    if (CanRename)
                        (Parent as ContentView)?.RequestRename(this);
                    break;
                default:
                    (Parent as ContentView).OnItemClick(this);
                    break;
                }
            }

            return base.OnMouseUp(location, button);
        }

        /// <inheritdoc />
        public override bool OnMouseDoubleClick(Float2 location, MouseButton button)
        {
            // Only open on left double-click
            if (button != MouseButton.Left)
                return base.OnMouseDoubleClick(location, button);

            Focus();

            // The open/rename decision is made by our own OS-independent timing in OnMouseUp (via
            // RegisterClickDown) rather than here - this just needs to feed that same timer and look like a
            // press to it, since the platform sends this instead of a second OnMouseDown once its own
            // double-click speed setting is satisfied. The actual action fires from the OnMouseUp that follows.
            _isMouseDown = true;
            _mouseDownStartPos = location;
            RegisterClickDown();

            return true;
        }

        /// <inheritdoc />
        public override void OnMouseMove(Float2 location)
        {
            // Check if start drag and drop
            if (_isMouseDown && Float2.Distance(_mouseDownStartPos, location) > 10.0f)
            {
                // Clear flag
                _isMouseDown = false;

                // Start drag drop
                DoDrag();
            }
        }

        /// <inheritdoc />
        public override void OnMouseLeave()
        {
            // Check if start drag and drop
            if (_isMouseDown)
            {
                // Clear flag
                _isMouseDown = false;

                // Start drag drop
                DoDrag();
            }

            base.OnMouseLeave();
        }

        /// <inheritdoc />
        public override void OnSubmit()
        {
            // Open
            (Parent as ContentView).OnItemDoubleClick(this);

            base.OnSubmit();
        }

        /// <inheritdoc />
        public override int Compare(Control other)
        {
            if (other is ContentItem otherItem)
            {
                if (otherItem.IsFolder)
                    return 1;
                return string.Compare(ShortName, otherItem.ShortName, StringComparison.InvariantCulture);
            }

            return base.Compare(other);
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            // Fire event
            while (_references.Count > 0)
            {
                var reference = _references[0];
                reference.OnItemDispose(this);
                RemoveReference(reference);
            }

            // Release thumbnail
            if (_thumbnail.IsValid)
            {
                ReleaseThumbnail();
            }

            base.OnDestroy();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Path;
        }
    }
}
