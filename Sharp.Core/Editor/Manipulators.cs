//https://github.com/CedricGuillemet/ImGuizmo/blob/master/ImGuizmo.cpp

using PluginAbstraction;
using Sharp.Physic;
using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.AssetPipeline;
using System;
using System.Numerics;

namespace Sharp.Editor
{
	public enum Gizmo
	{
		Invalid,

		ViewCubeBottomEdgeZ,
		ViewCubeBottomEdgeMinusX,
		ViewCubeBottomEdgeX,
		ViewCubeRightEdgeZ,

		ViewCubeLeftEdgeZ,
		ViewCubeTopEdgeZ,
		ViewCubeTopEdgeX,
		ViewCubeTopEdgeMinusX,


		ViewCubeTopEdgeMinusZ,
		ViewCubeLeftEdgeMinusZ,
		ViewCubeRightEdgeMinusZ,
		ViewCubeBottomEdgeMinusZ,



		ViewCubeMinusX,
		ViewCubeMinusZ,
		ViewCubeY,
		ViewCubeX,
		ViewCubeMinusY,
		ViewCubeZ,

		ViewCubeUpperLeftCornerX,
		ViewCubeLowerLeftCornerX,
		ViewCubeLowerRightCornerMinusX,
		ViewCubeUpperRightCornerMinusX,

		ViewCubeUpperRightCornerX,
		ViewCubeLowerRightCornerX,
		ViewCubeLowerLeftCornerMinusX,
		ViewCubeUpperLeftCornerMinusX,

		TranslateX,
		TranslateY,
		TranslateZ,
		TranslateXY,
		TranslateYZ,
		TranslateZX,
		RotateX,
		RotateY,
		RotateZ,
		ScaleX,
		ScaleY,
		ScaleZ,
		RotateScreen,
		UniformScale,

	}
	public static class Manipulators
	{
		private const float snapTension = 0.5f;
		private static readonly int halfCircleSegments = 64;
		public static readonly Color selectedColor = new Color(0xFF1080FF);
		public static readonly Color fillColor = new Color(0x801080FF);
		public static readonly Color xColor = new Color(0xFF0000AA);
		public static readonly Color yColor = new Color(0xFF00AA00);
		public static readonly Color zColor = new Color(0xFFAA0000);
		public static readonly Color screenRotColor = new Color(0xFF888888);
		public static bool preserveInsignificantCameraAngleWithViewCube = false;
		internal static Material discMaterial;
		public static Gizmo selectedGizmoId = Gizmo.Invalid;
		internal static Gizmo hoveredGizmoId = Gizmo.Invalid;
		internal static float? rotAngleOrigin;
		internal static float angle;
		private static Plane translationPlane;
		internal static Vector3 currentAngle = Vector3.Zero;
		internal static Vector3? relativeOrigin;
		internal static Vector3? planeOrigin;
		internal static Vector3? rotVectSource;
		internal static Vector3? scaleSource;
		internal static Vector3 transformOrigin;
		internal static Vector3 scaleOffset;
		internal static Vector4 transformationPlane;
		internal static Vector3 translationPlaneOrigin;
		internal static Matrix4x4 startMat;
		internal static Matrix4x4 mModel;
		internal static bool useUniformScale = false;
		//

		static Manipulators()
		{
			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoShader.shader");
			discMaterial = new Material();
			discMaterial.BindShader(0, shader);
			CreatePrimitiveMesh.numVertices = halfCircleSegments;
			var disc = CreatePrimitiveMesh.GenerateEditorDisc(Vector3.UnitY, Vector3.UnitX);

			disc.UsageHint = UsageHint.DynamicDraw;
			Pipeline.Get<Mesh>().Register(disc);
			discMaterial.BindProperty("mesh", disc);
			discMaterial.BindProperty("len", new Vector2(17.5f));
		}

		public static void DrawCombinedGizmos(Entity entity)
		{
			DrawCombinedGizmos(entity, 3f);
		}

