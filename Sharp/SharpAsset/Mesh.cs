using Sharp;
using SharpAsset.Pipeline;
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

		public Type VertType {
			set {
				if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(value))
					RegisterAsAttribute.ParseVertexFormat(value);
				MainWindow.backendRenderer.GenerateBuffers(ref VBO, ref EBO);
				MainWindow.backendRenderer.BindBuffers(ref VBO, ref EBO);
				MainWindow.backendRenderer.Allocate(ref UsageHint, ref SpanToMesh[0], ref Indices[0], SpanToMesh.Length, Indices.Length);

				foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats[value].Values)
					MainWindow.backendRenderer.BindVertexAttrib(ref vertAttrib.type, vertAttrib.shaderLocation, vertAttrib.dimension, stride, vertAttrib.offset);
			}
		}
		public IndiceType indiceType;

		public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public string Extension { get { return Path.GetExtension(FullPath); } set { } }
		public string FullPath { get; set; }
		public UsageHint UsageHint;

		public byte[] Indices;
		public byte[] verts;

		public BoundingBox bounds;

		internal static Dictionary<string, byte[]> sharedMeshes = new Dictionary<string, byte[]>();

		internal int VBO;
		internal int EBO;

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
			return ref Unsafe.As<byte, T>(ref SpanToMesh[index * stride]); //TODO: use Unsafe.Write to avoid unaligned issue ?
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

		public void PlaceIntoScene(Entity context, Vector3 worldPos)//TODO: wyrzucic to do kodu edytora PlaceIntoView(View view,)
		{
			var eObject = new Entity();
			eObject.transform.Position = worldPos;


			var renderer = eObject.AddComponent<MeshRenderer>();
			Pipeline.Pipeline.Get<TexturePipeline>().Import(@"B:\Sharp.Engine3\Sharp\bin\Debug\Content\duckCM.bmp");
			//zamienic na ref loading pipeliny
			renderer.Mesh = this;
			renderer.material.BindProperty("MyTexture", ref Pipeline.Pipeline.Get<TexturePipeline>().GetAsset("duckCM"));
			if (context != null) //make as child of context?
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