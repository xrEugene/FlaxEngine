// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor
{
    /// <summary>
    /// Extends <see cref="EditorIcons"/> with custom icons stored as standalone Texture assets (instead of sprites packed into the shared icons atlas).
    /// </summary>
    [HideInEditor]
    public class CustomEditorIcons : EditorIcons
    {
        /// <summary>
        /// The uniform scale applied to every custom (Texture-based) icon, relative to the draw rectangle each control asks for.
        /// </summary>
        public const float CustomIconScale = 1.25f;

        /// <summary>
        /// The color multiplier applied to every context-menu icon that's drawn slightly dimmer than the caller-provided color.
        /// </summary>
        private const float ContextMenuIconColorMultiplier = 0.9f;

        /// <summary>
        /// Brightness boost for thin-stroke context-menu icons that anti-alias into faint partial coverage at the
        /// small icon draw size; unlike <see cref="CustomIconScale"/> this keeps the icon at the same on-screen size
        /// as every other context-menu icon, only compensating for the loss of contrast.
        /// </summary>
        private const float ThinStrokeIconBrightnessBoost = 1.3f;

        /// <summary>
        /// The custom chevron-right icon texture, used in place of <see cref="EditorIcons.ArrowRight12"/>, <see cref="EditorIcons.Right32"/> and <see cref="EditorIcons.Right64"/>.
        /// </summary>
        public Texture ChevronRight;

        /// <summary>
        /// The custom chevron-down icon texture, used in place of <see cref="EditorIcons.ArrowDown12"/>, <see cref="EditorIcons.Down32"/> and <see cref="EditorIcons.Down64"/>.
        /// </summary>
        public Texture ChevronDown;

        /// <summary>
        /// The custom chevron-left icon texture, used in place of <see cref="EditorIcons.Left32"/> and <see cref="EditorIcons.Left64"/>.
        /// </summary>
        public Texture ChevronLeft;

        /// <summary>
        /// The custom chevron-up icon texture, used in place of <see cref="EditorIcons.Up32"/> and <see cref="EditorIcons.Up64"/>.
        /// </summary>
        public Texture ChevronUp;

        /// <summary>
        /// The custom cross (close/remove) icon texture, used in place of <see cref="EditorIcons.Cross12"/>.
        /// </summary>
        public Texture Cross;

        /// <summary>
        /// The custom Output Log tab icon texture.
        /// </summary>
        public Texture OutputLog;

        /// <summary>
        /// The brush for <see cref="OutputLog"/>, or null if it failed to load.
        /// </summary>
        public IBrush OutputLogBrush;

        /// <summary>
        /// The custom Debug Info tab icon texture.
        /// </summary>
        public Texture DebugInfo;

        /// <summary>
        /// The brush for <see cref="DebugInfo"/>, or null if it failed to load.
        /// </summary>
        public IBrush DebugInfoBrush;

        /// <summary>
        /// The custom "Close" context menu icon texture.
        /// </summary>
        public Texture Close;

        /// <summary>
        /// The brush for <see cref="Close"/>, or null if it failed to load.
        /// </summary>
        public IBrush CloseBrush;

        /// <summary>
        /// The custom "Close All" context menu icon texture.
        /// </summary>
        public Texture CloseAll;

        /// <summary>
        /// The brush for <see cref="CloseAll"/>, or null if it failed to load.
        /// </summary>
        public IBrush CloseAllBrush;

        /// <summary>
        /// The custom "Close All But This" context menu icon texture.
        /// </summary>
        public Texture CloseAllButThis;

        /// <summary>
        /// The brush for <see cref="CloseAllButThis"/>, or null if it failed to load.
        /// </summary>
        public IBrush CloseAllButThisBrush;

        /// <summary>
        /// The custom "Close All To The Right" context menu icon texture.
        /// </summary>
        public Texture CloseAllToRight;

        /// <summary>
        /// The brush for <see cref="CloseAllToRight"/>, or null if it failed to load.
        /// </summary>
        public IBrush CloseAllToRightBrush;

        /// <summary>
        /// The custom "Undock" context menu icon texture.
        /// </summary>
        public Texture Undock;

        /// <summary>
        /// The brush for <see cref="Undock"/>, or null if it failed to load.
        /// </summary>
        public IBrush UndockBrush;

        /// <summary>
        /// The custom "Expand All" context menu icon texture.
        /// </summary>
        public Texture ExpandAll;

        /// <summary>
        /// The brush for <see cref="ExpandAll"/>, or null if it failed to load.
        /// </summary>
        public IBrush ExpandAllBrush;

        /// <summary>
        /// The custom "Collapse All" context menu icon texture.
        /// </summary>
        public Texture CollapseAll;

        /// <summary>
        /// The brush for <see cref="CollapseAll"/>, or null if it failed to load.
        /// </summary>
        public IBrush CollapseAllBrush;

        /// <summary>
        /// The custom "Delete" context menu icon texture.
        /// </summary>
        public Texture Delete;

        /// <summary>
        /// The brush for <see cref="Delete"/>, or null if it failed to load.
        /// </summary>
        public IBrush DeleteBrush;

        /// <summary>
        /// The custom "Show In Explorer" context menu icon texture.
        /// </summary>
        public Texture ShowInExplorer;

        /// <summary>
        /// The brush for <see cref="ShowInExplorer"/>, or null if it failed to load.
        /// </summary>
        public IBrush ShowInExplorerBrush;

        /// <summary>
        /// The custom "New" context menu icon texture.
        /// </summary>
        public Texture NewAsset;

        /// <summary>
        /// The brush for <see cref="NewAsset"/>, or null if it failed to load.
        /// </summary>
        public IBrush NewAssetBrush;

        /// <summary>
        /// The custom "New folder" context menu icon texture.
        /// </summary>
        public Texture NewFolder;

        /// <summary>
        /// The brush for <see cref="NewFolder"/>, or null if it failed to load.
        /// </summary>
        public IBrush NewFolderBrush;

        /// <summary>
        /// The custom "New module" context menu icon texture.
        /// </summary>
        public Texture NewModule;

        /// <summary>
        /// The brush for <see cref="NewModule"/>, or null if it failed to load.
        /// </summary>
        public IBrush NewModuleBrush;

        /// <summary>
        /// The custom "Copy path" context menu icon texture.
        /// </summary>
        public Texture CopyPath;

        /// <summary>
        /// The brush for <see cref="CopyPath"/>, or null if it failed to load.
        /// </summary>
        public IBrush CopyPathBrush;

        /// <summary>
        /// The custom "Copy name" context menu icon texture.
        /// </summary>
        public Texture CopyName;

        /// <summary>
        /// The brush for <see cref="CopyName"/>, or null if it failed to load.
        /// </summary>
        public IBrush CopyNameBrush;

        /// <summary>
        /// The custom "Rename" context menu icon texture.
        /// </summary>
        public Texture Rename;

        /// <summary>
        /// The brush for <see cref="Rename"/>, or null if it failed to load.
        /// </summary>
        public IBrush RenameBrush;

        /// <summary>
        /// The custom "Paste" context menu icon texture.
        /// </summary>
        public Texture Paste;

        /// <summary>
        /// The brush for <see cref="Paste"/>, or null if it failed to load.
        /// </summary>
        public IBrush PasteBrush;

        /// <summary>
        /// The custom "Copy" context menu icon texture.
        /// </summary>
        public Texture Copy;

        /// <summary>
        /// The brush for <see cref="Copy"/>, or null if it failed to load.
        /// </summary>
        public IBrush CopyBrush;

        /// <summary>
        /// The custom "Cut" context menu icon texture.
        /// </summary>
        public Texture Cut;

        /// <summary>
        /// The brush for <see cref="Cut"/>, or null if it failed to load.
        /// </summary>
        public IBrush CutBrush;

        /// <summary>
        /// The custom "Duplicate" context menu icon texture.
        /// </summary>
        public Texture Duplicate;

        /// <summary>
        /// The brush for <see cref="Duplicate"/>, or null if it failed to load.
        /// </summary>
        public IBrush DuplicateBrush;

        /// <summary>
        /// The custom "Import file" context menu icon texture.
        /// </summary>
        public Texture ImportFile;

        /// <summary>
        /// The brush for <see cref="ImportFile"/>, or null if it failed to load.
        /// </summary>
        public IBrush ImportFileBrush;

        /// <summary>
        /// The custom "Refresh" context menu icon texture.
        /// </summary>
        public Texture Refresh;

        /// <summary>
        /// The brush for <see cref="Refresh"/>, or null if it failed to load.
        /// </summary>
        public IBrush RefreshBrush;

        /// <summary>
        /// The custom "Refresh Thumbnails" context menu icon texture.
        /// </summary>
        public Texture RefreshThumbnails;

        /// <summary>
        /// The brush for <see cref="RefreshThumbnails"/>, or null if it failed to load.
        /// </summary>
        public IBrush RefreshThumbnailsBrush;

        /// <summary>
        /// The custom "Re-Import" context menu icon texture.
        /// </summary>
        public Texture ReImport;

        /// <summary>
        /// The brush for <see cref="ReImport"/>, or null if it failed to load.
        /// </summary>
        public IBrush ReImportBrush;

        /// <summary>
        /// The custom "Reload" context menu icon texture.
        /// </summary>
        public Texture Reload;

        /// <summary>
        /// The brush for <see cref="Reload"/>, or null if it failed to load.
        /// </summary>
        public IBrush ReloadBrush;

        /// <summary>
        /// The custom "Copy Asset ID" context menu icon texture.
        /// </summary>
        public Texture CopyAssetId;

        /// <summary>
        /// The brush for <see cref="CopyAssetId"/>, or null if it failed to load.
        /// </summary>
        public IBrush CopyAssetIdBrush;

        /// <summary>
        /// The custom "Select Actors Using Asset" context menu icon texture.
        /// </summary>
        public Texture SelectActorsUsingAsset;

        /// <summary>
        /// The brush for <see cref="SelectActorsUsingAsset"/>, or null if it failed to load.
        /// </summary>
        public IBrush SelectActorsUsingAssetBrush;

        /// <summary>
        /// The custom "Show Asset References" context menu icon texture.
        /// </summary>
        public Texture ShowAssetReferences;

        /// <summary>
        /// The brush for <see cref="ShowAssetReferences"/>, or null if it failed to load.
        /// </summary>
        public IBrush ShowAssetReferencesBrush;

        /// <summary>
        /// The custom "Open as Additive" context menu icon texture.
        /// </summary>
        public Texture OpenAdditive;

        /// <summary>
        /// The brush for <see cref="OpenAdditive"/>, or null if it failed to load.
        /// </summary>
        public IBrush OpenAdditiveBrush;

        /// <summary>
        /// The custom "Create Particle System" context menu icon texture.
        /// </summary>
        public Texture CreateParticleSystem;

        /// <summary>
        /// The brush for <see cref="CreateParticleSystem"/>, or null if it failed to load.
        /// </summary>
        public IBrush CreateParticleSystemBrush;

        /// <summary>
        /// The custom "Create Material Instance" context menu icon texture.
        /// </summary>
        public Texture CreateMaterialInstance;

        /// <summary>
        /// The brush for <see cref="CreateMaterialInstance"/>, or null if it failed to load.
        /// </summary>
        public IBrush CreateMaterialInstanceBrush;

        /// <summary>
        /// The custom "Scale" (content view) context menu icon texture.
        /// </summary>
        public Texture ViewScale;

        /// <summary>
        /// The brush for <see cref="ViewScale"/>, or null if it failed to load.
        /// </summary>
        public IBrush ViewScaleBrush;

        /// <summary>
        /// The custom "Type" (content view) context menu icon texture.
        /// </summary>
        public Texture ViewType;

        /// <summary>
        /// The brush for <see cref="ViewType"/>, or null if it failed to load.
        /// </summary>
        public IBrush ViewTypeBrush;

        /// <summary>
        /// The custom "Show" (content view) context menu icon texture.
        /// </summary>
        public Texture ViewShow;

        /// <summary>
        /// The brush for <see cref="ViewShow"/>, or null if it failed to load.
        /// </summary>
        public IBrush ViewShowBrush;

        /// <summary>
        /// The custom "Filters" (content view) context menu icon texture.
        /// </summary>
        public Texture ViewFilters;

        /// <summary>
        /// The brush for <see cref="ViewFilters"/>, or null if it failed to load.
        /// </summary>
        public IBrush ViewFiltersBrush;

        /// <summary>
        /// The custom "Sort" (content view) context menu icon texture.
        /// </summary>
        public Texture ViewSort;

        /// <summary>
        /// The brush for <see cref="ViewSort"/>, or null if it failed to load.
        /// </summary>
        public IBrush ViewSortBrush;

        /// <summary>
        /// The custom context menu "checked" tick icon texture.
        /// </summary>
        public Texture ContextMenuCheck;

        /// <summary>
        /// The brush for <see cref="ContextMenuCheck"/>, or null if it failed to load.
        /// </summary>
        public IBrush ContextMenuCheckBrush;

        /// <summary>
        /// The custom "Export" context menu icon texture.
        /// </summary>
        public Texture Export;

        /// <summary>
        /// The brush for <see cref="Export"/>, or null if it failed to load.
        /// </summary>
        public IBrush ExportBrush;

        /// <summary>
        /// The custom "Show Import Location" context menu icon texture.
        /// </summary>
        public Texture ShowImportLocation;

        /// <summary>
        /// The brush for <see cref="ShowImportLocation"/>, or null if it failed to load.
        /// </summary>
        public IBrush ShowImportLocationBrush;

        /// <summary>
        /// The custom "Create Collision Data" context menu icon texture (shared with the "New Asset > Physics >
        /// Collision Data" menu icon - same asset).
        /// </summary>
        public Texture CreateCollisionData;

        /// <summary>
        /// The brush for <see cref="CreateCollisionData"/>, or null if it failed to load.
        /// </summary>
        public IBrush CreateCollisionDataBrush;

        /// <summary>
        /// The custom "Create Animation Graph" context menu icon texture (shared with the "New Asset > Animation >
        /// Animation Graph Function" menu icon - same asset).
        /// </summary>
        public Texture CreateAnimationGraph;

        /// <summary>
        /// The brush for <see cref="CreateAnimationGraph"/>, or null if it failed to load.
        /// </summary>
        public IBrush CreateAnimationGraphBrush;

        /// <inheritdoc />
        internal override void LoadIcons()
        {
            base.LoadIcons();

            var rightBrush = LoadCustomIcon(EditorAssets.ChevronRightIcon, "ChevronRight", out ChevronRight, CustomIconScale);
            if (rightBrush != null)
            {
                ArrowRight12Brush = rightBrush;
                Right32Brush = rightBrush;
                Right64Brush = rightBrush;
            }

            var downBrush = LoadCustomIcon(EditorAssets.ChevronDownIcon, "ChevronDown", out ChevronDown, CustomIconScale);
            if (downBrush != null)
            {
                ArrowDown12Brush = downBrush;
                Down32Brush = downBrush;
                Down64Brush = downBrush;
            }

            var leftBrush = LoadCustomIcon(EditorAssets.ChevronLeftIcon, "ChevronLeft", out ChevronLeft, CustomIconScale);
            if (leftBrush != null)
            {
                Left32Brush = leftBrush;
                Left64Brush = leftBrush;
            }

            var upBrush = LoadCustomIcon(EditorAssets.ChevronUpIcon, "ChevronUp", out ChevronUp, CustomIconScale);
            if (upBrush != null)
            {
                Up32Brush = upBrush;
                Up64Brush = upBrush;
            }

            // Cross is drawn at its natural size (no CustomIconScale) — the multiplier is chevrons-only.
            var crossBrush = LoadCustomIcon(EditorAssets.CrossIcon, "Cross", out Cross);
            if (crossBrush != null)
                Cross12Brush = crossBrush;

            OutputLogBrush = LoadCustomIcon(EditorAssets.OutputLogIcon, "OutputLog", out OutputLog, 0.75f);
            DebugInfoBrush = LoadCustomIcon(EditorAssets.DebugInfoIcon, "DebugInfo", out DebugInfo, 0.75f);

            // No scaling for the "Close" family of context menu icons; drawn slightly dimmer than the caller-provided color.
            CloseBrush = LoadCustomIcon(EditorAssets.CloseIcon, "Close", out Close, colorMultiplier: ContextMenuIconColorMultiplier);
            CloseAllBrush = LoadCustomIcon(EditorAssets.CloseAllIcon, "CloseAll", out CloseAll, colorMultiplier: ContextMenuIconColorMultiplier);
            CloseAllButThisBrush = LoadCustomIcon(EditorAssets.CloseAllButThisIcon, "CloseAllButThis", out CloseAllButThis, colorMultiplier: ContextMenuIconColorMultiplier);
            CloseAllToRightBrush = LoadCustomIcon(EditorAssets.CloseAllToRightIcon, "CloseAllToRight", out CloseAllToRight, colorMultiplier: ContextMenuIconColorMultiplier);

            UndockBrush = LoadCustomIcon(EditorAssets.UndockIcon, "Undock", out Undock, colorMultiplier: ContextMenuIconColorMultiplier);

            ExpandAllBrush = LoadCustomIcon(EditorAssets.ExpandAllIcon, "ExpandAll", out ExpandAll, colorMultiplier: ContextMenuIconColorMultiplier);
            CollapseAllBrush = LoadCustomIcon(EditorAssets.CollapseAllIcon, "CollapseAll", out CollapseAll, colorMultiplier: ContextMenuIconColorMultiplier);

            DeleteBrush = LoadCustomIcon(EditorAssets.DeleteIcon, "Delete", out Delete, colorMultiplier: ContextMenuIconColorMultiplier);
            ShowInExplorerBrush = LoadCustomIcon(EditorAssets.ShowInExplorerIcon, "ShowInExplorer", out ShowInExplorer, colorMultiplier: ContextMenuIconColorMultiplier);

            NewAssetBrush = LoadCustomIcon(EditorAssets.NewAssetIcon, "NewAsset", out NewAsset, colorMultiplier: ContextMenuIconColorMultiplier);
            NewFolderBrush = LoadCustomIcon(EditorAssets.NewFolderIcon, "NewFolder", out NewFolder, colorMultiplier: ContextMenuIconColorMultiplier);
            NewModuleBrush = LoadCustomIcon(EditorAssets.NewModuleIcon, "NewModule", out NewModule, colorMultiplier: ContextMenuIconColorMultiplier);
            CopyPathBrush = LoadCustomIcon(EditorAssets.CopyPathIcon, "CopyPath", out CopyPath, colorMultiplier: ContextMenuIconColorMultiplier);
            CopyNameBrush = LoadCustomIcon(EditorAssets.CopyNameIcon, "CopyName", out CopyName, colorMultiplier: ContextMenuIconColorMultiplier);
            RenameBrush = LoadCustomIcon(EditorAssets.RenameIcon, "Rename", out Rename, colorMultiplier: ContextMenuIconColorMultiplier);
            PasteBrush = LoadCustomIcon(EditorAssets.PasteIcon, "Paste", out Paste, colorMultiplier: ContextMenuIconColorMultiplier);
            CopyBrush = LoadCustomIcon(EditorAssets.CopyIcon, "Copy", out Copy, colorMultiplier: ContextMenuIconColorMultiplier);
            CutBrush = LoadCustomIcon(EditorAssets.CutIcon, "Cut", out Cut, colorMultiplier: ContextMenuIconColorMultiplier);
            DuplicateBrush = LoadCustomIcon(EditorAssets.DuplicateIcon, "Duplicate", out Duplicate, colorMultiplier: ContextMenuIconColorMultiplier);
            ImportFileBrush = LoadCustomIcon(EditorAssets.ImportFileIcon, "ImportFile", out ImportFile, colorMultiplier: ContextMenuIconColorMultiplier);
            RefreshBrush = LoadCustomIcon(EditorAssets.RefreshIcon, "Refresh", out Refresh, colorMultiplier: ContextMenuIconColorMultiplier);
            RefreshThumbnailsBrush = LoadCustomIcon(EditorAssets.RefreshThumbnailsIcon, "RefreshThumbnails", out RefreshThumbnails, colorMultiplier: ContextMenuIconColorMultiplier);
            ReImportBrush = LoadCustomIcon(EditorAssets.ReImportIcon, "ReImport", out ReImport, colorMultiplier: ContextMenuIconColorMultiplier);
            ReloadBrush = LoadCustomIcon(EditorAssets.ReloadIcon, "Reload", out Reload, colorMultiplier: ContextMenuIconColorMultiplier);
            CopyAssetIdBrush = LoadCustomIcon(EditorAssets.CopyAssetIdIcon, "CopyAssetId", out CopyAssetId, colorMultiplier: ContextMenuIconColorMultiplier);
            SelectActorsUsingAssetBrush = LoadCustomIcon(EditorAssets.SelectActorsUsingAssetIcon, "SelectActorsUsingAsset", out SelectActorsUsingAsset, colorMultiplier: ContextMenuIconColorMultiplier);
            ShowAssetReferencesBrush = LoadCustomIcon(EditorAssets.ShowAssetReferencesIcon, "ShowAssetReferences", out ShowAssetReferences, colorMultiplier: ContextMenuIconColorMultiplier);
            OpenAdditiveBrush = LoadCustomIcon(EditorAssets.OpenAdditiveIcon, "OpenAdditive", out OpenAdditive, colorMultiplier: ContextMenuIconColorMultiplier);
            CreateParticleSystemBrush = LoadCustomIcon(EditorAssets.CreateParticleSystemIcon, "CreateParticleSystem", out CreateParticleSystem, colorMultiplier: ContextMenuIconColorMultiplier);
            CreateMaterialInstanceBrush = LoadCustomIcon(EditorAssets.CreateMaterialInstanceIcon, "CreateMaterialInstance", out CreateMaterialInstance, colorMultiplier: ContextMenuIconColorMultiplier);

            // These are thin-stroke line art (like the chevrons above), which anti-aliases into faint partial
            // coverage at the small context-menu icon size; boost brightness instead of scaling up so they stay
            // the same on-screen size as every other context-menu icon.
            ViewScaleBrush = LoadCustomIcon(EditorAssets.ViewScaleIcon, "ViewScale", out ViewScale, colorMultiplier: ThinStrokeIconBrightnessBoost);
            ViewTypeBrush = LoadCustomIcon(EditorAssets.ViewTypeIcon, "ViewType", out ViewType, colorMultiplier: ThinStrokeIconBrightnessBoost);
            ViewShowBrush = LoadCustomIcon(EditorAssets.ViewShowIcon, "ViewShow", out ViewShow, colorMultiplier: ThinStrokeIconBrightnessBoost);
            ViewFiltersBrush = LoadCustomIcon(EditorAssets.ViewFiltersIcon, "ViewFilters", out ViewFilters, colorMultiplier: ThinStrokeIconBrightnessBoost);
            ViewSortBrush = LoadCustomIcon(EditorAssets.ViewSortIcon, "ViewSort", out ViewSort, colorMultiplier: ThinStrokeIconBrightnessBoost);
            ContextMenuCheckBrush = LoadCustomIcon(EditorAssets.ContextMenuCheckIcon, "ContextMenuCheck", out ContextMenuCheck, colorMultiplier: ThinStrokeIconBrightnessBoost);
            ExportBrush = LoadCustomIcon(EditorAssets.ExportIcon, "Export", out Export, colorMultiplier: ContextMenuIconColorMultiplier);
            ShowImportLocationBrush = LoadCustomIcon(EditorAssets.ShowImportLocationIcon, "ShowImportLocation", out ShowImportLocation, colorMultiplier: ContextMenuIconColorMultiplier);
            CreateCollisionDataBrush = LoadCustomIcon(EditorAssets.NewAssetItemCollisionDataIcon, "CreateCollisionData", out CreateCollisionData, colorMultiplier: ContextMenuIconColorMultiplier);
            CreateAnimationGraphBrush = LoadCustomIcon(EditorAssets.NewAssetItemAnimationGraphFunctionIcon, "CreateAnimationGraph", out CreateAnimationGraph, colorMultiplier: ContextMenuIconColorMultiplier);
        }

        /// <summary>
        /// Loads a standalone custom icon texture and wraps it in a brush, or returns null (logging an error) if it failed to load.
        /// </summary>
        /// <param name="assetPath">The content path of the standalone Texture asset.</param>
        /// <param name="name">The icon name, used for error logging.</param>
        /// <param name="texture">The loaded texture, or null if loading failed.</param>
        /// <param name="scale">The uniform scale applied relative to the caller-provided draw rectangle; 1 draws at natural size.</param>
        /// <param name="colorMultiplier">Multiplier applied to the caller-provided draw color; 1 draws unmodified. Ignored when <paramref name="scale"/> is not 1.</param>
        private IBrush LoadCustomIcon(string assetPath, string name, out Texture texture, float scale = 1.0f, float colorMultiplier = 1.0f)
        {
            texture = FlaxEngine.Content.LoadAsyncInternal<Texture>(assetPath);
            if (texture == null || texture.WaitForLoaded())
            {
                Editor.LogError($"Failed to load custom icon '{name}'.");
                return null;
            }
            if (scale != 1.0f)
                return new ScaledTextureBrush(texture, scale);
            return colorMultiplier != 1.0f ? new TintedTextureBrush(texture, colorMultiplier) : new TextureBrush(texture);
        }

        /// <summary>
        /// A texture-backed brush that draws itself scaled up (or down) relative to the caller-provided draw rectangle, keeping it centered in place.
        /// </summary>
        private sealed class ScaledTextureBrush : IBrush
        {
            private readonly Texture _texture;
            private readonly float _scale;

            public ScaledTextureBrush(Texture texture, float scale)
            {
                _texture = texture;
                _scale = scale;
            }

            /// <inheritdoc />
            public Float2 Size => _texture != null && !_texture.WaitForLoaded() ? _texture.Size * _scale : Float2.Zero;

            /// <inheritdoc />
            public void Draw(Rectangle rect, Color color)
            {
                var size = rect.Size * _scale;
                var scaledRect = new Rectangle(rect.Center - size * 0.5f, size);
                Render2D.DrawTexture(_texture, scaledRect, color);
            }

            /// <inheritdoc />
            public int CompareTo(object obj)
            {
                return ReferenceEquals(this, obj) ? 1 : 0;
            }
        }

        /// <summary>
        /// A texture-backed brush that draws with the caller-provided color multiplied by a fixed factor (eg. to draw slightly dimmer than the surrounding text/UI).
        /// </summary>
        private sealed class TintedTextureBrush : IBrush
        {
            private readonly Texture _texture;
            private readonly float _colorMultiplier;

            public TintedTextureBrush(Texture texture, float colorMultiplier)
            {
                _texture = texture;
                _colorMultiplier = colorMultiplier;
            }

            /// <inheritdoc />
            public Float2 Size => _texture != null && !_texture.WaitForLoaded() ? _texture.Size : Float2.Zero;

            /// <inheritdoc />
            public void Draw(Rectangle rect, Color color)
            {
                Render2D.DrawTexture(_texture, rect, color * _colorMultiplier);
            }

            /// <inheritdoc />
            public int CompareTo(object obj)
            {
                return ReferenceEquals(this, obj) ? 1 : 0;
            }
        }
    }
}
