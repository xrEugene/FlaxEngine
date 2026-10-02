// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.Collections.Generic;
using FlaxEditor.Content.GUI;
using FlaxEditor.GUI.Drag;
using FlaxEditor.GUI.Tree;
using FlaxEditor.Utilities;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.Content;

/// <summary>
/// Tree node for non-folder content items.
/// </summary>
public sealed class ContentItemTreeNode : TreeNode, IContentItemOwner
{
    private QueryFilterHelper.Range[] _highlightRanges;
    private float _lastHeaderClickDownTime = -1f;
    private HeaderClickGesture _pendingHeaderClickGesture = HeaderClickGesture.Select;

    private enum HeaderClickGesture
    {
        Select,
        Open,
        Rename,
    }

    // Same click-gesture timing as ContentFolderTreeNode (see its own remarks): a second left-click within
    // OpenMaxGap of the first opens the item (matches a fast double-click), one landing between OpenMaxGap and
    // RenameMaxGap instead starts a rename, like Windows Explorer's "click, pause, click" gesture - previously
    // only folders supported this in Tree View, leaving no way to rename an asset there via double-click.
    private const float OpenMaxGap = 0.25f;
    private const float RenameMaxGap = 1.0f;

    /// <summary>
    /// The content item.
    /// </summary>
    public ContentItem Item { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemTreeNode"/> class.
    /// </summary>
    /// <param name="item">The content item.</param>
    public ContentItemTreeNode(ContentItem item)
    : base(false, Editor.Instance.Icons.Document128, Editor.Instance.Icons.Document128)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        UpdateDisplayedName();
        IconColor = Color.Transparent; // Reserve icon space but draw custom thumbnail.
        Item.AddReference(this);
    }

    /// <summary>
    /// Updates the query search filter.
    /// </summary>
    /// <param name="filterText">The filter text.</param>
    public void UpdateFilter(string filterText)
    {
        bool noFilter = string.IsNullOrWhiteSpace(filterText);
        bool isVisible;
        if (noFilter)
        {
            _highlightRanges = null;
            isVisible = true;
        }
        else
        {
            if (QueryFilterHelper.Match(filterText, Text, out QueryFilterHelper.Range[] ranges))
            {
                _highlightRanges = ranges;
                isVisible = true;
            }
            else
            {
                _highlightRanges = null;
                isVisible = false;
            }
        }

        Visible = isVisible;
    }

    /// <inheritdoc />
    public override void Draw()
    {
        base.Draw();

        var contentWindow = Editor.Instance.Windows.ContentWin;
        var scale = contentWindow != null && contentWindow.IsTreeOnlyMode ? contentWindow.View.ViewScale : 1.0f;
        var iconSize = Mathf.Clamp(16.0f * scale, 12.0f, 28.0f);
        var textRect = TextRect;
        var iconRect = new Rectangle(textRect.Left - iconSize - 2.0f, (HeaderHeight - iconSize) * 0.5f, iconSize, iconSize);

        // GenericThumbnailIcon/Text takes priority over Thumbnail/DefaultThumbnail, matching ContentItem.Draw's own
        // order: for an item using that newer mechanism (eg. Animation Graph, Skeleton Mask), ThumbnailsModule still
        // populates Thumbnail alongside it with a real but tiny/wrong baked-in sprite (see
        // ThumbnailsModule.RequestPreview's own remarks on why it has to keep doing that), so checking Thumbnail
        // first would draw that wrong sprite instead of ever reaching the generic one.
        if (Item.HasGenericThumbnail)
        {
            // Keep the background fill (matches every other icon's own slot here), but skip the accent-color bar -
            // there's no room for that to read as anything but visual noise around an icon this small.
            Item.DrawGenericThumbnail(ref iconRect, true, false);
        }
        else
        {
            var icon = Item.Thumbnail;
            if (!icon.IsValid)
                icon = Item.DefaultThumbnail;
            if (icon.IsValid)
                Render2D.DrawSprite(icon, iconRect);
            else
                Render2D.DrawSprite(Editor.Instance.Icons.Document128, iconRect);
        }

        if (_highlightRanges != null && _highlightRanges.Length > 0)
        {
            var style = Style.Current;
            var color = style.ProgressNormal * 0.6f;
            var font = style.FontSmall;

            var text = Text;

            for (int i = 0; i < _highlightRanges.Length; i++)
            {
                var start = font.GetCharPosition(text, _highlightRanges[i].StartIndex);
                var end = font.GetCharPosition(text, _highlightRanges[i].EndIndex);

                Render2D.FillRectangle(new Rectangle(start.X + textRect.X, textRect.Y, end.X - start.X, textRect.Height), color);
            }
        }
    }

