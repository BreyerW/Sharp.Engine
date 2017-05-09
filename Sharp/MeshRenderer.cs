using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Sharp.Editor.Views;
using SharpAsset;

namespace Sharp
{
    public class MeshRenderer<IndexType> : Renderer where IndexType : struct, IConvertible //where VertexFormat : struct, IVertex
    {
        internal Mesh<IndexType> mesh;
        protected static readonly int sizeOfId = Marshal.SizeOf(typeof(IndexType));

        public Material material;

        public MeshRenderer(IAsset meshToRender, Material mat)
        {
            mesh = (Mesh<IndexType>)meshToRender;
            material = mat;
            if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(mesh.vertType))
                RegisterAsAttribute.ParseVertexFormat(mesh.vertType);
            Allocate();
        }

        private void Allocate()
        {
            MainWindow.backendRenderer.GenerateBuffers(ref mesh);
            MainWindow.backendRenderer.BindBuffers(ref mesh);
            MainWindow.backendRenderer.Allocate(ref mesh);
            material.BindProperty("model", () => { return ref entityObject.ModelMatrix; });
        }

        public override void Render()
        {
            if (Camera.main.frustum.Intersect(mesh.bounds, entityObject.ModelMatrix) == 0)
            {
                //Console.WriteLine("cull");
                return;
            }
            //Console.WriteLine ("no-cull ");

            //int current = GL.GetInteger (GetPName.CurrentProgram);
            //GL.ValidateProgram (material.shaderId);
            //if (current != material.shaderId) {
            //}
            //if (!IsLoaded) return;
            var shader = material.Shader;

            MainWindow.backendRenderer.Use(ref shader);
            MainWindow.backendRenderer.BindBuffers(ref material);

            MainWindow.backendRenderer.BindBuffers(ref mesh);

            foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats[mesh.vertType].Values)
                MainWindow.backendRenderer.BindVertexAttrib(mesh.stride, vertAttrib);

            MainWindow.backendRenderer.Use(ref mesh);
            MainWindow.backendRenderer.ChangeShader();
        }

        public override void SetupMatrices()
        {
            //int current = GL.GetInteger(GetPName.CurrentProgram);
            //GL.ValidateProgram (material.shaderId);
            //will return -1 without useprogram
            //if (current != material.shaderId)
            //	GL.UseProgram(material.shaderId);
        }

        public static void RegisterCustomAttribute()
        {
        }
    }
}