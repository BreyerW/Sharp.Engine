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
		void Allocate<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
		void Allocate(ref Shader shader);
		void Use<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
		void Delete (ref Shader shader);

		void GenerateBuffers (out int ebo, out int vbo);
		void BindBuffers (int ebo,int vbo);
		void ChangeShader(int program,ref Matrix4 mvp);
		void ChangeShader();
		void BindVertexAttrib(int program,int stride, RegisterAsAttribute attrib);
		void SetupGraphic();
		void ClearBuffer();
		void Scissor(int x, int y, int width, int height);

	}
}

