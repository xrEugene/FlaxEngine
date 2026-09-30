// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlaxEditor.Content;
using FlaxEditor.GUI.ContextMenu;
using FlaxEditor.Scripting;
using FlaxEngine;
using FlaxEngine.Assertions;
using FlaxEngine.GUI;
using FlaxEngine.Json;

namespace FlaxEditor.Windows
{
    public partial class ContentWindow
    {
        /// <summary>
        /// Occurs when content window wants to show the context menu for the given content item. Allows to add custom options.
        /// </summary>
        public event Action<ContextMenu, ContentItem> ContextMenuShow;

        /// <summary>
        /// Content paths for "New Asset" context menu icons, keyed by the node's path under "New" as it appears in a
        /// <see cref="ContentContextMenuAttribute"/> path with the leading "New/" stripped (eg. "AI" for the category
        /// node from "New/AI/Behavior Tree", or "AI/Behavior Tree" for that leaf item node). Add an entry here to give
        /// any other category or item (eg. "Animation", "Material/Material") its own icon the same way.
        /// </summary>
        private static readonly Dictionary<string, string> _newAssetMenuIcons = new Dictionary<string, string>
        {
            { "AI", EditorAssets.NewAssetCategoryAIIcon },
            { "AI/Behavior Tree", EditorAssets.NewAssetItemBehaviorTreeIcon },
            { "Animation", EditorAssets.NewAssetCategoryAnimationIcon },
            { "Animation/Animation", EditorAssets.NewAssetItemAnimationIcon },
            { "Animation/Animation Graph", EditorAssets.NewAssetItemAnimationGraphIcon },
            { "Animation/Animation Graph Function", EditorAssets.NewAssetItemAnimationGraphFunctionIcon },
            { "Animation/Scene Animation", EditorAssets.NewAssetItemSceneAnimationIcon },
            { "Animation/Skeleton Mask", EditorAssets.NewAssetItemSkeletonMaskIcon },
            { "Gameplay Globals", EditorAssets.NewAssetItemGameplayGlobalsIcon },
            { "Json Asset", EditorAssets.NewAssetItemJsonAssetIcon },
            { "Material", EditorAssets.NewAssetCategoryMaterialIcon },
            { "Material/Material", EditorAssets.NewAssetItemMaterialIcon },
            { "Material/Material Function", EditorAssets.NewAssetItemAnimationGraphFunctionIcon },
            { "Material/Material Instance", EditorAssets.NewAssetItemMaterialInstanceIcon },
            { "Particles", EditorAssets.NewAssetCategoryParticlesIcon },
            { "Particles/Particle Emitter", EditorAssets.NewAssetItemParticleEmitterIcon },
            { "Particles/Particle Emitter Function", EditorAssets.NewAssetItemAnimationGraphFunctionIcon },
            { "Particles/Particle System", EditorAssets.NewAssetItemParticleSystemIcon },
            { "Physics", EditorAssets.NewAssetCategoryPhysicsIcon },
            { "Physics/Collision Data", EditorAssets.NewAssetItemCollisionDataIcon },
            { "Physics/Physical Material", EditorAssets.NewAssetItemJsonAssetIcon },
            { "Prefab", EditorAssets.NewAssetItemPrefabIcon },
            { "Widget", EditorAssets.NewAssetItemWidgetIcon },
            { "Settings", EditorAssets.NewAssetItemSettingsIcon },
            { "Visual Script", EditorAssets.NewAssetItemVisualScriptIcon },
            { "Scene", EditorAssets.NewAssetItemSceneIcon },
        };

        /// <summary>
        /// Loaded "New Asset" menu icon brushes, keyed the same way as <see cref="_newAssetMenuIcons"/>. Only
        /// populated once a key's texture has actually finished loading - these menus get (re)built once per
        /// right-click rather than every frame (unlike eg. <see cref="ContentItem.DrawGenericIconThumbnail"/>), so
        /// caching a still-loading texture's null result here would otherwise leave that icon missing forever.
        /// </summary>
        private static readonly Dictionary<string, IBrush> _newAssetMenuIconCache = new Dictionary<string, IBrush>();

        private static IBrush GetNewAssetMenuIcon(string key)
        {
            if (key == null || !_newAssetMenuIcons.TryGetValue(key, out var path))
                return null;
            if (_newAssetMenuIconCache.TryGetValue(key, out var brush))
                return brush;
            var texture = FlaxEngine.Content.LoadAsyncInternal<Texture>(path);
            if (texture == null || texture.WaitForLoaded())
                return null;
            brush = new TextureBrush(texture);
            _newAssetMenuIconCache[key] = brush;
            return brush;
        }

        private void MakeContextMenuUnlimited(ContextMenu menu)
        {
            menu.MaximumItemsInViewCount = 10000;

            foreach (var item in menu.Items)
            {
                if (item is ContextMenuChildMenu childMenu)
                {
                    MakeContextMenuUnlimited(childMenu.ContextMenu);
                }
            }
        }

