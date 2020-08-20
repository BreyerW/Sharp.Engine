using Sharp;
using Sharp.Editor;
using SharpAsset;
using System;
using System.Collections.Generic;
using System.Numerics;

public static class CreatePrimitiveMesh
{

	public static int numVertices = 10;
	public static float radiusTop = 0.5f;
	public static float radiusBottom = 0.5f;
	public static float length = 1f;
	public static bool outside = true;
	public static bool inside = false;

	public static Mesh GenerateCylinder(string meshName = "cylinder")
	{
		Vector3 end_point = Vector3.Zero;
		Vector3 axis = Vector3.UnitX;
		Vector3 v1;
		if ((axis.Z < -0.01) || (axis.Z > 0.01))
			v1 = new Vector3(axis.Z, axis.Z, -axis.X - axis.Y);
		else
			v1 = new Vector3(-axis.Y - axis.Z, axis.X, axis.X);
		Vector3 v2 = Vector3.Cross(v1, axis);

		// Make the vectors have length radius.
		var vTop1 = v1 * (radiusTop / v1.Length());
		var vTop2 = v2 * (radiusTop / v2.Length());
		var vBottom1 = v1 * (radiusBottom / v1.Length());
		var vBottom2 = v2 * (radiusBottom / v2.Length());
		// Make the top end cap.
		// Make the end point.
		var vertices = new List<UIVertexFormat>();
		ushort pt0 = (ushort)vertices.Count; // Index of end_point.
		vertices.Add(new UIVertexFormat() { position = end_point });
		var indices = new List<ushort>();
		float theta = 0;
		float dtheta = 2 * MathF.PI / numVertices;
		ushort pt1 = 0;
		ushort pt2 = 0;
		// Make the top points.
		if (radiusTop > float.Epsilon)
		{
			for (int i = 0; i < numVertices; i++)
			{
				vertices.Add(new UIVertexFormat()
				{
					position = end_point +
					MathF.Cos(theta) * vTop1 +
					MathF.Sin(theta) * vTop2
				});
				theta += dtheta;
			}

			// Make the top triangles.
			pt1 = (ushort)(vertices.Count - 1); // Index of last point.
			pt2 = (ushort)(pt0 + 1);                  // Index of first point.
			for (int i = 0; i < numVertices; i++)
			{
				indices.Add(pt0);
				indices.Add(pt1);
				indices.Add(pt2);
				pt1 = pt2++;
			}
		}
		// Make the bottom end cap.
		// Make the end point.
		pt0 = (ushort)vertices.Count; // Index of end_point2.
		Vector3 end_point2 = end_point + axis;
		vertices.Add(new UIVertexFormat()
		{
			position = end_point2
		});

		// Make the bottom points.
		if (radiusBottom > float.Epsilon)
		{
			theta = 0;
			for (int i = 0; i < numVertices; i++)
			{
				vertices.Add(new UIVertexFormat()
				{
					position = end_point2 +
					MathF.Cos(theta) * vBottom1 +
					MathF.Sin(theta) * vBottom2
				});
				theta += dtheta;
			}

			// Make the bottom triangles.
			theta = 0;
			pt1 = (ushort)(vertices.Count - 1); // Index of last point.
			pt2 = (ushort)(pt0 + 1);                  // Index of first point.
			for (int i = 0; i < numVertices; i++)
			{
				indices.Add((ushort)(numVertices + 1));    // end_point2
				indices.Add(pt2);
				indices.Add(pt1);
				pt1 = pt2++;
			}
		}

		// Make the sides.
		// Add the points to the mesh.
		ushort first_side_point = (ushort)vertices.Count;
		theta = 0;
		for (int i = 0; i < numVertices; i++)
		{
			Vector3 p1 = end_point +
				MathF.Cos(theta) * vTop1 +
				MathF.Sin(theta) * vTop2;
			vertices.Add(new UIVertexFormat()
			{
				position = p1
			});
			Vector3 p2 = end_point2 +
				MathF.Cos(theta) * vBottom1 +
				MathF.Sin(theta) * vBottom2;
			vertices.Add(new UIVertexFormat()
			{
				position = p2
			});
			theta += dtheta;
		}

		// Make the side triangles.
		pt1 = (ushort)(vertices.Count - 2);
		pt2 = (ushort)(pt1 + 1);
		ushort pt3 = first_side_point;
		ushort pt4 = (ushort)(pt3 + 1);
		for (int i = 0; i < numVertices; i++)
		{
			indices.Add(pt1);
			indices.Add(pt2);
			indices.Add(pt4);

			indices.Add(pt1);
			indices.Add(pt4);
			indices.Add(pt3);

			pt1 = pt3;
			pt3 += 2;
			pt2 = pt4;
			pt4 += 2;
		}
		// can't access Camera.current
		//newCone.transform.position = Camera.current.transform.position + Camera.current.transform.forward * 5.0f;
		/*	int multiplier = (outside ? 1 : 0) + (inside ? 1 : 0);
			int offset = (outside && inside ? 2 * numVertices : 0);
			UIVertexFormat[] vertices = new UIVertexFormat[2 * multiplier * numVertices]; // 0..n-1: top, n..2n-1: bottom
			Vector3[] normals = new Vector3[2 * multiplier * numVertices];
			ushort[] tris;
			float slope = MathF.Atan((radiusBottom - radiusTop) / length); // (rad difference)/height
			float slopeSin = MathF.Sin(slope);
			float slopeCos = MathF.Cos(slope);
			int i;
			for (i = 0; i < numVertices; i++)
			{
				float angle = 2 * MathF.PI * i / numVertices;
				float angleSin = MathF.Sin(angle);
				float angleCos = MathF.Cos(angle);
				float angleHalf = 2 * MathF.PI * (i + 0.5f) / numVertices; // for degenerated normals at cone tips
				float angleHalfSin = MathF.Sin(angleHalf);
				float angleHalfCos = MathF.Cos(angleHalf);

				vertices[i].position = new Vector3(radiusTop * angleCos, radiusTop * angleSin, 0);
				vertices[i + numVertices].position = new Vector3(radiusBottom * angleCos, radiusBottom * angleSin, length);

				/*	if (radiusTop == 0)
						normals[i] = new Vector3(angleHalfCos * slopeCos, angleHalfSin * slopeCos, -slopeSin);
					else
						normals[i] = new Vector3(angleCos * slopeCos, angleSin * slopeCos, -slopeSin);
					if (radiusBottom == 0)
						normals[i + numVertices] = new Vector3(angleHalfCos * slopeCos, angleHalfSin * slopeCos, -slopeSin);
					else
						normals[i + numVertices] = new Vector3(angleCos * slopeCos, angleSin * slopeCos, -slopeSin);
				*
				vertices[i].texcoords = new Vector2(1.0f * i / numVertices, 1);
				vertices[i + numVertices].texcoords = new Vector2(1.0f * i / numVertices, 0);

				if (outside && inside)
				{
					// vertices and uvs are identical on inside and outside, so just copy
					vertices[i + 2 * numVertices] = vertices[i];
					vertices[i + 3 * numVertices] = vertices[i + numVertices];
				}
				if (inside)
				{
					// invert normals
					//normals[i + offset] = -normals[i];
					//normals[i + numVertices + offset] = -normals[i + numVertices];
				}
			}
			//mesh.normals = normals;

			// create triangles
			// here we need to take care of point order, depending on inside and outside
			int cnt = 0;
			if (radiusTop == 0)
			{
				// top cone
				tris = new ushort[numVertices * 3 * multiplier];
				if (outside)
					for (i = 0; i < numVertices; i++)
					{
						tris[cnt++] = (ushort)(i + numVertices);
						tris[cnt++] = (ushort)i;
						if (i == numVertices - 1)
							tris[cnt++] = (ushort)numVertices;
						else
							tris[cnt++] = (ushort)(i + 1 + numVertices);
					}
				if (inside)
					for (i = offset; i < numVertices + offset; i++)
					{
						tris[cnt++] = (ushort)i;
						tris[cnt++] = (ushort)(i + numVertices);
						if (i == numVertices - 1 + offset)
							tris[cnt++] = (ushort)(numVertices + offset);
						else
							tris[cnt++] = (ushort)(i + 1 + numVertices);
					}
			}
			else if (radiusBottom == 0)
			{
				// bottom cone
				tris = new ushort[numVertices * 3 * multiplier];
				if (outside)
					for (i = 0; i < numVertices; i++)
					{
						tris[cnt++] = (ushort)i;
						if (i == numVertices - 1)
							tris[cnt++] = 0;
						else
							tris[cnt++] = (ushort)(i + 1);
						tris[cnt++] = (ushort)(i + numVertices);
					}
				if (inside)
					for (i = offset; i < numVertices + offset; i++)
					{
						if (i == numVertices - 1 + offset)
							tris[cnt++] = (ushort)offset;
						else
							tris[cnt++] = (ushort)(i + 1);
						tris[cnt++] = (ushort)i;
						tris[cnt++] = (ushort)(i + numVertices);
					}
			}
			else
			{
				// truncated cone
				tris = new ushort[numVertices * 6 * multiplier];
				if (outside)
					for (i = 0; i < numVertices; i++)
					{
						ushort ip1 = (ushort)(i + 1);
						if (ip1 == numVertices)
							ip1 = 0;

						tris[cnt++] = (ushort)i;
						tris[cnt++] = ip1;
						tris[cnt++] = (ushort)(i + numVertices);

						tris[cnt++] = (ushort)(ip1 + numVertices);
						tris[cnt++] = (ushort)(i + numVertices);
						tris[cnt++] = ip1;
					}
				if (inside)
					for (i = offset; i < numVertices + offset; i++)
					{
						ushort ip1 = (ushort)(i + 1);
						if (ip1 == numVertices + offset)
							ip1 = (ushort)offset;

						tris[cnt++] = ip1;
						tris[cnt++] = (ushort)i;
						tris[cnt++] = (ushort)(i + numVertices);

						tris[cnt++] = (ushort)(i + numVertices);
						tris[cnt++] = (ushort)(ip1 + numVertices);
						tris[cnt++] = ip1;
					}
			}*/
		var newMesh = new Mesh
		{
			FullPath = meshName,
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadIndices<ushort>(indices.ToArray());
		newMesh.LoadVertices<UIVertexFormat>(vertices.ToArray());
		return newMesh;
	}
	public static Mesh GenerateCone()
	{
		if (radiusTop > float.Epsilon)
			radiusBottom = 0;
		return GenerateCylinder("cone");
	}
	public static Mesh GenerateCube()
	{
		float length = 1f;
		float width = 1f;
		float height = 1f;
		#region UVs
		Vector2 _00 = new Vector2(0f, 0f);
		Vector2 _10 = new Vector2(1f, 0f);
		Vector2 _01 = new Vector2(0f, 1f);
		Vector2 _11 = new Vector2(1f, 1f);

		#endregion
		#region Vertices
		Vector3 p0 = new Vector3(0, 0, 0);
		Vector3 p1 = new Vector3(length, 0, 0);
		Vector3 p2 = new Vector3(length, 0, width);
		Vector3 p3 = new Vector3(0, 0, width);

		Vector3 p4 = new Vector3(0, height, 0);
		Vector3 p5 = new Vector3(length, height, 0);
		Vector3 p6 = new Vector3(length, height, width);
		Vector3 p7 = new Vector3(0, height, width);

		var vertices = new UIVertexFormat[]
		{
					new UIVertexFormat(){position=p0, texcoords=_11 },
					new UIVertexFormat(){position=p1, texcoords=_01 },
					new UIVertexFormat(){position=p2, texcoords=_00 },
					new UIVertexFormat(){position=p3, texcoords=_10 },

					new UIVertexFormat(){position=p7, texcoords=_11 },
					new UIVertexFormat(){position=p4, texcoords=_01 },
					new UIVertexFormat(){position=p0, texcoords=_00 },
					new UIVertexFormat(){position=p3, texcoords=_10 },

					new UIVertexFormat(){position=p4, texcoords=_11 },
					new UIVertexFormat(){position=p5, texcoords=_01 },
					new UIVertexFormat(){position=p1, texcoords=_00 },
					new UIVertexFormat(){position=p0, texcoords=_10 },

					new UIVertexFormat(){position=p6, texcoords=_11 },
					new UIVertexFormat(){position=p7, texcoords=_01 },
					new UIVertexFormat(){position=p3, texcoords=_00 },
					new UIVertexFormat(){position=p2, texcoords=_10 },

					new UIVertexFormat(){position=p5, texcoords=_11 },
					new UIVertexFormat(){position=p6, texcoords=_01 },
					new UIVertexFormat(){position=p2, texcoords=_00 },
					new UIVertexFormat(){position=p1, texcoords=_10 },

					new UIVertexFormat(){position=p7, texcoords=_11 },
					new UIVertexFormat(){position=p6, texcoords=_01 },
					new UIVertexFormat(){position=p5, texcoords=_00 },
					new UIVertexFormat(){position=p4, texcoords=_10 }
		};
		#endregion

		/*			#region Normales
					Vector3 up = Vector3.UnitY;
					Vector3 down = -Vector3.UnitY;
					Vector3 front = Vector3.UnitZ;
					Vector3 back = -Vector3.UnitZ;
					Vector3 left = -Vector3.UnitX;
					Vector3 right = Vector3.UnitX;

					Vector3[] normales = new Vector3[]
					{
		// Bottom
		down, down, down, down,

		// Left
		left, left, left, left,

		// Front
		front, front, front, front,

		// Back
		back, back, back, back,

		// Right
		right, right, right, right,

		// Top
		up, up, up, up
					};
					#endregion*/



		#region Triangles
		ushort[] indices = new ushort[]
		{
	// Bottom
	3, 1, 0,
	3, 2, 1,			
 
	// Left
	3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
	3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
 
	// Front
	3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
	3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
 
	// Back
	3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
	3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
 
	// Right
	3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
	3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
 
	// Top
	3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
	3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,

		};
		#endregion
		var newMesh = new Mesh
		{
			FullPath = "cube",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadVertices<UIVertexFormat>(vertices);
		newMesh.LoadIndices<ushort>(indices);
		return newMesh;
	}
	public static float outerRadius = 1.0f;
	public static float innerRadius = 0.0f;
	public static float totalAngleDeg = 360.0f;

	public static Mesh GenerateDisc()
	{
		var vertices = new UIVertexFormat[2 * numVertices];
		//uv = new VectorArray2f(2 * Slices);
		//normals = new VectorArray3f(2 * Slices);
		var indices = new ushort[2 * numVertices * 3 + 3];

		bool bFullDisc = (totalAngleDeg > 359.99f);
		float fTotalRange = (totalAngleDeg) * NumericsExtensions.Deg2Rad;
		float fDelta = (bFullDisc) ? fTotalRange / numVertices : fTotalRange / (numVertices - 1);
		float fUVRatio = innerRadius / outerRadius;
		for (int k = 0; k < numVertices; ++k)
		{
			float angle = (float)k * fDelta;
			float cosa = MathF.Cos(angle), sina = MathF.Sin(angle);
			vertices[k] = new UIVertexFormat() { position = new Vector3(innerRadius * cosa, 0, innerRadius * sina) };
			vertices[numVertices + k] = new UIVertexFormat() { position = new Vector3(outerRadius * cosa, 0, outerRadius * sina) };
			//uv[k] = new Vector2f(0.5f * (1.0f + fUVRatio * cosa), 0.5f * (1.0f + fUVRatio * sina));
			//uv[Slices + k] = new Vector2f(0.5f * (1.0f + cosa), 0.5f * (1.0f + sina));
			//normals[k] = normals[Slices + k] = Vector3f.AxisY;
		}

		int ti = 0;
		for (int k = 0; k < numVertices - 1; ++k)
		{
			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k + 1);

			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(numVertices + k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k);
		}
		if (bFullDisc)
		{      // close disc if we went all the way
			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)0;
			indices[3 * ti + 2] = (ushort)numVertices;

			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)numVertices;
			indices[3 * ti + 2] = (ushort)(2 * numVertices - 1);
		}
		/*vertices[0].prev_position = vertices[0].vertex_position;
		vertices[0].next_position = vertices[1].vertex_position;
		vertices[2 * numVertices - 1].prev_position = vertices[2 * numVertices - 2].vertex_position;
		vertices[2 * numVertices - 1].next_position = vertices[2 * numVertices - 1].vertex_position;

		for (var id = 1; id < 2 * numVertices - 1; id++)
		{
			vertices[id].prev_position = vertices[id - 1].vertex_position;
			vertices[id].next_position = vertices[id + 1].vertex_position;
			id++;
		}*/
		var newMesh = new Mesh
		{
			FullPath = "disc",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadIndices<ushort>(indices);
		newMesh.LoadVertices<UIVertexFormat>(vertices);
		return newMesh;
	}
	internal static Mesh GenerateEditorDisc(Vector3 startAxis, Vector3 nextAxis)
	{
		var vertices = new UIVertexFormat[2 * numVertices];
		//uv = new VectorArray2f(2 * Slices);
		//normals = new VectorArray3f(2 * Slices);
		var indices = new ushort[2 * numVertices * 3 + 3];

		bool bFullDisc = (totalAngleDeg > 359.99f);
		float fTotalRange = (totalAngleDeg) * NumericsExtensions.Deg2Rad;
		float fDelta = (bFullDisc) ? fTotalRange / numVertices : fTotalRange / (numVertices - 1);
		float fUVRatio = innerRadius / outerRadius;
		var cross = Vector3.Cross(startAxis, nextAxis).Normalize();
		for (int k = 0; k < numVertices; ++k)
		{
			float angle = (float)k * fDelta;
			//float cosa = MathF.Cos(angle), sina = MathF.Sin(angle);
			var rotateMat = Matrix4x4.CreateFromAxisAngle(cross, angle);
			vertices[k] = new UIVertexFormat()
			{
				position = startAxis.Transformed(rotateMat) * innerRadius
			};
			vertices[numVertices + k] = new UIVertexFormat()
			{
				position = startAxis.Transformed(rotateMat) * outerRadius
			};
			//uv[k] = new Vector2f(0.5f * (1.0f + fUVRatio * cosa), 0.5f * (1.0f + fUVRatio * sina));
			//uv[Slices + k] = new Vector2f(0.5f * (1.0f + cosa), 0.5f * (1.0f + sina));
			//normals[k] = normals[Slices + k] = Vector3f.AxisY;
		}

		int ti = 0;
		for (int k = 0; k < numVertices - 1; ++k)
		{
			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k + 1);

			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(numVertices + k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k);
		}
		if (bFullDisc)
		{      // close disc if we went all the way
			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)0;
			indices[3 * ti + 2] = (ushort)numVertices;

			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)numVertices;
			indices[3 * ti + 2] = (ushort)(2 * numVertices - 1);
		}
		/*vertices[0].prev_position = vertices[0].vertex_position;
		vertices[0].next_position = vertices[1].vertex_position;
		vertices[2 * numVertices - 1].prev_position = vertices[2 * numVertices - 2].vertex_position;
		vertices[2 * numVertices - 1].next_position = vertices[2 * numVertices - 1].vertex_position;

		for (var id = 1; id < 2 * numVertices - 1; id++)
		{
			vertices[id].prev_position = vertices[id - 1].vertex_position;
			vertices[id].next_position = vertices[id + 1].vertex_position;
			id++;
		}*/
		var newMesh = new Mesh
		{
			FullPath = "editor_disc",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadIndices<ushort>(indices);
		newMesh.LoadVertices<UIVertexFormat>(vertices);
		return newMesh;
	}
	public static Mesh GenerateTorus()
	{
		float radius = 1f;
		float tube = 0.02f;
		int radialSegments = numVertices;
		int tubularSegments = numVertices;
		float arc = 360f;

		//List<Vector2> uvs = new List<Vector2>();
		List<UIVertexFormat> vertices = new List<UIVertexFormat>();
		//List<Vector3> normals = new List<Vector3>();
		List<ushort> indices = new List<ushort>();

		var center = new Vector3();

		for (var j = 0; j <= radialSegments; j++)
		{
			for (var i = 0; i <= tubularSegments; i++)
			{
				var u = i / (float)tubularSegments * arc * NumericsExtensions.Deg2Rad;
				var v = j / (float)radialSegments * MathF.PI * 2.0f;

				center.X = radius * MathF.Cos(u);
				center.Y = radius * MathF.Sin(u);

				var vertex = new Vector3();
				vertex.X = (radius + tube * MathF.Cos(v)) * MathF.Cos(u);
				vertex.Y = (radius + tube * MathF.Cos(v)) * MathF.Sin(u);
				vertex.Z = tube * MathF.Sin(v);

				vertices.Add(new UIVertexFormat() { position = vertex });

				//uvs.Add(new Vector2(i / (float)tubularSegments, j / (float)radialSegments));
				//Vector3 normal = vertex - center;
				//normal.Normalize();
				//normals.Add(normal);
			}
		}


		for (var j = 1; j <= radialSegments; j++)
		{
			for (var i = 1; i <= tubularSegments; i++)
			{
				var a = (tubularSegments + 1) * j + i - 1;
				var b = (tubularSegments + 1) * (j - 1) + i - 1;
				var c = (tubularSegments + 1) * (j - 1) + i;
				var d = (tubularSegments + 1) * j + i;

				indices.Add((ushort)a);
				indices.Add((ushort)b);
				indices.Add((ushort)d);

				indices.Add((ushort)b);
				indices.Add((ushort)c);
				indices.Add((ushort)d);
			}

		}

		var newMesh = new Mesh
		{
			FullPath = "torus",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadIndices<ushort>(indices.ToArray());
		newMesh.LoadVertices<UIVertexFormat>(vertices.ToArray());
		return newMesh;
	}
}