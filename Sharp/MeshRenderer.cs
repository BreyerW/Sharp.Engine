using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Sharp.Editor.Views;
using SharpAsset;
using System.Runtime.CompilerServices;

namespace Sharp
{
    public class MeshRenderer : Renderer //where VertexFormat : struct, IVertex
    {
        internal Mesh mesh;

        public Material material;

        public MeshRenderer(ref Mesh meshToRender, Material mat)
        {
            mesh = meshToRender;
            material = mat;
            if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(mesh.vertType))
                RegisterAsAttribute.ParseVertexFormat(mesh.vertType);
            Allocate();
        }

        private void Allocate()
        {
            MainWindow.backendRenderer.GenerateBuffers(ref mesh.VBO, ref mesh.EBO);
            MainWindow.backendRenderer.BindBuffers(ref mesh.VBO, ref mesh.EBO);
            MainWindow.backendRenderer.Allocate(ref mesh.UsageHint, ref mesh.SpanToMesh.DangerousGetPinnableReference(), ref mesh.Indices[0], mesh.SpanToMesh.Length, mesh.Indices.Length);
            material.BindProperty("model", () => ref entityObject.ModelMatrix);
            foreach (var vertAttrib in RegisterAsAttribute.registeredVertexFormats[mesh.vertType].Values)
                MainWindow.backendRenderer.BindVertexAttrib(ref vertAttrib.type, vertAttrib.shaderLocation, vertAttrib.dimension, mesh.stride, vertAttrib.offset);
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

            MainWindow.backendRenderer.Use(shader.Program);
            material.SendData();
            MainWindow.backendRenderer.Use(ref mesh.indiceType, mesh.Indices.Length);
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
    }
}