        private void ShowContextMenuForItem(ContentItem item, ref Float2 location, bool isTreeNode)
        {
            // Not a real, renameable/deletable/copyable item yet - it may still be showing (see
            // Rename(item, newShortName)) while its create-settings dialog (eg. Widget's root type picker) is up.
            if (item != null && item == _newElement)
                return;

            // Cache data
            bool isValidElement = item != null;
            var proxy = Editor.ContentDatabase.GetProxy(item);
            ContentFolder folder = null;
            bool isFolder = false;
            if (isValidElement)
            {
                isFolder = item.IsFolder;
                folder = isFolder ? (ContentFolder)item : item.ParentFolder;
            }
            else
            {
                folder = CurrentViewFolder;
            }
            Assert.IsNotNull(folder);

            // Check if folder is a protected root or 1st-level subroot (e.g. project root, engine root, Content, Source)
            bool isRootFolder = isFolder && IsRootFolder(folder);
            bool isSubRootFolder = isFolder && (folder.Node is MainContentFolderTreeNode || folder.Node?.ParentNode is ProjectFolderTreeNode || (folder.ParentFolder != null && folder.ParentFolder.Node is ProjectFolderTreeNode));
            bool isProtectedFolder = isRootFolder || isSubRootFolder;

            // Create context menu
            ContextMenuButton b;
            ContextMenu cm = new ContextMenu
            {
                Tag = item
            };
            var icons = Editor.Instance.Icons as CustomEditorIcons;

            if (isTreeNode)
            {
                b = cm.AddButton("Expand All", OnExpandAllClicked);
                b.Enabled = CurrentViewFolder.Node.ChildrenCount != 0 && !CurrentViewFolder.Node.IsFullyExpanded;
                b.IconBrush = icons?.ExpandAllBrush;

                b = cm.AddButton("Collapse All", OnCollapseAllClicked);
                b.Enabled = CurrentViewFolder.Node.ChildrenCount != 0 && !CurrentViewFolder.Node.IsFullyCollapsed;
                b.IconBrush = icons?.CollapseAllBrush;

                cm.AddSeparator();
            }

            if (isValidElement)
            {
                void AddShowInExplorer() => cm.AddButton(Utilities.Constants.ShowInExplorer, () => FileSystem.ShowFileExplorer(item.IsFolder ? item.Path : System.IO.Path.GetDirectoryName(item.Path))).IconBrush = icons?.ShowInExplorerBrush;
                int actionButtonsCount = 0;

                if (!_showAllContentInTree && !String.IsNullOrEmpty(Editor.Instance.Windows.ContentWin._itemsSearchBox.Text))
                {
                    cm.AddButton("Show in Content Panel", () =>
                    {
                        Editor.Instance.Windows.ContentWin.ClearItemsSearch();
                        Editor.Instance.Windows.ContentWin.Select(item);
                    });
                }

                if (!isFolder)
                {
                    // BinaryAssetItem.GetImportPath() synchronously loads the asset (with a timeout) purely to read its
                    // import path metadata - do that (and cache its result for the "Show Import Location" button below)
                    // before checking Reload's visibility, so Reload reflects that load within this same menu build
                    // instead of only becoming available on the next right-click.
                    string importLocation = null;
                    if (item is BinaryAssetItem binaryAssetForImportPath && !binaryAssetForImportPath.GetImportPath(out string importPath))
                    {
                        var candidateLocation = System.IO.Path.GetDirectoryName(importPath);
                        if (!string.IsNullOrEmpty(candidateLocation) && System.IO.Directory.Exists(candidateLocation))
                            importLocation = candidateLocation;
                    }

                    // Inserts a separator right before whatever this group just added to cm, but only if the group
                    // actually added something AND there's already content above it to separate from - avoids a
                    // leading separator with nothing above it, and back-to-back separators when a group in between
                    // (eg. Export, or Reload/Refresh Thumbnail, or the asset-info group below) turns out empty for
                    // this item type. Counts via cm.Items (filters to actual menu items), not the raw child list,
                    // which also includes the panel's own always-present VScrollBar.
                    void AddGroupSeparator(int itemCountBeforeGroup)
                    {
                        var itemsNow = cm.Items.ToList();
                        if (itemsNow.Count > itemCountBeforeGroup && itemCountBeforeGroup > 0)
                        {
                            var firstNewItem = (Control)itemsNow[itemCountBeforeGroup];
                            cm.AddSeparator();
                            var children = cm.ItemsContainer.Children;
                            var addedSeparator = children[^1];
                            children.RemoveAt(children.Count - 1);
                            children.Insert(children.IndexOf(firstNewItem), addedSeparator);
                        }
                    }

                    // Audio clips and Skinned Models use a reordered layout (Export first, then Reload/Refresh
                    // Thumbnail, then Re-Import/Show Import Location, each its own delimited group); every other
                    // type keeps Reload/Re-Import/Refresh Thumbnail as a single undivided group, closer to this
                    // menu's original layout.
                    bool useGroupedLayout = item is AudioClipItem || proxy is SkinnedModelProxy || proxy is TextureProxy;

                    if (useGroupedLayout)
                    {
                        int itemCountBeforeExport = cm.Items.Count();
                        if (Editor.CanExport(item.Path))
                        {
                            b = cm.AddButton("Export", ExportSelection);
                            b.IconBrush = icons?.ExportBrush;
                        }
                        AddGroupSeparator(itemCountBeforeExport);
                    }

                    // Reload + Refresh Thumbnail group (+ Re-Import in between, for the non-audio layout). For
                    // scenes, Reload is only offered while the scene is actually open (as the primary scene or
                    // loaded additively) - the underlying asset otherwise stays cached as "loaded" even after
                    // closing the scene, which would make Reload look available when there's nothing open to reload.
                    int itemCountBeforeReload = cm.Items.Count();
                    if (item is SceneItem reloadableScene)
                    {
                        if (Level.FindScene(reloadableScene.ID) != null)
                            cm.AddButton("Reload", reloadableScene.Reload).IconBrush = icons?.ReloadBrush;
                    }
                    else if (item is AssetItem reloadableItem)
                    {
                        var loadedAsset = FlaxEngine.Content.GetAsset(reloadableItem.ID);
                        if (loadedAsset != null && (loadedAsset.IsLoaded || loadedAsset.LastLoadFailed))
                            cm.AddButton("Reload", reloadableItem.Reload).IconBrush = icons?.ReloadBrush;
                    }

                    if (!useGroupedLayout)
                    {
                        b = cm.AddButton("Re-Import", ReimportSelection);
                        b.Enabled = proxy != null && proxy.CanReimport(item);
                        b.IconBrush = icons?.ReImportBrush;
                    }

                    if (item.HasDefaultThumbnail == false)
                    {
                        if (_view.SelectedCount > 1)
                            cm.AddButton("Refresh Thumbnails", () =>
                            {
                                foreach (var e in _view.Selection)
                                    e.RefreshThumbnail();
                            }).IconBrush = icons?.RefreshThumbnailsBrush;
                        else
                            cm.AddButton("Refresh Thumbnail", item.RefreshThumbnail).IconBrush = icons?.RefreshThumbnailsBrush;
                    }
                    AddGroupSeparator(itemCountBeforeReload);

                    if (useGroupedLayout)
                    {
                        // Re-Import + Show Import Location group.
                        int itemCountBeforeReImport = cm.Items.Count();
                        b = cm.AddButton("Re-Import", ReimportSelection);
                        b.Enabled = proxy != null && proxy.CanReimport(item);
                        b.IconBrush = icons?.ReImportBrush;

                        if (importLocation != null)
                        {
                            cm.AddButton("Show Import Location", () => FileSystem.ShowFileExplorer(importLocation)).IconBrush = icons?.ShowImportLocationBrush;
                        }
                        AddGroupSeparator(itemCountBeforeReImport);
                    }

                    // Type-specific custom options (e.g. Scene's "Open as Additive"), placed under Reload/Re-Import/Refresh Thumbnail,
                    // delimited from it only when the proxy actually adds something.
                    int itemCountBeforeCustom = cm.Items.Count();
                    proxy?.OnContentWindowContextMenu(cm, item);
                    AddGroupSeparator(itemCountBeforeCustom);

                    // Asset info group: Show Import Location and Export join this group for the non-audio layout
                    // (audio already placed them above); Copy Asset ID/Select Actors/Show Asset References always go here.
                    int itemCountBeforeAssetInfo = cm.Items.Count();
                    if (!useGroupedLayout && importLocation != null)
                    {
                        cm.AddButton("Show Import Location", () => FileSystem.ShowFileExplorer(importLocation)).IconBrush = icons?.ShowImportLocationBrush;
                    }

                    if (item is AssetItem assetItem)
                    {
                        cm.AddButton("Copy Asset ID", () =>
                        {
                            var idText = JsonSerializer.GetStringID(assetItem.ID);
                            Clipboard.Text = idText;
                            ShowCopiedToClipboardPopup(idText);
                        }).IconBrush = icons?.CopyAssetIdBrush;
                        cm.AddButton("Select Actors Using Asset", () => Editor.SceneEditing.SelectActorsUsingAsset(assetItem.ID)).IconBrush = icons?.SelectActorsUsingAssetBrush;
                        cm.AddButton("Show Asset References", () => Editor.Windows.Open(new AssetReferencesGraphWindow(Editor, assetItem))).IconBrush = icons?.ShowAssetReferencesBrush;
                    }

                    if (!useGroupedLayout && Editor.CanExport(item.Path))
                    {
                        b = cm.AddButton("Export", ExportSelection);
                        b.IconBrush = icons?.ExportBrush;
                    }
                    AddGroupSeparator(itemCountBeforeAssetInfo);

                    // Delimit this file-info section from the Cut/Copy/Paste/Duplicate block that follows.
                    if (cm.Items.Any())
                        cm.AddSeparator();
                }

                // Cut, Copy (only for non-protected items)
                if (!isProtectedFolder)
                {
                    if (_showAllContentInTree)
                    {
                        cm.AddButton("Cut", _treeOnlyPanel.Cut).IconBrush = icons?.CutBrush;
                        cm.AddButton("Copy", _treeOnlyPanel.Copy).IconBrush = icons?.CopyBrush;
                    }
                    else
                    {
                        cm.AddButton("Cut", _view.Cut).IconBrush = icons?.CutBrush;
                        cm.AddButton("Copy", _view.Copy).IconBrush = icons?.CopyBrush;
                    }
                    actionButtonsCount += 2;
                }

                // Paste (available for subroot folders like Content/Source and non-protected items, but not root project/engine folders)
                if (!isRootFolder)
                {
                    b = _showAllContentInTree ? cm.AddButton("Paste", _treeOnlyPanel.Paste) : cm.AddButton("Paste", _view.Paste);
                    b.Enabled = _view.CanPaste();
                    b.IconBrush = icons?.PasteBrush;
                    actionButtonsCount += 1;
                }

                // Duplicate (only for non-protected items)
                if (!isProtectedFolder)
                {
                    if (_showAllContentInTree)
                        cm.AddButton("Duplicate", _treeOnlyPanel.Duplicate).IconBrush = icons?.DuplicateBrush;
                    else
                        cm.AddButton("Duplicate", _view.Duplicate).IconBrush = icons?.DuplicateBrush;
                    actionButtonsCount += 1;
                }

                // Delete, Rename (only for non-protected items), placed under the Cut/Copy/Paste/Duplicate block above.
                // Delete and Rename always appear together (both or neither), so the separator only ever shows
                // when both are present, per the rule that it should require two options below it.
                if (!isProtectedFolder)
                {
                    cm.AddSeparator();
                    cm.AddButton("Delete", () => Delete(item)).IconBrush = icons?.DeleteBrush;
                    cm.AddButton("Rename", () =>
                    {
                        // For a tree-panel folder, delegate to the tree node's own rename (the same path F2
                        // uses) rather than ContentWindow.Rename(..., true) - that one re-navigates the view via
                        // Select()->Navigate() to bring the item into the grid, which visibly bounces the tree's
                        // current selection through the root folder on the way back to where it already was.
                        if (isTreeNode && item is ContentFolder folderToRename && folderToRename.Node != null)
                            folderToRename.Node.StartRenaming();
                        else
                            Rename(item, isTreeNode);
                    }).IconBrush = icons?.RenameBrush;
                    actionButtonsCount += 2;
                }

                // Show in Explorer, delimited from the Delete/Rename/Cut/Copy/Paste/Duplicate section above (when it has more than
                // one button - a single button reads fine right next to Show in Explorer without a separator) and from whatever follows below
                if (actionButtonsCount > 1)
                    cm.AddSeparator();
                AddShowInExplorer();
                cm.AddSeparator();

                // Custom options
                ContextMenuShow?.Invoke(cm, item);
                item.OnContextMenu(cm);

                cm.AddButton("Copy Name to Clipboard", () => Clipboard.Text = isRootFolder ? item.ShortName : (string.IsNullOrEmpty(item.NamePath) ? item.ShortName : item.NamePath)).IconBrush = icons?.CopyNameBrush;
                cm.AddButton("Copy Path to Clipboard", () => Clipboard.Text = item.Path.Replace('\\', '/')).IconBrush = icons?.CopyPathBrush;
            }
            else
            {
                b = cm.AddButton("Paste", _view.Paste);
                b.Enabled = _view.CanPaste();
                b.IconBrush = icons?.PasteBrush;

                cm.AddButton(Utilities.Constants.ShowInExplorer, () => FileSystem.ShowFileExplorer(CurrentViewFolder.Path)).IconBrush = icons?.ShowInExplorerBrush;
                cm.AddSeparator();

                cm.AddButton("Refresh", () => Editor.ContentDatabase.RefreshFolder(CurrentViewFolder, true)).IconBrush = icons?.RefreshBrush;

                cm.AddButton("Refresh Thumbnails", RefreshViewItemsThumbnails).IconBrush = icons?.RefreshThumbnailsBrush;
            }

            // Right-clicking a specific file doesn't get New Folder/New Asset/Import File - those create things
            // inside the containing folder, which isn't what a file under the cursor is about (it gets Re-Import instead, above).
            bool isFileClick = isValidElement && !isFolder;

            if (!isRootFolder && !isFileClick)
            {
                cm.AddSeparator();

                // Right-clicking directly on a folder tile targets New Folder/New Module/New Asset/Import File at
                // that folder (see below), but the view itself is still showing wherever the user was browsing -
                // navigate into it first so the newly created (or imported) item actually shows up where the user is looking.
                bool isFolderClick = isValidElement && isFolder;

                CreateNewFolderMenu(cm, folder, false, item, isTreeNode);
                CreateNewModuleMenu(cm, folder);
                CreateNewContentItemMenu(cm, folder);

                if (folder != null && folder.CanHaveAssets)
                {
                    cm.AddSeparator();
                    cm.AddButton("Import File", () =>
                    {
                        if (isFolderClick)
                            Open(folder);
                        _view.ClearSelection();
                        Editor.ContentImporting.ShowImportFileDialog(folder);
                    }).IconBrush = icons?.ImportFileBrush;
                }
            }

            // Remove any leftover separator
            if (cm.ItemsContainer.Children.LastOrDefault() is ContextMenuSeparator)
                cm.ItemsContainer.Children.Last().Dispose();
            
            // Make the context menu unlimited
            MakeContextMenuUnlimited(cm);

            // Show it
            cm.Show(this, location);
        }

