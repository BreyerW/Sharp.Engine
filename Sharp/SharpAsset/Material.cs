using Sharp;
using SharpAsset.Pipeline;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/*[StructLayout(LayoutKind.Explicit, Pack = 2)]
public struct Matrix4X4
{
[FieldOffset(0)]
private byte isUnmanaged;
	[FieldOffset(2)]
	private IntPtr ptr;

	[FieldOffset(2)]
	private Matrix4x4 matrix;

	internal ref Matrix4x4 Matrix{
	get{
			if (isUnmanaged is 0)
				return ref matrix;
			else { 
			unsafe {
					return ref Unsafe.AsRef<Matrix4x4>(ptr.ToPointer());
			}
				}
			
	}
	}
}*/

namespace SharpAsset
{
	[Serializable]
	public class Material : IDisposable//IAsset
	{
		private const byte VECTOR1 = 0;
		private const byte VECTOR2 = 1;
		private const byte VECTOR3 = 2;
		private const byte MATRIX4X4 = 3;
		private const byte TEXTURE = 4;
		private const byte VECTOR1PTR = byte.MaxValue - 0;
		private const byte VECTOR2PTR = byte.MaxValue - 1;
		private const byte VECTOR3PTR = byte.MaxValue - 2;
		private const byte MATRIX4X4PTR = byte.MaxValue - 3;
		private const byte TEXTUREPTR = byte.MaxValue - 4;

		private static ArrayPool<byte> pool = ArrayPool<byte>.Shared;

		//private int lastSlot;
		private int shaderId;

		private static Dictionary<string, byte[]> globalParams = new Dictionary<string, byte[]>();

		private Dictionary<string, byte[]> localParams;
		//internal Renderer attachedToRenderer;

		//public event OnShaderChanged
		//public RefAction<Material> onShaderDataRequest; //ref Material mat or shader visible in editor?

		public Shader Shader
		{
			get
			{
				return Pipeline.Pipeline.Get<ShaderPipeline>().GetAsset(shaderId);
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
					localParams = new Dictionary<string, byte[]>();
				}
			}
		}
		public void BindUnmanagedProperty<T>(string propName, in T data) where T : unmanaged
		{
			IntPtr ptr;
			unsafe
			{
				ptr = (IntPtr)Unsafe.AsPointer(ref Unsafe.AsRef(data));
			}
			if (!localParams.ContainsKey(propName))
			{
				var param = pool.Rent(Marshal.SizeOf<T>() + 1);
				param[0] = data switch
				{
					Matrix4x4 _ => MATRIX4X4PTR,
					_ => throw new NotSupportedException(typeof(T).Name)
				};
				Unsafe.WriteUnaligned(ref param[1], ptr);
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], ptr);
		}
		public void BindProperty(string propName, ref Texture data)
		{
			if (!localParams.ContainsKey(propName))
			{
				//lastSlot = ++lastSlot;
				var param = pool.Rent(5);
				param[0] = TEXTURE;
				Unsafe.WriteUnaligned(ref param[1], data.TBO);
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], data);
		}

		public void SetProperty(string propName, in Matrix4x4 data)
		{
			if (!localParams.ContainsKey(propName))
			{

				var param = pool.Rent(Unsafe.SizeOf<Matrix4x4>() + 1);
				param[0] = MATRIX4X4;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref Unsafe.AsRef(data));
				//param.DataAddress = Unsafe.ReadUnaligned<byte[]>(ref Unsafe.As<Matrix4x4, byte>(ref data))[0];
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.DataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], data);
		}

		internal void InternalSetProperty(string propName, in Matrix4x4 data)
		{
			MainWindow.backendRenderer.SendMatrix4(Shader.uniformArray[propName], ref Unsafe.AsRef(data.M11));
		}

		public static void SetGlobalProperty(string propName, ref Matrix4x4 data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey(propName))
			{
				var param = pool.Rent(Unsafe.SizeOf<Matrix4x4>() + 1);
				param[0] = MATRIX4X4;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[propName][1], data);

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
					MainWindow.backendRenderer.SendUniform3(Shader.uniformArray["lights[" + idLight + "].position"], ref light.Parent.transform.position.X);
					//GL.UniformMatrix4(mat.Shader.uniformArray[UniformType.FloatMat4]["lights[" + idLight + "].modelMatrix"],false,ref light.entityObject.ModelMatrix);
					MainWindow.backendRenderer.SendUniform4(Shader.uniformArray["lights[" + idLight + "].color"], ref Unsafe.As<byte, float>(ref light.color.A));
					MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["lights[" + idLight + "].intensity"], ref light.intensity);
					//GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["lights[" + idLight + "].angle"], light.angle);
					idLight++;
				}
			}
			foreach (var (key, value) in localParams)
			{
				SendToGPU(key, value);
				//Console.WriteLine("test " + key);
			}

			foreach (var (key, value) in globalParams)
				SendToGPU(key, value);
		}

		private void SendToGPU(string prop, byte[] data)
		{
			switch (data[0])
			{
				case VECTOR1: break;
				case VECTOR2: break;
				case VECTOR3: break;
				case MATRIX4X4: MainWindow.backendRenderer.SendMatrix4(Shader.uniformArray[prop], ref Unsafe.As<byte, Matrix4x4>(ref data[1]).M11); break;
				case TEXTURE: MainWindow.backendRenderer.SendTexture2D(Shader.uniformArray[prop], Unsafe.As<byte, int>(ref data[1])/*, Slot*/); break;


				case VECTOR1PTR: break;
				case VECTOR2PTR: break;
				case VECTOR3PTR: break;
				case MATRIX4X4PTR: unsafe { MainWindow.backendRenderer.SendMatrix4(Shader.uniformArray[prop], ref Unsafe.AsRef<Matrix4x4>(Unsafe.As<byte, IntPtr>(ref data[1]).ToPointer()).M11); } break;
				case TEXTUREPTR: MainWindow.backendRenderer.SendTexture2D(Shader.uniformArray[prop], Unsafe.As<byte, int>(ref data[1])/*, Slot*/); break;
			}
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					foreach (var value in localParams.Values)
						pool.Return(value);

					//TODO: move this to when application exit
					//foreach (var value in globalParams.Values)
					//	pool.Return(value);
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Material() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion IDisposable Support
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
