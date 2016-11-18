using OpenTK;
using System;
using System.Collections.Generic;

namespace SharpAsset
{
    public struct Material//IAsset
    {
        private int lastSlot;
        internal static Dictionary<string, IParameter> globalParams = new Dictionary<string, IParameter>();

        internal Dictionary<int, IParameter> localParams;

        private int shaderId;

        public Shader Shader
        {
            get
            {
                foreach (var shader in Shader.shaders.Values)
                {
                    if (shader.Program == shaderId)
                        return shader;
                }
                throw new IndexOutOfRangeException("Material dont point to any shader");
            }
            set
            {
                shaderId = value.Program;
                if (localParams == null)
                {
                    localParams = new Dictionary<int, IParameter>();
                }
            }
        }
        public void BindProperty(string propName, GetData<Texture> getData)
        {
            if (!Shader.uniformArray.ContainsKey(propName)) return;

            var shaderLoc = Shader.uniformArray[propName];

            if (!localParams.ContainsKey(shaderLoc))
            {
                localParams.Add(shaderLoc, new Texture2DParameter(getData, ++lastSlot));
                lastSlot = ++lastSlot;
            }
            //else
            //(localParams[shaderLoc] as Texture2DParameter).tbo = tex.TBO;
        }
        public void BindProperty(string propName, GetData<Matrix4> getData)
        {
            var shaderLoc = Shader.uniformArray[propName];

            if (!localParams.ContainsKey(shaderLoc))
                localParams.Add(shaderLoc, new Matrix4Parameter(getData));
            //else
            //(localParams[shaderLoc] as Matrix4Parameter).data[0] = mat;
        }

        /*public static void SetGlobalProperty(string propName, ref Texture tex)
        {

            if (globalTexArray.ContainsKey(propName))
                globalTexArray[propName] = tex.TBO;
            else
                globalTexArray.Add(propName, tex.TBO);
        }*/
        public static void BindGlobalProperty(string propName, GetData<Matrix4> getData)
        {
            if (!globalParams.ContainsKey(propName))
                globalParams.Add(propName, new Matrix4Parameter(getData));
            //else
            //(globalParams[propName] as Matrix4Parameter).data[0] = mat;
        }
    }
}

