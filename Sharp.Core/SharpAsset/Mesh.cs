using Sharp;
using SharpAsset.Pipeline;
using SharpSL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpAsset
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct Mesh : IAsset
	{
		public int stride;//talk to vertex format to be able to query exact attribute

		public Type VertType //TODO: make LoadVertices<T> where T: struct, IVertex on Mesh and here set up VertType same for indices LoadIndices(); 
		{
			internal set
			{
				if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(value))
					RegisterAsAttribute.ParseVertexFormat(value);
				vertsType = value;

			}
			get => vertsType;
		}
		public IndiceType indiceType;
		public Type vertsType;
		public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } }
		public string Extension { get { return Path.GetExtension(FullPath); } }
		public string FullPath { get; set; }
		public UsageHint UsageHint; //TODO: make enum flag with dynamic/static size & attributes static size but dynamic attrib mean unchanging indices but changing vertexes themselves

		internal byte[] Indices;
		internal byte[] verts;

		public BoundingBox bounds;

		internal static Dictionary<string, byte[]> sharedMeshes = new Dictionary<string, byte[]>();

		internal int VBO;
		internal int EBO;
		//internal bool needUpdate;

		public Span<byte> SpanToMesh
		{
			get
			{
				return verts is null ? SpanToSharedMesh : verts.AsSpan();
			}
		}

		public Span<byte> SpanToSharedMesh
		{
			get
			{
				return sharedMeshes[Name].AsSpan();
			}
		}

		public Span<byte> SpanToIndices
		{
			get
			{
				return Indices.AsSpan();
			}
		}

		public bool isMeshShared
		{
			get { return verts is null; }
			set
			{
				verts = value ? null : SpanToMesh.ToArray();
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public ref T ReadVertexAtIndex<T>(int index) where T : struct, IVertex
		{
			return ref Unsafe.As<byte, T>(ref verts[index * stride]);
		}

		//public TExpected ReadVertexAttributeAtIndex<TExpected>(VertexAttribute attrib,int layer, int index) where TExpected : struct {
		//consult vertex format in order to read only attributes
		//}
		public Span<TTo> SnapshotConvertToFormat<TTo>() where TTo : struct, IVertex //PermamentConvertToFormat
		{
			var size = Unsafe.SizeOf<TTo>();
			//if (size == stride)
			//    return
			var tmpSpan = new Span<TTo>(new TTo[SpanToMesh.Length]);//danger! copy instead of reference. Change it so that unamaged memory is resized and this new span point to resized memory?

			var condition = size > stride;
			for (int i = 0; i < SpanToMesh.Length; i++)
			{
				tmpSpan[i] = Unsafe.As<byte, TTo>(ref SpanToMesh.Slice(i * stride, condition ? stride : size)[0]);//check for SpanExiensions.Read/Write use Unsafe.Write to avoid unaligned issue?
			}
			return tmpSpan;
			//Unsafe.As<byte[], TTo[]>(ref verts);
		}
		public void LoadVertices(byte[] vertices)
		{
			verts = vertices;
			// = true;
		}
		public void LoadIndices(byte[] indices)
		{
			Indices = indices;
			//needUpdate = true;
		}
		public void LoadVertices<T>(Span<T> vertices) where T : struct, IVertex
		{
			stride = Unsafe.SizeOf<T>();
			VertType = typeof(T);
			verts = MemoryMarshal.AsBytes(vertices).ToArray();
			//needUpdate = true;
		}
		public void LoadIndices<T>(Span<T> indices) where T : struct
		{
			if (typeof(T) == typeof(ushort))
				indiceType = IndiceType.UnsignedShort;
			else if (typeof(T) == typeof(uint))
				indiceType = IndiceType.UnsignedInt;
			else if (typeof(T) == typeof(sbyte))
				indiceType = IndiceType.UnsignedByte;
			Indices = MemoryMarshal.AsBytes(indices).ToArray();
			//needUpdate = true;
		}
		public void PlaceIntoScene(Entity context, Vector3 worldPos)//TODO: wyrzucic to do kodu edytora PlaceIntoView(View view,)
		{
			var eObject = new Entity();
			eObject.transform.Position = worldPos;
			var angles = eObject.transform.Rotation * NumericsExtensions.Deg2Rad;
			eObject.transform.ModelMatrix = Matrix4x4.CreateScale(eObject.transform.Scale) * Matrix4x4.CreateFromYawPitchRoll(angles.Y,angles.X,angles.Z) * Matrix4x4.CreateTranslation(eObject.transform.Position);
			var renderer = eObject.AddComponent<MeshRenderer>();
			var shader = (Shader)Pipeline.Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\TextureOnlyShader.shader");
			renderer.material = new Material();
			renderer.material.Shader = shader;
			var texture = (Texture)Pipeline.Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\duckCM.bmp");
			//zamienic na ref loading pipeliny
			renderer.material.BindProperty("mesh", this);
			renderer.material.BindProperty("MyTexture", texture);
			if (context is not null) //make as child of context?
			{
			}

		}
	}

	public enum UsageHint
	{
		StreamDraw = 35040,
		StreamRead,
		StreamCopy,
		StaticDraw = 35044,
		StaticRead,
		StaticCopy,
		DynamicDraw = 35048,
		DynamicRead,
		DynamicCopy
	}

	public enum IndiceType
	{
		UnsignedByte = 5121,
		UnsignedShort = 5123,
		UnsignedInt = 5125
	}

	public class SafePointer : SafeHandle
	{
		public SafePointer(IntPtr invalidHandleValue) : base(invalidHandleValue, true)
		{
			SetHandle(invalidHandleValue);
		}

		public override bool IsInvalid
		{
			[System.Security.SecurityCritical]
			get { return handle == new IntPtr(-1); }
		}

		protected override bool ReleaseHandle()
		{
			Marshal.FreeHGlobal(handle);
			return true;
		}
	}
}