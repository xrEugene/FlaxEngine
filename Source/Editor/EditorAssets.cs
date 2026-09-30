// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;

namespace FlaxEditor
{
    /// <summary>
    /// Helper collection of Flax Editor in-build asset names.
    /// </summary>
    [HideInEditor]
    public static class EditorAssets
    {
        internal class Cache
        {
            private static MaterialInstance _highlightMaterial;

            /// <summary>
            /// Gets the highlight material instance.
            /// </summary>
            public static MaterialInstance HighlightMaterialInstance
            {
                get
                {
                    if (!_highlightMaterial)
                    {
                        var material = FlaxEngine.Content.LoadAsyncInternal<MaterialBase>(HighlightMaterial);
                        if (material && !material.WaitForLoaded())
                        {
                            _highlightMaterial = material.CreateVirtualInstance();
                            OnEditorOptionsChanged(Editor.Instance.Options.Options);
                        }
                    }
                    return _highlightMaterial;
                }
            }

            public static void OnEditorOptionsChanged(Options.EditorOptions options)
            {
                if (!_highlightMaterial)
                    return;
                var param = _highlightMaterial.GetParameter("Color");
                if (param != null)
                    param.Value = options.Visual.HighlightColor;
            }
        }

        /// <summary>
        /// The icons atlas.
        /// </summary>
        public static string IconsAtlas = "Editor/IconsAtlas";

        /// <summary>
        /// The custom chevron-right icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ChevronRightIcon = "Editor/Icons/Thumb/Chevrons/ChevronRight";

        /// <summary>
        /// The custom chevron-down icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ChevronDownIcon = "Editor/Icons/Thumb/Chevrons/ChevronDown";

        /// <summary>
        /// The custom chevron-left icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ChevronLeftIcon = "Editor/Icons/Thumb/Chevrons/ChevronLeft";

        /// <summary>
        /// The custom chevron-up icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ChevronUpIcon = "Editor/Icons/Thumb/Chevrons/ChevronUp";

        /// <summary>
        /// The custom cross (close/remove) icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CrossIcon = "Editor/Icons/Thumb/Cross";

        /// <summary>
        /// The custom Output Log tab icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string OutputLogIcon = "Editor/Icons/Thumb/OutputLog";

        /// <summary>
        /// The custom Debug Info tab icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string DebugInfoIcon = "Editor/Icons/Thumb/DebugInfo";

        /// <summary>
        /// The custom "Close" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CloseIcon = "Editor/Icons/Thumb/Close/Close";

        /// <summary>
        /// The custom "Close All" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CloseAllIcon = "Editor/Icons/Thumb/Close/CloseAll";

        /// <summary>
        /// The custom "Close All But This" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CloseAllButThisIcon = "Editor/Icons/Thumb/Close/CloseAllButThis";

        /// <summary>
        /// The custom "Close All To The Right" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CloseAllToRightIcon = "Editor/Icons/Thumb/Close/CloseAllToRight";

        /// <summary>
        /// The custom "Undock" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string UndockIcon = "Editor/Icons/Thumb/Undock";

        /// <summary>
        /// The custom "Expand All" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ExpandAllIcon = "Editor/Icons/Thumb/ExpandAll";

        /// <summary>
        /// The custom "Collapse All" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CollapseAllIcon = "Editor/Icons/Thumb/CollapseAll";

        /// <summary>
        /// The custom "Delete" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string DeleteIcon = "Editor/Icons/Thumb/Delete";

        /// <summary>
        /// The custom "Show In Explorer" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ShowInExplorerIcon = "Editor/Icons/Thumb/ShowInExplorer";

        /// <summary>
        /// The custom "New" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetIcon = "Editor/Icons/Thumb/NewAsset";

        /// <summary>
        /// The custom "New folder" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewFolderIcon = "Editor/Icons/Thumb/NewFolder";

        /// <summary>
        /// The custom "New module" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewModuleIcon = "Editor/Icons/Thumb/NewModule";

        /// <summary>
        /// The custom "Copy path" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CopyPathIcon = "Editor/Icons/Thumb/CopyPath";

        /// <summary>
        /// The custom "Copy name" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CopyNameIcon = "Editor/Icons/Thumb/CopyName";

        /// <summary>
        /// The custom "Rename" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string RenameIcon = "Editor/Icons/Thumb/Rename";

        /// <summary>
        /// The custom "Paste" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string PasteIcon = "Editor/Icons/Thumb/Paste";

        /// <summary>
        /// The custom "Copy" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CopyIcon = "Editor/Icons/Thumb/Copy";

