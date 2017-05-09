using System;
using OpenTK;
using System.Drawing;
using SharpAsset;

namespace SharpSL.BackendRenderers
{
    public interface IBackendRenderer
    {
        //void Do(Work whatToDo, ref Shader shader);
        //void Do<IndexType> (Work whatToDo,ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
        void Allocate<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible;

        void Allocate(ref Shader shader);

        void Allocate(ref Texture tex);

        void Use<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible;

        void Use(ref Shader shader);

        void Delete(ref Shader shader);

        void Send(ref int location, ref Matrix4 mat);

        //void Send(ref int location,ref int[] i);
        void Send(ref int location, ref int tbo, int slot);

        void GenerateBuffers<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible;

        void GenerateBuffers(ref Texture tex);

        void GenerateBuffers(ref Shader shader);

        void BindBuffers<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible;

        void BindBuffers(ref Texture tex);

        void BindBuffers(ref Material mat);

        void ChangeShader();

        void BindVertexAttrib(int stride, RegisterAsAttribute attrib);

        void SetupGraphic();

        void ClearBuffer();

        void ClearColor();

        void ClearColor(float r, float b, float g, float a);

        void Scissor(int x, int y, int width, int height);

        void SetStandardState();

        void SetFlatColorState();

        void SaveState();

        void RestoreState();

        void FinishCommands();

        byte[] ReadPixels(int x, int y, int width, int height);
    }
}