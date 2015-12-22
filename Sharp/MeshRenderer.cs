using System;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK;
using Sharp.Editor;
using System.Linq;
using System.Numerics;
using Sharp.Editor.Views;
using System.Reflection;

namespace Sharp
{
	/*public class MeshRenderer<IndexType>: Renderer where IndexType : struct,IConvertible
	{
		

		public MeshRenderer (Mesh<IndexType> meshToRender)
		{
			mesh = meshToRender;
		}
		#region implemented abstract members of Renderer
		public override void Render ()
		{
		}
		public override void SetupMatrices ()
		{
		}
		#endregion
		public static void RegisterAttribute(SupportedVertexAttributes attribute, VertexAttribPointerType pointerType){
			format |= attribute;
			selectedPointerTypes.Add(pointerType);
			if (attribute.HasFlag (SupportedVertexAttributes.X) || attribute.HasFlag (SupportedVertexAttributes.Y) || attribute.HasFlag (SupportedVertexAttributes.Z)) {
				if (attribute.HasFlag (SupportedVertexAttributes.X))
					dim++;
				if (attribute.HasFlag (SupportedVertexAttributes.Y))
					dim++;
				if (attribute.HasFlag (SupportedVertexAttributes.Z))
					dim++;
			} 

			//renderingStage++;
		}
	}*/
	public class MeshRenderer<IndexType,VertexFormat>: Renderer where IndexType : struct,IConvertible where VertexFormat : struct, IVertex
	{
		internal Mesh<IndexType> mesh;
		protected static readonly IndexType defaultId=default(IndexType);	
		protected static int dim=1;

		public Material material=new Material();

		private static readonly VertexFormat defaultVert=default(VertexFormat);

		private static DrawElementsType drawElesType = DrawElementsType.UnsignedByte;

		public static event Action renderAction;

		private static int numOfUV=0;

		static MeshRenderer(){
			
			drawElesType = DrawElementsType.UnsignedByte;
			if (defaultId is ushort)
				drawElesType = DrawElementsType.UnsignedShort;
			else if (defaultId is uint) 
				drawElesType = DrawElementsType.UnsignedInt;
		}
		public MeshRenderer (IAsset meshToRender)
		{
			mesh = (Mesh<IndexType>)meshToRender;
			var type=typeof(VertexFormat);

			if(!RegisterAsAttribute.registeredVertexFormats.ContainsKey(type)){
				var fields=type.GetFields ().Where (
					p => p.GetCustomAttribute<RegisterAsAttribute>()!=null);
				int? lastFormat=null;
				var vertFormat = new Dictionary<VertexAttribute,RegisterAsAttribute> ();

				foreach(var field in fields){
					var attrib=field.GetCustomAttribute<RegisterAsAttribute>();
					if (lastFormat != (int)attrib.format) {

						lastFormat = (int)attrib.format;
						attrib.offset = Marshal.OffsetOf<VertexFormat> (field.Name);
						vertFormat.Add (attrib.format,attrib);
						if (attrib.format.HasFlag (VertexAttribute.POSITION))
							renderAction += BindPos;
						if (attrib.format.HasFlag (VertexAttribute.COLOR))
							renderAction += BindColor;
						if (attrib.format.HasFlag (VertexAttribute.UV)) {
							renderAction += BindUV;
						}
						if (attrib.format.HasFlag (VertexAttribute.NORMAL))
							renderAction += BindNormal;
					} else if (attrib.format == VertexAttribute.POSITION)
						dim++; //error prone
					else if (attrib.format == VertexAttribute.UV)
						numOfUV++;
				}
				RegisterAsAttribute.registeredVertexFormats.Add (type,vertFormat);
			}
				Allocate ();
		}
		private void Allocate(){
			//if (IsLoaded) return;
			//VBO
			//int tmpVBO;
			//GL.GenBuffers(1, out tmpVBO);
			//Console.WriteLine ("error check"+GL.DebugMessageCallback);
			mesh.VBO = GL.GenBuffer();

			var watch =System.Diagnostics.Stopwatch.StartNew();
			var verts = mesh.Vertices.Cast<VertexFormat>().ToArray();
			GL.BindBuffer (BufferTarget.ArrayBuffer, mesh.VBO);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.Vertices.Length * defaultVert.Stride),ref verts[0], mesh.UsageHint);

