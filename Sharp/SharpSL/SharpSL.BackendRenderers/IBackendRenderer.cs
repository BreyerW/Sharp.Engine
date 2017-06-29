using System;
using System.Collections.Generic;
using SharpAsset;

namespace SharpSL.BackendRenderers
{
    public interface IBackendRenderer
    {
        void CreateContext(Func<string, IntPtr> GetProcAddress, Func<IntPtr> GetCurrentContext);

        //void Do(Work whatToDo, ref Shader shader);
        //void Do<IndexType> (Work whatToDo,ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
        void Allocate(ref UsageHint usageHint, ref byte vertsMemAddr, ref byte indicesMemAddr, int vertsMemLength, int indicesMemLength);

        void Allocate(ref int Program, ref int VertexID, ref int FragmentID, ref string VertexSource, ref string FragmentSource, ref Dictionary<string, int> uniformArray);

        void Allocate(ref byte bitmap, int width, int height, bool ui = false);

        void Use(ref IndiceType indiceType, int length);

        void Use(int Program);

        void Delete(ref int Program, ref int VertexID, ref int FragmentID);

        void SendMatrix4(int location, ref float mat);

        //void Send(ref int location,ref int[] i);
        void SendTexture2D(int location, ref int tbo/*, int slot*/);

        void SendUniform1(int location, ref float data);

        void SendUniform2(int location, ref float data);

        void SendUniform3(int location, ref float data);

        void SendUniform4(int location, ref float data);

        void GenerateBuffers(ref int VBO, ref int EBO);

        void GenerateBuffers(ref int TBO);

        void GenerateBuffers(ref int Program, ref int VertexID, ref int FragmentID);

        void BindBuffers(ref int VBO, ref int EBO);

        void BindBuffers(ref int TBO);

        void ChangeShader();

        void BindVertexAttrib(ref AttributeType type, int shaderLoc, int dim, int stride, int offset);//vertextype and IndicesType move to SL

        void SetupGraphic();

        void ClearBuffer();

        void ClearColor();

        void ClearColor(float r, float b, float g, float a);

        void ClearDepth();

        void EnableScissor();

        void Viewport(int x, int y, int width, int height);

        void SetStandardState();

        void SetFlatColorState();

        void WriteDepth(bool enable = true);

        void Clip(int x, int y, int width, int height);

        void FinishCommands();

        byte[] ReadPixels(int x, int y, int width, int height);
    }
}