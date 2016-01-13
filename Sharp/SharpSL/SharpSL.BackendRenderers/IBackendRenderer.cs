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
		void Allocate (ref Texture tex);
		void Use<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
		void Use(ref Shader shader);
		void Delete (ref Shader shader);

		void GenerateBuffers<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
		void GenerateBuffers (ref Texture tex);
		void GenerateBuffers (ref Shader shader);
		void BindBuffers<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
		void BindBuffers (ref Texture tex);
		void BindBuffers (ref Material mat);
		void ChangeShader();
		void BindVertexAttrib(int stride, RegisterAsAttribute attrib);
		void SetupGraphic();
		void ClearBuffer();
		void Scissor(int x, int y, int width, int height);

	}
}

