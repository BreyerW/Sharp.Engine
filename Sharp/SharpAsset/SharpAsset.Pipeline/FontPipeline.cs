﻿using System;
using System.Collections.Generic;
using SharpFont;
using System.IO;
using System.Threading;
using OpenTK;
using System.Runtime.CompilerServices;

namespace SharpAsset.Pipeline
{
    [SupportedFiles(".ttf", ".otf")]
    internal class FontPipeline : Pipeline<Font>
    {
        public ThreadLocal<Library> lib = new ThreadLocal<Library>(() => new Library());

        public override void Export(string pathToExport, string format)
        {
            throw new NotImplementedException();
        }

        public override ref Font GetAsset(int index)
        {
            return ref this[index];
        }

        public override IAsset Import(string pathToFile)
        {
            var name = Path.GetFileNameWithoutExtension(pathToFile);
            if (nameToKey.Contains(name))
                return this[nameToKey.IndexOf(name)];
            var face = new Face(lib.Value, pathToFile);
            nameToKey.Add(name);
            var font = new Font() { face = face, fontAtlas = new Dictionary<uint, (Texture, int, (int, int))>() };
            font.Size = 16;
            this[nameToKey.IndexOf(name)] = font;
            return font;
        }
    }
}