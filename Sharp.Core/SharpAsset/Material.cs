using PluginAbstraction;
using Sharp;
using Sharp.Core;
using SharpAsset.AssetPipeline;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
		internal const string MESHSLOT = "__mesh__";
		private const byte INVALID = 0;
		private const byte FLOAT = 1;
		private const byte VECTOR2 = 2;
		private const byte VECTOR3 = 3;
		private const byte MATRIX4X4 = 4;
		private const byte TEXTURE = 5;
		private const byte MESH = 6;
		private const byte COLOR4 = 7;
		private const byte UVECTOR2 = 8;
		private const byte MATRIX4X4PTR = byte.MaxValue;
		private const byte COLOR4PTR = byte.MaxValue - 1;

		private static int lastShaderUsed = -1;
		//TODO maybe change this to StaticDictionary and  turn BindProperty to generic otherwise change to const/readonly declarations
		//if everything fails use typeof().TypeHandle.value trick
		private static Dictionary<Type, int> sizeTable = new()
		{
			[typeof(Vector2)] = Unsafe.SizeOf<Vector2>(),
			[typeof(Vector3)] = Unsafe.SizeOf<Vector3>(),
			[typeof(Vector4)] = Unsafe.SizeOf<Vector4>(),
			[typeof(Matrix4x4)] = Unsafe.SizeOf<Matrix4x4>(),
			[typeof(Texture)] = Unsafe.SizeOf<IntPtr>(),
			[typeof(Mesh)] = Unsafe.SizeOf<int>(),
			[typeof(Color)] = Unsafe.SizeOf<Color>(),
			[typeof(IntPtr)] = Unsafe.SizeOf<IntPtr>(),
			[typeof(float)] = Unsafe.SizeOf<float>(),
			[typeof(uint)] = Unsafe.SizeOf<uint>(),
		};
		[JsonInclude]
		private int[] shadersId = Array.Empty<int>();
		private static Dictionary<(uint winId, string property), byte[]> globalParams = new Dictionary<(uint winId, string property), byte[]>();
		[JsonInclude]
		private Dictionary<string, byte[]> localParams;

		public bool IsBlendRequiredForPass(int pass)
		{
			ref var shader = ref ShaderPipeline.GetAsset(shadersId[pass]);
			return shader.dstColor is not BlendEquation.None;
		}
		//convert all binds to async binds so that they can await until tbo, ebo etc is filled
		public void BindShader(int pass, in Shader shader)
		{
			if (pass >= shadersId.Length)
				Array.Resize(ref shadersId, pass + 1);
			shadersId[pass] = ShaderPipeline.nameToKey.IndexOf(shader.Name.ToString());
			if (localParams is null)
			{
				localParams = new Dictionary<string, byte[]>();
			}
		}
		public void BindUnmanagedProperty<T>(string propName, in T data) where T : unmanaged
		{
			unsafe
			{
				IntPtr ptr = (IntPtr)Unsafe.AsPointer(ref Unsafe.AsRef(data));
				ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
				if (exists is false)
				{
					prop = new byte[sizeTable[typeof(IntPtr)] + 1];
					prop[0] = data switch
					{
						Matrix4x4 _ => MATRIX4X4PTR,
						_ => throw new NotSupportedException(typeof(T).Name)
					};
				}
				Unsafe.WriteUnaligned(ref prop[1], ptr);
			}
		}
		//change texture to use POH and include TBO in it along with bitmap 
		public void BindProperty(string propName, in Texture data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = TEXTURE;
			}
			ref var t = ref TexturePipeline.GetAsset(data.Name.ToString());
			Unsafe.WriteUnaligned(ref prop[1], t.DataAddr);
			//Unsafe.WriteUnaligned(ref prop[1], t.TBO);
		}
		public void BindProperty(string propName, in Color data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			//move these parts to shader attachment, remove constants like COLOR and sizeTable
			//and instead rely on what info shader compiler produces
			//then OrAddDefault change to OrNullRef


			//create dictionary that will hold IntPtr with items that use them probably only Materials
			//when mesh or texture gets destroyed scan dictionary and invalidate properties
			//in other words replace with default values eg point texture to white texture
			//do the same when resizing pinned arrays except dont invalidate
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = COLOR4;
			}
			Unsafe.WriteUnaligned(ref prop[1], data);
		}
		public void BindProperty(string propName, in float data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = FLOAT;
			}
			Unsafe.WriteUnaligned(ref prop[1], data);
		}
		internal void BindProperty(string propName, in Mesh data)//TODO: change to BindMesh without property name?
		{
			var i = MeshPipeline.nameToKey.IndexOf(data.Name.ToString());//TODO: change to rely on pinned heap object and intptr?
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = MESH;
			}
			Unsafe.WriteUnaligned(ref prop[1], i);
		}
		public void BindProperty(string propName, in Matrix4x4 data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = MATRIX4X4;
			}
			Unsafe.WriteUnaligned(ref prop[1], data);
		}
		public void BindProperty(string propName, in Vector3 data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = VECTOR3;
			}
			Unsafe.WriteUnaligned(ref prop[1], data);
		}
		public void BindProperty(string propName, in Vector2 data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data.GetType()] + 1];
				prop[0] = VECTOR2;
			}
			Unsafe.WriteUnaligned(ref prop[1], data);
		}
		public void BindProperty(string propName, uint[] data)
		{
			ref var prop = ref CollectionsMarshal.GetValueRefOrAddDefault(localParams, propName, out var exists);
			if (exists is false)
			{
				prop = new byte[sizeTable[data[0].GetType()] * data.Length + 1];
				if (data.Length is 2)
					prop[0] = UVECTOR2;
			}
			Unsafe.WriteUnaligned(ref prop[1], data);
		}
		public bool TryGetProperty(string prop, out Mesh data)
		{
			if (localParams.TryGetValue(prop, out var addr))
			{
				var index = Unsafe.As<byte, int>(ref addr[1]);
				data = MeshPipeline.GetAsset(index);
				return true;
			}
			data = default;
			return false;
		}
		public ref Mesh GetProperty(string prop)
		{
			if (localParams.TryGetValue(prop, out var addr))
			{
				var index = Unsafe.As<byte, int>(ref addr[1]);
				return ref MeshPipeline.GetAsset(index);
			}
			return ref Unsafe.NullRef<Mesh>();
		}
		//TODO: now bugged since TBO is stored not id of texture
		//either resort to GPU fetching texture if possible
		//or store both id and tbo for textures
		//do the same for mesh?
		public bool TryGetProperty(string prop, out Texture data)
		{

			if (localParams.TryGetValue(prop, out var addr))
			{
				data = TexturePipeline.GetAsset(Unsafe.As<byte, int>(ref addr[1]));
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
			if (!globalParams.ContainsKey((PluginManager.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = MATRIX4X4;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((PluginManager.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(PluginManager.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public static void BindGlobalProperty(string propName, in Color data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((PluginManager.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = COLOR4;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((PluginManager.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(PluginManager.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public static void BindGlobalProperty(string propName, in float data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((PluginManager.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = FLOAT;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((PluginManager.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(PluginManager.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public static void BindGlobalProperty(string propName, in Vector2 data/*, bool store = true*/)
		{
			if (!globalParams.ContainsKey((PluginManager.backendRenderer.currentWindow, propName)))
			{
				var param = new byte[sizeTable[data.GetType()] + 1];
				param[0] = VECTOR2;
				//param.DataAddress = Unsafe.As<Matrix4x4, byte>(ref data);
				Unsafe.WriteUnaligned(ref param[1], data);
				//Unsafe.CopyBlock(ref param.dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
				globalParams.Add((PluginManager.backendRenderer.currentWindow, propName), param);
			}
			else
				Unsafe.WriteUnaligned(ref globalParams[(PluginManager.backendRenderer.currentWindow, propName)][1], data);

			//Unsafe.CopyBlock(ref (globalParams[propName] as Matrix4Parameter).dataAddress, ref Unsafe.As<Matrix4x4, byte>(ref data), (uint)Unsafe.SizeOf<Matrix4x4>());
		}
		public void Draw(Range rangeOfSubMeshes, int pass = 0)
		{
			ref var shader = ref ShaderPipeline.GetAsset(shadersId[pass]);
			if (shader.IsAllocated is false) return;
			if (TryGetProperty(Material.MESHSLOT, out Mesh mesh) is false || mesh.VBO is -1)
				return;
			if (Material.lastShaderUsed != shader.Program)
			{
				PluginManager.backendRenderer.Use(shader.Program);
				lastShaderUsed = shader.Program;
			}
			if (shader.dstColor is not BlendEquation.None)
				PluginManager.backendRenderer.SetBlendState(shader.srcColor, shader.dstColor, shader.srcAlpha, shader.dstAlpha);

			/*var idLight = 0;
			if (Shader.uniformArray.ContainsKey("ambient"))
			{
				PluginManager.backendRenderer.SendUniform1(Shader.uniformArray["ambient"], ref Unsafe.As<float, byte>(ref Light.ambientCoefficient));
				foreach (var light in Light.lights)
				{
					PluginManager.backendRenderer.SendUniform3(Shader.uniformArray["lights[" + idLight + "].position"], ref Unsafe.As<float, byte>(ref light.Parent.transform.position.X));
					PluginManager.backendRenderer.SendUniform4(Shader.uniformArray["lights[" + idLight + "].color"], ref Unsafe.As<float, byte>(ref light.color.a));
					PluginManager.backendRenderer.SendUniform1(Shader.uniformArray["lights[" + idLight + "].intensity"], ref Unsafe.As<float, byte>(ref light.intensity));
					//GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["lights[" + idLight + "].angle"], light.angle);
					idLight++;
				}
			}*/


			foreach (var (key, value) in globalParams)
				if (key.winId == PluginManager.backendRenderer.currentWindow)
					SendToGPU(shader, key.property, value);

			foreach (var (key, value) in localParams)
			{
				SendToGPU(shader, key, value);
				//Console.WriteLine("test " + key);
			}
			PluginManager.backendRenderer.BindBuffers(Target.Mesh, mesh.VBO);

			PluginManager.backendRenderer.BindBuffers(Target.Indices, mesh.EBO);
			if (mesh.UsageHint is UsageHint.DynamicDraw)
			{
				PluginManager.backendRenderer.Allocate(Target.Indices, mesh.UsageHint, ref mesh.Indices[0], mesh.Indices.Length);
				PluginManager.backendRenderer.Allocate(Target.Mesh, mesh.UsageHint, ref mesh.SpanToMesh[0], mesh.SpanToMesh.Length);
			}
			foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats[mesh.VertType].attribs)
			{
				if (shader.uniformArray.TryGetValue(vertAttrib.shaderLocation, out var loc))
					PluginManager.backendRenderer.BindVertexAttrib(vertAttrib.type, loc, vertAttrib.size, Marshal.SizeOf(mesh.VertType), vertAttrib.offset);
			}
			var startIndex = rangeOfSubMeshes.Start.Value;
			var endIndex = rangeOfSubMeshes.End.Value;

			if (mesh.subMeshesDescriptor is null || (startIndex is 0 && endIndex is 0))
				PluginManager.backendRenderer.Draw(mesh.indexStride, 0, mesh.Indices.Length);
			else //if (mesh.subMeshesDescriptor is not null)
			{
				//TODO: eliminate null descriptor
				var start = startIndex is 0 ? 0 : mesh.subMeshesDescriptor[startIndex - 1];
				var end = endIndex is 0 ? mesh.subMeshesDescriptor.Length : endIndex;

				PluginManager.backendRenderer.Draw(mesh.indexStride, start, mesh.subMeshesDescriptor[end - 1] - start);
			}
			//var tbo = 0;
			//PluginManager.backendRenderer.SendTexture2D(0, ref Unsafe.As<int, byte>(ref tbo));//TODO: generalize this

		}
		public void Draw(int subMesh = -1, int pass = 0)
		{
			Draw(subMesh is -1 ? .. : subMesh..(subMesh + 1), pass);
		}
		private void SendToGPU(in Shader shader, string prop, byte[] data)
		{
			//TODO inline shader locations into data so that dict lookup is no longer needed on hot path
			if (shader.uniformArray.TryGetValue(prop,out var loc))
				switch (data[0])
				{
					case FLOAT: PluginManager.backendRenderer.SendUniform1(loc, ref data[1]); break;
					case VECTOR2: PluginManager.backendRenderer.SendUniformFloat2(loc, ref data[1]); break;
					case UVECTOR2: PluginManager.backendRenderer.SendUniformUInt2(loc, ref data[1]); break;

					case VECTOR3: PluginManager.backendRenderer.SendUniform3(loc, ref data[1]); break;
					case COLOR4: PluginManager.backendRenderer.SendUniform4(loc, ref data[1]); break;
					case MATRIX4X4: PluginManager.backendRenderer.SendMatrix4(loc, ref data[1]); break;
					case TEXTURE:
						//Console.WriteLine("TBO: "+Unsafe.As<byte,int>(ref data[1]));
						//TryGetProperty(prop, out Texture tex);
						unsafe
						{
							PluginManager.backendRenderer.SendTexture2D(loc, ref Unsafe.AsRef<byte>(Unsafe.As<byte, IntPtr>(ref data[1]).ToPointer())/*, Slot*/);
						}
						//PluginManager.backendRenderer.SendTexture2D(shader.uniformArray[prop],ref data[1]/*, Slot*/);
						break;
					case MATRIX4X4PTR: /*unsafe { PluginManager.backendRenderer.SendMatrix4(Shader.uniformArray[prop], ref Unsafe.AsRef<Matrix4x4>(Unsafe.As<byte, IntPtr>(ref data[1]).ToPointer()).M11); }*/ break;
					case MESH: break;
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
			PluginManager.serializer.objToIdMapping.Remove(localParams);
			PluginManager.serializer.objToIdMapping.Remove(this);
			foreach (var (_, item) in localParams)
				item.Dispose();
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		
		 //keep it max 64 bit + 8 bit for tag sized
	[StructLayout(LayoutKind.Explicit)]
	struct Union
	{
		[FieldOffset(0)]
		public int intOrIdField;
		[FieldOffset(0)]
		public float floatField;
		[FieldOffset(0)]
		public Vector2 vec2Field;
		/*[FieldOffset(0)]
		public int[] intArrayField;
		[FieldOffset(0)]
		public float[] floatArrayField;
		[FieldOffset(0)]
		public Vector2[] vec2ArrayField;
		[FieldOffset(0)]
		public Vector3[] vec3ArrayField;
		[FieldOffset(0)]
		public Vector4[] vec4ArrayField;
		[FieldOffset(0)]
		public unsafe void* ptr;*/

		//[FieldOffset(8)]
		//public ParameterInfo Tag;

	}
		 
		#endregion IDisposable Support
	}
}
