// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using FlaxEditor.GUI;
using FlaxEditor.GUI.ContextMenu;
using FlaxEditor.GUI.Input;
using FlaxEditor.Options;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.Windows
{
    /// <summary>
    /// Editor window used to show engine output logs.
    /// </summary>
    /// <seealso cref="FlaxEditor.Windows.EditorWindow" />
    public sealed class OutputLogWindow : EditorWindow
    {
        private class ViewDropdown : ComboBox
        {
            /// <inheritdoc />
            public override void Draw()
            {
                var clientRect = new Rectangle(Float2.Zero, Size);
                float margin = clientRect.Height * 0.2f;
                float boxSize = clientRect.Height - margin * 2;
                bool isOpened = IsPopupOpened;
                bool enabled = EnabledInHierarchy;
                Color backgroundColor = BackgroundColor;
                Color borderColor = BorderColor;
                Color arrowColor = ArrowColor;

                if (!enabled)
                {
                    backgroundColor *= 0.5f;
                    arrowColor *= 0.7f;
                }
                else if (isOpened || _mouseDown)
                {
                    backgroundColor = BackgroundColorSelected;
                    borderColor = BorderColorSelected;
                    arrowColor = ArrowColorSelected;
                }
                else if (IsMouseOver)
                {
                    backgroundColor = BackgroundColorHighlighted;
                    borderColor = BorderColorHighlighted;
                    arrowColor = ArrowColorHighlighted;
                }

                Render2D.FillRectangle(clientRect, backgroundColor);
                if (enabled && (isOpened || _mouseDown))
                {
                    var accent = new Rectangle(0, clientRect.Height - 2.0f, clientRect.Width, 2.0f);
                    Render2D.FillRectangle(accent, FlaxEngine.GUI.Style.Current.BackgroundSelected);
                }
                else
                {
                    Render2D.DrawRectangle(clientRect, borderColor);
                }

                var textRect = new Rectangle(margin, 0, clientRect.Width - boxSize - 2.0f * margin, clientRect.Height);
                Render2D.PushClip(textRect);

                var textColor = TextColor;
                Render2D.DrawText(Font.GetFont(), "View", textRect, enabled ? textColor : textColor * 0.5f, TextAlignment.Near, TextAlignment.Center, TextWrapping.NoWrap, 1.0f, 1.0f);
                Render2D.PopClip();

                ArrowImage?.Draw(new Rectangle(clientRect.Width - margin - boxSize, margin, boxSize, boxSize), arrowColor);
            }

            /// <inheritdoc />
            public override bool OnMouseUp(Float2 location, MouseButton button)
            {
                if (_mouseDown && !_blockPopup)
                {
                    _mouseDown = false;
                    if (_popupMenu == null)
                    {
                        _popupMenu = OnCreatePopup();
                        _popupMenu.MaximumItemsInViewCount = MaximumItemsInViewCount;
                        _popupMenu.VisibleChanged += cm =>
                        {
                            var win = Root;
                            _blockPopup = win != null && new Rectangle(Float2.Zero, Size).Contains(PointFromWindow(win.MousePosition));
                            if (!_blockPopup)
                                Focus();
                        };
                    }
                    if (_popupMenu.Visible)
                    {
                        _popupMenu.Hide();
                        return true;
                    }
                    _popupMenu.Show(this, new Float2(1, Height));
                }
                else
                {
                    _blockPopup = false;
                }
                return true;
            }
        }

        /// <summary>
        /// The single log message entry.
        /// </summary>
        private struct Entry
        {
            /// <summary>
            /// The log level.
            /// </summary>
            public LogType Level;

            /// <summary>
            /// The log time (in UTC local format).
            /// </summary>
            public DateTime Time;

            /// <summary>
            /// The message contents.
            /// </summary>
            public string Message;
        };

        private struct TextBlockTag
        {
            internal enum Types
            {
                CodeLocation
            };

            public Types Type;
            public string Url;
            public int Line;
        }

        /// <summary>
        /// The output log textbox.
        /// </summary>
        /// <seealso cref="FlaxEngine.GUI.RichTextBoxBase" />
        private sealed class OutputTextBox : RichTextBoxBase
        {
            /// <summary>
            /// The parent window.
            /// </summary>
            public OutputLogWindow Window;

            /// <summary>
            /// The default text style.
            /// </summary>
            public TextBlockStyle DefaultStyle;

            /// <summary>
            /// The warning text style.
            /// </summary>
            public TextBlockStyle WarningStyle;

            /// <summary>
            /// The error text style.
            /// </summary>
            public TextBlockStyle ErrorStyle;

            public OutputTextBox()
            {
                _consumeAllKeyDownEvents = false;
            }

            /// <inheritdoc />
            public override void OnMouseEnter(Float2 location)
            {
                base.OnMouseEnter(location);
                Cursor = CursorType.IBeam;
            }

            /// <inheritdoc />
            public override void OnMouseLeave()
            {
                Cursor = CursorType.Default;
                base.OnMouseLeave();
            }

            /// <inheritdoc />
            protected override void OnParseTextBlocks()
            {
                if (ParseTextBlocks != null)
                {
                    ParseTextBlocks(_text, _textBlocks);
                    return;
                }

                // Use cached text blocks
                _textBlocks.Clear();
                _textBlocks.AddRange(Window._textBlocks);
            }

            /// <inheritdoc />
            public override bool OnMouseDoubleClick(Float2 location, MouseButton button)
            {
                // Click on text block
                int textLength = TextLength;
                if (textLength != 0)
                {
                    var hitPos = CharIndexAtPoint(ref location);
                    if (hitPos != -1 && GetTextBlock(hitPos, out var textBlock) && textBlock.Tag is TextBlockTag tag)
                    {
                        switch (tag.Type)
                        {
                        case TextBlockTag.Types.CodeLocation:
                            Window.Editor.CodeEditing.OpenFile(tag.Url, tag.Line);
                            return true;
                        }
                    }
                }

                return base.OnMouseDoubleClick(location, button);
            }
        }

        /// <summary>
        /// Command line input textbox control which can execute debug commands.
        /// </summary>
        private class CommandLineBox : TextBox
        {
            private sealed class Item : ItemsListContextMenu.Item
            {
                public CommandLineBox Owner;

                public Item()
                {
                }

                protected override void GetTextRect(out Rectangle rect)
                {
                    rect = new Rectangle(2, 0, Width - 4, Height);
                }

                public override bool OnCharInput(char c)
                {
                    if (Owner != null && (!Owner._searchPopup?.Visible ?? true))
                    {
                        // Redirect input into search textbox while typing and using command history
                        Owner.Set(Owner.Text + c);
                        return true;
                    }
                    else if (Owner != null && Owner._searchPopup != null && Owner._searchPopup.Visible)
                    {
                        // Redirect input into search textbox while typing and using command history
                        Owner.OnCharInput(c);
                        return true;
                    }
                    return false;
                }

                public override bool OnKeyDown(KeyboardKeys key)
                {
                    switch (key)
                    {
                    case KeyboardKeys.Delete:
                    case KeyboardKeys.Backspace:
                        if (Owner != null && (!Owner._searchPopup?.Visible ?? true))
                        {
                            // Redirect input into search textbox while typing and using command history
                            Owner.OnKeyDown(key);
                            return true;
                        }
                        break;
                    case KeyboardKeys.ArrowLeft:
                        if (Owner != null && (!Owner._searchPopup?.Visible ?? true))
                        {
                            // Focus back the input field as user want to modify command from history
                            Owner.HideHistory();
                            Owner.HideSearch();
                            Owner.RootWindow.Focus();
                            Owner.Focus();
                            Owner.OnKeyDown(key);
                            return true;
                        }
                        break;
                    case KeyboardKeys.ArrowDown:
                    case KeyboardKeys.ArrowUp:
                        // UI navigation
                        return base.OnKeyDown(key);
                    default:
                        if (Owner != null && (Owner._searchPopup?.Visible ?? false))
                        {
                            // Redirect input into search textbox while typing and using command history
                            Owner.OnKeyDown(key);
                            return true;
                        }
                        break;
                    }

                    return base.OnKeyDown(key);
                }

                public override void OnDestroy()
                {
                    Owner = null;
                    base.OnDestroy();
                }
            }

            private OutputLogWindow _window;
            private ItemsListContextMenu _searchPopup;
            private ItemsListContextMenu _historyPopup;
            private bool _isSettingText;

            private const string Prompt = ">";

            public CommandLineBox(float x, float y, float width, OutputLogWindow window)
            : base(false, x, y, width)
            {
                _window = window;
            }

            private float GetPromptWidth()
            {
                var font = Font?.GetFont();
                return font ? font.MeasureText(Prompt).X + 2.0f : 0.0f;
            }

            /// <inheritdoc />
            protected override Rectangle TextRectangle
            {
                get
                {
                    var rect = base.TextRectangle;
                    var promptWidth = GetPromptWidth();
                    return new Rectangle(rect.X + promptWidth, rect.Y, rect.Width - promptWidth, rect.Height);
                }
            }

            /// <inheritdoc />
            public override void DrawSelf()
            {
                base.DrawSelf();

                // Draw the persistent command prompt glyph next to the editable text (not part of the actual command)
                var font = Font.GetFont();
                if (font)
                {
                    var rect = base.TextRectangle;
                    rect.Width = GetPromptWidth();
                    var color = EnabledInHierarchy ? TextColor * 0.8f : TextColor * 0.6f;
                    Render2D.DrawText(font, Prompt, rect, color, TextAlignment.Near, TextAlignment.Center);
                }
            }

            private void Set(string command)
            {
                _isSettingText = true;
                SetText(command);
                SetSelection(command.Length);
                _isSettingText = false;
            }

            private void HideSearch()
            {
                if (_searchPopup != null)
                {
                    _searchPopup.Hide();
                    _searchPopup = null;
                }
            }

            private void HideHistory()
            {
                if (_historyPopup != null)
                {
                    _historyPopup.Dispose();
                    _historyPopup = null;
                }
            }

            private void ShowPopup(ref ItemsListContextMenu cm, IEnumerable<string> commands, string searchText = null)
            {
                if (cm == null)
                    cm = new ItemsListContextMenu(180, 220, false);
                else
                    cm.ClearItems();

                // Add items
                ItemsListContextMenu.Item lastItem = null;
                var itemFont = Style.Current.FontSmall;
                var maxWidth = 0.0f;
                foreach (var command in commands)
                {
                    cm.AddItem(lastItem = new Item
                    {
                        Name = command,
                        Owner = this,
                        Height = 20,
                    });
                    var flags = DebugCommands.GetCommandFlags(command);
                    if (flags.HasFlag(DebugCommands.CommandFlags.Exec))
                        lastItem.TintColor = new Color(0.75f, 0.75f, 1.0f, 1.0f);
                    else if (flags.HasFlag(DebugCommands.CommandFlags.Read) && !flags.HasFlag(DebugCommands.CommandFlags.Write))
                        lastItem.TintColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
                    lastItem.ItemFocused += item =>
                    {
                        // Set command
                        Set(item.Name);
                    };
                    maxWidth = Mathf.Max(maxWidth, itemFont.MeasureText(command).X);
                }

                // Setup popup
                var count = commands.Count();
                var totalHeight = count * lastItem.Height + cm.ItemsPanel.Margin.Height + cm.ItemsPanel.Spacing * (count - 1);
                cm.Height = 220;
                if (cm.Height > totalHeight)
                    cm.Height = totalHeight; // Limit popup height if list is small
                maxWidth += 8.0f + ScrollBar.DefaultSize; // Margin
                if (cm.Width < maxWidth)
                    cm.Width = maxWidth;
                if (searchText != null)
                {
                    cm.SortItems();
                    cm.Search(searchText);
                    cm.UseVisibilityControl = false;
                    cm.UseInput = false;
                }

                // Show popup
                cm.Show(this, Float2.Zero, ContextMenuDirection.RightUp);
                cm.ScrollViewTo(lastItem);
                if (searchText == null)
                {
                    lastItem.Focus();
                }
            }

            /// <inheritdoc />
            public override void Update(float deltaTime)
            {
                base.Update(deltaTime);

                if (_searchPopup != null)
                {
                    var mainWin = RootWindow?.Window;
                    var popupWin = _searchPopup.RootWindow?.Window;
                    bool isAppFocused = (mainWin != null && mainWin.IsFocused) || (popupWin != null && popupWin.IsFocused);
                    if (!isAppFocused || !VisibleInHierarchy)
                    {
                        HideSearch();
                    }
                    else if (Input.GetMouseButtonDown(MouseButton.Left) || Input.GetMouseButtonDown(MouseButton.Right) || Input.GetMouseButtonDown(MouseButton.Middle))
                    {
                        var mousePos = Platform.MousePosition;
                        var hasMouseOverPopup = popupWin != null && new Rectangle(popupWin.Position, popupWin.Size).Contains(mousePos);
                        var hasMouseOverBox = Rectangle.FromPoints(PointToScreen(Float2.Zero), PointToScreen(Size)).Contains(mousePos);
                        if (!hasMouseOverPopup && !hasMouseOverBox)
                        {
                            HideSearch();
                        }
                    }
                }

                if (_historyPopup != null)
                {
                    var mainWin = RootWindow?.Window;
                    var popupWin = _historyPopup.RootWindow?.Window;
                    bool isAppFocused = (mainWin != null && mainWin.IsFocused) || (popupWin != null && popupWin.IsFocused);
                    if (!isAppFocused || !VisibleInHierarchy)
                    {
                        HideHistory();
                    }
                    else if (Input.GetMouseButtonDown(MouseButton.Left) || Input.GetMouseButtonDown(MouseButton.Right) || Input.GetMouseButtonDown(MouseButton.Middle))
                    {
                        var mousePos = Platform.MousePosition;
                        var hasMouseOverPopup = popupWin != null && new Rectangle(popupWin.Position, popupWin.Size).Contains(mousePos);
                        var hasMouseOverBox = Rectangle.FromPoints(PointToScreen(Float2.Zero), PointToScreen(Size)).Contains(mousePos);
                        if (!hasMouseOverPopup && !hasMouseOverBox)
                        {
                            HideHistory();
                        }
                    }
                }
            }

            /// <inheritdoc />
            public override void OnLostFocus()
            {
                base.OnLostFocus();

                if (_searchPopup != null && !_searchPopup.ContainsFocus && !_searchPopup.IsMouseOver)
                    HideSearch();
                if (_historyPopup != null && !_historyPopup.ContainsFocus && !_historyPopup.IsMouseOver)
                    HideHistory();
            }

            /// <inheritdoc />
            protected override void OnVisibleChanged()
            {
                base.OnVisibleChanged();

                if (!VisibleInHierarchy)
                {
                    HideSearch();
                    HideHistory();
                }
            }

            /// <inheritdoc />
            public override void OnGotFocus()
            {
                // Precache debug commands to reduce time-to-interactive
                DebugCommands.InitAsync();

                base.OnGotFocus();
            }

            /// <inheritdoc />
            protected override void OnTextChanged()
            {
                base.OnTextChanged();

                // Skip when editing text from code
                if (_isSettingText)
                    return;

                // Show commands search popup based on current text input
                var text = Text.Trim();
                bool isWhitespaceOnly = string.IsNullOrWhiteSpace(Text) && !string.IsNullOrEmpty(Text);
                if (text.Length != 0 || isWhitespaceOnly)
                {
                    DebugCommands.Search(text, out var matches);
                    if (matches.Length != 0 || isWhitespaceOnly)
                    {
                        string[] commands = [];
                        if (isWhitespaceOnly)
                            DebugCommands.GetAllCommands(out commands);

                        HideHistory();
                        ShowPopup(ref _searchPopup, isWhitespaceOnly ? commands : matches, text);

                        if (isWhitespaceOnly)
                        {
                            // Scroll to and select first item for consistent behaviour
                            var firstItem = _searchPopup.ItemsPanel.Children[0] as Item;
                            _searchPopup.ScrollToAndHighlightItemByName(firstItem.Name);
                        }
                        else if (text.Length == 1)
                        {
                            // Scroll to the first item starting with the typed letter without highlighting it
                            _searchPopup.ScrollToFirstItemStartingWith(text);
                        }

                        return;
                    }
                }
                HideSearch();
            }

            /// <inheritdoc />
            public override bool OnKeyDown(KeyboardKeys key)
            {
                switch (key)
                {
                case KeyboardKeys.Escape:
                {
                    if (_searchPopup != null || _historyPopup != null)
                    {
                        HideSearch();
                        HideHistory();
                        return true;
                    }
                    break;
                }
                case KeyboardKeys.Return:
                {
                    // Run command
                    HideSearch();
                    HideHistory();
                    var command = Text.Trim();
                    if (command.Length == 0)
                        return true;
                    DebugCommands.Execute(command);
                    SetText(string.Empty);

                    // Update history buffer
                    if (_window._commandHistory == null)
                        _window._commandHistory = new List<string>();
                    else if (_window._commandHistory.Count != 0 && _window._commandHistory.Contains(command))
                        _window._commandHistory.Remove(command);
                    _window._commandHistory.Add(command);
                    if (_window._commandHistory.Count > CommandHistoryLimit)
                        _window._commandHistory.RemoveAt(0);
                    _window.SaveHistory();

                    return true;
                }
                case KeyboardKeys.Tab:
                {
                    // Auto-complete
                    DebugCommands.Search(Text, out var matches, true);
                    if (matches.Length == 0)
                    {
                        // Nothing found
                    }
                    else if (matches.Length == 1)
                    {
                        // Exact match
                        Set(matches[0]);
                    }
                    else
                    {
                        // Find the most common part
                        Array.Sort(matches);
                        int minLength = Text.Length;
                        int maxLength = matches[0].Length;
                        int sharedLength = minLength + 1;
                        bool allMatch = true;
                        for (; allMatch && sharedLength < maxLength; sharedLength++)
                        {
                            var shared = matches[0].Substring(0, sharedLength);
                            for (int i = 1; i < matches.Length; i++)
                            {
                                if (!matches[i].StartsWith(shared, StringComparison.OrdinalIgnoreCase))
                                {
                                    sharedLength -= 2;
                                    allMatch = false;
                                    break;
                                }
                            }
                        }
                        if (sharedLength > minLength)
                        {
                            // Use the largest shared part of all matches
                            Set(matches[0].Substring(0, sharedLength));
                        }
                    }
                    return true;
                }
                case KeyboardKeys.ArrowUp:
                {
                    if (_searchPopup != null && _searchPopup.Visible)
                    {
                        // Route navigation to active popup
                        var focusedItem = _searchPopup.RootWindow.FocusedControl as Item;
                        if (focusedItem == null)
                            _searchPopup.SelectItem((Item)_searchPopup.ItemsPanel.Children.Last());
                        else
                            _searchPopup.OnKeyDown(key);
                    }
                    else if (TextLength == 0)
                    {
                        if (_window._commandHistory != null && _window._commandHistory.Count != 0)
                        {
                            // Show command history popup
                            HideSearch();
                            ShowPopup(ref _historyPopup, _window._commandHistory);
                        }
                    }
                    return true;
                }
                case KeyboardKeys.ArrowDown:
                {
                    if (_searchPopup != null && _searchPopup.Visible)
                    {
                        // Route navigation to active popup
                        var focusedItem = _searchPopup.RootWindow.FocusedControl as Item;
                        if (focusedItem == null)
                            _searchPopup.SelectItem((Item)_searchPopup.ItemsPanel.Children.First());
                        else
                            _searchPopup.OnKeyDown(key);
                    }
                    return true;
                }
                }

                return base.OnKeyDown(key);
            }

            /// <inheritdoc />
            public override void OnDestroy()
            {
                HideSearch();
                HideHistory();

                base.OnDestroy();
            }
        }

        private InterfaceOptions.TimestampsFormats _timestampsFormats;
        private bool _showLogType;

        private List<Entry> _entries = new List<Entry>(1024);
        private bool _isDirty;
        private int _logTypeShowMask = (int)LogType.Info | (int)LogType.Warning | (int)LogType.Error | (int)LogType.Fatal;
        private float _scrollSize = 18.0f;
        private const int OutCapacity = 64;
        private string[] _outMessages = new string[OutCapacity];
        private byte[] _outLogTypes = new byte[OutCapacity];
        private long[] _outLogTimes = new long[OutCapacity];
        private int _textBufferCount;
        private StringBuilder _textBuffer = new StringBuilder();
        private List<TextBlock> _textBlocks = new List<TextBlock>();
        private DateTime _startupTime;
        private Regex _compileRegex = new Regex("(?<path>^(?:[a-zA-Z]\\:|\\\\\\\\[ \\-\\.\\w\\.]+\\\\[ \\-\\.\\w.$]+)\\\\(?:[ \\-\\.\\w]+\\\\)*\\w([ \\w.])+)\\((?<line>\\d{1,}),\\d{1,},\\d{1,},\\d{1,}\\): (?<level>error|warning) (?<message>.*)", RegexOptions.Compiled);
        private List<string> _commandHistory;
        private const string CommandHistoryKey = "CommandHistory";
        private const int CommandHistoryLimit = 30;

        private ViewDropdown _viewDropdown;
        private TextBox _searchBox;
        private HScrollBar _hScroll;
        private VScrollBar _vScroll;
        private OutputTextBox _output;
        private CommandLineBox _commandLineBox;
        private ContextMenu _contextMenu;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogWindow"/> class.
        /// </summary>
        /// <param name="editor">The editor.</param>
        public OutputLogWindow(Editor editor)
        : base(editor, true, ScrollBars.None)
        {
            Title = "Output Log";
            Icon = editor.Icons.Info64;
            ClipChildren = false;
            BackgroundColor = Style.Current.ContentBackground;
            FlaxEditor.Utilities.Utils.SetupCommonInputActions(this);

            // Setup UI
            const float viewWidth = 60.0f;
            const float viewHeight = 25.0f;
            float searchTop = 6.0f;
            float searchHeight = TextBoxBase.DefaultHeight;
            float viewTop = searchTop + Mathf.Floor((searchHeight - viewHeight) * 0.5f);
            _searchBox = new SearchBox(false, 2, searchTop, Width - viewWidth - 6)
            {
                Parent = this,
            };
            _searchBox.Height -= 1.5f;
            _viewDropdown = new ViewDropdown
            {
                TooltipText = "Change output log view options",
                X = Width - viewWidth - 2,
                Y = viewTop + 3.25f,
                Width = viewWidth,
                Height = viewHeight,
                AnchorPreset = AnchorPresets.TopRight,
                Parent = this,
            };
            _viewDropdown.PopupCreate += OnViewDropdownPopupCreate;
            _searchBox.TextChanged += Refresh;

            _hScroll = new HScrollBar(this, Height - _scrollSize - TextBox.DefaultHeight - 2, width: default, _scrollSize)
            {
                Maximum = 0
            };
            _vScroll = new VScrollBar(this, Width - _scrollSize, height: default, _scrollSize)
            {
                Maximum = 0
            };
            _vScroll.Y += 33f;

            _hScroll.ValueChanged += OnHScrollValueChanged;
            _vScroll.ValueChanged += OnVScrollValueChanged;

            var inactiveTabColor = Style.Current.Background;
            _output = new OutputTextBox
            {
                Window = this,
                IsReadOnly = true,
                IsMultiline = true,
                BackgroundSelectedFlashSpeed = 0.0f,
                Location = new Float2(2, _viewDropdown.Bottom + 7.5f),
                BackgroundColor = inactiveTabColor,
                BackgroundSelectedColor = inactiveTabColor,
                BorderColor = Color.Transparent,
                BorderSelectedColor = Color.Transparent,
                Parent = this
            };
            _output.TargetViewOffsetChanged += OnOutputTargetViewOffsetChanged;
            _output.TextChanged += OnOutputTextChanged;
            _commandLineBox = new CommandLineBox(2, Height - 7 - TextBox.DefaultHeight, Width - 4, this)
            {
                Height = TextBox.DefaultHeight + 5,
                Parent = this,
            };

            // Setup context menu
            _contextMenu = new ContextMenu();
            _contextMenu.AddButton("Clear log", Clear);
            _contextMenu.AddButton("Copy selection", _output.Copy);
            _contextMenu.AddButton("Select All", _output.SelectAll);
            _contextMenu.AddButton(Utilities.Constants.ShowInExplorer, () => FileSystem.ShowFileExplorer(Path.Combine(Globals.ProjectFolder, "Logs")));
            _contextMenu.AddButton("Scroll to bottom", () => { _vScroll.TargetValue = _vScroll.Maximum; }).Icon = Editor.Icons.ArrowDown12;

            // Setup editor options
            Editor.Options.OptionsChanged += OnEditorOptionsChanged;
            OnEditorOptionsChanged(Editor.Options.Options);

            InputActions.Add(options => options.Search, _searchBox.Focus);

            GameCooker.Event += OnGameCookerEvent;
            ScriptsBuilder.CompilationFailed += OnScriptsCompilationFailed;
        }

        private ContextMenu OnViewDropdownPopupCreate(ComboBox comboBox)
        {
            var menu = new ContextMenu();

            var infoLogButton = menu.AddButton("Info");
            infoLogButton.CloseMenuOnClick = false;
            infoLogButton.AutoCheck = true;
            infoLogButton.Checked = (_logTypeShowMask & (int)LogType.Info) != 0;
            infoLogButton.Clicked += () => ToggleLogTypeShow(LogType.Info);

            var warningLogButton = menu.AddButton("Warning");
            warningLogButton.CloseMenuOnClick = false;
            warningLogButton.AutoCheck = true;
            warningLogButton.Checked = (_logTypeShowMask & (int)LogType.Warning) != 0;
            warningLogButton.Clicked += () => ToggleLogTypeShow(LogType.Warning);

            var errorLogButton = menu.AddButton("Error");
            errorLogButton.CloseMenuOnClick = false;
            errorLogButton.AutoCheck = true;
            errorLogButton.Checked = (_logTypeShowMask & (int)LogType.Error) != 0;
            errorLogButton.Clicked += () => ToggleLogTypeShow(LogType.Error);

            menu.AddSeparator();
            menu.AddButton("Load log file...", LoadLogFile);
          
            return menu;
        }

        private void ToggleLogTypeShow(LogType type)
        {
            _logTypeShowMask ^= (int)type;
            Refresh();
        }

        private void OnHScrollValueChanged()
        {
            var viewOffset = _output.ViewOffset;
            viewOffset.X = _hScroll.Value;
            _output.TargetViewOffset = viewOffset;
        }

        private void OnVScrollValueChanged()
        {
            var viewOffset = _output.ViewOffset;
            viewOffset.Y = _vScroll.Value;
            _output.TargetViewOffset = viewOffset;
        }

        private void OnOutputTargetViewOffsetChanged()
        {
            if (!_hScroll.IsThumbClicked)
                _hScroll.TargetValue = _output.TargetViewOffset.X;
            if (!_vScroll.IsThumbClicked)
                _vScroll.TargetValue = _output.TargetViewOffset.Y;
        }

        private void OnOutputTextChanged()
        {
            if (IsLayoutLocked || _output == null)
                return;

            _hScroll.Maximum = Mathf.Max(_output.TextSize.X, _hScroll.Minimum);
            _vScroll.Maximum = Mathf.Max(_output.TextSize.Y - _output.Height, _vScroll.Minimum);
        }

        private void OnEditorOptionsChanged(EditorOptions options)
        {
            if (options.Interface.OutputLogTimestampsFormat == _timestampsFormats &&
                options.Interface.OutputLogShowLogType == _showLogType &&
                _output.DefaultStyle.Font == options.Interface.OutputLogTextFont &&
                _output.DefaultStyle.Color == options.Visual.LogInfoColor &&
                _output.DefaultStyle.ShadowColor == options.Interface.OutputLogTextShadowColor &&
                _output.DefaultStyle.ShadowOffset == options.Interface.OutputLogTextShadowOffset &&
                _output.WarningStyle.Color == options.Visual.LogWarningColor &&
                _output.ErrorStyle.Color == options.Visual.LogErrorColor)
                return;

            _output.DefaultStyle = new TextBlockStyle
            {
                Font = options.Interface.OutputLogTextFont,
                Color = options.Visual.LogInfoColor,
                ShadowColor = options.Interface.OutputLogTextShadowColor,
                ShadowOffset = options.Interface.OutputLogTextShadowOffset,
                BackgroundSelectedBrush = new SolidColorBrush(Style.Current.BackgroundSelected),
            };

            _output.WarningStyle = _output.DefaultStyle;
            _output.WarningStyle.Color = options.Visual.LogWarningColor;
            _output.ErrorStyle = _output.DefaultStyle;
            _output.ErrorStyle.Color = options.Visual.LogErrorColor;

            _timestampsFormats = options.Interface.OutputLogTimestampsFormat;
            _showLogType = options.Interface.OutputLogShowLogType;

            Refresh();
        }

        private void OnGameCookerEvent(GameCooker.EventType eventType)
        {
            if (eventType == GameCooker.EventType.BuildFailed && !Editor.IsHeadlessMode && Editor.Options.Options.Interface.FocusOutputLogOnGameBuildError)
                FocusOrShow();
        }

        private void OnScriptsCompilationFailed()
        {
            if (!Editor.IsHeadlessMode && Editor.Options.Options.Interface.FocusOutputLogOnCompilationError)
                FocusOrShow();
        }

        private void SaveHistory()
        {
            if (_commandHistory == null || _commandHistory.Count == 0)
                Editor.ProjectCache.RemoveCustomData(CommandHistoryKey);
            else
                Editor.ProjectCache.SetCustomData(CommandHistoryKey, FlaxEngine.Json.JsonSerializer.Serialize(_commandHistory));
        }

        /// <summary>
        /// Refreshes the log output.
        /// </summary>
        private void Refresh()
        {
            _textBufferCount = 0;
            _textBuffer.Clear();
            _textBlocks.Clear();
            _isDirty = true;
        }

        /// <summary>
        /// Clears the log.
        /// </summary>
        public void Clear()
        {
            _entries?.Clear();
            Refresh();
        }

        /// <summary>
        /// Loads the log from the file selected by the user with the file pickup dialog.
        /// </summary>
        public void LoadLogFile()
        {
            if (FileSystem.ShowOpenFileDialog(null, Path.Combine(Globals.ProjectFolder, "Logs"), null, false, "Pick a log file to load", out var files))
                return;
            if (files != null && files.Length > 0)
            {
                LoadLogFile(files[0]);
            }
        }

        /// <summary>
        /// Loads the log file.
        /// </summary>
        /// <param name="path">The path.</param>
        public void LoadLogFile(string path)
        {
            using (var file = File.OpenRead(path))
            using (var stream = new StreamReader(file))
            {
                _entries.Clear();
                var regex = new Regex(@"\[ (\d\d:\d\d:\d\d.\d\d\d) \]\: \[(\w*)\]");

                while (!stream.EndOfStream)
                {
                    // Read next line
                    var line = stream.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // Parse with regex
                    var match = regex.Match(line);
                    if (!match.Success || match.Groups.Count != 3)
                    {
                        // Try to add the line for multi-line logs
                        if (_entries.Count != 0 && !line.StartsWith("======"))
                        {
                            ref var last = ref CollectionsMarshal.AsSpan(_entries)[_entries.Count - 1];
                            last.Message += '\n';
                            last.Message += line;
                        }

                        continue;
                    }

                    // Parse log time and type
                    var time = match.Groups[1].Value;
                    var level = match.Groups[2].Value;
                    if (time.Length != 12)
                        continue;
                    int hours = int.Parse(time.Substring(0, 2));
                    int minutes = int.Parse(time.Substring(3, 2));
                    int seconds = int.Parse(time.Substring(6, 2));
                    int milliseconds = int.Parse(time.Substring(9, 3));
                    var timeSinceStartup = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                    var logType = (LogType)Enum.Parse(typeof(LogType), level);

                    // Add new entry
                    var e = new Entry
                    {
                        Time = _startupTime + timeSinceStartup,
                        Level = logType,
                        Message = line.Substring(match.Index + match.Length)
                    };
                    _entries.Add(e);
                }

                Refresh();
            }
        }

        /// <inheritdoc />
        protected override void PerformLayoutBeforeChildren()
        {
            base.PerformLayoutBeforeChildren();

            if (_output != null)
            {
                _searchBox.Width = Width - _viewDropdown.Width - 6;

                _commandLineBox.Width = Width - 4;
                _commandLineBox.Y = Height - 2 - _commandLineBox.Height;

                _hScroll.Bounds = new Rectangle(0, _commandLineBox.Y - _hScroll.Height, Width - _scrollSize + 5, _hScroll.Height);
                _vScroll.Bounds = new Rectangle(Width - _scrollSize, _vScroll.Y, _vScroll.Width, _hScroll.Y - _vScroll.Y + 5);

                _output.Size = new Float2(_vScroll.X - 1, _hScroll.Y - 4 - _viewDropdown.Bottom - 2);
            }
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            base.Draw();

            bool showHint = (((int)LogType.Info & _logTypeShowMask) == 0 &&
                            ((int)LogType.Warning & _logTypeShowMask) == 0 &&
                            ((int)LogType.Error & _logTypeShowMask) == 0) ||
                            string.IsNullOrEmpty(_output.Text) ||
                            _entries.Count == 0;
            if (showHint)
            {
                var textRect = _output.Bounds;
                var style = Style.Current;
                var text = "No log level filter active or no entries that apply to the current filter exist";
                if (_entries.Count == 0)
                    text = "No log";
                Render2D.DrawText(style.FontMedium, text, textRect, style.ForegroundGrey, TextAlignment.Center, TextAlignment.Center, TextWrapping.WrapWords);
            }
        }

        /// <inheritdoc />
        public override bool OnKeyDown(KeyboardKeys key)
        {
            var input = Editor.Options.Options.Input;
            if (input.Search.Process(this, key))
            {
                if (!_searchBox.ContainsFocus)
                {
                    _searchBox.Focus();
                    _searchBox.SelectAll();
                }
                return true;
            }

            return base.OnKeyDown(key);
        }

        /// <inheritdoc />
        public override bool OnMouseUp(Float2 location, MouseButton button)
        {
            if (base.OnMouseUp(location, button))
                return true;

            if (button == MouseButton.Right)
            {
                _contextMenu.Show(this, location);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            // Update scroll range
            OnOutputTextChanged();
        }

        /// <summary>
        /// Focus the debug command line and ensure that the output log window is visible.
        /// </summary>
        public void FocusCommand()
        {
            FocusOrShow();
            _commandLineBox.Focus();
        }

        /// <inheritdoc />
        public override void Update(float deltaTime)
        {
            FlaxEngine.Profiler.BeginEvent("OutputLogWindow.Update");

            // Read the incoming log messages
            int logCount;
            do
            {
                logCount = Editor.Internal_ReadOutputLogs(ref _outMessages, ref _outLogTypes, ref _outLogTimes, OutCapacity);

                for (int i = 0; i < logCount; i++)
                {
                    var entry = new Entry
                    {
                        Level = (LogType)_outLogTypes[i],
                        Time = new DateTime(_outLogTimes[i], DateTimeKind.Utc),
                        Message = _outMessages[i],
                    };
                    _entries.Add(entry);
                    _outMessages[i] = null;
                    _isDirty = true;
                }
            } while (logCount != 0);

            if (_isDirty)
            {
                _isDirty = false;
                var wasEmpty = _output.TextLength == 0;

                // Cache fonts
                _output.DefaultStyle.Font.GetFont();
                _output.WarningStyle.Font.GetFont();
                _output.ErrorStyle.Font.GetFont();

                // Generate the output log
                Span<Entry> entries = CollectionsMarshal.AsSpan(_entries);
                var searchQuery = _searchBox.Text;
                for (int i = _textBufferCount; i < _entries.Count; i++)
                {
                    ref var entry = ref entries[i];
                    if (((int)entry.Level & _logTypeShowMask) == 0)
                        continue;
                    if (searchQuery.Length != 0 && entry.Message.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) == -1)
                        continue;

                    var startIndex = _textBuffer.Length;
                    switch (_timestampsFormats)
                    {
                    case InterfaceOptions.TimestampsFormats.Utc:
                        _textBuffer.AppendFormat("[ {0} ]: ", entry.Time.ToUniversalTime());
                        break;
                    case InterfaceOptions.TimestampsFormats.LocalTime:
                        _textBuffer.AppendFormat("[ {0} ]: ", entry.Time);
                        break;
                    case InterfaceOptions.TimestampsFormats.TimeSinceStartup:
                        var diff = entry.Time - _startupTime;
                        _textBuffer.AppendFormat("[ {0:00}:{1:00}:{2:00}.{3:000} ]: ", diff.Hours, diff.Minutes, diff.Seconds, diff.Milliseconds);
                        break;
                    }
                    if (_showLogType)
                    {
                        _textBuffer.AppendFormat("[{0}] ", entry.Level);
                    }

                    var prefixLength = _textBuffer.Length - startIndex;
                    if (entry.Message.IndexOf('\r') != -1)
                        entry.Message = entry.Message.Replace("\r", "");
                    _textBuffer.Append(entry.Message);
                    var endIndex = _textBuffer.Length;
                    _textBuffer.Append('\n');

                    var textBlock = new TextBlock
                    {
                        Range = new TextRange
                        {
                            StartIndex = startIndex,
                            EndIndex = endIndex - 1,
                        },
                    };

                    switch (entry.Level)
                    {
                    case LogType.Info:
                        textBlock.Style = _output.DefaultStyle;
                        break;
                    case LogType.Warning:
                        textBlock.Style = _output.WarningStyle;
                        break;
                    case LogType.Error:
                    case LogType.Fatal:
                        textBlock.Style = _output.ErrorStyle;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                    }
                    var prevBlockBottom = _textBlocks.Count == 0 ? 0.0f : _textBlocks[_textBlocks.Count - 1].Bounds.Bottom;
                    var entryText = _textBuffer.ToString(startIndex, endIndex - startIndex);
                    var font = textBlock.Style.Font.GetFont();
                    if (!font)
                        continue;
                    var style = textBlock.Style;
                    var lines = font.ProcessText(entryText);
                    for (int j = 0; j < lines.Length; j++)
                    {
                        ref var line = ref lines[j];
                        textBlock.Range.StartIndex = startIndex + line.FirstCharIndex;
                        textBlock.Range.EndIndex = startIndex + line.LastCharIndex + 1;
                        textBlock.Bounds = new Rectangle(new Float2(0.0f, prevBlockBottom), line.Size);

                        if (textBlock.Range.Length > 0)
                        {
                            // Parse compilation error/warning
                            var regexStart = line.FirstCharIndex;
                            if (j == 0)
                                regexStart += prefixLength;
                            var regexLength = line.LastCharIndex + 1 - regexStart;
                            if (regexLength > 0)
                            {
                                var match = _compileRegex.Match(entryText, regexStart, regexLength);
                                if (match.Success)
                                {
                                    switch (match.Groups["level"].Value)
                                    {
                                    case "error":
                                        textBlock.Style = _output.ErrorStyle;
                                        break;
                                    case "warning":
                                        textBlock.Style = _output.WarningStyle;
                                        break;
                                    }
                                    textBlock.Tag = new TextBlockTag
                                    {
                                        Type = TextBlockTag.Types.CodeLocation,
                                        Url = match.Groups["path"].Value,
                                        Line = int.Parse(match.Groups["line"].Value),
                                    };
                                }
                                // TODO: parsing hyperlinks with link
                                // TODO: parsing file paths with link
                            }
                        }

                        prevBlockBottom += line.Size.Y;
                        _textBlocks.Add(textBlock);
                        textBlock.Style = style;
                    }
                }

                // Update the output
                var cachedScrollValue = _vScroll.Value;
                var cachedSelection = _output.SelectionRange;
                var cachedOutputTargetViewOffset = _output.TargetViewOffset;
                var isBottomScroll = _vScroll.Value >= _vScroll.Maximum - (_scrollSize * 2) || wasEmpty;
                _output.Text = _textBuffer.ToString();
                if (_hScroll.Maximum <= 0.0)
                    cachedOutputTargetViewOffset.X = 0;
                if (_vScroll.Maximum <= 0.0)
                    cachedOutputTargetViewOffset.Y = 0;
                _output.TargetViewOffset = cachedOutputTargetViewOffset;
                _textBufferCount = _entries.Count;
                if (!_vScroll.IsThumbClicked)
                    _vScroll.TargetValue = isBottomScroll ? _vScroll.Maximum : cachedScrollValue;
                _output.SelectionRange = cachedSelection;
            }

            base.Update(deltaTime);

            FlaxEngine.Profiler.EndEvent();
        }

        /// <inheritdoc />
        public override void OnInit()
        {
            _startupTime = Time.StartupTime;

            // Load debug commands history
            if (Editor.ProjectCache.TryGetCustomData(CommandHistoryKey, out string history))
            {
                try
                {
                    _commandHistory = (List<string>)FlaxEngine.Json.JsonSerializer.Deserialize(history, typeof(List<string>));
                    for (int i = _commandHistory.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(_commandHistory[i]))
                            _commandHistory.RemoveAt(i);
                    }
                }
                catch
                {
                    // Ignore errors
                    _commandHistory = null;
                }
            }
        }

        /// <inheritdoc />
        public override bool UseLayoutData => true;

        /// <inheritdoc />
        public override void OnLayoutSerialize(XmlWriter writer)
        {
            writer.WriteAttributeString("LogTypeShowMask", _logTypeShowMask.ToString());
        }

        /// <inheritdoc />
        public override void OnLayoutDeserialize(XmlElement node)
        {
            if (int.TryParse(node.GetAttribute("LogTypeShowMask"), out int value1))
                _logTypeShowMask = value1;
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            if (IsDisposing)
                return;

            // Unbind events
            Editor.Options.OptionsChanged -= OnEditorOptionsChanged;
            GameCooker.Event -= OnGameCookerEvent;
            ScriptsBuilder.CompilationFailed -= OnScriptsCompilationFailed;

            // Cleanup
            _textBuffer.Clear();
            _textBuffer = null;
            _textBlocks.Clear();
            _textBlocks = null;
            _entries.Clear();
            _entries = null;
            _outMessages = null;
            _outLogTypes = null;
            _outLogTimes = null;
            _compileRegex = null;
            _commandHistory = null;

            // Unlink controls
            _viewDropdown = null;
            _searchBox = null;
            _hScroll = null;
            _vScroll = null;
            _output = null;
            _commandLineBox = null;
            _contextMenu = null;

            base.OnDestroy();
        }
    }
}