        private void CreateNewModuleMenu(ContextMenu menu, ContentFolder folder, bool disableUncreatable = false)
        {
            // Check if is source folder to add new module
            if (folder?.ParentFolder?.Node is ProjectFolderTreeNode parentFolderNode && folder.Node == parentFolderNode.Source)
            {
                var button = menu.AddButton("New Module");
                button.CloseMenuOnClick = false;
                button.Clicked += () =>
                {
                    if (folder != CurrentViewFolder)
                        Open(folder);
                    NewModule(button, parentFolderNode.Source.Path);
                };
                button.IconBrush = (Editor.Instance.Icons as CustomEditorIcons)?.NewModuleBrush;
            }
            else if (disableUncreatable)
            {
                var button = menu.AddButton("New Module");
                button.Enabled = false;
                button.IconBrush = (Editor.Instance.Icons as CustomEditorIcons)?.NewModuleBrush;
            }
        }

        private bool CanCreateFolder(ContentFolder targetFolder, ContentItem item = null)
        {
            bool canCreateFolder = targetFolder != null && !IsRootFolder(targetFolder);
            return canCreateFolder;
        }

        /// <summary>
        /// Checks if the given folder is a protected root or 1st-level project folder (e.g. project root, engine root) where content items cannot be created directly.
        /// </summary>
        private bool IsRootFolder(ContentFolder folder)
        {
            return folder != null && (folder.Node is ProjectFolderTreeNode || folder.Node is RootContentFolderTreeNode || folder.ParentFolder == null);
        }

