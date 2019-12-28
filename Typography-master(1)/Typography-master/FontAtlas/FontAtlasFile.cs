﻿//MIT, 2018-present, WinterDev

using System;
using System.Collections.Generic;
using System.IO;
using Typography.Rendering;

namespace PixelFarm.Drawing.Fonts
{


    public class FontAtlasFile
    {
        //Typography's custom font atlas file        
        SimpleFontAtlas _atlas;
        List<SimpleFontAtlas> _simpleFontAtlasList;
        enum FontTextureObjectKind : ushort
        {
            End,
            TotalImageInfo,
            GlyphList,
            OverviewFontInfo,
            OverviewMultiSizeFontInfo,
        }


        public List<SimpleFontAtlas> ResultSimpleFontAtlasList => _simpleFontAtlasList;

        public void Read(Stream inputStream)
        {
            //custom font atlas file                        
            _simpleFontAtlasList = new List<SimpleFontAtlas>();
            using (BinaryReader reader = new BinaryReader(inputStream, System.Text.Encoding.UTF8))
            {
                //1. version
                ushort fileversion = reader.ReadUInt16();
                bool stop = false;
                int listCount = 0;

                while (!stop)
                {
                    //2. read object kind
                    FontTextureObjectKind objKind = (FontTextureObjectKind)reader.ReadUInt16();
                    switch (objKind)
                    {
                        default: throw new NotSupportedException();
                        case FontTextureObjectKind.OverviewMultiSizeFontInfo:
                            listCount = reader.ReadUInt16();
                            break;
                        case FontTextureObjectKind.OverviewFontInfo:
                            //start new atlas
                            _atlas = new SimpleFontAtlas();
                            _simpleFontAtlasList.Add(_atlas);
                            ReadOverviewFontInfo(reader);
                            break;
                        case FontTextureObjectKind.End:
                            stop = true;
                            break;
                        case FontTextureObjectKind.GlyphList:
                            ReadGlyphList(reader);
                            break;
                        case FontTextureObjectKind.TotalImageInfo:
                            ReadTotalImageInfo(reader);
                            break;
                    }
                }

            }
        }

        void ReadTotalImageInfo(BinaryReader reader)
        {
            //read total
            //this version compose of width and height
            _atlas.Width = reader.ReadUInt16();
            _atlas.Height = reader.ReadUInt16();
            byte colorComponent = reader.ReadByte(); //1 or 4
            _atlas.TextureKind = (PixelFarm.Drawing.BitmapAtlas.TextureKind)reader.ReadByte();
        }
        void ReadGlyphList(BinaryReader reader)
        {
            ushort glyphCount = reader.ReadUInt16();
            for (int i = 0; i < glyphCount; ++i)
            {
                //read each glyph map info
                //1. codepoint
                var glyphMap = new TextureGlyphMapData();
                ushort glyphIndex = reader.ReadUInt16();
                //2. area, left,top,width,height

                glyphMap.Left = reader.ReadUInt16();
                glyphMap.Top = reader.ReadUInt16();
                glyphMap.Width = reader.ReadUInt16();
                glyphMap.Height = reader.ReadUInt16();
                //---------------------------------------
                //3. texture offset
                glyphMap.TextureXOffset = reader.ReadInt16();
                glyphMap.TextureYOffset = reader.ReadInt16();

                //---------------------------------------
                _atlas.AddGlyph(glyphIndex, glyphMap);
            }
        }

        void ReadOverviewFontInfo(BinaryReader reader)
        {
            _atlas.FontFilename = reader.ReadString();
            _atlas.FontKey = reader.ReadInt32();
            _atlas.OriginalFontSizePts = reader.ReadSingle();
        }

        //------------------------------------------------------------
        BinaryWriter _writer;
        internal void StartWrite(Stream outputStream)
        {
            _writer = new BinaryWriter(outputStream, System.Text.Encoding.UTF8);
            //version            
            _writer.Write((ushort)2);
        }
        internal void EndWrite()
        {
            //write end marker
            _writer.Write((ushort)FontTextureObjectKind.End);
            //
            _writer.Flush();
            _writer = null;
        }
        internal void WriteOverviewMultiSizeFontInfo(ushort count)
        {
            _writer.Write((ushort)FontTextureObjectKind.OverviewMultiSizeFontInfo);
            _writer.Write((ushort)count);
        }
        internal void WriteOverviewFontInfo(string fontFileName, int fontKey, float sizeInPt)
        {
            _writer.Write((ushort)FontTextureObjectKind.OverviewFontInfo);

#if DEBUG
            if (string.IsNullOrEmpty(fontFileName))
            {
                throw new NotSupportedException();
            }
            if (fontKey == 0)
            {
                throw new NotSupportedException();
            }
#endif
            _writer.Write(fontFileName);
            _writer.Write(fontKey);
            _writer.Write(sizeInPt);
        }
        internal void WriteTotalImageInfo(ushort width, ushort height, byte colorComponent, PixelFarm.Drawing.BitmapAtlas.TextureKind textureKind)
        {
            _writer.Write((ushort)FontTextureObjectKind.TotalImageInfo);
            _writer.Write(width);
            _writer.Write(height);
            _writer.Write(colorComponent);
            _writer.Write((byte)textureKind);
        }
        internal void WriteGlyphList(Dictionary<ushort, CacheGlyph> glyphs)
        {
            _writer.Write((ushort)FontTextureObjectKind.GlyphList);
            //total number
            int totalNum = glyphs.Count;
#if DEBUG
            if (totalNum >= ushort.MaxValue)
            {
                throw new NotSupportedException();
            }
#endif

            _writer.Write((ushort)totalNum);
            // 
            foreach (CacheGlyph g in glyphs.Values)
            {
                //1. code point
                _writer.Write((ushort)g.glyphIndex);
                //2. area, left,top,width,height
                _writer.Write((ushort)g.area.Left);
                _writer.Write((ushort)g.area.Top);
                _writer.Write((ushort)g.area.Width);
                _writer.Write((ushort)g.area.Height);


                //3. texture offset                
                GlyphImage img = g.img;
                _writer.Write((short)img.TextureOffsetX);//short
                _writer.Write((short)img.TextureOffsetY);//short

            }
        }
        //--------------------
        internal void WriteGlyphList(Dictionary<ushort, TextureGlyphMapData> glyphs)
        {
            _writer.Write((ushort)FontTextureObjectKind.GlyphList);
            //total number
            int totalNum = glyphs.Count;
#if DEBUG
            if (totalNum >= ushort.MaxValue)
            {
                throw new NotSupportedException();
            }
#endif

            _writer.Write((ushort)totalNum);
            // 
            foreach (var kp in glyphs)
            {
                ushort glyphIndex = kp.Key;
                TextureGlyphMapData g = kp.Value;
                //1. code point
                _writer.Write((ushort)glyphIndex);
                //2. area, left,top,width,height
                _writer.Write((ushort)g.Left);
                _writer.Write((ushort)g.Top);
                _writer.Write((ushort)g.Width);
                _writer.Write((ushort)g.Height);
                //3. texture offset                
                _writer.Write((short)g.TextureXOffset);//short
                _writer.Write((short)g.TextureYOffset);//short
            }
        }



    }

}