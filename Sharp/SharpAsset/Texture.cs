using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System;
using Sharp.Editor.Views;
using OpenTK;
using Sharp;

namespace SharpAsset
{
    public struct Texture : IAsset
    {
        private bool allocated;

        internal static Texture[] textures = new Texture[1];
        internal static List<string> nameToKey = new List<string>();

        internal int TBO;

        internal Bitmap bitmap;
        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }

        public static ref Texture getAsset(string name)
        {

            ref var tex = ref textures[nameToKey.IndexOf(name)];
            if (!tex.allocated)
            {
                SceneView.backendRenderer.GenerateBuffers(ref tex);
                SceneView.backendRenderer.BindBuffers(ref tex);
                SceneView.backendRenderer.Allocate(ref tex);
                tex.allocated = true;
                textures[nameToKey.IndexOf(name)] = tex;
            }
            return ref textures[nameToKey.IndexOf(name)];
        }

        public override string ToString()
        {
            return Name;
        }

        public IAsset GetOrLoadAsset()
        {
            throw new NotImplementedException();
        }

        public void PlaceIntoScene(Entity context, Vector3 worldPos)
        {
            throw new NotImplementedException();
        }
    }
}

