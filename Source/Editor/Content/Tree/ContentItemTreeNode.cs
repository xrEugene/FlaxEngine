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

    private static SpriteHandle GetIcon(ContentItem item)
    {
        if (item == null)
            return SpriteHandle.Invalid;
        var icon = item.Thumbnail;
        if (!icon.IsValid)
            icon = item.DefaultThumbnail;
        if (!icon.IsValid)
            icon = Editor.Instance.Icons.Document128;
        return icon;
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

        var icon = GetIcon(Item);
        if (icon.IsValid)
        {
            var contentWindow = Editor.Instance.Windows.ContentWin;
            var scale = contentWindow != null && contentWindow.IsTreeOnlyMode ? contentWindow.View.ViewScale : 1.0f;
            var iconSize = Mathf.Clamp(16.0f * scale, 12.0f, 28.0f);
            var textRect = TextRect;
            var iconRect = new Rectangle(textRect.Left - iconSize - 2.0f, (HeaderHeight - iconSize) * 0.5f, iconSize, iconSize);
            Render2D.DrawSprite(icon, iconRect);
        }

        if (_highlightRanges != null && _highlightRanges.Length > 0)
        {
            var style = Style.Current;
            var color = style.ProgressNormal * 0.6f;
            var font = style.FontSmall;

            var text = Text;
            var textRect = TextRect;

            for (int i = 0; i < _highlightRanges.Length; i++)
            {
                var start = font.GetCharPosition(text, _highlightRanges[i].StartIndex);
                var end = font.GetCharPosition(text, _highlightRanges[i].EndIndex);

                Render2D.FillRectangle(new Rectangle(start.X + textRect.X, textRect.Y, end.X - start.X, textRect.Height), color);
            }
        }
    }

    /// <inheritdoc />
    protected override bool OnMouseDoubleClickHeader(ref Float2 location, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            Editor.Instance.Windows.ContentWin.Open(Item);
            return true;
        }

        return base.OnMouseDoubleClickHeader(ref location, button);
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