        /// <summary>
        /// The custom "Cut" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CutIcon = "Editor/Icons/Thumb/Cut";

        /// <summary>
        /// The custom "Duplicate" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string DuplicateIcon = "Editor/Icons/Thumb/Duplicate";

        /// <summary>
        /// The custom "Import file" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ImportFileIcon = "Editor/Icons/Thumb/ImportFile";

        /// <summary>
        /// The custom "Refresh" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string RefreshIcon = "Editor/Icons/Thumb/Refresh";

        /// <summary>
        /// The custom "Refresh Thumbnails" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string RefreshThumbnailsIcon = "Editor/Icons/Thumb/RefreshThumbnails";

        /// <summary>
        /// The custom "Re-Import" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ReImportIcon = "Editor/Icons/Thumb/ReImport";

        /// <summary>
        /// The custom "Reload" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ReloadIcon = "Editor/Icons/Thumb/Reload";

        /// <summary>
        /// The custom "Copy Asset ID" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CopyAssetIdIcon = "Editor/Icons/Thumb/CopyAssetId";

        /// <summary>
        /// The custom "Select Actors Using Asset" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string SelectActorsUsingAssetIcon = "Editor/Icons/Thumb/SelectActorsUsingAsset";

        /// <summary>
        /// The custom "Show Asset References" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ShowAssetReferencesIcon = "Editor/Icons/Thumb/ShowAssetReferences";

        /// <summary>
        /// The custom "Open as Additive" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string OpenAdditiveIcon = "Editor/Icons/Thumb/OpenAdditive";

        /// <summary>
        /// The custom "Create Particle System" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CreateParticleSystemIcon = "Editor/Icons/Thumb/CreateParticleSystem";

        /// <summary>
        /// The custom "Create Material Instance" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CreateMaterialInstanceIcon = "Editor/Icons/Thumb/CreateMaterialInstance";

        /// <summary>
        /// The custom "Scale" (content view) context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ViewScaleIcon = "Editor/Icons/Thumb/ContextMenu/View/Scale";

        /// <summary>
        /// The custom "Type" (content view) context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ViewTypeIcon = "Editor/Icons/Thumb/ContextMenu/View/Type";

        /// <summary>
        /// The custom "Show" (content view) context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ViewShowIcon = "Editor/Icons/Thumb/ContextMenu/View/Show";

        /// <summary>
        /// The custom "Filters" (content view) context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ViewFiltersIcon = "Editor/Icons/Thumb/ContextMenu/View/Filters";

        /// <summary>
        /// The custom "Sort" (content view) context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ViewSortIcon = "Editor/Icons/Thumb/ContextMenu/View/Sort";

        /// <summary>
        /// The custom context menu "checked" tick icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ContextMenuCheckIcon = "Editor/Icons/Thumb/ContextMenu/View/Check";

        /// <summary>
        /// The custom "Export" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ExportIcon = "Editor/Icons/Thumb/ContextMenu/Export";

        /// <summary>
        /// The custom "Show Import Location" context menu icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ShowImportLocationIcon = "Editor/Icons/Thumb/ContextMenu/ShowImportLocation";

        /// <summary>
        /// The Behavior Tree asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string BehaviorTreeThumbIcon = "Editor/Icons/Thumb/Assets/BehaviorTree";

        /// <summary>
        /// The Gameplay Globals asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string GameplayGlobalsThumbIcon = "Editor/Icons/Thumb/Assets/GameplayGlobals";

        /// <summary>
        /// The Json Asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string JsonAssetThumbIcon = "Editor/Icons/Thumb/Assets/JsonAsset";

        /// <summary>
        /// The Scene asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string SceneThumbIcon = "Editor/Icons/Thumb/Assets/Scene";

        /// <summary>
        /// The Visual Script asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string VisualScriptThumbIcon = "Editor/Icons/Thumb/Assets/VisualScript";

        /// <summary>
        /// The Animation asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string AnimationThumbIcon = "Editor/Icons/Thumb/Assets/Animation";

        /// <summary>
        /// The Animation Graph asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string AnimationGraphThumbIcon = "Editor/Icons/Thumb/Assets/AnimationGraph";

        /// <summary>
        /// The "Function" asset generic thumbnail icon texture (standalone asset, not part of the icons atlas), shared by Animation Graph Function, Material Function and Particle Emitter Function.
        /// </summary>
        public static string FunctionThumbIcon = "Editor/Icons/Thumb/Assets/Function";

        /// <summary>
        /// The Scene Animation asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string SceneAnimationThumbIcon = "Editor/Icons/Thumb/Assets/SceneAnimation";