    /// <summary>
    /// Classifies a just-started left-click press on the header against the previous one's timestamp (see
    /// <see cref="OpenMaxGap"/>/<see cref="RenameMaxGap"/>) and records it as the one to act on once this press
    /// is released. Called from both <see cref="OnMouseDown"/> and <see cref="OnMouseDoubleClickHeader"/> - a
    /// platform may substitute the latter for the former's second call when its own double-click speed setting
    /// is satisfied, but either way this is "a press just happened", so both feed the same timer.
    /// </summary>
    private void RegisterHeaderClickDown()
    {
        var now = Time.UnscaledGameTime;
        var sinceLastClick = _lastHeaderClickDownTime < 0f ? float.MaxValue : now - _lastHeaderClickDownTime;
        _lastHeaderClickDownTime = now;
        if (sinceLastClick <= OpenMaxGap)
            _pendingHeaderClickGesture = HeaderClickGesture.Open;
        else if (sinceLastClick <= RenameMaxGap)
            _pendingHeaderClickGesture = HeaderClickGesture.Rename;
        else
            _pendingHeaderClickGesture = HeaderClickGesture.Select;
    }

    /// <inheritdoc />
    public override bool OnMouseDown(Float2 location, MouseButton button)
    {
        if (button == MouseButton.Left && TestHeaderHit(ref location))
            RegisterHeaderClickDown();

        return base.OnMouseDown(location, button);
    }

    /// <inheritdoc />
    protected override bool OnMouseDoubleClickHeader(ref Float2 location, MouseButton button)
    {
        if (button != MouseButton.Left)
            return base.OnMouseDoubleClickHeader(ref location, button);

        // The open-vs-rename decision is made by our own OS-independent timing in OnMouseUp (via
        // RegisterHeaderClickDown) rather than here - this just needs to feed that same timer, since the platform
        // sends this instead of a second OnMouseDown once its own double-click speed setting is satisfied. The
        // actual action fires from the OnMouseUp that follows (both clicks' releases still hit that override).
        if (TestHeaderHit(ref location))
            RegisterHeaderClickDown();

        return true;
    }

    /// <inheritdoc />
    public override bool OnMouseUp(Float2 location, MouseButton button)
    {
        bool handled = base.OnMouseUp(location, button);

        if (button == MouseButton.Left && TestHeaderHit(ref location))
        {
            switch (_pendingHeaderClickGesture)
            {
            case HeaderClickGesture.Open:
                _lastHeaderClickDownTime = -1f;
                Editor.Instance.Windows.ContentWin.Open(Item);
                break;
            case HeaderClickGesture.Rename:
                _lastHeaderClickDownTime = -1f;
                if (Item.CanRename)
                    Editor.Instance.Windows.ContentWin.Rename(Item);
                break;
            }
        }

        return handled;
    }

    /// <inheritdoc />
    protected override void DoDragDrop()
    {
        DoDragDrop(DragItems.GetDragData(Item));
    }

    /// <inheritdoc />
    protected override bool ShowTooltip => true;

    /// <inheritdoc />
    public override bool OnShowTooltip(out string text, out Float2 location, out Rectangle area)
    {
        Item.UpdateTooltipText();
        TooltipText = Item.TooltipText;
        return base.OnShowTooltip(out text, out location, out area);
    }

    /// <inheritdoc />
    void IContentItemOwner.OnItemDeleted(ContentItem item)
    {
    }

    /// <inheritdoc />
    void IContentItemOwner.OnItemRenamed(ContentItem item)
    {
        UpdateDisplayedName();
    }

    /// <inheritdoc />
    void IContentItemOwner.OnItemReimported(ContentItem item)
    {
    }

    /// <inheritdoc />
    void IContentItemOwner.OnItemDispose(ContentItem item)
    {
    }

    /// <inheritdoc />
    public override int Compare(Control other)
    {
        if (other is ContentFolderTreeNode)
            return 1;
        if (other is ContentItemTreeNode otherItem)
            return ApplySortOrder(string.Compare(Text, otherItem.Text, StringComparison.InvariantCulture));
        return base.Compare(other);
    }

    /// <inheritdoc />
    public override void OnDestroy()
    {
        Item.RemoveReference(this);
        base.OnDestroy();
    }

    /// <summary>
    /// Updates the text of the node.
    /// </summary>
    public void UpdateDisplayedName()
    {
        var contentWindow = Editor.Instance?.Windows?.ContentWin;
        var showExtensions = contentWindow?.View?.ShowFileExtensions ?? true;
        Text = Item.ShowFileExtension || showExtensions ? Item.FileName : Item.ShortName;
    }

    private static SortType GetSortType()
    {
        return Editor.Instance?.Windows?.ContentWin?.CurrentSortType ?? SortType.AlphabeticOrder;
    }

    private static int ApplySortOrder(int result)
    {
        return GetSortType() == SortType.AlphabeticReverse ? -result : result;
    }
}
