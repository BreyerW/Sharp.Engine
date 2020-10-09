﻿using Sharp.Commands;
using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.Pipeline;
using System;
using System.Numerics;

namespace Sharp.Editor
{
	public static class Manipulators
	{
		private static readonly int halfCircleSegments = 64;
		public static readonly Color selectedColor = new Color(0xFF1080FF);
		public static readonly Color fillColor = new Color(0x801080FF);
		public static readonly Color xColor = new Color(0xFF0000AA);
		public static readonly Color yColor = new Color(0xFF00AA00);
		public static readonly Color zColor = new Color(0xFFAA0000);

		internal static Material discMaterial;
		internal static int selectedAxisId = 0;
		internal static float? rotAngleOrigin;
		internal static float angle;
		internal static Vector3 currentAngle = Vector3.Zero;
		internal static Vector3? relativeOrigin;
		internal static Vector3? planeOrigin;
		internal static Vector3? rotVectSource;
		internal static Vector3? scaleOrigin;
		internal static Vector3 scaleOffset;
		internal static Vector4 transformationPlane;
		internal static Vector3 startAxis;
		internal static Matrix4x4 startMat;

		static Manipulators()
		{
			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoShader.shader");
			discMaterial = new Material();
			discMaterial.Shader = shader;
			CreatePrimitiveMesh.numVertices = halfCircleSegments;
			var disc = CreatePrimitiveMesh.GenerateEditorDisc(Vector3.UnitY, Vector3.UnitX);

			disc.UsageHint = UsageHint.DynamicDraw;
			Pipeline.Get<Mesh>().Register(disc);
			discMaterial.BindProperty("mesh", disc);
			discMaterial.BindProperty("len", new Vector2(17.5f));
		}

		public static void DrawCombinedGizmos(Entity entity, Vector2 winSize)
		{
			DrawCombinedGizmos(entity, winSize, (selectedAxisId == 1 ? selectedColor : xColor), (selectedAxisId == 2 ? selectedColor : yColor), (selectedAxisId == 3 ? selectedColor : zColor), (selectedAxisId == 4 ? selectedColor : xColor), (selectedAxisId == 5 ? selectedColor : yColor), (selectedAxisId == 6 ? selectedColor : zColor), (selectedAxisId == 7 ? selectedColor : xColor), (selectedAxisId == 8 ? selectedColor : yColor), (selectedAxisId == 9 ? selectedColor : zColor), 3f);
		}

