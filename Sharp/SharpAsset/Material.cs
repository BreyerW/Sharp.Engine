using System.Numerics;
using System;
using Sharp;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SharpAsset.Pipeline;

namespace SharpAsset
{
    [Serializable]
    public class Material//IAsset
    {
        //private int lastSlot;
        private int shaderId;

        internal static Dictionary<string, IParameter> globalParams = new Dictionary<string, IParameter>();

        internal Dictionary<string, IParameter> localParams;
        //internal Renderer attachedToRenderer;

        //public event OnShaderChanged
        //public RefAction<Material> onShaderDataRequest; //ref Material mat or shader visible in editor?

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
                shaderId = ShaderPipeline.nameToKey.IndexOf(value.Name);
                if (localParams == null)
                {
                    localParams = new Dictionary<string, IParameter>();
                }
            }
        }

        public void BindProperty(string propName, ref Texture data, bool store = true)
        {
            if (store)
            {
                if (!localParams.ContainsKey(propName))
                {
                    //lastSlot = ++lastSlot;
                    var param = new Texture2DParameter(/*, lastSlot*/);
                    Unsafe.WriteUnaligned(ref param.DataAddress, data.TBO);
                    localParams.Add(propName, param);
                }
            }
            else
                Texture2DParameter.SendToGPU(data.TBO, Shader.uniformArray[propName]);
            //else
            //(localParams[shaderLoc] as Texture2DParameter).tbo = tex.TBO;
        }

        public void SetProperty(string propName, in Matrix4x4 data, bool store = true)
        {
            if (store)
            {
                if (!localParams.ContainsKey(propName))
                {
                    var param = new Matrix4Parameter();
                    Unsafe.WriteUnaligned(ref param.DataAddress, data);
                    localParams.Add(propName, param);
                }
            }
            else
                Matrix4Parameter.SendToGPU(Shader.uniformArray[propName], ref Unsafe.AsRef(data.M11));

            //else
            //(localParams[shaderLoc] as Matrix4Parameter).data[0] = mat;
        }

        public static void SetGlobalProperty(string propName, ref Matrix4x4 data/*, bool store = true*/)
        {
            // if (store)
            // {
            if (!globalParams.ContainsKey(propName))
            {
                var param = new Matrix4Parameter();
                Unsafe.WriteUnaligned(ref param.DataAddress, data);
                //Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
                globalParams.Add(propName, param);
            }
            // }
            //else
            // Matrix4Parameter.SendToGPU(Shader.uniformArray[propName], ref data.Row0.X);
            else
                Unsafe.WriteUnaligned(ref globalParams[propName].DataAddress, data);

            //Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
        }

        internal void SendData()
        {
            var idLight = 0;
            if (Shader.uniformArray.ContainsKey("ambient"))
            {
                MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["ambient"], ref Light.ambientCoefficient);
                foreach (var light in Light.lights)
                {
                    MainWindow.backendRenderer.SendUniform3(Shader.uniformArray["lights[" + idLight + "].position"], ref light.entityObject.position.X);
                    //GL.UniformMatrix4(mat.Shader.uniformArray[UniformType.FloatMat4]["lights[" + idLight + "].modelMatrix"],false,ref light.entityObject.ModelMatrix);
                    MainWindow.backendRenderer.SendUniform4(Shader.uniformArray["lights[" + idLight + "].color"], ref Unsafe.As<byte, float>(ref light.color.A));
                    MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["lights[" + idLight + "].intensity"], ref light.intensity);
                    //GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["lights[" + idLight + "].angle"], light.angle);
                    idLight++;
                }
            }
            foreach (var (key, value) in localParams)
            {
                value.ConsumeData(Shader.uniformArray[key]);
                //Console.WriteLine(OpenTK.Graphics.OpenGL.GL.GetError() + " " + key);
            }

            foreach (var (key, value) in globalParams)
                value.ConsumeData(Shader.uniformArray[key]);
        }
    }
}

/*

     using OpenTK;
using System;
using Sharp;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SharpAsset.Pipeline;
using TupleExtensions;

namespace SharpAsset
{
    public struct Material//IAsset
    {
        //private int lastSlot;
        private int shaderId;

        internal static Dictionary<string, IParameter> globalParams = new Dictionary<string, IParameter>();

        internal Dictionary<string, IParameter> localParams;
        internal Renderer attachedToRenderer;

        //public event OnShaderChanged
        //public RefAction<Material> onShaderDataRequest;

        public Shader Shader
        {
            get
            {
                return Pipeline.Pipeline.GetPipeline<ShaderPipeline>().GetAsset(shaderId);
                /*     if (shader!=null)
                         return shader;
                 throw new IndexOutOfRangeException("Material dont point to any shader");
                 *
            }
            set
            {
                shaderId = ShaderPipeline.nameToKey.IndexOf(value.Name);
                if (localParams == null)
                {
                    localParams = new Dictionary<string, IParameter>();
                }
            }
        }

        public void SetProperty(string propName, ref Texture data)
{
    if (!localParams.ContainsKey(propName))
    {
        //lastSlot = ++lastSlot;
        localParams.Add(propName, new Texture2DParameter(ref data/*, lastSlot*));
    }
    //else
    //(localParams[shaderLoc] as Texture2DParameter).tbo = tex.TBO;
}

public void SetProperty(string propName, ref Matrix4 data)
{
    if (!localParams.ContainsKey(propName))
        localParams.Add(propName, new Matrix4Parameter(ref data));
    //else
    //(localParams[shaderLoc] as Matrix4Parameter).data[0] = mat;
}

public static void SetGlobalProperty(string propName, ref Matrix4 data)
{
    if (!globalParams.ContainsKey(propName))
        globalParams.Add(propName, new Matrix4Parameter(ref data));
    //else
    //(globalParams[propName] as Matrix4Parameter).data[0] = mat;
}

internal void SendData()
{
    var idLight = 0;
    if (Shader.uniformArray.ContainsKey("ambient"))
    {
        MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["ambient"], ref Light.ambientCoefficient);
        foreach (var light in Light.lights)
        {
            MainWindow.backendRenderer.SendUniform3(Shader.uniformArray["lights[" + idLight + "].position"], ref light.entityObject.position.X);
            //GL.UniformMatrix4(mat.Shader.uniformArray[UniformType.FloatMat4]["lights[" + idLight + "].modelMatrix"],false,ref light.entityObject.ModelMatrix);
            MainWindow.backendRenderer.SendUniform4(Shader.uniformArray["lights[" + idLight + "].color"], ref Unsafe.As<byte, float>(ref light.color.A));
            MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["lights[" + idLight + "].intensity"], ref light.intensity);
            //GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["lights[" + idLight + "].angle"], light.angle);
            idLight++;
        }
    }
    foreach (var (key, value) in localParams)
    {
        value.ConsumeData(Shader.uniformArray[key]);
        //Console.WriteLine(OpenTK.Graphics.OpenGL.GL.GetError() + " " + key);
    }

    foreach (var (key, value) in globalParams)
        value.ConsumeData(Shader.uniformArray[key]);
}
    }
}
     */