using System;
using OpenTK;

namespace Sharp
{
	public class Frustum {
		
		private Vector4[] _frustum;

		public Frustum(Matrix4 vp) {
			Update (vp);
		}

		public void Update(Matrix4 vp) {
			_frustum = new[] {
				new Vector4(vp.M14 + vp.M11, vp.M24 + vp.M21, vp.M34 + vp.M31, vp.M44 + vp.M41),
				new Vector4(vp.M14 - vp.M11, vp.M24 - vp.M21, vp.M34 - vp.M31, vp.M44 - vp.M41),
				new Vector4(vp.M14 - vp.M12, vp.M24 - vp.M22, vp.M34 - vp.M32, vp.M44 - vp.M42),
				new Vector4(vp.M14 + vp.M12, vp.M24 + vp.M22, vp.M34 + vp.M32, vp.M44 + vp.M42),
				new Vector4(vp.M13, vp.M23, vp.M33, vp.M43),
				new Vector4(vp.M14 - vp.M13, vp.M24 - vp.M23, vp.M34 - vp.M33, vp.M44 - vp.M43)
			};
			foreach (var plane in _frustum) {
				plane.Normalize();
			}
		}
		// Return values: 0 = no intersection, 
		//                1 = intersection, 
		//                2 = box is completely inside frustum
		//fix object bigger than frustum case
		public int Intersect(BoundingBox box, Matrix4 modelMatrix) {
			int result = 2;

			for( uint i = 0; i < 6; i++ )
			{
				float pos = _frustum[i].W;
				var normal = new Vector3(_frustum[i]);

				if( Vector3.Dot(normal,box.getPositiveVertex(normal, modelMatrix))+pos < 0.0f )
				{
					return 0;
				}

				if( Vector3.Dot(normal, box.getNegativeVertex(normal, modelMatrix))+pos < 0.0f )
				{
					result = 1;
				}
			}

			return result;
		}

	}
}

