using PluginAbstraction;
using Sharp;
using Sharp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SharpAsset
{
    [Serializable]
    public struct Shader : IAsset//move backendrenderer here?
    {
        public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }

        public string FullPath { get; set; }
        public bool IsAllocated => Program is not -1;
        public string VertexSource;
        public string FragmentSource;

        internal int VertexID;
        internal int FragmentID;
        internal BlendEquation dstColor;
        internal BlendEquation srcColor;
        internal BlendEquation dstAlpha;
        internal BlendEquation srcAlpha;
        internal int Program;

        internal Dictionary<string, int> uniformArray;//=new Dictionary<UniformType, Dictionary<string, int>> ();
        internal Dictionary<int, (ParameterType location, int size)> attribArray;
        //internal static Dictionary<string, int> globalUniformArray;

        public override string ToString()
        {
            return Name.ToString();
        }

        public void Dispose()
        {
            PluginManager.backendRenderer.DeleteBuffer(Target.Shader, Program);
            PluginManager.backendRenderer.DeleteBuffer(Target.VertexShader, VertexID);
            PluginManager.backendRenderer.DeleteBuffer(Target.FragmentShader, FragmentID);
        }
    }
}