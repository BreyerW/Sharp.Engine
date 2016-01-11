﻿using System;
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
		protected static readonly int sizeOfId=Marshal.SizeOf<IndexType>();	

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
			SceneView.backendRenderer.GenerateBuffers (out mesh.EBO, out mesh.VBO);

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
				SceneView.backendRenderer.ChangeShader(material.shaderId, ref entityObject.MVPMatrix);
				SceneView.backendRenderer.BindBuffers(mesh.EBO, mesh.VBO);

			foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats [mesh.Vertices[0].GetType()].Values)
				SceneView.backendRenderer.BindVertexAttrib (material.shaderId,stride,vertAttrib);

			SceneView.backendRenderer.Use (ref mesh);
			SceneView.backendRenderer.ChangeShader();
		}
		public override void SetupMatrices ()
		{
			entityObject.MVPMatrix =entityObject.ModelMatrix*Camera.main.modelViewMatrix*Camera.main.projectionMatrix;
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

