using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Sharp.Editor.Views;
using SharpAsset;

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
		protected static readonly int sizeOfId=Marshal.SizeOf(typeof(IndexType));	

		public Material material=new Material();

		private static int stride;

		//public static VertexAttribute[] vertAttribs; //convert to cache type


		public MeshRenderer (IAsset meshToRender)
		{
			mesh = (Mesh<IndexType>)meshToRender;
			var type=mesh.Vertices[0].GetType();
			stride = Marshal.SizeOf (type);

			if(!RegisterAsAttribute.registeredVertexFormats.ContainsKey(type))
				RegisterAsAttribute.ParseVertexFormat (type);

			Allocate ();
		}

		private void Allocate(){
			SceneView.backendRenderer.GenerateBuffers (ref mesh);
			SceneView.backendRenderer.BindBuffers (ref mesh);
			SceneView.backendRenderer.Allocate (ref mesh);
		}
        public override void Render ()
		{
			
			if (Camera.main.frustum.Intersect (mesh.bounds,entityObject.ModelMatrix) == 0) {
				//Console.WriteLine ("cull");
				return;
			}
			//Console.WriteLine ("no-cull ");

				//int current = GL.GetInteger (GetPName.CurrentProgram);
				//GL.ValidateProgram (material.shaderId);
				//if (current != material.shaderId) {

				
				//}
				//if (!IsLoaded) return;
			var shader = material.Shader;

            SceneView.backendRenderer.Use(ref shader);
			SceneView.backendRenderer.BindBuffers(ref material);

			SceneView.backendRenderer.BindBuffers(ref mesh);

            foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats [mesh.Vertices[0].GetType()].Values)
				SceneView.backendRenderer.BindVertexAttrib (stride,vertAttrib);
			
			SceneView.backendRenderer.Use (ref mesh);
			SceneView.backendRenderer.ChangeShader();
		}
		public override void SetupMatrices ()
		{
			material.SetGlobalProperty ("camView", ref Camera.main.modelViewMatrix);
			material.SetGlobalProperty ("camProjection", ref Camera.main.projectionMatrix);
            material.SetProperty("model", ref entityObject.ModelMatrix);
            //int current = GL.GetInteger(GetPName.CurrentProgram);
            //GL.ValidateProgram (material.shaderId);
            //will return -1 without useprogram
            //if (current != material.shaderId) 
            //	GL.UseProgram(material.shaderId);
        }

		public static void RegisterCustomAttribute(){
		
		}
	}
}

