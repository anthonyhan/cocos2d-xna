using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace Cocos2D
{
    public class CCLabelBMFont : CCSpriteBatchNode, ICCLabelProtocol, ICCRGBAProtocol
    {
        public const int kCCLabelAutomaticWidth = -1;

        public static Dictionary<string, CCBMFontConfiguration> s_pConfigurations = new Dictionary<string, CCBMFontConfiguration>();

        protected bool m_bLineBreakWithoutSpaces;
        protected CCTextAlignment m_pHAlignment = CCTextAlignment.Center;
        protected CCVerticalTextAlignment m_pVAlignment = CCVerticalTextAlignment.Top;
        protected CCBMFontConfiguration m_pConfiguration;
        protected string m_sFntFile;
        protected string m_sInitialString;
        protected string m_sString = "";
        protected CCPoint m_tImageOffset;
        protected CCSize m_tDimensions;
        protected CCSprite m_pReusedChar;
        protected bool m_bLabelDirty;

        private bool richTextEnabled = false;
        protected List<RichTextSegmentColor> richTextSegmentColors = new List<RichTextSegmentColor>();

        protected class RichTextSegmentColor
        {
            public RichTextSegmentColor()
            {
                BeginIndex = -1;
                EndIndex = -1;
                Color = CCColor3B.White;
            }

            public int BeginIndex;
            public int EndIndex;
            public  CCColor3B Color;
        }

        public override CCPoint AnchorPoint
        {
            get { return base.AnchorPoint; }
            set
            {
                if (!m_obAnchorPoint.Equals(value))
                {
                    base.AnchorPoint = value;
                    m_bLabelDirty = true;
                }
            }
        }

        public override float Scale
        {
            get { return base.Scale; }
            set
            {
                base.Scale = value;
                m_bLabelDirty = true;
            }
        }

        public override float ScaleX
        {
            get { return base.ScaleX; }
            set
            {
                base.ScaleX = value;
                UpdateLabel();
            }
        }

        public override float ScaleY
        {
            get { return base.ScaleY; }
            set
            {
                base.ScaleY = value;
                m_bLabelDirty = true;
            }
        }

        public CCTextAlignment HorizontalAlignment
        {
            get { return m_pHAlignment; }
            set
            {
                if (m_pHAlignment != value)
                {
                    m_pHAlignment = value;
                    m_bLabelDirty = true;
                }
            }
        }

        public CCVerticalTextAlignment VerticalAlignment
        {
            get { return m_pVAlignment; }
            set
            {
                if (m_pVAlignment != value)
                {
                    m_pVAlignment = value;
                    m_bLabelDirty = true;
                }
            }
        }

        public CCSize Dimensions
        {
            get { return m_tDimensions; }
            set
            {
                if (m_tDimensions != value)
                {
                    m_tDimensions = value;
                    m_bLabelDirty = true;
                }
            }
        }

        public bool LineBreakWithoutSpace
        {
            get { return m_bLineBreakWithoutSpaces; }
            set
            {
                m_bLineBreakWithoutSpaces = value;
                m_bLabelDirty = true;
            }
        }

        public string FntFile
        {
            get { return m_sFntFile; }
            set
            {
                if (value != null && m_sFntFile != value)
                {
                    CCBMFontConfiguration newConf = FNTConfigLoadFile(value);

                    Debug.Assert(newConf != null, "CCLabelBMFont: Impossible to create font. Please check file");

                    m_sFntFile = value;

                    m_pConfiguration = newConf;

                    Texture = CCTextureCache.SharedTextureCache.AddImage(m_pConfiguration.AtlasName);

                    m_bLabelDirty = true;
                }
            }
        }

        #region ICCLabelProtocol Members

        public virtual string Text
        {
            get { return m_sInitialString; }
            set
            {
                if (m_sInitialString != value)
                {
                    m_sInitialString = value;
                    m_bLabelDirty = true;
                }
            }
        }

        [Obsolete("Use Label Property")]
        public void SetString(string label)
        {
            Text = label;
        }

        [Obsolete("Use Label Property")]
        public string GetString()
        {
            return Text;
        }

        #endregion

        #region ICCRGBAProtocol Members

        protected byte m_cDisplayedOpacity = 255;
        protected byte m_cRealOpacity = 255;
        protected CCColor3B m_tDisplayedColor = CCTypes.CCWhite;
        protected CCColor3B m_tRealColor = CCTypes.CCWhite;
        protected bool m_bCascadeColorEnabled = true;
        protected bool m_bCascadeOpacityEnabled = true;
        protected bool m_bIsOpacityModifyRGB = false;

        public virtual CCColor3B Color
        {
            get { return m_tRealColor; }
            set
            {
                m_tDisplayedColor = m_tRealColor = value;

                if (m_bCascadeColorEnabled)
                {
                    var parentColor = CCTypes.CCWhite;
                    var parent = m_pParent as ICCRGBAProtocol;
                    if (parent != null && parent.CascadeColorEnabled)
                    {
                        parentColor = parent.DisplayedColor;
                    }

                    UpdateDisplayedColor(parentColor);
                }
            }
        }

        public virtual CCColor3B DisplayedColor
        {
            get { return m_tDisplayedColor; }
        }

        public virtual byte Opacity
        {
            get { return m_cRealOpacity; }
            set
            {
                m_cDisplayedOpacity = m_cRealOpacity = value;

                if (m_bCascadeOpacityEnabled)
                {
                    byte parentOpacity = 255;
                    var pParent = m_pParent as ICCRGBAProtocol;
                    if (pParent != null && pParent.CascadeOpacityEnabled)
                    {
                        parentOpacity = pParent.DisplayedOpacity;
                    }
                    UpdateDisplayedOpacity(parentOpacity);
                }
            }
        }

        public virtual byte DisplayedOpacity
        {
            get { return m_cDisplayedOpacity; }
        }

        public virtual bool IsOpacityModifyRGB
        {
            get { return m_bIsOpacityModifyRGB; }
            set
            {
                m_bIsOpacityModifyRGB = value;
                if (m_pChildren != null && m_pChildren.count > 0)
                {
                    for (int i = 0, count = m_pChildren.count; i < count; i++)
                    {
                        var item = m_pChildren.Elements[i] as ICCRGBAProtocol;
                        if (item != null)
                        {
                            item.IsOpacityModifyRGB = value;
                        }
                    }
                }
            }
        }

        public virtual bool CascadeColorEnabled
        {
            get { return false; }
            set { m_bCascadeColorEnabled = value; }
        }

        public virtual bool CascadeOpacityEnabled
        {
            get { return false; }
            set { m_bCascadeOpacityEnabled = value; }
        }

        public bool RichTextEnabled
        {
            get => richTextEnabled; set
            {
                richTextEnabled = value;
                m_bLabelDirty = true;
            }
        }

        public virtual void UpdateDisplayedColor(CCColor3B parentColor)
        {
            m_tDisplayedColor.R = (byte)(m_tRealColor.R * parentColor.R / 255.0f);
            m_tDisplayedColor.G = (byte)(m_tRealColor.G * parentColor.G / 255.0f);
            m_tDisplayedColor.B = (byte)(m_tRealColor.B * parentColor.B / 255.0f);

            if (m_pChildren != null)
            {
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    ((CCSprite)m_pChildren.Elements[i]).UpdateDisplayedColor(m_tDisplayedColor);
                }
            }
        }

        public virtual void UpdateDisplayedOpacity(byte parentOpacity)
        {
            m_cDisplayedOpacity = (byte)(m_cRealOpacity * parentOpacity / 255.0f);

            if (m_pChildren != null)
            {
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    ((CCSprite)m_pChildren.Elements[i]).UpdateDisplayedOpacity(m_cDisplayedOpacity);
                }
            }
        }

        #endregion

        public static void FNTConfigRemoveCache()
        {
            if (s_pConfigurations != null)
            {
                s_pConfigurations.Clear();
            }
        }

        public static void PurgeCachedData()
        {
            FNTConfigRemoveCache();
        }

        public CCLabelBMFont()
        {
            Init();
        }

        public CCLabelBMFont(string str, string fntFile, float width)
            : this(str, fntFile, width, CCTextAlignment.Left, CCPoint.Zero)
        {
        }

        public CCLabelBMFont(string str, string fntFile)
            : this(str, fntFile, kCCLabelAutomaticWidth, CCTextAlignment.Left, CCPoint.Zero)
        {
        }

        public CCLabelBMFont(string str, string fntFile, float width, CCTextAlignment alignment)
            : this(str, fntFile, width, alignment, CCPoint.Zero)
        {
        }

        public CCLabelBMFont(string str, string fntFile, float width, CCTextAlignment alignment, CCPoint imageOffset)
        {
            InitWithString(str, fntFile, new CCSize(width, 0), alignment, CCVerticalTextAlignment.Top, imageOffset, null);
        }

        public override bool Init()
        {
            return InitWithString(null, null, new CCSize(kCCLabelAutomaticWidth, 0), CCTextAlignment.Left, CCVerticalTextAlignment.Top, CCPoint.Zero, null);
        }

        protected virtual bool InitWithString(string theString, string fntFile, CCSize dimentions, CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment,
                                              CCPoint imageOffset, CCTexture2D texture)
        {
            Debug.Assert(m_pConfiguration == null, "re-init is no longer supported");
            Debug.Assert((theString == null && fntFile == null) || (theString != null && fntFile != null),
                         "Invalid params for CCLabelBMFont");

            if (!String.IsNullOrEmpty(fntFile))
            {
                CCBMFontConfiguration newConf = FNTConfigLoadFile(fntFile);
                if (newConf == null)
                {
                    CCLog.Log("CCLabelBMFont: Impossible to create font. Please check file: '{0}'", fntFile);
                    return false;
                }

                m_pConfiguration = newConf;

                m_sFntFile = fntFile;

                if (texture == null)
                {
                    try
                    {
                        texture = CCTextureCache.SharedTextureCache.AddImage(m_pConfiguration.AtlasName);
                    }
                    catch (Exception)
                    {
                        // Try the 'images' ref location just in case.
                        try
                        {
                            texture =
                                CCTextureCache.SharedTextureCache.AddImage(System.IO.Path.Combine("images",
                                                                                                  m_pConfiguration
                                                                                                      .AtlasName));
                        }
                        catch (Exception)
                        {
                            // Lastly, try <font_path>/images/<font_name>
                            string dir = System.IO.Path.GetDirectoryName(m_pConfiguration.AtlasName);
                            string fname = System.IO.Path.GetFileName(m_pConfiguration.AtlasName);
                            string newName = System.IO.Path.Combine(System.IO.Path.Combine(dir, "images"), fname);
                            texture = CCTextureCache.SharedTextureCache.AddImage(newName);
                        }
                    }
                }
            }
            else
            {
                texture = new CCTexture2D();
            }

            if (String.IsNullOrEmpty(theString))
            {
                theString = String.Empty;
            }

            if (base.InitWithTexture(texture, theString.Length))
            {
                m_tDimensions = dimentions;

                m_pHAlignment = hAlignment;
                m_pVAlignment = vAlignment;

                m_cDisplayedOpacity = m_cRealOpacity = 255;
                m_tDisplayedColor = m_tRealColor = CCTypes.CCWhite;
                m_bCascadeOpacityEnabled = true;
                m_bCascadeColorEnabled = true;

                m_obContentSize = CCSize.Zero;

                m_bIsOpacityModifyRGB = m_pobTextureAtlas.Texture.HasPremultipliedAlpha;
                AnchorPoint = new CCPoint(0.5f, 0.5f);

                m_tImageOffset = imageOffset;

                m_pReusedChar = new CCSprite();
                m_pReusedChar.InitWithTexture(m_pobTextureAtlas.Texture, CCRect.Zero, false);
                m_pReusedChar.BatchNode = this;

                SetString(theString, true);

                return true;
            }
            return false;
        }

        private int KerningAmountForFirst(int first, int second)
        {
            int ret = 0;
            int key = (first << 16) | (second & 0xffff);

            if (m_pConfiguration.m_pKerningDictionary != null)
            {
                CCBMFontConfiguration.CCKerningHashElement element;
                if (m_pConfiguration.m_pKerningDictionary.TryGetValue(key, out element))
                {
                    ret = element.amount;
                }
            }
            return ret;
        }

        public void CreateFontChars()
        {
            int nextFontPositionX = 0;
            int nextFontPositionY = 0;
            char prev = (char)255;
            int kerningAmount = 0;

            CCSize tmpSize = CCSize.Zero;

            int longestLine = 0;
            int totalHeight = 0;

            int quantityOfLines = 1;

            if (String.IsNullOrEmpty(m_sString))
            {
                return;
            }

            int stringLen = m_sString.Length;

            var charSet = m_pConfiguration.CharacterSet;
            if (charSet.Count == 0)
            {
                throw (new InvalidOperationException(
                    "Can not compute the size of the font because the character set is empty."));
            }

            for (int i = 0; i < stringLen; ++i)
            {
                if (m_sString[i] == '\n')
                {
                    quantityOfLines++;
                }
            }

            totalHeight = m_pConfiguration.m_nCommonHeight * quantityOfLines;
            nextFontPositionY = m_pConfiguration.m_nCommonHeight * (quantityOfLines - 1);

            CCBMFontConfiguration.CCBMFontDef fontDef = null;
            CCRect rect;

            for (int i = 0; i < stringLen; i++)
            {
                char c = m_sString[i];

                if (c == '\n')
                {
                    nextFontPositionX = 0;
                    nextFontPositionY -= m_pConfiguration.m_nCommonHeight;
                    continue;
                }

                if (charSet.IndexOf(c) == -1)
                {
                    CCLog.Log("Cocos2D.CCLabelBMFont: Attempted to use character not defined in this bitmap: {0}",
                              (int)c);
                    continue;
                }

                kerningAmount = this.KerningAmountForFirst(prev, c);

                // unichar is a short, and an int is needed on HASH_FIND_INT
                if (!m_pConfiguration.m_pFontDefDictionary.TryGetValue(c, out fontDef))
                {
                    CCLog.Log("cocos2d::CCLabelBMFont: characer not found {0}", (int)c);
                    continue;
                }

                rect = fontDef.rect;
                rect = rect.PixelsToPoints();

                rect.Origin.X += m_tImageOffset.X;
                rect.Origin.Y += m_tImageOffset.Y;

                CCSprite fontChar;

                //bool hasSprite = true;
                fontChar = (CCSprite)(GetChildByTag(i));
                if (fontChar != null)
                {
                    // Reusing previous Sprite
                    fontChar.Visible = true;
                    fontChar.UpdateDisplayedColor(m_tDisplayedColor);
                }
                else
                {
                    // New Sprite ? Set correct color, opacity, etc...
                    //if( false )
                    //{
                    //    /* WIP: Doesn't support many features yet.
                    //     But this code is super fast. It doesn't create any sprite.
                    //     Ideal for big labels.
                    //     */
                    //    fontChar = m_pReusedChar;
                    //    fontChar.BatchNode = null;
                    //    hasSprite = false;
                    //}
                    //else
                    {
                        fontChar = new CCSprite();
                        fontChar.InitWithTexture(m_pobTextureAtlas.Texture, rect);
                        AddChild(fontChar, i, i);
                    }

                    // Apply label properties
                    fontChar.IsOpacityModifyRGB = m_bIsOpacityModifyRGB;

                    // Color MUST be set before opacity, since opacity might change color if OpacityModifyRGB is on
                    fontChar.UpdateDisplayedColor(m_tDisplayedColor);
                    fontChar.UpdateDisplayedOpacity(m_cDisplayedOpacity);
                }

                // updating previous sprite
                fontChar.SetTextureRect(rect, false, rect.Size);

                // See issue 1343. cast( signed short + unsigned integer ) == unsigned integer (sign is lost!)
                int yOffset = m_pConfiguration.m_nCommonHeight - fontDef.yOffset;
                var fontPos =
                    new CCPoint(
                        (float)nextFontPositionX + fontDef.xOffset + fontDef.rect.Size.Width * 0.5f + kerningAmount,
                        (float)nextFontPositionY + yOffset - rect.Size.Height * 0.5f * CCMacros.CCContentScaleFactor());
                fontChar.Position = fontPos.PixelsToPoints();

                // update kerning
                nextFontPositionX += fontDef.xAdvance + kerningAmount;
                prev = c;

                if (longestLine < nextFontPositionX)
                {
                    longestLine = nextFontPositionX;
                }

                //if (! hasSprite)
                //{
                //  UpdateQuadFromSprite(fontChar, i);
                //}
            }

            // If the last character processed has an xAdvance which is less that the width of the characters image, then we need
            // to adjust the width of the string to take this into account, or the character will overlap the end of the bounding
            // box
            if (fontDef.xAdvance < fontDef.rect.Size.Width)
            {
                tmpSize.Width = longestLine + fontDef.rect.Size.Width - fontDef.xAdvance;
            }
            else
            {
                tmpSize.Width = longestLine;
            }
            tmpSize.Height = totalHeight;

            tmpSize = new CCSize(
                m_tDimensions.Width > 0 ? m_tDimensions.Width : tmpSize.Width,
                m_tDimensions.Height > 0 ? m_tDimensions.Height : tmpSize.Height
                );

            ContentSize = tmpSize;
        }

        public virtual void SetString(string newString, bool needUpdateLabel)
        {
            if (!needUpdateLabel)
            {
                m_sString = newString;
            }
            else
            {
                m_sInitialString = newString;
            }

            UpdateString(needUpdateLabel);
        }

        private void UpdateString(bool needUpdateLabel)
        {
            if (m_pChildren != null && m_pChildren.count != 0)
            {
                CCNode[] elements = m_pChildren.Elements;
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    elements[i].Visible = false;
                }
            }

            CreateFontChars();

            if (needUpdateLabel)
            {
                UpdateLabel();
            }
        }

        protected void UpdateLabel()
        {
            SetString(m_sInitialString, false);

            if (m_sString == null)
            {
                return;
            }

            //parsing rich text
            if(RichTextEnabled)
            {
                richTextSegmentColors.Clear();
                string parsed_str = ParseRichTextLabels(m_sString, richTextSegmentColors);
                SetString(parsed_str, false);
            }

            // Step 1: Make multiline
            if (m_tDimensions.Width > 0)
            {
                string multiline_str = BreakLines();
                SetString(multiline_str, false);
            }

            // Step 2: Make alignment
            if (m_pHAlignment != CCTextAlignment.Left)
            {
                int i = 0;

                int lineNumber = 0;
                int str_len = m_sString.Length;
                var last_line = new CCRawList<char>();
                for (int ctr = 0; ctr <= str_len; ++ctr)
                {
                    if (ctr == str_len || m_sString[ctr] == '\n')
                    {
                        float lineWidth = 0.0f;
                        int line_length = last_line.Count;
                        // if last line is empty we must just increase lineNumber and work with next line
                        if (line_length == 0)
                        {
                            lineNumber++;
                            continue;
                        }
                        int index = i + line_length - 1 + lineNumber;
                        if (index < 0) continue;

                        var lastChar = (CCSprite)GetChildByTag(index);
                        if (lastChar == null)
                            continue;

                        lineWidth = lastChar.Position.X + lastChar.ContentSize.Width / 2.0f;

                        float shift = 0;
                        switch (m_pHAlignment)
                        {
                            case CCTextAlignment.Center:
                                shift = ContentSize.Width / 2.0f - lineWidth / 2.0f;
                                break;
                            case CCTextAlignment.Right:
                                shift = ContentSize.Width - lineWidth;
                                break;
                            default:
                                break;
                        }

                        if (shift != 0)
                        {
                            for (int j = 0; j < line_length; j++)
                            {
                                index = i + j + lineNumber;
                                if (index < 0) continue;

                                var characterSprite = (CCSprite)GetChildByTag(index);
                                if (characterSprite != null)
                                    characterSprite.Position = characterSprite.Position + new CCPoint(shift, 0.0f);
                            }
                        }

                        i += line_length;
                        lineNumber++;

                        last_line.Clear();
                        continue;
                    }

                    last_line.Add(m_sString[ctr]);
                }
            }

            if (m_pVAlignment != CCVerticalTextAlignment.Bottom && m_tDimensions.Height > 0)
            {
                int lineNumber = 1;
                int str_len = m_sString.Length;
                for (int ctr = 0; ctr < str_len; ++ctr)
                {
                    if (m_sString[ctr] == '\n')
                    {
                        lineNumber++;
                    }
                }

                float yOffset = 0;

                if (m_pVAlignment == CCVerticalTextAlignment.Center)
                {
                    yOffset = m_tDimensions.Height / 2f - (m_pConfiguration.m_nCommonHeight / CCMacros.CCContentScaleFactor() * lineNumber) / 2f;
                }
                else
                {
                    yOffset = m_tDimensions.Height - m_pConfiguration.m_nCommonHeight * lineNumber / CCMacros.CCContentScaleFactor();
                }

                for (int i = 0; i < str_len; i++)
                {
                    var characterSprite = GetChildByTag(i);
                    if (characterSprite != null)
                        characterSprite.PositionY += yOffset;
                }
            }

            //update richtext 
            if(RichTextEnabled && m_sString.Length>0)
            {
                //color
                int strLength = m_sString.Length;
                for (int i = 0; i < strLength; i++)
                {
                    CCSprite charSprite = (CCSprite)GetChildByTag(i);
                    if (charSprite != null)
                    {
                        foreach(var segmentColor in richTextSegmentColors)
                        {
                            if (i >= segmentColor.BeginIndex && i < segmentColor.EndIndex)
                                charSprite.UpdateDisplayedColor(segmentColor.Color);
                        }
                    }
                }
            }
        }

        private float GetLetterPosXLeft(CCSprite sp)
        {
            return sp.Position.X * m_fScaleX - (sp.ContentSize.Width * m_fScaleX * sp.AnchorPoint.X);
        }

        private float GetLetterPosXRight(CCSprite sp)
        {
            return sp.Position.X * m_fScaleX + (sp.ContentSize.Width * m_fScaleX * sp.AnchorPoint.X);
        }


        public static CCBMFontConfiguration FNTConfigLoadFile(string file)
        {
            CCBMFontConfiguration pRet;

            if (!s_pConfigurations.TryGetValue(file, out pRet))
            {
                pRet = CCBMFontConfiguration.Create(file);
                s_pConfigurations.Add(file, pRet);
            }

            return pRet;
        }

        public static CCBMFontConfiguration FNTConfigLoadFile(string fntName, Stream src)
        {
            CCBMFontConfiguration pRet;

            if (!s_pConfigurations.TryGetValue(fntName, out pRet))
            {
                pRet = CCBMFontConfiguration.Create(src, fntName);
                s_pConfigurations.Add(fntName, pRet);
            }

            return pRet;
        }

        public override void Visit()
        {
            if (m_bLabelDirty)
            {
                UpdateLabel();
                m_bLabelDirty = false;
            }

            base.Visit();

            //CCDrawingPrimitives.Begin();
            //for(int i=0; i<m_pChildren.Count; ++i)
            //{
            //    var sp = m_pChildren[i];
            //    if(sp.Visible)
            //        CCDrawingPrimitives.DrawRect(sp.WorldBoundingBox, CCColor4B.White);
            //}
            //CCDrawingPrimitives.End();
        }

        private string BreakLines()
        {
            var in_string = m_sString;
            var out_string = new StringBuilder(m_sString.Length);

            int pos = 0;

            int numlines = 0;
            int numchars = 0;
            float linewidth = 0f;

            while (pos < in_string.Length && in_string[pos] > 0)
            {
                float charw = 0f;

                CCBMFontConfiguration.CCBMFontDef fontDef;
                if (m_pConfiguration.m_pFontDefDictionary.TryGetValue(in_string[pos], out fontDef))
                {
                    charw = fontDef.xAdvance / CCDirector.SharedDirector.ContentScaleFactor;
                }

                linewidth += charw;
                numchars++;
                if (linewidth > m_tDimensions.Width)
                {
                    for (int i = 0; i < numchars - 1; i++)
                    {
                        if (Char.IsWhiteSpace(in_string[pos]))
                            break;

                        if ((!IsLetter(in_string[pos]) || !IsLetter(in_string[pos - 1])) &&
                            CanNewLineBefore(in_string[pos]) && CanNewLineAfter(in_string[pos - 1]))
                            break;

                        out_string.Remove(out_string.Length - 1, 1);
                        pos--;
                    }

                    if (m_bLineBreakWithoutSpaces)
                    {
                        bool trimed = false;
                        while (Char.IsWhiteSpace(out_string[out_string.Length - 1]))
                        {
                            out_string.Remove(out_string.Length - 1, 1);
                            trimed = true;
                        }

                        while (pos < in_string.Length && Char.IsWhiteSpace(in_string[pos]))
                        {
                            pos++;
                            trimed = true;
                        }

                        if (richTextEnabled && trimed)
                            UpdateRichTextSegmentIndices(pos, out_string.Length - pos);
                    }

                    out_string.Append('\n');

                    //update richtext indices
                    if (richTextEnabled)
                        UpdateRichTextSegmentIndices(out_string.Length, 1);

                    linewidth = 0.0f;
                    numchars = 0;
                    numlines++;
                }
                else
                {
                    if (in_string[pos] == '\n')
                    {
                        out_string.Append(in_string[pos++]);

                        if (m_bLineBreakWithoutSpaces)
                        {
                            bool trimed = true;

                            while (pos < in_string.Length && Char.IsWhiteSpace(in_string[pos]))
                            {
                                pos++;
                                trimed = true;
                            }

                            if (richTextEnabled && trimed)
                                UpdateRichTextSegmentIndices(pos, out_string.Length - pos);
                        }

                        linewidth = 0f;
                        numchars = 0;
                        numlines++;
                    }
                    else
                    {
                        out_string.Append(in_string[pos++]);
                    }
                }

                if (linewidth > 0f)
                    linewidth += 0;
            }

            return out_string.ToString();
        }


        private static readonly ushort[] s_notatstart =
{
	//L'!', L'%', L')', L',', L'.', L':', L';', L'>', L'?', L']', L'_',
	0x0021, 0x0025, 0x0029,0x002c,0x002e,0x003a,0x003b,0x003e,0x003f,0x005d,0x0f,
	//"£¥", "£©",   "¡¢",   "¡±",    "£º",   "£»",   "£¡",   "£¬",
	0xff05, 0xff09, 0x3001, 0x201d, 0xff1a, 0xff1b, 0xff01, 0xff0c,
	//"¡£", "£¿",   "¡¤",    "££",   "£«",   "£­",   "¡¯",    "¡­"
	0x3002, 0xff1f, 0x00b7, 0xff03, 0xff0b, 0xff0d, 0x2019, 0x2026
};
        private static readonly ushort[] s_notatend =
        {
	//L'$', L'(', L'<', L'[', L'_',
	0x0024,0x0028,0x003c,0x005b,0x005f,
	//"£¨", "¡°",    "¡¤",    "££",   "£«",   "£­",   "¡®"
	0xff08, 0x201c, 0x00b7, 0xff03, 0xff0b, 0xff0d, 0x2018
};
        private static bool CanNewLineBefore(char c)
        {
            for (int i = 0; i < s_notatstart.Length; i++)
            {
                if (c == s_notatstart[i])
                    return false;
            }
            return true;
        }
        private static bool CanNewLineAfter(char c)
        {
            for (int i = 0; i < s_notatend.Length; i++)
            {
                if (c == s_notatend[i])
                    return false;
            }
            return true;
        }
        private static bool IsLetter(char c)
        {
            if (c < 0x0080)
                return true;

            //Latin-1 Ext 0x00a1 - 0x00ff
            //Latin Ext-A  0x0100 - 0x017f
            //Latin Ext-B  0x0180 - 0x024f
            if (c >= 0x00a1 && c <= 0x024f)
                return true;

            return false;
        }

        public int GetLineNumber()
        {
            int lineNumber = 0;

            if (String.IsNullOrEmpty(m_sString))
                return lineNumber;

            string multiline_str = m_sString;

            if (m_tDimensions.Width>0)
            {
                multiline_str = BreakLines();
            }

            lineNumber = 1;
            int str_len = multiline_str.Length;
            for (int ctr = 0; ctr < str_len; ++ctr)
            {
                if (multiline_str[ctr] == '\n')
                {
                    lineNumber++;
                }
            }

            return lineNumber;
        }

        public int GetTextHeight()
        {
            return m_pConfiguration != null ? m_pConfiguration.m_nCommonHeight * GetLineNumber() : 0;
        }

        protected string ParseRichTextLabels(string text, List<RichTextSegmentColor> segmentColors)
        {
            StringBuilder parsed_str = new StringBuilder(text.Length);

            while (text.Length > 0)
            {
                if (text.IndexOf("<color:") >= 0)
                {
                    // Parsing Color
                    int idx = text.IndexOf("<color:");
                    if (idx > 0)
                    {
                        parsed_str.Append(text.Substring(0, idx));
                        text = text.Remove(0, idx);
                    }

                    text = text.Remove(0, 7);
                    RichTextSegmentColor segmentColor = new RichTextSegmentColor();

                    idx = text.IndexOf('>');
                    if (idx >= 0)
                    {
                        //color value
                        string color_str = text.Substring(0, idx);
                        if(!string.IsNullOrWhiteSpace(color_str))
                        {
                            uint color = Convert.ToUInt32(color_str, 16);
                            byte r = (byte)((color & 0x00FF0000) >> 16);
                            byte g = (byte)((color & 0x0000FF00) >> 8);
                            byte b = (byte)((color & 0x000000FF));
                            segmentColor.Color = new CCColor3B(r, g, b);
                        }
                        else
                        {
                            throw new FormatException("a color value must be set");
                        }
                    }
                    else
                    {
                        throw new FormatException("incorrect color tag");
                    }

                    text = text.Remove(0, idx + 1); //remove until ']' 

                    //color text start
                    segmentColor.BeginIndex = parsed_str.Length;

                    idx = text.IndexOf("</color>");
                    if (idx >= 0)
                    {
                        //color text end
                        segmentColor.EndIndex = parsed_str.Length + idx;

                        parsed_str.Append(text.Substring(0, idx));
                        text = text.Remove(0, idx);
                    }
                    else
                    {
                        throw new FormatException("color closing tag is missing");
                    }

                    text = text.Remove(0, 8);

                    richTextSegmentColors.Add(segmentColor);

                }
                else
                {
                    parsed_str.Append(text);
                    text = "";
                }
            }


            return parsed_str.ToString();
        }

        private void UpdateRichTextSegmentIndices(int index, int count)
        {
            //color
            for (int i = 0; i < richTextSegmentColors.Count; i++)
            {
                var segmentColor = richTextSegmentColors[i];
                if (segmentColor.BeginIndex >= index)
                {
                    segmentColor.BeginIndex += count;
                    segmentColor.EndIndex += count;
                }
            }
        }
    }
}