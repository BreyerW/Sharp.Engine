﻿using Newtonsoft.Json;
using Sharp;
using SharpAsset.Pipeline;
using SharpSL;
using SixLabors.ImageSharp.Processing;
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
	public class Material : IDisposable, IEngineObject//IAsset
	{//TODO: load locations lazily LoadLocation on IBackendRenderer when binding property? and when constructing material for global props  


		private const byte FLOAT = 0;
		private const byte VECTOR2 = 1;
		private const byte VECTOR3 = 2;
		private const byte MATRIX4X4 = 3;
		private const byte TEXTURE = 4;
		private const byte MESH = 5;
		private const byte COLOR4 = 6;
		private const byte UVECTOR2 = 7;
		private const byte MATRIX4X4PTR = byte.MaxValue;
		private const byte COLOR4PTR = byte.MaxValue - 1;

		private static int lastShaderUsed = -1;
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
			[typeof(uint)] = Marshal.SizeOf<uint>(),
		};
		[JsonProperty]
		private int[] shadersId = Array.Empty<int>();
		private static Dictionary<(uint winId, string property), byte[]> globalParams = new Dictionary<(uint winId, string property), byte[]>();
		[JsonProperty]
		private Dictionary<string, byte[]> localParams;
		/*public Shader Shader
		{
			get
			{
				return Pipeline.Pipeline.Get<Shader>().GetAsset(shaderId);
				/*     if (shader!=null)
                         return shader;
                 throw new IndexOutOfRangeException("Material dont point to any shader");
                 *
			}
			set
			{
				shaderId = ShaderPipeline.nameToKey.IndexOf(value.Name.ToString());
				if (localParams == null)
				{
					localParams = new Dictionary<string, byte[]>();
				}
			}
		}*/
		public bool IsMainPassTransparent
		{
			get;
			private set;
		} = false;
		public void BindShader(int pass, in Shader shader)
		{
			if (pass >= shadersId.Length)
				Array.Resize(ref shadersId, pass + 1);
			shadersId[pass] = ShaderPipeline.nameToKey.IndexOf(shader.Name.ToString());
			if (pass is 0 && shader.dstColor is not BlendEquation.None)
				IsMainPassTransparent = true;
			if (localParams is null)
			{
				localParams = new Dictionary<string, byte[]>();
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
				var param = new byte[sizeTable[typeof(IntPtr)] + 1];
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
		public void BindProperty(string propName, in Texture data)
		{
			var i = TexturePipeline.nameToKey.IndexOf(data.Name.ToString());
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
			var i = MeshPipeline.nameToKey.IndexOf(data.Name.ToString());//TODO: change to rely on pinned heap object and intptr?
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

				var param = new byte[sizeTable[data.GetType()] + 1];
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

				var param = new byte[sizeTable[data.GetType()] + 1];
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

				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = VECTOR2;
				Unsafe.WriteUnaligned(ref param[1], data);
				localParams.Add(propName, param);
			}
			else
				Unsafe.WriteUnaligned(ref localParams[propName][1], data);
		}
		public void BindProperty(string propName, in uint[] data)
		{
			if (!localParams.ContainsKey(propName))
			{

				var param = new byte[sizeTable[data[0].GetType()] * data.Length + 1];
				if (data.Length is 2)
					param[0] = UVECTOR2;
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
				var index = Unsafe.As<byte, int>(ref addr[1]);
				data = Pipeline.Pipeline.Get<Mesh>().GetAsset(index);
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
		public bool TryGetProperty(string prop, out float data)
		{
			if (localParams.TryGetValue(prop, out var addr))
			{
				data = Unsafe.As<byte, float>(ref addr[1]);
				return true;
			}
			data = default;
			return false;
		}
		public bool TryGetProperty<T>(string prop, out T data) where T : unmanaged
		{
			if (localParams.TryGetValue(prop, out var addr))
			{
				data = Unsafe.As<byte, T>(ref addr[1]);
				return true;
			}
			data = default;
			return false;
		}
		public static void BindGlobalProperty(string propName, in Matrix4x4 data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((MainWindow.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = MATRIX4X4;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((MainWindow.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(MainWindow.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public static void BindGlobalProperty(string propName, in Color data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((MainWindow.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = COLOR4;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((MainWindow.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(MainWindow.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public static void BindGlobalProperty(string propName, in float data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((MainWindow.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = FLOAT;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((MainWindow.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(MainWindow.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public static void BindGlobalProperty(string propName, in Vector2 data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((MainWindow.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = VECTOR2;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((MainWindow.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(MainWindow.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public void Draw(Range rangeOfSubMeshes, int pass = 0)
		{
			ref var shader = ref Pipeline.Pipeline.Get<Shader>().GetAsset(shadersId[pass]);
			if (shader.IsAllocated is false) return;
			if (TryGetProperty("mesh", out Mesh mesh) is false || mesh.VBO is -1)
				return;
			if (Material.lastShaderUsed != shader.Program)
			{
				MainWindow.backendRenderer.Use(shader.Program);
				lastShaderUsed = shader.Program;
			}
			/*var idLight = 0;
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
			}*/


			foreach (var (key, value) in globalParams)
				if (key.winId == MainWindow.backendRenderer.currentWindow)
					SendToGPU(shader, key.property, value);

			foreach (var (key, value) in localParams)
			{
				SendToGPU(shader, key, value);
				//Console.WriteLine("test " + key);
			}
			MainWindow.backendRenderer.BindBuffers(Target.Mesh, mesh.VBO);

			MainWindow.backendRenderer.BindBuffers(Target.Indices, mesh.EBO);
			if (mesh.UsageHint is UsageHint.DynamicDraw)
			{
				MainWindow.backendRenderer.Allocate(Target.Indices, mesh.UsageHint, ref mesh.Indices[0], mesh.Indices.Length);
				MainWindow.backendRenderer.Allocate(Target.Mesh, mesh.UsageHint, ref mesh.SpanToMesh[0], mesh.SpanToMesh.Length);
			}
			foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats[mesh.VertType].attribs)
			{
				if (shader.attribArray.TryGetValue(vertAttrib.shaderLocation, out var attrib))
					MainWindow.backendRenderer.BindVertexAttrib(vertAttrib.type, attrib.location, vertAttrib.size, Marshal.SizeOf(mesh.VertType), vertAttrib.offset);
			}
			var startIndex = rangeOfSubMeshes.Start.Value;
			var endIndex = rangeOfSubMeshes.End.Value;

			if (mesh.subMeshesDescriptor is null || (startIndex is 0 && endIndex is 0))
				MainWindow.backendRenderer.Draw(mesh.indexStride, 0, mesh.Indices.Length);
			else if (mesh.subMeshesDescriptor is not null)
			{
				endIndex = endIndex is 0 ? mesh.subMeshesDescriptor.Length / 2 : endIndex;
				var start = startIndex is 0 ? 0 : mesh.subMeshesDescriptor[startIndex * 2 - 2];
				MainWindow.backendRenderer.Draw(mesh.indexStride, start, mesh.subMeshesDescriptor[endIndex * 2 - 2] - start);
			}
			//var tbo = 0;
			//MainWindow.backendRenderer.SendTexture2D(0, ref Unsafe.As<int, byte>(ref tbo));//TODO: generalize this

		}
		public void Draw(int subMesh = -1, int pass = 0)
		{
			Draw(subMesh is -1 ? .. : subMesh..(subMesh + 1), pass);
		}
		private void SendToGPU(in Shader shader, string prop, byte[] data)
		{
			if (prop is not "mesh" && shader.uniformArray.ContainsKey(prop))
				switch (data[0])
				{
					case FLOAT: MainWindow.backendRenderer.SendUniform1(shader.uniformArray[prop], ref data[1]); break;
					case VECTOR2: MainWindow.backendRenderer.SendUniformFloat2(shader.uniformArray[prop], ref data[1]); break;
					case UVECTOR2: MainWindow.backendRenderer.SendUniformUInt2(shader.uniformArray[prop], ref data[1]); break;

					case VECTOR3: MainWindow.backendRenderer.SendUniform3(shader.uniformArray[prop], ref data[1]); break;
					case COLOR4: MainWindow.backendRenderer.SendUniform4(shader.uniformArray[prop], ref data[1]); break;
					case MATRIX4X4: MainWindow.backendRenderer.SendMatrix4(shader.uniformArray[prop], ref data[1]); break;
					case TEXTURE:
						TryGetProperty(prop, out Texture tex);
						MainWindow.backendRenderer.SendTexture2D(shader.uniformArray[prop], ref Unsafe.As<int, byte>(ref tex.TBO)/*, Slot*/);
						break;
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
					//foreach (var value in localParams.Values)
					//pool.Return(value);

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
			Extension.objectToIdMapping.Remove(localParams);
			Extension.objectToIdMapping.Remove(this);
			foreach (var (_, item) in localParams)
				item.Dispose();
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion IDisposable Support
	}
}
