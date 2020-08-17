using Newtonsoft.Json;
using Sharp;
using SharpAsset.Pipeline;
using SharpSL;
using System;
using System.Buffers;
using System.Collections;
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
	{//TODO: load locations lazily LoadLocation on IBackendRenderer when binding property? and when constructing material for global props  
		private const byte FLOAT = 0;
		private const byte VECTOR2 = 1;
		private const byte VECTOR3 = 2;
		private const byte MATRIX4X4 = 3;
		private const byte TEXTURE = 4;
		private const byte MESH = 5;
		private const byte COLOR4 = 6;
		private const byte MATRIX4X4PTR = byte.MaxValue;
		private const byte COLOR4PTR = byte.MaxValue - 1;

		private static ArrayPool<byte> pool = ArrayPool<byte>.Shared;
		private static Dictionary<Type, int> sizeTable = new Dictionary<Type, int>()
		{
			[typeof(Vector2)] = Marshal.SizeOf<Vector2>(),
			[typeof(Vector3)] = Marshal.SizeOf<Vector3>(),
			[typeof(Vector4)] = Marshal.SizeOf<Vector4>(),
			[typeof(Matrix4x4)] = Marshal.SizeOf<Matrix4x4>(),
			[typeof(Texture)] = Marshal.SizeOf<int>(),
			[typeof(Mesh)] = Marshal.SizeOf<int>(),
			[typeof(Color)] = Marshal.SizeOf<Color>(),
			[typeof(IntPtr)] = Marshal.SizeOf<IntPtr>(),
			[typeof(float)] = Marshal.SizeOf<float>(),
		};
		//private int lastSlot;
		private int shaderId;

		private static Dictionary<string, byte[]> globalParams = new Dictionary<string, byte[]>();
		[JsonProperty]
		internal Dictionary<string, byte[]> localParams;
		//internal Renderer attachedToRenderer;
		//TODO: split pipeline into importer/exporter?																								 //public RefAction<Material> onShaderDataRequest; //ref Material mat or shader visible in editor?

		public Shader Shader
		{
			get
			{
				return Pipeline.Pipeline.Get<Shader>().GetAsset(shaderId);
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
				var param = pool.Rent(sizeTable[typeof(IntPtr)] + 1);
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
			var i = TexturePipeline.nameToKey.IndexOf(data.Name);
			if (localParams.TryGetValue(propName, out var addr))
				Unsafe.WriteUnaligned(ref addr[1], i);
			else
			{
				//lastSlot = ++lastSlot;
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = TEXTURE;
				Unsafe.WriteUnaligned(ref param[1], i);
				localParams.Add(propName, param);
			}
		}
		public void BindProperty(string propName, in Color data)
		{
			if (localParams.TryGetValue(propName, out var addr))
				Unsafe.WriteUnaligned(ref addr[1], data);
			else
			{
				//lastSlot = ++lastSlot;
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = COLOR4;
				Unsafe.WriteUnaligned(ref param[1], data);
				localParams.Add(propName, param);
			}
		}
		public void BindProperty(string propName, in float data)
		{
			if (localParams.TryGetValue(propName, out var addr))
				Unsafe.WriteUnaligned(ref addr[1], data);
			else
			{
				//lastSlot = ++lastSlot;
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = FLOAT;
				Unsafe.WriteUnaligned(ref param[1], data);
				localParams.Add(propName, param);
			}
		}
		internal void BindProperty(string propName, in Mesh data)//TODO: change to BindMesh without property name?
		{
			var i = MeshPipeline.nameToKey.IndexOf(data.Name);
			if (localParams.TryGetValue(propName, out var addr))
				Unsafe.WriteUnaligned(ref addr[1], i);
			else
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = MESH;
				Unsafe.WriteUnaligned(ref param[1], i);
				localParams.Add(propName, param);
			}
		}
		public void BindProperty(string propName, in Matrix4x4 data)
		{
			if (!localParams.ContainsKey(propName))
			{

				var param = pool.Rent(sizeTable[data.GetType()] + 1);
				param[0] = MATRIX4X4;
				Unsafe.WriteUnaligned(ref param[1], data);
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], data);
		}
		public void BindProperty(string propName, in Vector3 data)
		{
			if (!localParams.ContainsKey(propName))
			{

				var param = pool.Rent(sizeTable[data.GetType()] + 1);
				param[0] = VECTOR3;
				Unsafe.WriteUnaligned(ref param[1], data);
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], data);
		}
		public void BindProperty(string propName, in Vector2 data)
		{
			if (!localParams.ContainsKey(propName))
			{

				var param = pool.Rent(sizeTable[data.GetType()] + 1);
				param[0] = VECTOR2;
				Unsafe.WriteUnaligned(ref param[1], data);
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], data);
		}
		public bool TryGetProperty(string prop, out Mesh data)
		{
			if (localParams.TryGetValue(prop, out var addr))
			{
				data = Pipeline.Pipeline.Get<Mesh>().GetAsset(Unsafe.As<byte, int>(ref addr[1]));
				return true;
			}
			data = default;
			return false;
		}
		public bool TryGetProperty(string prop, out Texture data)
		{
			if (localParams.TryGetValue(prop, out var addr))
			{
				data = Pipeline.Pipeline.Get<Texture>().GetAsset(Unsafe.As<byte, int>(ref addr[1]));
				return true;
			}
			data = default;
			return false;
		}
		internal void InternalSetProperty(string propName, in Matrix4x4 data)
		{
			if (Shader.IsAllocated)
				MainWindow.backendRenderer.SendMatrix4(Shader.uniformArray[propName], ref Unsafe.As<float, byte>(ref Unsafe.AsRef(data).M11));
		}
		internal void InternalSetProperty(string propName, in Texture data)
		{
			if (Shader.IsAllocated)
				MainWindow.backendRenderer.SendTexture2D(Shader.uniformArray[propName], ref Unsafe.As<int, byte>(ref Unsafe.AsRef(data).TBO));
		}
		public static void BindGlobalProperty(string propName, in Matrix4x4 data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey(propName))
			{
				var param = pool.Rent(sizeTable[data.GetType()] + 1);
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
		public static void BindGlobalProperty(string propName, in Vector2 data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey(propName))
			{
				var param = pool.Rent(sizeTable[data.GetType()] + 1);
				param[0] = VECTOR2;
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
			if (Shader.IsAllocated is false) return;

			var idLight = 0;
			if (Shader.uniformArray.ContainsKey("ambient"))
			{
				MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["ambient"], ref Unsafe.As<float, byte>(ref Light.ambientCoefficient));
				foreach (var light in Light.lights)
				{
					MainWindow.backendRenderer.SendUniform3(Shader.uniformArray["lights[" + idLight + "].position"], ref Unsafe.As<float, byte>(ref light.Parent.transform.position.X));
					MainWindow.backendRenderer.SendUniform4(Shader.uniformArray["lights[" + idLight + "].color"], ref Unsafe.As<float, byte>(ref light.color.a));
					MainWindow.backendRenderer.SendUniform1(Shader.uniformArray["lights[" + idLight + "].intensity"], ref Unsafe.As<float, byte>(ref light.intensity));
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
			if (TryGetProperty("mesh", out Mesh mesh) is false) return;

			MainWindow.backendRenderer.BindBuffers(Target.Mesh, mesh.VBO);
			foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats[mesh.VertType].attribs)
			{
				if (Shader.attribArray.TryGetValue(vertAttrib.shaderLocation, out var attrib))
					MainWindow.backendRenderer.BindVertexAttrib(vertAttrib.type, attrib.location, vertAttrib.size, Marshal.SizeOf(mesh.VertType), vertAttrib.offset);
			}
			MainWindow.backendRenderer.BindBuffers(Target.Indices, mesh.EBO);
			MainWindow.backendRenderer.Draw(mesh.indiceType, mesh.Indices.Length);
			var tbo = 0;
			MainWindow.backendRenderer.SendTexture2D(0, ref Unsafe.As<int, byte>(ref tbo));//TODO: generalize this
		}

		private void SendToGPU(string prop, byte[] data)
		{
			if (!(prop is "mesh") && Shader.uniformArray.ContainsKey(prop))
				switch (data[0])
				{
					case FLOAT: MainWindow.backendRenderer.SendUniform1(Shader.uniformArray[prop], ref data[1]); break;
					case VECTOR2: MainWindow.backendRenderer.SendUniform2(Shader.uniformArray[prop], ref data[1]); break;
					case VECTOR3: MainWindow.backendRenderer.SendUniform3(Shader.uniformArray[prop], ref data[1]); break;
					case COLOR4: MainWindow.backendRenderer.SendUniform4(Shader.uniformArray[prop], ref data[1]); break;
					case MATRIX4X4: MainWindow.backendRenderer.SendMatrix4(Shader.uniformArray[prop], ref data[1]); break;
					case TEXTURE:
						TryGetProperty(prop, out Texture tex);
						MainWindow.backendRenderer.SendTexture2D(Shader.uniformArray[prop], ref Unsafe.As<int, byte>(ref tex.TBO)/*, Slot*/); break;
					case MATRIX4X4PTR: /*unsafe { MainWindow.backendRenderer.SendMatrix4(Shader.uniformArray[prop], ref Unsafe.AsRef<Matrix4x4>(Unsafe.As<byte, IntPtr>(ref data[1]).ToPointer()).M11); }*/ break;
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