        /// <summary>
        /// The Skeleton Mask asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string SkeletonMaskThumbIcon = "Editor/Icons/Thumb/Assets/SkeletonMask";

        /// <summary>
        /// The Collision Data asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CollisionDataThumbIcon = "Editor/Icons/Thumb/Assets/CollisionData";

        /// <summary>
        /// The Audio Clip asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string AudioClipThumbIcon = "Editor/Icons/Thumb/Assets/AudioClip";

        /// <summary>
        /// The generic "File" content item's thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string FileThumbIcon = "Editor/Icons/Thumb/Assets/File";

        /// <summary>
        /// The Font Asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string FontAssetThumbIcon = "Editor/Icons/Thumb/Assets/FontAsset";

        /// <summary>
        /// The Shader asset's generic thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ShaderThumbIcon = "Editor/Icons/Thumb/Assets/Shader";

        /// <summary>
        /// The Video content item's thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string VideoThumbIcon = "Editor/Icons/Thumb/Assets/Video";

        /// <summary>
        /// The Settings content item's thumbnail icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string SettingsThumbIcon = "Editor/Icons/Thumb/Assets/Settings";

        /// <summary>
        /// The "New Widget" placeholder's thumbnail icon texture, shown only while naming it - the created item is
        /// a Prefab and uses a real rendered preview from then on (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string WidgetThumbIcon = "Editor/Icons/Thumb/Assets/Widget";

        /// <summary>
        /// The "New Prefab" placeholder's thumbnail icon texture, shown only while naming it - the created item
        /// uses a real rendered preview from then on (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string PrefabThumbIcon = "Editor/Icons/Thumb/Assets/Prefab";

        /// <summary>
        /// The "New Particle Emitter" placeholder's thumbnail icon texture, shown only while naming it - the
        /// created item uses a real rendered preview from then on (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ParticleEmitterThumbIcon = "Editor/Icons/Thumb/Assets/ParticleEmitter";

        /// <summary>
        /// The "New Particle System" placeholder's thumbnail icon texture, shown only while naming it - the
        /// created item uses a real rendered preview from then on (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string ParticleSystemThumbIcon = "Editor/Icons/Thumb/Assets/ParticleSystem";

        /// <summary>
        /// The "New Material" placeholder's thumbnail icon texture, shown only while naming it - the created item
        /// uses a real rendered preview from then on (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string MaterialThumbIcon = "Editor/Icons/Thumb/Assets/Material";

        /// <summary>
        /// The "New Material Instance" placeholder's thumbnail icon texture, shown only while naming it - the
        /// created item uses a real rendered preview from then on (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string MaterialInstanceThumbIcon = "Editor/Icons/Thumb/Assets/MaterialInstance";

        /// <summary>
        /// The "Copied to Clipboard" popup's info icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string CopiedToClipboardInfoIcon = "Editor/Icons/Thumb/Misc/Info";

        /// <summary>
        /// The "New Asset" context menu's "AI" category icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetCategoryAIIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/AI";

        /// <summary>
        /// The "New Asset" context menu's "AI/Behavior Tree" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemBehaviorTreeIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/BehaviorTree";

        /// <summary>
        /// The "New Asset" context menu's "Animation" category icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetCategoryAnimationIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Animation";

        /// <summary>
        /// The "New Asset" context menu's "Animation/Animation" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemAnimationIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Animation";

        /// <summary>
        /// The "New Asset" context menu's "Animation/Animation Graph" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemAnimationGraphIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/AnimationGraph";

        /// <summary>
        /// The "New Asset" context menu's "Animation/Animation Graph Function" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemAnimationGraphFunctionIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/AnimationGraphFunction";

        /// <summary>
        /// The "New Asset" context menu's "Animation/Scene Animation" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemSceneAnimationIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/SceneAnimation";

        /// <summary>
        /// The "New Asset" context menu's "Animation/Skeleton Mask" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemSkeletonMaskIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/SkeletonMask";

        /// <summary>
        /// The "New Asset" context menu's "Gameplay Globals" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemGameplayGlobalsIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/GameplayGlobals";

        /// <summary>
        /// The "New Asset" context menu's "Json Asset" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemJsonAssetIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/JsonAsset";

        /// <summary>
        /// The "New Asset" context menu's "Material" category icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetCategoryMaterialIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Material";

        /// <summary>
        /// The "New Asset" context menu's "Material/Material" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemMaterialIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Material";

        /// <summary>
        /// The "New Asset" context menu's "Material/Material Instance" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemMaterialInstanceIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/MaterialInstance";

        /// <summary>
        /// The "New Asset" context menu's "Particles" category icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetCategoryParticlesIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Particles";