			watch.Stop();
			Console.WriteLine("cast: "+ watch.ElapsedTicks);
			//int tmpEBO;
			//GL.GenBuffers(1, out tmpEBO);
			mesh.EBO =GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.EBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mesh.Indices.Length * Marshal.SizeOf(defaultId)), mesh.Indices, mesh.UsageHint);

		//GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			//GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			/*// Generate Array Buffer Id
			mesh.TBO=GL.GenBuffer();

			// Bind current context to Array Buffer ID
			GL.BindBuffer(BufferTarget.ArrayBuffer, mesh.TBO);

			// Send data to buffer
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.Vertices.Count * 8), shape.Texcoords, BufferUsageHint.StaticDraw);
*/
			// Validate that the buffer is the correct size
			//GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
			//if (shape.Texcoords.Length * 8 != bufferSize)
			//	throw new ApplicationException("TexCoord array not uploaded correctly");

			// Clear the buffer Binding
			//GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


			//IsLoaded = true;
		}

	
		public override void Render ()
		{
			
			if (Camera.main.frustum.Intersect (mesh.bounds,entityObject.ModelMatrix) == 0) {
				//Console.WriteLine ("cull");
				return;
			}
			//Console.WriteLine ("no-cull ");

				int current = GL.GetInteger (GetPName.CurrentProgram);
				//GL.ValidateProgram (material.shaderId);
				//if (current != material.shaderId) {

				GL.UseProgram (material.shaderId);
				//}
				//if (!IsLoaded) return;
				GL.BindBuffer (BufferTarget.ArrayBuffer, mesh.VBO);
			renderAction?.Invoke ();

				int mvp_matrix_location = GL.GetUniformLocation (material.shaderId, "mvp_matrix");
			GL.UniformMatrix4 (mvp_matrix_location, false, ref entityObject.MVPMatrix);
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, mesh.EBO);
				//GL.InvalidateBufferData (mesh.EBO);
				GL.DrawElements (PrimitiveType.Triangles, mesh.Indices.Length, drawElesType, IntPtr.Zero);

				GL.UseProgram (0);
		}
		public override void SetupMatrices ()
		{
			entityObject.MVPMatrix =entityObject.ModelMatrix*Camera.main.modelViewMatrix*Camera.main.projectionMatrix;
			int current = GL.GetInteger(GetPName.CurrentProgram);
			//GL.ValidateProgram (material.shaderId);
			//will return -1 without useprogram
			//if (current != material.shaderId) 
			//	GL.UseProgram(material.shaderId);
		}

		private void BindPos(){
			var posLoc = GL.GetAttribLocation (material.shaderId, "vertex_position");
			//Console.WriteLine (material.shaderId);
			GL.EnableVertexAttribArray(posLoc);
			var attrib=RegisterAsAttribute.registeredVertexFormats[defaultVert.GetType()][VertexAttribute.POSITION];
			GL.VertexAttribPointer(posLoc,dim, attrib.type, false,defaultVert.Stride, attrib.offset);
			}

		private void BindColor(){
			var colorLoc = GL.GetAttribLocation (material.shaderId, "vertex_color");
			if (colorLoc != -1)
			GL.EnableVertexAttribArray(colorLoc);
			var attrib=RegisterAsAttribute.registeredVertexFormats[defaultVert.GetType()][VertexAttribute.COLOR];
			//if (colorLoc != -1)
			GL.VertexAttribPointer(colorLoc, 4,attrib.type, false, defaultVert.Stride,attrib.offset);
			}

		private void BindUV(){
			var uvLoc = GL.GetAttribLocation (material.shaderId, "vertex_texcoord");
			GL.EnableVertexAttribArray(uvLoc);
			var attrib=RegisterAsAttribute.registeredVertexFormats[defaultVert.GetType()][VertexAttribute.UV];
			GL.VertexAttribPointer(uvLoc, 2,attrib.type, false, defaultVert.Stride,attrib.offset);
			}

		private void BindNormal(){
			GL.EnableClientState(ArrayCap.NormalArray);
			var attrib=RegisterAsAttribute.registeredVertexFormats[defaultVert.GetType()][VertexAttribute.NORMAL];
			//GL.NormalPointer(attrib.type, defaultVert.Stride, attrib.offset);
			}
		public static void RegisterCustomAttribute(){
		
		}
	}
}

