using System;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK;
using Sharp.Editor;
using System.Linq;
using System.Numerics;
using Sharp.Editor.Views;

namespace Sharp
{
	public class MeshRenderer<IndexType,VertexFormat>:Renderer where VertexFormat :struct, IVertex  where IndexType : struct,IConvertible
	{
		internal Mesh<IndexType> mesh;

		private bool allocated = false;
		public Material material=new Material();
		private static readonly VertexFormat defaultVert=default(VertexFormat);
		private static readonly IndexType defaultId=default(IndexType);

		private static DrawElementsType drawElesType = DrawElementsType.UnsignedByte;

		private static int dim;

		public event Action renderAction;
		private static int numOfUV=0;

		static readonly int Stride=Marshal.SizeOf(typeof(VertexFormat));
		private static int intPtr;

		private static int renderingStage;
		private static List<VertexAttribPointerType> selectedPointerTypes; //was Enum

		static MeshRenderer(){
			
			drawElesType = DrawElementsType.UnsignedByte;
			selectedPointerTypes = new List<VertexAttribPointerType> ();
			if (defaultId is ushort)
				drawElesType = DrawElementsType.UnsignedShort;
			else if (defaultId is uint) 
				drawElesType = DrawElementsType.UnsignedInt;
			defaultVert.RegisterAttributes<IndexType> ();
		}
		public MeshRenderer (Mesh<IndexType> meshToRender)
		{
			
			mesh = meshToRender;
		}

		private void Allocate(){
			//if (IsLoaded) return;
			//VBO
			//int tmpVBO;
			//GL.GenBuffers(1, out tmpVBO);
			//Console.WriteLine ("error check"+GL.DebugMessageCallback);
			mesh.VBO = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, mesh.VBO);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.Vertices.Count * Marshal.SizeOf(defaultVert)),mesh.Vertices.Cast<VertexFormat>().ToArray(), mesh.UsageHint);

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

			if (format.HasFlag (SupportedVertexAttributes.X) || format.HasFlag (SupportedVertexAttributes.Y) || format.HasFlag (SupportedVertexAttributes.Z))
				renderAction += BindPos;
			if (format.HasFlag (SupportedVertexAttributes.COLOR))
				renderAction += BindColor;
			if (format.HasFlag (SupportedVertexAttributes.UV)) {
				renderAction += BindUV;
			}
			if (format.HasFlag (SupportedVertexAttributes.NORMAL))
				renderAction += BindNormal;
			//IsLoaded = true;
		}
		public void SetModelMatrix(){
			mesh.ModelMatrix=Matrix4.CreateScale(entityObject.scale)*Matrix4.CreateRotationX(entityObject.rotation.X) * Matrix4.CreateRotationY(entityObject.rotation.Y) * Matrix4.CreateRotationZ(entityObject.rotation.Z) *Matrix4.CreateTranslation(entityObject.position) ;
		}
	
		public override void Render ()
		{
			if (!allocated) {
				Allocate ();
				allocated = true;
			}
			if (Camera.main.frustum.Intersect (mesh.bounds, mesh.ModelMatrix) == 0) {
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

				intPtr = 0;
				renderingStage = 0;
				renderAction ();

				int mvp_matrix_location = GL.GetUniformLocation (material.shaderId, "mvp_matrix");
				GL.UniformMatrix4 (mvp_matrix_location, false, ref mesh.MVPMatrix);
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, mesh.EBO);
				//GL.InvalidateBufferData (mesh.EBO);
				GL.DrawElements (PrimitiveType.Triangles, mesh.Indices.Length, drawElesType, IntPtr.Zero);

				GL.UseProgram (0);
			GL.PushMatrix ();
			//var tmpMat =mesh.ModelMatrix* Camera.main.modelViewMatrix * Camera.main.projectionMatrix;
			GL.LoadMatrix (ref mesh.MVPMatrix);

			DrawHelper.DrawBox (mesh.bounds.Min,mesh.bounds.Max);
			if (Selection.assets.Contains (entityObject)) {
				//float cameraObjectDistance =(Camera.main.entityObject.position-entityObject.position).Length;
				//float worldSize = (float)(2 * Math.Tan((double)(Camera.main.FieldOfView / 2.0)) * cameraObjectDistance);
				//Manipulators.gizmoScale =0.25f* worldSize;
				Manipulators.DrawTranslateGizmo ();
				DrawHelper.DrawSphere (30, 25, 25, System.Drawing.Color.Aqua);
			}
			GL.PopMatrix ();
				
		}
		public override void SetupMatrices ()
		{
			mesh.MVPMatrix =mesh.ModelMatrix*Camera.main.modelViewMatrix*Camera.main.projectionMatrix;
			int current = GL.GetInteger(GetPName.CurrentProgram);
			//GL.ValidateProgram (material.shaderId);
			//will return -1 without useprogram
			//if (current != material.shaderId) 
			//	GL.UseProgram(material.shaderId);
		}

		public static SupportedVertexAttributes format;

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
		private void BindPos(){
			var posLoc = GL.GetAttribLocation (material.shaderId, "vertex_position");
			//Console.WriteLine (material.shaderId);
			GL.EnableVertexAttribArray(posLoc);
			var pointerType = selectedPointerTypes [renderingStage];
			GL.VertexAttribPointer(posLoc, dim, pointerType, false, Stride, intPtr);

			/*GL.EnableClientState(ArrayCap.VertexArray);
			var pointerType = (VertexPointerType)selectedPointerTypes [renderingStage];
			GL.VertexPointer(dim, pointerType, Stride, 0);

			*/
			intPtr =dim*SizeInBytes.GetSizeInBytes(pointerType);
			renderingStage++;
		}

		private void BindColor(){
			var colorLoc = GL.GetAttribLocation (material.shaderId, "vertex_color");
			if (colorLoc != -1)
			GL.EnableVertexAttribArray(colorLoc);
			var pointerType =selectedPointerTypes [renderingStage];
			//if (colorLoc != -1)
			GL.VertexAttribPointer(colorLoc, 4,pointerType, false, Stride, intPtr);
			intPtr +=4* SizeInBytes.GetSizeInBytes(pointerType);
			renderingStage++;
		}

		private void BindUV(){
			var uvLoc = GL.GetAttribLocation (material.shaderId, "vertex_texcoord");
			GL.EnableVertexAttribArray(uvLoc);
			var pointerType =selectedPointerTypes [renderingStage];
			GL.VertexAttribPointer(uvLoc, 2,pointerType, false, Stride, intPtr);
			intPtr +=numOfUV+2* SizeInBytes.GetSizeInBytes(pointerType);
			renderingStage++;
		}

		private void BindNormal(){
			GL.EnableClientState(ArrayCap.NormalArray);
			var pointerType = (NormalPointerType)selectedPointerTypes [renderingStage];
			GL.NormalPointer(pointerType, Stride, intPtr);
			intPtr +=3* SizeInBytes.GetSizeInBytes(pointerType);
			renderingStage++;
		}
		public static void RegisterCustomAttribute(){
		
		}
	}
}

