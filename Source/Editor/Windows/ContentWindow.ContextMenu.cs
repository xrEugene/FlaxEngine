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
            Assert.IsNull(_newElement);

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

                    // Reload goes above Re-Import. For scenes, only offer it while the scene is actually open (as the
                    // primary scene or loaded additively) - the underlying asset otherwise stays cached as "loaded"
                    // even after closing the scene, which would make Reload look available when there's nothing open to reload.
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

                    b = cm.AddButton("Re-Import", ReimportSelection);
                    b.Enabled = proxy != null && proxy.CanReimport(item);
                    b.IconBrush = icons?.ReImportBrush;

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

                    // Type-specific custom options (e.g. Scene's "Open as Additive"), placed under Reload/Re-Import/Refresh Thumbnail,
                    // delimited from it only when the proxy actually adds something.
                    int childCountBeforeCustom = cm.ItemsContainer.Children.Count;
                    proxy?.OnContentWindowContextMenu(cm, item);
                    if (cm.ItemsContainer.Children.Count > childCountBeforeCustom)
                    {
                        cm.AddSeparator();
                        var children = cm.ItemsContainer.Children;
                        var addedSeparator = children[^1];
                        children.RemoveAt(children.Count - 1);
                        children.Insert(childCountBeforeCustom, addedSeparator);
                    }

                    // Delimit Reload/Re-Import/Refresh Thumbnail/custom options from the asset info section below.
                    cm.AddSeparator();

                    if (importLocation != null)
                    {
                        cm.AddButton("Show Import Location", () => FileSystem.ShowFileExplorer(importLocation));
                    }

                    if (item is AssetItem assetItem)
                    {
                        cm.AddButton("Copy Asset ID", () => Clipboard.Text = JsonSerializer.GetStringID(assetItem.ID)).IconBrush = icons?.CopyAssetIdBrush;
                        cm.AddButton("Select Actors Using Asset", () => Editor.SceneEditing.SelectActorsUsingAsset(assetItem.ID)).IconBrush = icons?.SelectActorsUsingAssetBrush;
                        cm.AddButton("Show Asset References", () => Editor.Windows.Open(new AssetReferencesGraphWindow(Editor, assetItem))).IconBrush = icons?.ShowAssetReferencesBrush;
                    }

                    if (Editor.CanExport(item.Path))
                    {
                        b = cm.AddButton("Export", ExportSelection);
                    }

                    // Delimit this file-info section from the Cut/Copy/Paste/Duplicate block that follows.
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
                    cm.AddButton("Rename", () => Rename(item)).IconBrush = icons?.RenameBrush;
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
            nameTextBox.TextChanged += () =>
            {
                if (string.IsNullOrEmpty(nameTextBox.Text))
                {
                    nameTextBox.BorderColor = defaultTextBoxBorderColor;
                    nameTextBox.BorderSelectedColor = defaultTextBoxBorderSelectedColor;
                    return;
                }

                var pluginPath = Path.Combine(Globals.ProjectFolder, "Source", nameTextBox.Text);
                if (!IsValidModuleName(nameTextBox.Text) || Directory.Exists(pluginPath))
                {
                    nameTextBox.BorderColor = Color.Red;
                    nameTextBox.BorderSelectedColor = Color.Red;
                }
                else
                {
                    nameTextBox.BorderColor = defaultTextBoxBorderColor;
                    nameTextBox.BorderSelectedColor = defaultTextBoxBorderSelectedColor;
                }
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

            var submitButton = new Button
            {
                Parent = popup,
                AnchorPreset = AnchorPresets.TopLeft,
                Text = "Create",
                Width = 70,
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
    }
}
