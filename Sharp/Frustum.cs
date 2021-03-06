﻿using System;
using OpenTK;
using SharpAsset;

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
	/*	public static Frustum UnprojectRectangle(System.Drawing.Rectangle source, int width,int height)
		{
			// Transform rectangle into projection space. 
			float x1 = source.Left; 
			float y1 = source.Top; 
			float x2 = source.Right; 
			float y2 = source.Bottom; 

			x1 /= (float)viewport.Width; 
			y1 /= (float)viewport.Height; 
			x2 /= (float)viewport.Width; 
			y2 /= (float)viewport.Height; 
			x1 = x1 * 2 - 1; 
			y1 = -(y1 * 2 - 1); 
			x2 = x2 * 2 - 1; 
			y2 = -(y2 * 2 - 1); 

			// Unproject the projection space rectangle into the view space. 
			Vector4 unproj = new Vector4(x1, y1, 0, 1); 
			unproj = Vector4.Transform(unproj, invertProjection); 
			unproj /= unproj.W; 

			x1 = unproj.X; 
			y1 = unproj.Y; 

			unproj = new Vector4(x2, y2, 0, 1); 
			unproj = Vector4.Transform(unproj, invertProjection); 
			unproj /= unproj.W; 

			x2 = unproj.X; 
			y2 = unproj.Y; 

			// Build the new projection matrix. 
			Matrix4 regionProjMatrix = Matrix4.CreateOrthographicOffCenter(x1, x2, y2, y1, Camera.main.ZNear, farPlz); 

			return new Frustum(Camera.main.modelViewMatrix * regionProjMatrix);
		}*/
		//http://stackoverflow.com/questions/28155749/opengl-matrix-setup-for-tiled-rendering
		public static Matrix4 TileFrustum(float tileX,float tileY,float placeInXRow, float placeInYRow){
			var tx = -(-1 + 2f / (2f * tileX) + (2f / tileX) * placeInXRow);
			var	ty= - (-1 + 2f/(2f*tileY) + (2f/tileY) * placeInYRow);
			return Matrix4.CreateScale (tileX,tileY,1f)*Matrix4.CreateTranslation(tx,ty,0)*Camera.main.projectionMatrix;
		}
		public static void Perspective(float fovy,float aspect, float zNear, float zFar)
		{
			float xmin, xmax, ymin, ymax;

			ymax = zNear * (float)Math.Tan ((double)(fovy * (float)Math.PI / 360.0f)); // M_PI / 360.0 == DEG_TO_RAD
			ymin = -ymax;

			xmin = ymin * aspect;
			xmax = ymax * aspect;
		//glFrustum(xmin, xmax, ymin, ymax, zNear, zFar);
		}
	}
}

