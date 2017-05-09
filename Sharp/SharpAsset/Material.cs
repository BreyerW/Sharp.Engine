using OpenTK;
using System;
using System.Collections.Generic;
using SharpAsset.Pipeline;

namespace SharpAsset
{
    public struct Material//IAsset
    {
        private int lastSlot;
        private int shaderId;

        internal static Dictionary<string, IParameter> globalParams = new Dictionary<string, IParameter>();

        internal Dictionary<string, IParameter> localParams;

        //public event OnShaderChanged

        public Shader Shader
        {
            get
            {
                return Pipeline.Pipeline.GetPipeline<ShaderPipeline>().GetAsset(shaderId);
                /*     if (shader!=null)
                         return shader;
                 throw new IndexOutOfRangeException("Material dont point to any shader");
                 */
            }
            set
            {
                var id = 0;
                foreach (var shader in ShaderPipeline.assets)
                {
                    if (shader.Program == value.Program)
                    {
                        shaderId = id;
                        break;
                    }
                    id++;
                }
                if (localParams == null)
                {
                    localParams = new Dictionary<string, IParameter>();
                }
            }
        }

        public void BindProperty(string propName, GetData<Texture> getData)
        {
            if (!localParams.ContainsKey(propName))
            {
                localParams.Add(propName, new Texture2DParameter(getData, ++lastSlot));
                lastSlot = ++lastSlot;
            }
            //else
            //(localParams[shaderLoc] as Texture2DParameter).tbo = tex.TBO;
        }

        public void BindProperty(string propName, GetData<Matrix4> getData)
        {
            if (!localParams.ContainsKey(propName))
                localParams.Add(propName, new Matrix4Parameter(getData));
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