﻿//MIT, 2019-present, WinterDev
//----------------------------------- 

using System;
using System.Collections.Generic;
using PixelFarm.Platforms;
namespace PixelFarm.Drawing.BitmapAtlas
{
    //TODO: review class and method names
    public delegate U LoadNewBmpDelegate<T, U>(T src);

    public class BitmapCache<T, U> : IDisposable
        where U : IDisposable
    {
        Dictionary<T, U> _loadBmps = new Dictionary<T, U>();
        LoadNewBmpDelegate<T, U> _loadNewBmpDel;
        public BitmapCache(LoadNewBmpDelegate<T, U> loadNewBmpDel)
        {
            _loadNewBmpDel = loadNewBmpDel;
        }
        public U GetOrCreateNewOne(T key)
        {
            U found;
            if (!_loadBmps.TryGetValue(key, out found))
            {
                return _loadBmps[key] = _loadNewBmpDel(key);
            }
            return found;
        }
        public void Dispose()
        {
            Clear();
        }
        public void Clear()
        {
            foreach (U glbmp in _loadBmps.Values)
            {
                glbmp.Dispose();
            }
            _loadBmps.Clear();
        }
        public void Delete(T key)
        {
            U found;
            if (_loadBmps.TryGetValue(key, out found))
            {
                found.Dispose();
                _loadBmps.Remove(key);
            }
        }
    }


    public class BitmapAtlasManager<B>
        where B : IDisposable
    {
        BitmapCache<SimpleBitmaptAtlas, B> _loadAtlases;
        Dictionary<string, SimpleBitmaptAtlas> _createdAtlases = new Dictionary<string, SimpleBitmaptAtlas>();
        TextureKind _textureKind;

        public BitmapAtlasManager(TextureKind textureKind,
            LoadNewBmpDelegate<SimpleBitmaptAtlas, B> _createNewDel)
            : this(textureKind)
        {
            //glyph cahce for specific atlas 
            SetLoadNewBmpDel(_createNewDel);
        }
        public BitmapAtlasManager(TextureKind textureKind)
        {
            _textureKind = textureKind;
        }
        protected void SetLoadNewBmpDel(LoadNewBmpDelegate<SimpleBitmaptAtlas, B> _createNewDel)
        {
            _loadAtlases = new BitmapCache<SimpleBitmaptAtlas, B>(_createNewDel);
        }

#if DEBUG
        System.Diagnostics.Stopwatch _dbugStopWatch = new System.Diagnostics.Stopwatch();
#endif 
        /// <summary>
        /// get from cache or create a new one
        /// </summary>
        /// <param name="reqFont"></param>
        /// <returns></returns>
        public SimpleBitmaptAtlas GetBitmapAtlas(string atlasName, out B outputBitmap)
        {

#if DEBUG
            _dbugStopWatch.Reset();
            _dbugStopWatch.Start();
#endif


            if (!_createdAtlases.TryGetValue(atlasName, out SimpleBitmaptAtlas foundAtlas))
            {
                //check from pre-built cache (if availiable)   
                string fontTextureInfoFile = atlasName + ".info";
                string fontTextureImgFilename = atlasName + ".png";
                //check if the file exist

                if (StorageService.Provider.DataExists(fontTextureInfoFile) &&
                    StorageService.Provider.DataExists(fontTextureImgFilename))
                {
                    SimpleBitmapAtlasBuilder atlasBuilder = new SimpleBitmapAtlasBuilder();
                    using (System.IO.Stream dataStream = StorageService.Provider.ReadDataStream(fontTextureInfoFile))
                    using (System.IO.Stream fontImgStream = StorageService.Provider.ReadDataStream(fontTextureImgFilename))
                    {
                        try
                        {
                            foundAtlas = atlasBuilder.LoadAtlasInfo(dataStream);
                            PixelFarm.CpuBlit.MemBitmap memBmp = PixelFarm.CpuBlit.MemBitmap.LoadBitmap(fontImgStream);
                            AtlasItemImage atlasImg = new AtlasItemImage(memBmp.Width, memBmp.Height); //TODO: review new .ctor
                            atlasImg.SetBitmap(memBmp, false);
                            foundAtlas.TotalImg = atlasImg;
                            _createdAtlases.Add(atlasName, foundAtlas);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }

                }
            }

            outputBitmap = _loadAtlases.GetOrCreateNewOne(foundAtlas);
            return foundAtlas;
        }

        public void Clear()
        {
            _loadAtlases.Clear();
        }
    }

}