		public static void DrawCombinedGizmos(Entity entity, Vector2 winSize, Color xColor, Color yColor, Color zColor, Color xRotColor, Color yRotColor, Color zRotColor, Color xScaleColor, Color yScaleColor, Color zScaleColor, float thickness = 5f)
		{
			float scale = (Camera.main.Parent.transform.Position - entity.transform.Position).Length() / 100.0f;
			Matrix4x4.Decompose(SceneView.globalMode ? entity.transform.ModelMatrix.Inverted() : entity.transform.ModelMatrix, out _, out var rot, out var trans);

			var scaleMat = Matrix4x4.CreateScale(scale, scale, scale);
			var mat = Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(trans);

			var rotationX = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, NumericsExtensions.Deg2Rad * 90);
			var rotationY = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, NumericsExtensions.Deg2Rad * 90);
			var rotationZ = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, NumericsExtensions.Deg2Rad * 90);


			DrawHelper.DrawRotationGizmo(scaleMat * rotationX * mat, scaleMat * rotationY * mat, scaleMat * rotationZ * mat, xRotColor, yRotColor, zRotColor);
			DrawHelper.DrawScaleGizmo(scaleMat * rotationX * mat, scaleMat * rotationY * mat, scaleMat * rotationZ * mat, xScaleColor, yScaleColor, zScaleColor, scaleOffset);
			DrawHelper.DrawTranslationGizmo(scaleMat * rotationX * mat, scaleMat * rotationY * mat, scaleMat * rotationZ * mat, xColor, yColor, zColor);
			if (rotVectSource.HasValue)
			{
				var fullAngle = NumericsExtensions.CalculateAngle(startAxis, currentAngle);
				CreatePrimitiveMesh.numVertices = halfCircleSegments;
				CreatePrimitiveMesh.totalAngleDeg = fullAngle * NumericsExtensions.Rad2Deg;
				CreatePrimitiveMesh.innerRadius = 0.80f;
				var disc = CreatePrimitiveMesh.GenerateEditorDisc(startAxis, currentAngle);
				ref var origDisc = ref Pipeline.Get<Mesh>().GetAsset("editor_disc");
				origDisc.LoadVertices(disc.verts);
				origDisc.LoadIndices(disc.Indices);

				var sMat = Matrix4x4.CreateTranslation(trans);
				discMaterial.BindProperty("model", scaleMat * sMat);
				discMaterial.BindProperty("color", fillColor);

				discMaterial.SendData();
				/*var (startRot, up) = selectedAxisId switch
				{
					1 or 4 or 7 => (rotationX, Vector3.UnitX),
					2 or 5 or 8 => (rotationZ, Vector3.UnitY),
					_ => (rotationY, Vector3.UnitZ)
				};
				var realNormal = Vector3.TransformNormal(Vector3.UnitX, startRot * startMat).Normalized();
				var sign = MathF.Sign(Vector3.Dot(realNormal, Vector3.UnitZ.Transformed(Camera.main.ViewMatrix).Normalized()));
				if (sign is 0) sign = 1;
				var rotToAxis = Matrix4x4.CreateFromQuaternion(RotationFromTo(Vector3.UnitY, sign* realNormal));
				//var extraAngle = NumericsExtensions.CalculateAngle(Vector3.TransformNormal(Vector3.UnitY, startRot * startMat).Normalized(), startAxis.Normalized());
				var extraRot = Matrix4x4.CreateFromQuaternion(RotationFromTo(Vector3.TransformNormal(Vector3.UnitY, startRot * startMat).Normalized(), startAxis.Normalized()));
				var sMat =  rotToAxis *extraRot * Matrix4x4.CreateTranslation(trans);
				var angle = AngleBetween(startAxis.Normalized(), currentAngle.Normalized(), Vector3.UnitY.Transformed(scaleMat * sMat).Normalized()) * NumericsExtensions.Rad2Deg;
				if (angle < 0)
					discMaterial.BindProperty("flipU", 0);
				else
					discMaterial.BindProperty("flipU", 1);
				discMaterial.BindProperty("model", scaleMat * sMat);
				discMaterial.BindProperty("progress", angle / 360f);
				OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
				discMaterial.SendData();
				OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);*/
			}
		}
		private static Quaternion RotationFromTo(Vector3 source, Vector3 destination)
		{
			Quaternion quat;
			Vector3 tmpvec3;
			var dot = Vector3.Dot(source, destination);
			if (dot < -0.999999f)
			{
				tmpvec3 = Vector3.Cross(Vector3.UnitX, source);
				if (tmpvec3.Length() < 0.000001f)
					tmpvec3 = Vector3.Cross(Vector3.UnitY, source);
				tmpvec3 = Vector3.Normalize(tmpvec3);
				quat = Quaternion.CreateFromAxisAngle(tmpvec3, MathF.PI);
			}
			else if (dot > 0.999999f)
			{
				quat.X = 0;
				quat.Y = 0;
				quat.Z = 0;
				quat.W = 1f;
			}
			else
			{
				tmpvec3 = Vector3.Cross(source, destination);
				quat.X = tmpvec3.X;
				quat.Y = tmpvec3.Y;
				quat.Z = tmpvec3.Z;
				quat.W = 1f + dot;
			}
			return Quaternion.Normalize(quat);
		}
		public static void Reset()
		{
			//if (newCommand != null)
			//    newCommand.StoreCommand();
			//UI.Property.PropertyDrawer.StopCommandCommits = false;
			rotVectSource = null;
			rotAngleOrigin = null;
			planeOrigin = null;
			relativeOrigin = null;
			scaleOrigin = null;
			scaleOffset = Vector3.Zero;
			currentAngle = Vector3.Zero;
			angle = 0;
			startAxis = Vector3.Zero;
			startMat = default;
		}

		private static Vector3 GetAxis()
		{
			if (selectedAxisId is 1 or 4 or 7)
			{
				return Vector3.UnitX;
			}
			else if (selectedAxisId is 2 or 5 or 8)
			{
				return Vector3.UnitY;
			}
			else
				return Vector3.UnitZ;
		}

		public static void HandleTranslation(Entity entity, ref Ray ray)
		{
			var v = GetAxis();
			if (!SceneView.globalMode)
			{
				Matrix4x4.Decompose(entity.transform.ModelMatrix, out _, out var r, out _);
				v.Transform(r).Normalize();//TransformVector
			}
			var len = ComputeLength(ref ray, entity.transform.Position);
			if (!relativeOrigin.HasValue)
			{
				planeOrigin = ray.origin + ray.direction * len;
				relativeOrigin = (planeOrigin - entity.transform.Position) * (1f / (0.1f * GetUniform(entity.transform.Position, Camera.main.ProjectionMatrix)));
				//UI.Property.PropertyDrawer.StopCommandCommits = true;
			}
			var newPos = ray.origin + ray.direction * len;
			var newOrigin = newPos - relativeOrigin.Value * (0.1f * GetUniform(entity.transform.Position, Camera.main.ProjectionMatrix));
			var delta = newOrigin - entity.transform.Position;
			var lenOnAxis = Vector3.Dot(delta, v);
			delta = v * lenOnAxis;
			entity.transform.Position += delta;
		}

		//private static Quaternion startRot = Quaternion.Identity;
		private static Vector3 MakePositive(Vector3 euler)
		{
			float negativeFlip = -0.0001f * NumericsExtensions.Rad2Deg;
			float positiveFlip = 360.0f + negativeFlip;

			if (euler.X < negativeFlip)
				euler.X += 360.0f;
			else if (euler.X > positiveFlip)
				euler.X -= 360.0f;

			if (euler.Y < negativeFlip)
				euler.Y += 360.0f;
			else if (euler.Y > positiveFlip)
				euler.Y -= 360.0f;

			if (euler.Z < negativeFlip)
				euler.Z += 360.0f;
			else if (euler.Z > positiveFlip)
				euler.Z -= 360.0f;

			return euler;
		}

		public static void HandleRotation(Entity entity, ref Ray ray)//bugged when rescaled
		{
			var v = GetAxis();
			Matrix4x4.Decompose(entity.transform.ModelMatrix, out _, out var rot, out _);
			if (!SceneView.globalMode)
			{
				v.Transform(rot).Normalize();
			}
			var plane = BuildPlane(entity.transform.Position, v);
			transformationPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
			var len = ComputeLength(ref ray, entity.transform.Position);

			if (!rotVectSource.HasValue)
			{
				var origin = ray.origin + ray.direction * len - entity.transform.Position;
				var rotVec = constrain(origin, v).Normalize();
				rotVectSource = rotVec;
				rotAngleOrigin = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
				startMat = entity.transform.ModelMatrix;
				startAxis = origin.Normalized();
				//UI.Property.PropertyDrawer.StopCommandCommits = true;
			}
			angle = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
			//Console.WriteLine("angle: " + angle * NumericsExtensions.Rad2Deg);
			var rotAxis = (new Vector3(transformationPlane.X, transformationPlane.Y, transformationPlane.Z).Transform(Quaternion.Inverse(rot))).Normalize();
			currentAngle = (ray.origin + ray.direction * len - entity.transform.Position).Normalize();
			var deltaRot = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(rotAxis, angle - rotAngleOrigin.Value));
			entity.transform.ModelMatrix = Matrix4x4.CreateFromQuaternion(deltaRot) * entity.transform.ModelMatrix;//TODO: add rotate to entity that deals with delta euler angles to quats/mats and other things
			entity.transform.Rotation += deltaRot.ToEulerAngles() * NumericsExtensions.Rad2Deg;
			rotAngleOrigin = angle;

		}
		public static void HandleScale(Entity entity, ref Ray ray)
		{
			var v = GetAxis();
			if (!SceneView.globalMode)
			{
				Matrix4x4.Decompose(entity.transform.ModelMatrix, out _, out var r, out _);
				v.Transform(r).Normalize(); //TransformVector
			}
			var len = ComputeLength(ref ray, entity.transform.Position);
			if (!planeOrigin.HasValue)
			{
				planeOrigin = ray.origin + ray.direction * len;
				scaleOrigin = entity.transform.Scale;
				// UI.Property.PropertyDrawer.StopCommandCommits = true;
			}
			var newPos = ray.origin + ray.direction * len;
			var delta = (newPos - entity.transform.Position).Length() / (planeOrigin.Value - entity.transform.Position).Length();
			scaleOffset = newPos * v;
			entity.transform.Scale = scaleOrigin.Value + v * delta - v;
		}

		private static float GetUniform(Vector3 pos, Matrix4x4 mat)
		{
			var trf = new Vector4(pos, 1f);
			trf = Vector4.Transform(trf, mat);
			return trf.W;
		}

		private static Vector3 constrain(Vector3 vec, Vector3 axis)
		{
			var onPlane = Vector3.Subtract(vec, axis * Vector3.Dot(axis, vec));
			var norm = onPlane.LengthSquared();
			if (norm > 0)
			{
				if (onPlane.Z < 0) onPlane = -onPlane;
				return onPlane * (1 / (float)Math.Sqrt(norm));
			}
			if (axis.Z is 1) onPlane = Vector3.UnitX;
			else onPlane = new Vector3(-axis.Y, axis.X, 0).Normalize();
			return onPlane;
		}

		private static Plane BuildPlane(Vector3 pos, Vector3 normal)
		{
			Vector4 baseForPlane = Vector4.Zero;
			normal.Normalize();
			baseForPlane.W = Vector3.Dot(pos, normal);
			baseForPlane.X = normal.X;
			baseForPlane.Y = normal.Y;
			baseForPlane.Z = normal.Z;
			return new Plane(baseForPlane);
		}

		private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
		{
			var len = ray.IntersectPlane(ref plane);
			var localPos = (ray.origin + ray.direction * len - entity.transform.Position).Normalize();
			var perpendicularVect = Vector3.Cross(rotVectSource.Value, new Vector3(plane.X, plane.Y, plane.Z)).Normalize();
			var angle = NumericsExtensions.CalculateAngle(localPos, rotVectSource.Value);//(float)Math.Acos(NumericsExtensions.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

			return angle *= NumericsExtensions.Clamp((Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f, -0.9999f, 0.9999f);
		}

		/*private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
        {
            var len = ray.IntersectPlane(ref plane);
            var localPos = (ray.origin + ray.direction * len - entity.Position).Normalized();
            var perpendicularVect = Vector3.Cross(rotVectSource.Value, plane.Xyz).Normalized();
            var angle = (float)Math.Acos(NumericsExtensions.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));//(float)Math.Acos(NumericsExtensions.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

            return angle *= (Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f;
        }*/

		private static float AngleBetween(Vector3 vector1, Vector3 vector2, Vector3 originNormal)
		{
			//var angle = MathF.Acos(Vector3.Dot(vector1.Normalize(), vector2.Normalize()));
			var cross = Vector3.Cross(vector1, vector2);
			var dot = Vector3.Dot(vector1, vector2);
			var angle = MathF.Atan2(cross.Length(), dot);
			if (Vector3.Dot(originNormal, cross) < 0)
			{ // Or > 0
				angle = -angle;
			}
			/*var angle = MathF.Atan2(vector1.Y, vector1.X) - MathF.Atan2(vector2.Y, vector2.X);// * (180 / Math.PI);
			if (angle < 0)
			{
				//angle = angle + NumericsExtensions.TwoPi;
			}*/
			return angle;
		}

		private static float ComputeLength(ref Ray ray, Vector3 pos)
		{
			//var plane = BuildPlane(pos, -ray.direction);
			//var intersectPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
			return ray.IntersectPlane(ref transformationPlane);
		}
	}
}

/*var v = GetAxis();
            var len = ComputeLength(ref ray, entity.Position);
            if (!relativeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                relativeOrigin = (planeOrigin - entity.Position);// * (1f / (1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix)));
            }
            var newPos = ray.origin + ray.direction * len;
            var newOrigin = newPos - relativeOrigin.Value;// * (1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix));
            var delta = newOrigin - entity.Position;
            var lenOnAxis = Vector3.Dot(v, delta);
            delta = v * lenOnAxis;
            var baseVector = planeOrigin.Value - entity.Position;
            float ratio = Vector3.Dot(v, baseVector + delta) / Vector3.Dot(v, baseVector);
            var scale = ratio * v;
            scale.X = scale.X == 0 ? entity.Scale.X : scale.X;
            scale.Y = scale.Y == 0 ? entity.Scale.Y : scale.Y;
            scale.Z = scale.Z == 0 ? entity.Scale.Z : scale.Z;
            entity.Scale = scale;*/