        /// <summary>
        /// The "New Asset" context menu's "Particles/Particle Emitter" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemParticleEmitterIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/ParticleEmitter";

        /// <summary>
        /// The "New Asset" context menu's "Particles/Particle System" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemParticleSystemIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/ParticleSystem";

        /// <summary>
        /// The "New Asset" context menu's "Physics" category icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetCategoryPhysicsIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Physics";

        /// <summary>
        /// The "New Asset" context menu's "Physics/Collision Data" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemCollisionDataIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/CollisionData";

        /// <summary>
        /// The "New Asset" context menu's "Prefab" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemPrefabIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Prefab";

        /// <summary>
        /// The "New Asset" context menu's "Widget" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemWidgetIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Widget";

        /// <summary>
        /// The "New Asset" context menu's "Settings" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemSettingsIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Settings";

        /// <summary>
        /// The "New Asset" context menu's "Visual Script" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemVisualScriptIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/VisualScript";

        /// <summary>
        /// The "New Asset" context menu's "Scene" item icon texture (standalone asset, not part of the icons atlas).
        /// </summary>
        public static string NewAssetItemSceneIcon = "Editor/Icons/Thumb/ContextMenu/NewAsset/Scene";

        /// <summary>
        /// The primary font.
        /// </summary>
        public static string PrimaryFont = "Editor/Fonts/Roboto-Regular";

        /// <summary>
        /// The secondary (fallback) font to use for missing characters rendering (CJK - Chinese/Japanese/Korean characters).
        /// </summary>
        public static string FallbackFont = "Editor/Fonts/NotoSansSC-Regular";

        /// <summary>
        /// The Inconsolata Regular font.
        /// </summary>
        public static string InconsolataRegularFont = "Editor/Fonts/Inconsolata-Regular";

        /// <summary>
        /// The window icon.
        /// </summary>
        public static string WindowIcon = "Editor/EditorIcon";

        /// <summary>
        /// The material used for the HS color wheel.
        /// </summary>
        public static string HSWheelMaterial = "Editor/HSWheel";

        /// <summary>
        /// The window icons font.
        /// </summary>
        public static string WindowIconsFont = "Editor/Fonts/SegMDL2";

        /// <summary>
        /// The default font material.
        /// </summary>
        public static string DefaultFontMaterial = "Editor/DefaultFontMaterial";

        /// <summary>
        /// The highlight material.
        /// </summary>
        public static string HighlightMaterial = "Editor/Highlight Material";

        /// <summary>
        /// The highlight terrain material.
        /// </summary>
        public static string HighlightTerrainMaterial = "Editor/Terrain/Highlight Terrain Material";

        /// <summary>
        /// The terrain circle brush material.
        /// </summary>
        public static string TerrainCircleBrushMaterial = "Editor/Terrain/Circle Brush Material";

        /// <summary>
        /// The debug material (wireframe).
        /// </summary>
        public static string WiresDebugMaterial = "Editor/Wires Debug Material";

        /// <summary>
        /// The default sky cube texture.
        /// </summary>
        public static string DefaultSkyCubeTexture = "Editor/SimplySky";

        /// <summary>
        /// The default sprite material.
        /// </summary>
        public static string DefaultSpriteMaterial = "Editor/SpriteMaterial";

        /// <summary>
        /// The IES Profile assets preview material.
        /// </summary>
        public static string IesProfilePreviewMaterial = "Editor/IesProfilePreviewMaterial";

        /// <summary>
        /// The foliage painting brush material.
        /// </summary>
        public static string FoliageBrushMaterial = "Editor/Gizmo/FoliageBrushMaterial";

        /// <summary>
        /// The model vertex colors preview material.
        /// </summary>
        public static string VertexColorsPreviewMaterial = "Editor/Gizmo/VertexColorsPreviewMaterial";

        /// <summary>
        /// The Flax icon texture.
        /// </summary>
        public static string FlaxIconTexture = "Engine/Textures/FlaxIcon";

        /// <summary>
        /// The Flax icon (blue) texture.
        /// </summary>
        public static string FlaxIconBlueTexture = "Engine/Textures/FlaxIconBlue";

        /// <summary>
        /// The icon lists used by editor from the SegMDL2 font.
        /// </summary>
        /// <remarks>
        /// Reference: https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font.
        /// </remarks>
        public enum SegMDL2Icons
        {
#pragma warning disable 1591
            Cancel = 0xE711,
            ChromeMinimize = 0xE921,
            ChromeMaximize = 0xE922,
            ChromeRestore = 0xE923,
            ChromeClose = 0xE8BB,
#pragma warning restore 1591
        }
    }
}