        private void CreateNewFolderMenu(ContextMenu menu, ContentFolder folder, bool disableUncreatable = false, ContentItem item = null, bool isTreeNode = false)
        {
            bool canCreateFolder = CanCreateFolder(folder, item);
            if (canCreateFolder || disableUncreatable)
            {
                var b = menu.AddButton("New Folder", () => NewFolder(folder, isTreeNode));
                b.Enabled = canCreateFolder;
                b.IconBrush = (Editor.Instance.Icons as CustomEditorIcons)?.NewFolderBrush;
            }
        }

        private void CreateNewContentItemMenu(ContextMenu menu, ContentFolder folder, bool showNew = true, bool disableUncreatable = false)
        {
            // Loop through each proxy and user defined json type and add them to the context menu
            var actorType = new ScriptType(typeof(Actor));
            var scriptType = new ScriptType(typeof(Script));
            foreach (var type in Editor.CodeEditing.All.Get())
            {
                if (type.IsAbstract || type.Type == null)
                    continue;
                if (actorType.IsAssignableFrom(type) || scriptType.IsAssignableFrom(type))
                    continue;

                // Get attribute
                ContentContextMenuAttribute attribute = null;
                foreach (var typeAttribute in type.GetAttributes(false))
                {
                    if (typeAttribute is ContentContextMenuAttribute contentContextMenuAttribute)
                    {
                        attribute = contentContextMenuAttribute;
                        break;
                    }
                }
                if (attribute == null)
                    continue;

                // Get context proxy
                ContentProxy p = null;
                if (type.Type.IsSubclassOf(typeof(ContentProxy)))
                {
                    p = Editor.ContentDatabase.Proxy.Find(x => x.GetType() == type.Type);
                }
                else if (type.CanCreateInstance)
                {
                    // User can use attribute to put their own assets into the content context menu
                    var generic = typeof(SpawnableJsonAssetProxy<>).MakeGenericType(type.Type);
                    var instance = Activator.CreateInstance(generic);
                    p = instance as AssetProxy;
                }
                if (p == null)
                    continue;

                bool canCreate = p.CanCreate(folder);
                if (canCreate || disableUncreatable)
                {
                    var parts = attribute.Path.Split('/');
                    ContextMenuChildMenu childCM = null;
                    bool mainCM = true;
                    for (int i = 0; i < parts?.Length; i++)
                    {
                        var part = parts[i].Trim();
                        if (part == "New" && !showNew)
                            continue;
                        // The node's path under "New" (eg. "AI/Behavior Tree"), used as the _newAssetMenuIcons key.
                        var menuIconKey = parts.Length > 1 ? string.Join("/", parts, 1, i) : null;

                        if (i == parts.Length - 1)
                        {
                            if (mainCM)
                            {
                                var b = menu.AddButton(part, () => NewItem(p, targetFolder: folder));
                                b.Enabled = canCreate;
                                b.IconBrush = GetNewAssetMenuIcon(menuIconKey);
                                mainCM = false;
                            }
                            else if (childCM != null)
                            {
                                var b = childCM.ContextMenu.AddButton(part, () => NewItem(p, targetFolder: folder));
                                b.Enabled = canCreate;
                                b.IconBrush = GetNewAssetMenuIcon(menuIconKey);
                                childCM.ContextMenu.AutoSort = true;
                            }
                        }
                        else
                        {
                            if (mainCM)
                            {
                                childCM = menu.GetOrAddChildMenu(part == "New" ? "New Asset" : part);
                                childCM.ContextMenu.AutoSort = true;
                                childCM.Enabled = canCreate;
                                if (part == "New")
                                    childCM.IconBrush = (Editor.Instance.Icons as CustomEditorIcons)?.NewAssetBrush;
                                else if (childCM.IconBrush == null)
                                    childCM.IconBrush = GetNewAssetMenuIcon(menuIconKey);
                                mainCM = false;
                            }
                            else if (childCM != null)
                            {
                                childCM = childCM.ContextMenu.GetOrAddChildMenu(part);
                                childCM.ContextMenu.AutoSort = true;
                                childCM.Enabled = canCreate;
                                if (childCM.IconBrush == null)
                                    childCM.IconBrush = GetNewAssetMenuIcon(menuIconKey);
                            }
                        }
                    }
                }
            }
        }