		public static void DrawCombinedGizmos(Entity entity, float thickness = 5f)
		{
			float scale = (Camera.main.Parent.transform.Position - entity.transform.Position).Length() / 100.0f;
			if (SceneView.globalMode is false)
			{
				mModel = entity.transform.ModelMatrix;
				mModel.OrthoNormalize();
			}
			else
			{
				mModel = Matrix4x4.CreateTranslation(entity.transform.Position);
			}
			var scaleMat = Matrix4x4.CreateScale(scale, scale, scale);
			var finalMat = scaleMat * mModel;
			var alignToScreen = scaleMat * Matrix4x4.CreateBillboard(entity.transform.Position, Camera.main.Parent.transform.Position, Camera.main.ViewMatrix.Up(), Camera.main.ViewMatrix.Forward());
			DrawHelper.DrawGizmo(finalMat, alignToScreen);

			if (rotVectSource.HasValue)
			{
				var fullAngle = NumericsExtensions.CalculateAngle(rotVectSource.Value, currentAngle);
				CreatePrimitiveMesh.numVertices = halfCircleSegments;
				CreatePrimitiveMesh.totalAngleDeg = fullAngle * (float)NumericsExtensions.Rad2Deg;
				CreatePrimitiveMesh.innerRadius = 0.80f;
				var disc = CreatePrimitiveMesh.GenerateEditorDisc(rotVectSource.Value, currentAngle);
				ref var origDisc = ref Pipeline.Get<Mesh>().GetAsset("editor_disc");
				origDisc.LoadVertices(disc.verts);
				origDisc.LoadIndices(disc.Indices);

				var sMat = Matrix4x4.CreateTranslation(mModel.Translation);
				discMaterial.BindProperty("model", scaleMat * sMat);
				discMaterial.BindProperty("color", fillColor);

				discMaterial.Draw();
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
			rotVectSource = null;
			rotAngleOrigin = null;
			planeOrigin = null;
			relativeOrigin = null;
			scaleSource = null;
			scaleOffset = default;
			currentAngle = default;
			angle = default;
			startMat = default;
			transformOrigin = default;
			transformationPlane = default;
			useUniformScale = false;
		}

		private static Vector3 GetAxis()
		{
			if (selectedGizmoId is Gizmo.TranslateX or Gizmo.TranslateXY or Gizmo.RotateX or Gizmo.ScaleX)
			{
				return Vector3.UnitX;
			}
			else if (selectedGizmoId is Gizmo.TranslateY or Gizmo.TranslateYZ or Gizmo.RotateY or Gizmo.ScaleY)
			{
				return Vector3.UnitY;
			}
			else
				return Vector3.UnitZ;
		}

		public static void HandleTranslation(Entity entity, ref Ray ray)
		{
			if (SceneView.globalMode is false)
			{
				mModel = entity.transform.ModelMatrix;
				mModel.OrthoNormalize();
			}
			else
			{
				mModel = Matrix4x4.Identity * Matrix4x4.CreateTranslation(entity.transform.Position);
			}
			// move
			if (relativeOrigin.HasValue)
			{
				float len = ray.IntersectPlane(transformationPlane); // near plan
				var newPos = ray.origin + ray.direction * len;

				// compute delta
				Vector3 newOrigin = newPos - relativeOrigin.Value * Camera.main.AspectRatio;//TODO: when moving XZ plane to infinity relativeorigin or something seems to become 0 and object pos become camera pos
				Vector3 delta = newOrigin - entity.transform.Position;
				Matrix4x4.Decompose(entity.transform.ModelMatrix, out var scale, out var rot, out var trans);
				// 1 axis constraint
				if (selectedGizmoId is Gizmo.TranslateX or Gizmo.TranslateY or Gizmo.TranslateZ)
				{
					mModel.DecomposeDirections(out var right, out var up, out var forward);
					var (direction, snapLimit) = selectedGizmoId switch
					{
						Gizmo.TranslateX => (right, SceneView.translateSnap.X),
						Gizmo.TranslateY => (up, SceneView.translateSnap.Y),
						_ => (forward, SceneView.translateSnap.Z)
					};
					Vector3 axisValue = direction; //.Normalize();// direction;
					float lengthOnAxis = Vector3.Dot(axisValue, delta);
					if (SceneView.snapMode)
						ComputeSnap(ref lengthOnAxis, snapLimit); //lengthOnAxis = MathF.Abs(lengthOnAxis) > snapLimit ? MathF.CopySign(snapLimit, lengthOnAxis) : 0;
					delta = axisValue * lengthOnAxis;
				}
				else if (SceneView.snapMode)
				{
					ComputeSnap(ref delta.X, SceneView.translateSnap.X);
					ComputeSnap(ref delta.Y, SceneView.translateSnap.Y);
					ComputeSnap(ref delta.Z, SceneView.translateSnap.Z);
				}
				// compute matrix & delta
				entity.transform.Position = trans + delta;
				Matrix4x4 scaleOrigin = Matrix4x4.CreateScale(scaleSource.Value);
				Matrix4x4 translateOrigin = Matrix4x4.CreateTranslation(trans + delta);
				Matrix4x4 rotateOrigin = Matrix4x4.CreateFromQuaternion(rot);
				entity.transform.ModelMatrix = scaleOrigin * rotateOrigin * translateOrigin; //Matrix4x4.CreateScale(entity.transform.Scale) * Matrix4x4.CreateFromYawPitchRoll(angles.Y, angles.X, angles.Z) * Matrix4x4.CreateTranslation(entity.transform.Position);
				entity.transform.onTransformChanged?.Invoke();
			}
			else
			{
				mModel.DecomposeDirections(out var right, out var up, out var forward);
				Vector3[] movePlaneNormal = { right, up, forward,
				 right, up, forward,
			   -Camera.main.ViewMatrix.Forward()/*free movement*/ };

				Vector3 cameraToModelNormalized = Vector3.Normalize(entity.transform.Position - Camera.main.Parent.transform.Position);
				for (int i = 0; i < 3; i++)
				{
					Vector3 orthoVector = Vector3.Cross(movePlaneNormal[i], cameraToModelNormalized);
					movePlaneNormal[i] = Vector3.Cross(movePlaneNormal[i], orthoVector).Normalize();
				}
				var index = selectedGizmoId switch
				{
					Gizmo.TranslateX => 1,
					Gizmo.TranslateY => 0,//TODO: choose 0 or 2 based on camera view?
					Gizmo.TranslateZ => 1,
					Gizmo.TranslateXY => 5,
					Gizmo.TranslateYZ => 3,
					Gizmo.TranslateZX => 4,
					_ => 6
				};
				startMat = entity.transform.ModelMatrix;
				transformOrigin = entity.transform.Position;
				var plane = BuildPlane(entity.transform.Position, movePlaneNormal[index]);//TODO: bugged look up imguizmo again
				transformationPlane = new Vector4(plane.Normal, plane.D);
				float len = ray.IntersectPlane(transformationPlane); // near plan
				var newPos = ray.origin + ray.direction * len;
				entity.transform.ModelMatrix.DecomposeDirections(out var eRight, out var eUp, out var eForward);
				scaleSource = new Vector3(eRight.Length(), eUp.Length(), eForward.Length());
				relativeOrigin = (newPos - entity.transform.Position) * (1.0f / Camera.main.AspectRatio);

			}
		}
		static void ComputeSnap(ref float value, float snap)
		{
			if (snap <= float.Epsilon)
			{
				return;
			}

			float modulo = value % snap;
			float moduloRatio = MathF.Abs(modulo) / snap;
			if (moduloRatio < snapTension)
			{
				value -= modulo;
			}
			else if (moduloRatio > (1.0f - snapTension))
			{
				value = value - modulo + snap * ((value < 0.0f) ? -1.0f : 1.0f);
			}
		}
		private static Vector3 MakePositive(Vector3 euler)
		{
			float negativeFlip = -0.0001f * (float)NumericsExtensions.Rad2Deg;
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
		public static void HandleRotation(Entity entity, ref Ray ray)
		{

			if (SceneView.globalMode is false || selectedGizmoId is Gizmo.RotateScreen)
			{
				mModel = entity.transform.ModelMatrix;
				mModel.OrthoNormalize();
			}
			else
			{
				mModel = Matrix4x4.Identity * Matrix4x4.CreateTranslation(entity.transform.Position);
			}
			if (rotVectSource.HasValue)
			{
				angle = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
				if (SceneView.snapMode)
				{
					var snapInRadian = selectedGizmoId switch
					{
						Gizmo.RotateX => SceneView.rotateSnap.X,
						Gizmo.RotateY => SceneView.rotateSnap.Y,
						Gizmo.RotateZ => SceneView.rotateSnap.Z,
						_ => SceneView.screenRotateSnap
					};
					snapInRadian *= NumericsExtensions.Deg2Rad;
					ComputeSnap(ref angle, snapInRadian);
				}
				var rotationAxisLocalSpace = Vector3.TransformNormal(new Vector3(transformationPlane.X, transformationPlane.Y, transformationPlane.Z), mModel.Inverted());
				rotationAxisLocalSpace.Normalize();
				var deltaInRad = angle - rotAngleOrigin.Value;

				var deltaRot = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(rotationAxisLocalSpace, deltaInRad));
				var len = ComputeLength(ref ray, entity.transform.Position);
				currentAngle = (ray.origin + ray.direction * len - entity.transform.Position).Normalize();
				rotAngleOrigin = angle;
				Matrix4x4.Decompose(entity.transform.ModelMatrix, out _, out var rot, out var trans);

				Matrix4x4 scaleOrigin = Matrix4x4.CreateScale(scaleSource.Value);
				Matrix4x4 translateOrigin = Matrix4x4.CreateTranslation(trans);
				Matrix4x4 rotateOrigin = Matrix4x4.CreateFromQuaternion(rot * deltaRot);
				entity.transform.ModelMatrix = scaleOrigin * rotateOrigin * translateOrigin;
				//entity.transform.Rotation = (rot * deltaRot).ToEulerAngles() * NumericsExtensions.Rad2Deg;

				entity.transform.Rotation += deltaRot.ToEulerAngles() * NumericsExtensions.Rad2Deg;
				entity.transform.onTransformChanged?.Invoke();
			}
			else
			{
				var camDir = Camera.main.Parent.transform.Rotation is { Y: var y, X: 0, Z: 0 } && y % 180f is 0 ? Vector3.UnitZ : Vector3.TransformNormal(Vector3.UnitZ, Matrix4x4.CreateBillboard(entity.transform.Position, Camera.main.Parent.transform.Position, Camera.main.ViewMatrix.Up(), Camera.main.ViewMatrix.Forward())).Normalized();
				mModel.DecomposeDirections(out var right, out var up, out var forward);
				Vector3[] movePlanNormal = { right,up,forward,
			   -camDir
				};
				var index = selectedGizmoId switch
				{
					Gizmo.RotateX => 0,
					Gizmo.RotateY => 1,//TODO: choose 0 or 2 based on camera view?
					Gizmo.RotateZ => 2,
					Gizmo.RotateScreen => 3,
					_ => throw new NotSupportedException($"Rotate doesnt support {selectedGizmoId}")
				};
				// pickup plan
				var plane = BuildPlane(entity.transform.Position, movePlanNormal[index]);
				entity.transform.ModelMatrix.DecomposeDirections(out var eRight, out var eUp, out var eForward);
				scaleSource = new Vector3(eRight.Length(), eUp.Length(), eForward.Length());
				transformationPlane = new Vector4(plane.Normal, plane.D);
				float len = ray.IntersectPlane(transformationPlane); // near plan
				var localPos = ray.origin + ray.direction * len - entity.transform.Position;
				rotVectSource = Vector3.Normalize(localPos);
				currentAngle = rotVectSource.Value;
				rotAngleOrigin = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
			}
		}
		public static void HandleViewCube(Entity entity)//null means orbit camera around camera focus point rather than selected object
		{
			var rotateVector = selectedGizmoId switch
			{
				Gizmo.ViewCubeX => new Vector3(preserveInsignificantCameraAngleWithViewCube ? Camera.main.Parent.transform.Rotation.X : 0, 90, 0),
				Gizmo.ViewCubeMinusX => new Vector3(preserveInsignificantCameraAngleWithViewCube ? Camera.main.Parent.transform.Rotation.X : 0, -90, 0),
				Gizmo.ViewCubeY => new Vector3(90, preserveInsignificantCameraAngleWithViewCube ? Camera.main.Parent.transform.Rotation.Y : 180, 0),
				Gizmo.ViewCubeMinusY => new Vector3(-90, preserveInsignificantCameraAngleWithViewCube ? Camera.main.Parent.transform.Rotation.Y : 180, 0),
				Gizmo.ViewCubeZ => new Vector3(preserveInsignificantCameraAngleWithViewCube ? Camera.main.Parent.transform.Rotation.X : 0, 0, 0),
				Gizmo.ViewCubeMinusZ => new Vector3(preserveInsignificantCameraAngleWithViewCube ? Camera.main.Parent.transform.Rotation.X : 0, 180, 0),

				Gizmo.ViewCubeBottomEdgeZ => new Vector3(-45, 180, 0),
				Gizmo.ViewCubeLeftEdgeMinusZ => new Vector3(0, 45, 0),
				Gizmo.ViewCubeTopEdgeZ => new Vector3(45, 180, 0),
				Gizmo.ViewCubeTopEdgeMinusZ => new Vector3(45, 0, 0),
				Gizmo.ViewCubeTopEdgeMinusX => new Vector3(45, -90, 0),
				Gizmo.ViewCubeBottomEdgeMinusZ => new Vector3(-45, 0, 0),
				Gizmo.ViewCubeBottomEdgeMinusX => new Vector3(-45, -90, 0),
				Gizmo.ViewCubeRightEdgeMinusZ => new Vector3(0, -45, 0),
				Gizmo.ViewCubeRightEdgeZ => new Vector3(0, 135, 0),
				Gizmo.ViewCubeTopEdgeX => new Vector3(45, 90, 0),
				Gizmo.ViewCubeLeftEdgeZ => new Vector3(0, -135, 0),
				Gizmo.ViewCubeBottomEdgeX => new Vector3(-45, 90, 0),


				Gizmo.ViewCubeLowerLeftCornerX => new Vector3(-45, 135, 0),
				Gizmo.ViewCubeLowerRightCornerX => new Vector3(-45, 45, 0),
				Gizmo.ViewCubeUpperRightCornerX => new Vector3(45, 45, 0),
				Gizmo.ViewCubeUpperLeftCornerX => new Vector3(45, 135, 0),
				Gizmo.ViewCubeLowerLeftCornerMinusX => new Vector3(-45, -45, 0),
				Gizmo.ViewCubeLowerRightCornerMinusX => new Vector3(-45, -135, 0),
				Gizmo.ViewCubeUpperRightCornerMinusX => new Vector3(45, -135, 0),
				Gizmo.ViewCubeUpperLeftCornerMinusX => new Vector3(45, -45, 0),
				_ => new Vector3(0)
			};

			if (entity is not null)
			{//TODO calculate radius based on bounding box or instead rely on centering tool
				var rotationMatrix = Matrix4x4.CreateRotationY(0) * Matrix4x4.CreateRotationX(0);

				var camPos = entity.transform.Position + Vector3.UnitZ * 100f; //entity.transform.Position - Vector3.Normalize(objTocamDir) * 300;
				var localPos = Vector3.Transform(entity.transform.Position - camPos, rotationMatrix);
				var newAngles = rotateVector * NumericsExtensions.Deg2Rad;
				Camera.main.Parent.transform.Rotation = rotateVector;
				var newRotationMatrix = Matrix4x4.CreateRotationY(newAngles.Y) * Matrix4x4.CreateRotationX(newAngles.X);

				var pos = Vector3.Transform(localPos, newRotationMatrix.Inverted());
				var newCameraPos = entity.transform.Position - pos;
				Camera.main.ViewMatrix = Matrix4x4.CreateTranslation(-newCameraPos) * newRotationMatrix;
				Camera.main.Parent.transform.Position = newCameraPos;
			}
			else
			{
				Camera.main.Parent.transform.Rotation = rotateVector;
				var translationMatrix = Matrix4x4.CreateTranslation(-Camera.main.Parent.transform.Position);
				var angles = Camera.main.Parent.transform.Rotation * NumericsExtensions.Deg2Rad;
				var rotationMatrix = Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationX(angles.X);
				Camera.main.ViewMatrix = translationMatrix * rotationMatrix; //pan
			}
		}
		public static void HandleScale(Entity entity, ref Ray ray)
		{

			// move
			if (relativeOrigin.HasValue)
			{
				entity.transform.ModelMatrix.OrthoNormalize();
				float len = ray.IntersectPlane(transformationPlane); // near plan
				var newPos = ray.origin + ray.direction * len;

				// compute delta
				Vector3 newOrigin = newPos - relativeOrigin.Value * Camera.main.AspectRatio;
				Vector3 delta = newOrigin - entity.transform.Position;

				// 1 axis constraint
				Matrix4x4.Decompose(entity.transform.ModelMatrix, out var scale, out var rot, out var trans);

				var (direction, snapLimit) = selectedGizmoId switch
				{
					Gizmo.ScaleX => (entity.transform.ModelMatrix.Right(), SceneView.scaleSnap.X),
					Gizmo.ScaleY => (entity.transform.ModelMatrix.Up(), SceneView.scaleSnap.Y),
					_ => (entity.transform.ModelMatrix.Forward(), SceneView.scaleSnap.Z)
				};
				Vector3 axisValue = direction;// direction;
				float lengthOnAxis = Vector3.Dot(axisValue, delta);
				delta = axisValue * lengthOnAxis;

				// compute matrix & delta

				scaleOffset = delta;
				Vector3 baseVector = translationPlaneOrigin - entity.transform.Position;
				float ratio = Vector3.Dot(axisValue, baseVector + delta) / Vector3.Dot(axisValue, baseVector);
				//if (float.IsNaN(ratio) || float.IsInfinity(ratio)) ratio = float.MaxValue;
				var newScale = Math.Clamp(MathF.Max(ratio, 0.00001f), float.MinValue, float.MaxValue);
				if (SceneView.snapMode)
				{
					newScale -= 1f;
					ComputeSnap(ref newScale, snapLimit);
					newScale += 1f;
					newScale = Math.Clamp(MathF.Max(newScale, 0.00001f), float.MinValue, float.MaxValue);
				}
				var vScale = useUniformScale ? new Vector3(newScale, newScale, newScale) : selectedGizmoId switch
				{
					Gizmo.ScaleX => new Vector3(newScale, 1, 1),
					Gizmo.ScaleY => new Vector3(1, newScale, 1),
					Gizmo.ScaleZ => new Vector3(1, 1, newScale),
					_ => Vector3.One
				};
				vScale *= scaleSource.Value;
				Matrix4x4 scaleOrigin = Matrix4x4.CreateScale(vScale);

				Matrix4x4 translateOrigin = Matrix4x4.CreateTranslation(trans);
				Matrix4x4 rotateOrigin = Matrix4x4.CreateFromQuaternion(rot);

				entity.transform.Scale = vScale;//new Vector3(float.IsNaN(newScale.X) || float.IsInfinity(newScale.X) ? 1 : newScale.X, float.IsNaN(newScale.Y) || float.IsInfinity(newScale.Y) ? 1 : newScale.Y, float.IsNaN(newScale.Z) || float.IsInfinity(newScale.Z) ? 1 : newScale.Z);
				entity.transform.ModelMatrix = scaleOrigin * rotateOrigin * translateOrigin;
				entity.transform.onTransformChanged?.Invoke();
			}
			else
			{
				entity.transform.ModelMatrix.DecomposeDirections(out var eRight, out var eUp, out var eForward);
				Vector3[] movePlanNormal = { eRight, eUp, eForward };

				var index = selectedGizmoId switch
				{
					Gizmo.ScaleX => 1,
					Gizmo.ScaleY => 0,//TODO: choose 0 or 2 based on camera view?
					Gizmo.ScaleZ => 1,
					_ => throw new NotSupportedException($"Scale doesnt support {selectedGizmoId}")
				};
				startMat = entity.transform.ModelMatrix;
				startMat.DecomposeDirections(out var right, out var up, out var forward);
				scaleSource = new Vector3(right.Length(), up.Length(), forward.Length());
				var plane = BuildPlane(entity.transform.Position, movePlanNormal[index]);//TODO: bugged look up imguizmo again
				transformationPlane = new Vector4(plane.Normal, plane.D);
				float len = ray.IntersectPlane(transformationPlane); // near plan
				var newPos = ray.origin + ray.direction * len;
				translationPlaneOrigin = newPos;
				relativeOrigin = (newPos - entity.transform.Position) * (1.0f / Camera.main.AspectRatio);

			}
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
			var len = ray.IntersectPlane(plane);
			var localPos = (ray.origin + ray.direction * len - entity.transform.Position).Normalize();
			var perpendicularVect = Vector3.Cross(rotVectSource.Value, new Vector3(plane.X, plane.Y, plane.Z)).Normalize();
			var angle = MathF.Acos(Math.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -1f, 1f));
			angle *= (Vector3.Dot(localPos, perpendicularVect) < 0) ? 1f : -1f;
			return angle;
		}

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
			return ray.IntersectPlane(transformationPlane);
		}
	}
}
