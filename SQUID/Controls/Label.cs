﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Squid
{
    /// <summary>
    /// A Label without selection.
    /// Supports multi- and singleline, textwrap and some bbcode tags.
    /// </summary>
    [Toolbox]
    public class Label : Control, ISelectable
    {
        private List<TextLine> Lines = new List<TextLine>();
        private bool IsDirty;
        private Point TextSize;
        private string ActiveHref;
        private Point LastSize;
        private string _text = string.Empty;

        /// <summary>
        /// Delegate LinkClickedEventHandler
        /// </summary>
        /// <param name="href">The href.</param>
        public delegate void LinkClickedEventHandler(string href);

        public delegate Control RequestControlHandler(string data);

        /// <summary>
        /// Raised when [link clicked].
        /// </summary>
        public event LinkClickedEventHandler LinkClicked;

        public event RequestControlHandler ControlRequest;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Label"/> is selected.
        /// </summary>
        /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the leading.
        /// </summary>
        /// <value>The leading.</value>
        [DefaultValue(0)]
        public int Leading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [text wrap].
        /// </summary>
        /// <value><c>true</c> if [text wrap]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool TextWrap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [BB code enabled].
        /// </summary>
        /// <value><c>true</c> if [BB code enabled]; otherwise, <c>false</c>.</value>
        [DefaultValue(true)]
        public bool BBCodeEnabled { get; set; }

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        /// <value>The color of the text.</value>
        [IntColor, DefaultValue(-1)]
        public int TextColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the link.
        /// </summary>
        /// <value>The color of the link.</value>
        [IntColor, DefaultValue(-1)]
        public int LinkColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use text color].
        /// </summary>
        /// <value><c>true</c> if [use text color]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool UseTextColor { get; set; }

        /// <summary>
        /// Gets or sets the text align.
        /// </summary>
        /// <value>The text align.</value>
        public Alignment TextAlign { get; set; }

        /// <summary>
        /// Gets or sets the text padding.
        /// </summary>
        /// <value>The text padding.</value>
        public Margin TextPadding { get; set; }

        /// <summary>
        /// Get or sets the ellipsis
        /// </summary>
        public bool AutoEllipsis { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Multiline]
        public string Text
        {
            get { return _text; }
            set
            {
                if (value == _originalText) return;

                _originalText = value;
                _text = value;

                if (UseTranslation)
                    _text = TranslateText(_originalText);
                else
                    _text = value;

                IsDirty = true;
            }
        }

        private string _originalText;

        protected override void TranslationChanged(bool from, bool to)
        {
            base.TranslationChanged(from, to);

            if (string.IsNullOrEmpty(_originalText)) return;

            if (from == false && to == true)
                _text = TranslateText(_originalText);
            else if (from == true && to == false)
                _text = _originalText;

            IsDirty = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Label"/> class.
        /// </summary>
        public Label()
        {
            AutoEllipsis = true;
            TextAlign = Alignment.Inherit;
            Style = "label";
            LinkColor = ColorInt.ARGB(.25f, 1f, 1f, 1f);
            TextColor = -1;
            BBCodeEnabled = false;
            MouseClick += Label_MouseClick;
        }

        private void Label_MouseClick(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            if (LinkClicked != null && ActiveHref != null)
                LinkClicked(ActiveHref);
        }

        private Dictionary<string, bool> activeLibrary = new Dictionary<string, bool>();
        private Dictionary<string, Control> library = new Dictionary<string, Control>();

        private void UpdateText(Style style)
        {
            activeLibrary.Clear();
            Lines.Clear();

            TextElement def = new TextElement();
            def.Font = style.Font;

            List<TextElement> elements = BBCode.Parse(_text, style, BBCodeEnabled);
            List<TextElement> textElements = new List<TextElement>();

            Point pos = new Point();
            Point tsize = new Point();
            int advx = 0;

            int lineHeight = 0;
            List<TextElement> thisLine = new List<TextElement>();

            TextSize = Point.Zero;

            if (TextWrap)
            {
                #region TextWrap = true

                bool firstInLine = true;

                foreach (TextElement element in elements)
                {
                    int font = UI.Renderer.GetFont(element.Font);

                    if (element.Linebreak)
                    {
                        if (firstInLine)
                            lineHeight = UI.Renderer.GetTextSize(" ", font, 0).y;

                        pos.x = 0;
                        pos.y += lineHeight + Leading;

                        foreach (TextElement el in thisLine)
                            el.Position.y += lineHeight - el.Size.y;

                        thisLine.Clear();
                        lineHeight = 0;

                        textElements.Add(element);
                        firstInLine = true;
                    }
                    else if (element.IsControl)
                    {
                        Control ctrl = null;

                        if (library.ContainsKey(element.Control))
                        {
                            ctrl = library[element.Control];
                        }
                        else if (ControlRequest != null)
                        {
                            ctrl = ControlRequest(element.Control);
                            library.Add(element.Control, ctrl);
                        }

                        if (ctrl != null)
                        {
                            activeLibrary.Add(element.Control, true);

                            ctrl.Position = pos;

                            if (ctrl.Parent == null)
                                Childs.Add(ctrl);

                            element.Size = ctrl.Size;
                            tsize = ctrl.Size;

                            if (pos.x + tsize.x < Size.x - (style.TextPadding.Left + style.TextPadding.Right))
                            {
                                element.Position = pos;
                                pos.x += ctrl.Size.x;
                                lineHeight = Math.Max(lineHeight, tsize.y);
                            }
                            else
                            {
                                pos.x = 0;
                                pos.y += lineHeight + Leading;

                                foreach (TextElement el in thisLine)
                                    el.Position.y += lineHeight - el.Size.y;

                                thisLine.Clear();

                                textElements.Add(new TextElement { Linebreak = true });

                                firstInLine = true;

                                element.Position = pos;
                                pos.x += ctrl.Size.x;
                                lineHeight = tsize.y;
                            }

                            textElements.Add(element);
                            //thisLine.Add(element);
                        }
                    }
                    else
                    {
                        #region wrap

                        string[] words = System.Text.RegularExpressions.Regex.Split(element.Text, @"(?=(?<=[^\s])\s+)");

                        List<TextElement> sub = new List<TextElement>();

                        TextElement e = new TextElement(element);
                        e.Text = string.Empty;
                        e.Position = pos;

                        int c = 0;
                        bool isBreak = false;

                        foreach (string word in words)
                        {
                            if (word.Length == 0) continue;

                            string temp = word;

                            // if this is the first word in a new line
                            // remove leading whitespaces
                            if (firstInLine) temp = word.TrimStart();

                            tsize = UI.Renderer.GetTextSize(e.Text + temp, font, 0);
                            lineHeight = Math.Max(lineHeight, tsize.y);

                            if (pos.x + tsize.x < Size.x - (style.TextPadding.Left + style.TextPadding.Right))
                            {
                                // the word fits, add to current element
                                e.Text += temp;
                                e.Size = tsize;
                                firstInLine = false;
                            }
                            else
                            {
                                #region new

                                if (firstInLine)
                                {
                                    // the word fits, add to current element
                                    e.Text += temp;
                                    e.Size = tsize;
                                    firstInLine = false;
                                }
                                else
                                {
                                    // word does not fit, add the current element
                                    thisLine.Add(e);
                                    sub.Add(e);
                                    textElements.AddRange(sub);

                                    foreach (TextElement el in thisLine)
                                        el.Position.y += lineHeight - el.Size.y;

                                    sub.Clear();
                                    thisLine.Clear();

                                    // reset line pos
                                    pos.x = 0;
                                    pos.y += lineHeight + Leading;

                                    lineHeight = 0;

                                    // add a break
                                    TextElement linebreak = new TextElement(e);
                                    linebreak.Linebreak = true;
                                    sub.Add(linebreak);

                                    // create new starting element
                                    e = new TextElement(element);
                                    e.Text = temp.TrimStart();
                                    e.Position = pos;
                                    e.Size = UI.Renderer.GetTextSize(e.Text, font, 0);
                                    lineHeight = Math.Max(lineHeight, e.Size.y);
                                    firstInLine = false;
                                }

                                #endregion new

                                #region old

                                //if (c > 0)
                                //{
                                //    isBreak = true;

                                //    // the word does not fit
                                //    pos.x = 0;
                                //    pos.y += lineHeight + Leading;
                                //}

                                //// is more than one word in this line exceeding the break limit?
                                //if (c > 0)
                                //{
                                //    // if so, we need to first add the current element,
                                //    thisLine.Add(e);
                                //    sub.Add(e);

                                //    foreach (TextElement el in thisLine)
                                //    {
                                //        el.Position.y += lineHeight - el.Size.y;
                                //        // el.Size.y += lineHeight - el.Size.y;
                                //    }

                                //    thisLine.Clear();
                                //    lineHeight = 0;

                                //    TextElement linebreak = new TextElement(e);
                                //    linebreak.Linebreak = true;
                                //    sub.Add(linebreak);

                                //    e = new TextElement(element);
                                //    e.Text = temp.TrimStart();
                                //    e.Position = pos;
                                //    e.Size = Gui.Renderer.GetTextSize(e.Text, font);

                                //    lineHeight = Math.Max(lineHeight, e.Size.y);

                                //    if (c <= words.Length)
                                //        isBreak = false;
                                //}
                                //else
                                //{
                                //    // if not, we just add the current word
                                //    e.Position = pos;
                                //    e.Text = temp;
                                //    e.Size = Gui.Renderer.GetTextSize(e.Text, font);
                                //    //sub.Add(e);
                                //}

                                #endregion old

                                //pos.x += tsize.x;
                            }

                            c++;
                        }

                        if (!isBreak)
                        {
                            //e.Size = Gui.Renderer.GetTextSize(e.Text, font);
                            pos.x = pos.x + e.Size.x;
                            lineHeight = Math.Max(lineHeight, e.Size.y);
                            sub.Add(e);
                        }

                        thisLine.AddRange(sub);
                        textElements.AddRange(sub);

                        #endregion wrap
                    }
                }

                foreach (TextElement el in thisLine)
                    el.Position.y += lineHeight - el.Size.y;

                #endregion TextWrap = true
            }
            else
            {
                #region TextWrap = false

                bool firstInLine = true;
                bool singleLine = true;

                foreach (TextElement element in elements)
                {
                    int font = UI.Renderer.GetFont(element.Font);

                    if (element.Linebreak)
                    {
                        if (firstInLine)
                            lineHeight = UI.Renderer.GetTextSize(" ", font, 0).y;

                        pos.x = 0;
                        pos.y += lineHeight + Leading;

                        foreach (TextElement el in thisLine)
                        {
                            el.Position.y += lineHeight - el.Size.y;
                            // el.Size.y += lineHeight - el.Size.y;
                        }

                        thisLine.Clear();
                        lineHeight = 0;

                        element.Position = pos;
                        textElements.Add(element);
                        firstInLine = true;
                        singleLine = false;
                    }
                    else if (element.IsControl)
                    {
                        Control ctrl = null;

                        if (library.ContainsKey(element.Control))
                        {
                            ctrl = library[element.Control];
                        }
                        else if (ControlRequest != null)
                        {
                            ctrl = ControlRequest(element.Control);
                            library.Add(element.Control, ctrl);
                        }

                        if (ctrl != null)
                        {
                            ctrl.Position = pos;

                            if (ctrl.Parent == null)
                                Childs.Add(ctrl);

                            activeLibrary.Add(element.Control, true);

                            element.Position = pos;
                            element.Size = ctrl.Size;

                            pos.x += ctrl.Size.x;

                            tsize = element.Size;
                            lineHeight = Math.Max(lineHeight, tsize.y);
                            textElements.Add(element);
                            //thisLine.Add(element);
                        }
                    }
                    else
                    {
                        if (firstInLine)
                        {
                            element.Text = element.Text.TrimStart();
                            firstInLine = false;
                        }

                        tsize = UI.Renderer.GetTextSize(string.IsNullOrEmpty(element.Text) ? " " : element.Text, font, 0);
                        lineHeight = Math.Max(lineHeight, tsize.y);

                        element.Position = pos;
                        element.Size = tsize;

                        textElements.Add(element);

                        pos.x += tsize.x;

                        thisLine.Add(element);
                    }
                }

                foreach (TextElement el in thisLine)
                    el.Position.y += lineHeight - el.Size.y;

                #endregion TextWrap = false

                #region AutoEllipsis (...)

                if (singleLine && AutoEllipsis && (AutoSize == Squid.AutoSize.None || AutoSize == AutoSize.Vertical))
                {
                    int removeAt = -1;
                    int width = 0;
                    int limit = Size.x - style.TextPadding.Left - style.TextPadding.Right;

                    Alignment align = TextAlign != Alignment.Inherit ? TextAlign : style.TextAlign;

                    if (align == Alignment.TopLeft || align == Alignment.MiddleLeft || align == Alignment.BottomLeft)
                    {
                        for (int i = 0; i < textElements.Count; i++)
                        {
                            int font = UI.Renderer.GetFont(textElements[i].Font);
                            int ellipsis = UI.Renderer.GetTextSize("...", font, 0).x;

                            if (width + textElements[i].Size.x + ellipsis <= limit)
                            {
                                width += textElements[i].Size.x;
                                continue;
                            }
                            else
                            {
                                string text = string.Empty;
                                string final = string.Empty;
                                removeAt = i + 1;

                                foreach (char c in textElements[i].Text)
                                {
                                    final = text + c + "...";

                                    int w = UI.Renderer.GetTextSize(final, font, 0).x;

                                    if (width + w >= limit)
                                    {
                                        textElements[i].Text = text + "...";
                                        break;
                                    }
                                    else
                                    {
                                        text += c;
                                    }
                                }

                                break;
                            }
                        }

                        if (removeAt > -1)
                            textElements.RemoveRange(removeAt, textElements.Count - removeAt);
                    }
                    else if (align == Alignment.TopRight || align == Alignment.MiddleRight || align == Alignment.BottomRight)
                    {
                        for (int i = textElements.Count - 1; i >= 0; i--)
                        {
                            int font = UI.Renderer.GetFont(textElements[i].Font);
                            int ellipsis = UI.Renderer.GetTextSize("...", font, 0).x;
                            int fullsize = textElements[i].Size.x;
                            Point oldpos = textElements[i].Position;

                            if (width + textElements[i].Size.x + ellipsis <= limit)
                            {
                                width += textElements[i].Size.x;
                            }
                            else
                            {
                                string inc = string.Empty;
                                string final = string.Empty;
                                string text = textElements[i].Text;
                                removeAt = i;

                                for (int j = text.Length; j >= 0; j--)
                                {
                                    char c = new char();

                                    if (j < text.Length)
                                    {
                                        c = text[j];
                                        final = "..." + c + inc;
                                        inc = c + inc;
                                    }
                                    else
                                    {
                                        final = "..." + inc;
                                    }

                                    if (j == 0 && removeAt == 0)
                                        final = inc;

                                    int w = UI.Renderer.GetTextSize(final, font, 0).x;

                                    Point position = oldpos;
                                    position.x = oldpos.x + (fullsize - w);
                                    textElements[i].Text = final;
                                    textElements[i].Position = position;

                                    if (width + w > limit && j > 0)
                                        break;
                                }

                                break;
                            }
                        }

                        if (removeAt > 0)
                        {
                            int ww = 0;
                            for (int i = 0; i < removeAt; i++)
                                ww += textElements[i].Size.x;

                            textElements.RemoveRange(0, removeAt);

                            for (int i = 0; i < textElements.Count; i++)
                            {
                                Point position = textElements[i].Position;
                                position.x -= ww;
                                textElements[i].Position = position;
                            }
                        }
                    }
                }

                #endregion AutoEllipsis (...)
            }

            TextLine line = new TextLine();

            foreach (TextElement element in textElements)
            {
                line.Width += element.Size.x;
                line.Elements.Add(element);

                TextSize.x = Math.Max(TextSize.x, line.Width);
                TextSize.y = Math.Max(TextSize.y, element.Position.y + element.Size.y);

                if (element.Linebreak)
                {
                    Lines.Add(line);
                    line = new TextLine();
                }
            }

            TextSize += new Point(style.TextPadding.Left + style.TextPadding.Right, style.TextPadding.Top + style.TextPadding.Bottom);

            Lines.Add(line);

            LastSize = Size;
            IsDirty = false;

            FinalizeTextLayout(style);

            foreach (KeyValuePair<string, Control> pair in library)
            {
                if (!activeLibrary.ContainsKey(pair.Key))
                    Childs.Remove(pair.Value);
            }
        }

        protected override void OnStateChanged()
        {
            IsDirty = true;
        }

        protected override void OnAutoSize()
        {
            if (IsDirty)
            {
                Style style = Canvas.GetStyle(Style).Styles[State];
                UpdateText(style);
            }

            if (AutoSize == Squid.AutoSize.Vertical)
                Size = new Point(Size.x, TextSize.y);
            else if (AutoSize == Squid.AutoSize.Horizontal)
                Size = new Point(TextSize.x, Size.y);
            else if (AutoSize == Squid.AutoSize.HorizontalVertical)
                Size = TextSize;
        }

        protected override void OnLateUpdate()
        {
            if (!IsDirty)
                IsDirty = LastSize.x != Size.x || LastSize.y != Size.y;

            if (IsDirty)
            {
                Style style = Canvas.GetStyle(Style).Styles[State];
                UpdateText(style);
            }

            if (Desktop.HotControl == this)
            {
                Point m = UI.MousePosition;
                ActiveHref = null;

                foreach (TextLine line in Lines)
                {
                    foreach (TextElement element in line.Elements)
                    {
                        if (!element.IsLink) continue;

                        if (element.Rectangle.Contains(m))
                        {
                            Desktop.CurrentCursor = CursorNames.Link;
                            ActiveHref = element.Href;
                            break;
                        }
                    }
                }
            }
        }

        private void FinalizeTextLayout(Style style)
        {
            if (Lines.Count == 0) return;

            int font;
            Point p1, p2, size;

            Alignment align = TextAlign != Alignment.Inherit ? TextAlign : style.TextAlign;

            foreach (TextLine line in Lines)
            {
                foreach (TextElement element in line.Elements)
                {
                    if (element.Linebreak) continue;

                    font = UI.Renderer.GetFont(element.Font);

                    size = element.Size;
                    p1 = Point.Zero;

                    if (align == Alignment.TopLeft || align == Alignment.TopCenter || align == Alignment.TopRight)
                        p1.y += style.TextPadding.Top;

                    if (align == Alignment.BottomLeft || align == Alignment.BottomCenter || align == Alignment.BottomRight)
                        p1.y += Size.y - TextSize.y;

                    if (align == Alignment.MiddleLeft || align == Alignment.MiddleCenter || align == Alignment.MiddleRight)
                        p1.y += (Size.y - (TextSize.y - (style.TextPadding.Top + style.TextPadding.Bottom))) / 2;

                    if (align == Alignment.TopLeft || align == Alignment.MiddleLeft || align == Alignment.BottomLeft)
                        p1.x += style.TextPadding.Left;

                    if (align == Alignment.TopRight || align == Alignment.MiddleRight || align == Alignment.BottomRight)
                        p1.x += Size.x - line.Width - style.TextPadding.Right;

                    if (align == Alignment.TopCenter || align == Alignment.MiddleCenter || align == Alignment.BottomCenter)
                        p1.x += (Size.x - line.Width) / 2;

                    p2 = element.Position + p1;

                    element.Rectangle = new Rectangle(p2, size);
                    element.Position = p2;

                    if (element.IsControl)
                    {
                        element.Position = p2;

                        if (library.ContainsKey(element.Control))
                            library[element.Control].Position = p2;
                    }
                }
            }
        }

        protected override void DrawText(Style style, float opacity, int charsToDraw)
        {
            if (IsDirty)
            {
                UpdateText(style);
            }

            if (Lines.Count == 0)
                return;

            int font;
            Point p1, p2, size;

            foreach (TextLine line in Lines)
            {
                foreach (TextElement element in line.Elements)
                {
                    if (element.Linebreak) continue;

                    font = UI.Renderer.GetFont(element.Font);
                    size = element.Size;
                    p2 = element.Position + Location;

                    element.Rectangle = new Rectangle(p2, size);

                    if (!element.Rectangle.Intersects(ClipRect))
                        continue;

                    if (element.IsControl)
                        continue;

                    if (element.IsLink)
                        UI.Renderer.DrawBox(p2.x, p2.y + size.y, size.x - 1, 1, ColorInt.FromArgb(opacity, ColorInt.FromArgb(opacity, element.Color.HasValue ? (int)element.Color : style.TextColor)));

                    //if (element.IsLink && element.Href == ActiveHref)
                    //  Gui.Renderer.DrawBox(p2.x, p2.y, size.x - 1, size.y, ColorInt.FromArgb(opacity, LinkColor));

                    if (UseTextColor)
                        UI.Renderer.DrawText(element.Text, p2.x, p2.y, size.x, size.y, font, ColorInt.FromArgb(opacity, TextColor), 0);
                    else
                        UI.Renderer.DrawText(element.Text, p2.x, p2.y, size.x, size.y, font, ColorInt.FromArgb(opacity, element.Color.HasValue ? (int)element.Color : style.TextColor), 0);
                }
            }
        }
    }
}