        private void OnExpandAllClicked(ContextMenuButton button)
        {
            CurrentViewFolder.Node.ExpandAll();
        }

        private void OnCollapseAllClicked(ContextMenuButton button)
        {
            CurrentViewFolder.Node.CollapseAll();
        }

        /// <summary>
        /// Refreshes thumbnails for all the items in the view.
        /// </summary>
        private void RefreshViewItemsThumbnails()
        {
            var items = _view.Items;
            for (int i = 0; i < items.Count; i++)
            {
                items[i].RefreshThumbnail();
            }
        }

        /// <summary>
        /// Reimports the selected assets.
        /// </summary>
        private void ReimportSelection()
        {
            var selection = _view.Selection;
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] is BinaryAssetItem binaryAssetItem)
                    Editor.ContentImporting.Reimport(binaryAssetItem);
                else if (selection[i] is PrefabItem prefabItem)
                {
                    var prefab = FlaxEngine.Content.Load<Prefab>(prefabItem.ID);
                    var modelPrefab = prefab.GetDefaultInstance().GetScript<ModelPrefab>();
                    if (!modelPrefab)
                        continue;
                    var importPath = modelPrefab.ImportPath;
                    var editor = Editor.Instance;
                    if (editor.ContentImporting.GetReimportPath("Model Prefab", ref importPath))
                        continue;
                    var folder = editor.ContentDatabase.Find(Path.GetDirectoryName(prefab.Path)) as ContentFolder;
                    if (folder == null)
                        continue;
                    var importOptions = modelPrefab.ImportOptions;
                    importOptions.Type = FlaxEngine.Tools.ModelTool.ModelType.Prefab;
                    editor.ContentImporting.Import(importPath, folder, true, importOptions);
                }
            }
        }

        private bool Export(ContentItem item, string outputFolder)
        {
            if (item is ContentFolder folder)
            {
                for (int i = 0; i < folder.Children.Count; i++)
                {
                    if (Export(folder.Children[i], outputFolder))
                        return true;
                }
            }
            else if (item is AssetItem asset && Editor.CanExport(asset.Path))
            {
                if (Editor.Export(asset.Path, outputFolder))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Exports the selected items.
        /// </summary>
        private void ExportSelection()
        {
            if (FileSystem.ShowBrowseFolderDialog(Editor.Windows.MainWindow, null, "Select the output folder", out var outputFolder))
                return;

            var selection = _view.Selection;
            for (int i = 0; i < selection.Count; i++)
            {
                if (Export(selection[i], outputFolder))
                    return;
            }
        }

        private void NewModule(ContextMenuButton button, string path)
        {
            var popup = new ContextMenuBase
            {
                Size = new Float2(230, 125),
                ClipChildren = false,
                CullChildren = false,
            };
            popup.Show(button, new Float2(button.Width, 0));

            // When there isn't enough space below to show it top-aligned with the button, ContextMenuBase flips it
            // upward (Direction ends up *Up) by subtracting its own height from the button's TOP - which lands its
            // bottom edge a bit off from the button's BOTTOM edge (the alignment that actually looks right beside a
            // menu button), rather than exactly on it. Recompute the Y position directly from the button's actual
            // bottom instead of nudging by a guessed constant, so it lines up exactly regardless of button height.
            if (popup.Direction == ContextMenuDirection.RightUp || popup.Direction == ContextMenuDirection.LeftUp)
            {
                var window = popup.RootWindow?.Window;
                if (window != null)
                {
                    // button.Height alone overshoots the row's actual visual bottom by a few px (likely the
                    // button's own bottom border/separator isn't included in its layout Height) - compensate.
                    var buttonBottomSS = button.PointToScreen(new Float2(0, button.Height)).Y + 4.0f * window.DpiScale;
                    var bounds = window.ClientBounds;
                    window.ClientPosition = new Float2(bounds.X, buttonBottomSS - bounds.Height);
                }
            }

            var nameLabel = new Label
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
                Text = "Name",
                HorizontalAlignment = TextAlignment.Near,
            };
            nameLabel.LocalX += 10;
            nameLabel.LocalY += 10;

            var nameTextBox = new TextBox
            {
                Parent = popup,
                WatermarkText = "Module Name",
                AnchorPreset = AnchorPresets.TopLeft,
                IsMultiline = false,
            };
            nameTextBox.LocalX += 100;
            nameTextBox.LocalY += 10;
            var defaultTextBoxBorderColor = nameTextBox.BorderColor;
            var defaultTextBoxBorderSelectedColor = nameTextBox.BorderSelectedColor;

            // Forward-declared so the TextChanged handler below (wired before the button exists) can disable it.
            Button submitButton = null;

            bool IsNameEntryValid()
            {
                var text = nameTextBox.Text;
                if (string.IsNullOrWhiteSpace(text))
                    return false;
                if (!IsValidModuleName(text))
                    return false;
                return !Directory.Exists(Path.Combine(Globals.ProjectFolder, "Source", text));
            }

            nameTextBox.TextChanged += () =>
            {
                bool isEmpty = string.IsNullOrEmpty(nameTextBox.Text);
                bool isValid = !isEmpty && IsNameEntryValid();
                if (isEmpty || isValid)
                {
                    nameTextBox.BorderColor = defaultTextBoxBorderColor;
                    nameTextBox.BorderSelectedColor = defaultTextBoxBorderSelectedColor;
                }
                else
                {
                    nameTextBox.BorderColor = Color.Red;
                    nameTextBox.BorderSelectedColor = Color.Red;
                }

                if (submitButton != null)
                    submitButton.Enabled = isValid;
            };

            var editorLabel = new Label
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
                Text = "Editor",
                HorizontalAlignment = TextAlignment.Near,
            };
            editorLabel.LocalX += 10;
            editorLabel.LocalY += 35;

            var editorCheckBox = new CheckBox
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
            };
            editorCheckBox.LocalY += 35;
            editorCheckBox.LocalX += 100;

            var cppLabel = new Label
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
                Text = "C++",
                HorizontalAlignment = TextAlignment.Near,
            };
            cppLabel.LocalX += 10;
            cppLabel.LocalY += 60;

            var cppCheckBox = new CheckBox
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
            };
            cppCheckBox.LocalY += 60;
            cppCheckBox.LocalX += 100;

            submitButton = new Button
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
                Text = "Create",
                Width = 70,
                Enabled = false,
            };
            submitButton.LocalX += 40;
            submitButton.LocalY += 90;
            submitButton.Clicked += () =>
            {
                // TODO: Check all modules in project including plugins
                if (!IsValidModuleName(nameTextBox.Text))
                {
                    Editor.LogWarning("Invalid module name. Module names cannot contain spaces, start with a number or contain non-alphanumeric characters.");
                    return;
                }
                
                if (Directory.Exists(Path.Combine(Globals.ProjectFolder, "Source", nameTextBox.Text)))
                {
                    Editor.LogWarning("Cannot create module due to name conflict.");
                    return;
                }
                Editor.CodeEditing.CreateModule(path, nameTextBox.Text, editorCheckBox.Checked, cppCheckBox.Checked);
                nameTextBox.Clear();
                editorCheckBox.Checked = false;
                cppCheckBox.Checked = false;
                popup.Hide();
                button.ParentContextMenu.Hide();
            };

            var cancelButton = new Button
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
                Text = "Cancel",
                Width = 70,
            };
            cancelButton.LocalX += 120;
            cancelButton.LocalY += 90;
            cancelButton.Clicked += () =>
            {
                nameTextBox.Clear();
                editorCheckBox.Checked = false;
                cppCheckBox.Checked = false;
                popup.Hide();
                button.ParentContextMenu.Hide();
            };
        }

        private static bool IsValidModuleName(string text)
        {
            if (text.Contains(' '))
                return false;
            if (char.IsDigit(text[0]))
                return false;
            if (text.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                return false;
            return true;
        }

        /// <summary>
        /// Shows a large, centered, self-dismissing popup confirming a value was copied to the clipboard (eg. the
        /// "Copy Asset ID" context menu action).
        /// </summary>
        private void ShowCopiedToClipboardPopup(string value)
        {
            var plainMessage = $"Copied \"{value}\" to Clipboard";
            var richMessage = $"Copied \"<b>{value}</b>\" to Clipboard";
            var popup = new CopiedToClipboardPopup(plainMessage, richMessage);
            var mainWindowGUI = Editor.Instance.Windows.MainWindow.GUI;
            var location = (mainWindowGUI.Size - popup.Size) * 0.5f;
            popup.Show(mainWindowGUI, location);
        }

        /// <summary>
        /// A centered popup (styled the same as <see cref="ContextMenuBase"/>'s own background/border), shown over
        /// the whole editor rather than just the current window, with an info icon and a line of text, that closes
        /// itself after a short delay instead of waiting for user interaction.
        /// </summary>
        private sealed class CopiedToClipboardPopup : ContextMenuBase
        {
            private const float IconSize = 28.0f;
            private const float PaddingH = 8.5f;
            private const float PaddingV = 13.5f;
            private const float IconTextGap = 9.0f;
            private const float ContentShiftRight = 6.0f;

            private static Texture _infoIconCache;

            private float _remainingTime = 2.0f;

            /// <param name="plainMessage">The message with no markup - used only to measure how big the popup needs to be.</param>
            /// <param name="richMessage">The message with <see cref="RichTextBox"/> markup (eg. "&lt;b&gt;") applied - what's actually shown.</param>
            public CopiedToClipboardPopup(string plainMessage, string richMessage)
            {
                ClipChildren = false;
                CullChildren = false;

                var font = Style.Current.FontMedium;
                // Measure with the bold variant, not the plain one - part of the real text renders bold (wider than
                // plain), and measuring with the plain font undersized the popup enough to clip the right edge of
                // the text.
                var textSize = font.Asset.GetBold().CreateFont(font.Size).MeasureText(plainMessage);
                var textAreaLeft = PaddingH + IconSize + IconTextGap;
                // RichTextBox lays its text out inset by TextBoxBase.DefaultMargin from its own control rect
                // regardless of ClipText, so the text's real right edge sits DefaultMargin short of where the
                // control rect (and PaddingH below) alone would suggest - add it back so "text end to border"
                // ends up the same distance as "icon to left border" (both just PaddingH).
                Size = new Float2(textAreaLeft + textSize.X + TextBoxBase.DefaultMargin + PaddingH, PaddingV * 2 + Mathf.Max(IconSize, textSize.Y));

                new RichTextBox
                {
                    Text = richMessage,
                    IsMultiline = false,
                    IsReadOnly = true,
                    IsSelectable = false,
                    HasBorder = false,
                    AutoFocus = false,
                    BackgroundColor = Color.Transparent,
                    BackgroundSelectedColor = Color.Transparent,
                    // Text never scrolls/overflows here, so the clip mask serves no purpose - it was the actual
                    // cause of the "C" looking cut: ClipText's rect starts exactly at the text's own left edge,
                    // just barely inside a curved glyph's natural left overshoot.
                    ClipText = false,
                    TextStyle = new TextBlockStyle
                    {
                        Font = new FontReference(font),
                        Color = Color.White,
                        Alignment = TextBlockStyle.Alignments.Left | TextBlockStyle.Alignments.Middle,
                    },
                    AnchorPreset = AnchorPresets.StretchAll,
                    Offsets = new Margin(textAreaLeft + ContentShiftRight, PaddingH, 0.0f, 0.0f),
                    Parent = this,
                };
            }

            /// <inheritdoc />
            public override void Draw()
            {
                base.Draw();

                // Thin blue outline frame
                Render2D.DrawRectangle(new Rectangle(Float2.Zero, Size), Color.FromRGB(0x0079CC), 4.0f);

                // The standalone info icon texture (loaded once and cached, like ContentItem's generic icons)
                if (_infoIconCache == null)
                    _infoIconCache = FlaxEngine.Content.LoadAsyncInternal<Texture>(EditorAssets.CopiedToClipboardInfoIcon);
                if (_infoIconCache != null && !_infoIconCache.WaitForLoaded())
                {
                    var iconRect = new Rectangle(PaddingH + ContentShiftRight, (Height - IconSize) * 0.5f, IconSize, IconSize);
                    Render2D.DrawTexture(_infoIconCache, iconRect, Color.White);
                }
            }

            /// <inheritdoc />
            public override void Update(float deltaTime)
            {
                base.Update(deltaTime);

                _remainingTime -= deltaTime;
                if (_remainingTime <= 0.0f)
                    Hide();
            }

            /// <inheritdoc />
            public override bool OnMouseDown(Float2 location, MouseButton button)
            {
                Hide();
                return true;
            }
        }
    }
}
