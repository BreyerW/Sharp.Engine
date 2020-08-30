﻿using Newtonsoft.Json;
using SharpAsset;
using System;
using System.Numerics;

namespace Sharp
{
	public class Camera : Component
	{
		public static Camera main;//rename to or add current camera and generate camera for each window and sceneview

		/* public static Camera main
         {
             set
             {
                 if (Main != value)
                 {
                     Main = value;
                     Material.SetGlobalProperty("camView", ref value.modelViewMatrix);
                     Material.SetGlobalProperty("camProjection", ref value.projectionMatrix);
                 }
             }
             get { return Main; }
         }*/

		public Frustum frustum;
		private Matrix4x4 projectionMatrix;
		public Matrix4x4 ProjectionMatrix
		{
			get
			{
				return projectionMatrix;
			}
			private set
			{
				projectionMatrix = value;
				if (main != null)
					Material.BindGlobalProperty("camProjection", main.projectionMatrix);
				// else
				//     Material.SetGlobalProperty("camProjection", ref projectionMatrix);
			}
		}

		private Matrix4x4 orthoMatrix;
		public Matrix4x4 OrthoMatrix
		{
			get
			{
				return orthoMatrix;
			}
			private set
			{
				orthoMatrix = value;
				if (main != null)
					Material.BindGlobalProperty("camOrtho", main.orthoMatrix);
			}
		}

		private Matrix4x4 modelViewMatrix;
		public Matrix4x4 ViewMatrix
		{
			get
			{
				return modelViewMatrix;
			}
			private set
			{
				modelViewMatrix = value;
				if (main != null)
				{
					Material.BindGlobalProperty("camView", main.modelViewMatrix);
					Material.BindGlobalProperty("camWorldView", main.modelViewMatrix.Inverted());
				}
			}
		}

		public bool moved = false;

		#region Constructors

		static Camera()
		{
		}

		public Camera(Entity entity) : base(entity)
		{
			Speed = 50.0f;
			//TargetPosition =  new Vector3();
			//TargetOrientation = new Quaternion();
			MouseRotation = new Vector2(0, 0);
			Movement = new Vector3(0, 0, 0);
			//MouseLookEnabled = mouseLook;

			AspectRatio = 1f;
			FieldOfView = 75;
			ZNear = 0.1f;
			ZFar = 1000f;
			//Orientation = TargetOrientation;
			SetProjectionMatrix();
		}

		#endregion Constructors

		#region Members

		#region Properties

		//public Quaternion Orientation { get; set; }

		//public Vector3 TargetPosition { get; set; }
		//public Quaternion TargetOrientation { get; set; }
		//public Quaternion TargetOrientationY { get; set; }

		public float MouseYSensitivity = 1f;
		public float MouseXSensitivity = 1f;

		public Vector2 MouseRotation;
		public Vector3 Movement;

		public float Speed { get; set; }
		public float Acceleration { get; set; }
		public float ZNear { get; set; }
		public float ZFar { get; set; }
		public float FieldOfView { get; set; }
		public float AspectRatio { get; set; }
		public int width;
		public int height;
		public CamMode CameraMode = CamMode.FlightCamera;

		#endregion Properties

		#region Public Methods


		public void SetProjectionMatrix()
		{
			ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView((float)(FieldOfView * Math.PI / 180.0), AspectRatio, ZNear, ZFar);
		}

		public void SetOrthoMatrix(int width, int height)
		{
			OrthoMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1); //Matrix4.CreateOrthographic(width, height, ZNear, ZFar);

			this.width = width;
			this.height = height;
		}

		public void SetModelviewMatrix()
		{
			var translationMatrix = Matrix4x4.CreateTranslation(-Parent.transform.Position);
			var angles = Parent.transform.Rotation * NumericsExtensions.Deg2Rad;
			var rotationMatrix = Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationZ(angles.Z);
			//modelViewMatrix = rotationMatrix*translationMatrix; orbit
			ViewMatrix = translationMatrix * rotationMatrix; //pan
		}

		private static bool WithinEpsilon(float a, float b)
		{
			float num = a - b;
			return ((-1.401298E-45f <= num) && (num <= float.Epsilon));
		}

		public Vector3 ScreenToWorld(int x, int y, int width, int height, float time = -1f)
		{
			Vector4 vec;

			vec.X = 2.0f * x / (float)width - 1;
			vec.Y = -(2.0f * y / (float)height - 1);
			vec.Z = time;
			vec.W = 1.0f;

			var viewInv = modelViewMatrix.Inverted();
			var projInv = main.projectionMatrix.Inverted();
			vec.Transform(projInv).Transform(viewInv);

			if (vec.W > 0.000001f || vec.W < -0.000001f)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}

			return new Vector3(vec.X, vec.Y, vec.Z);
		}

		public Vector3 WorldToScreen(Vector3 pos, int width, int height)
		{
			var pos4 = Vector4.Transform(new Vector4(pos, 1), modelViewMatrix * projectionMatrix);

			var NDCSpace = new Vector3(pos4.X, pos4.Y, pos4.Z) / pos4.W;
			return new Vector3(NDCSpace.X * (width / (2f)), NDCSpace.Y * (height / (2f)), NDCSpace.Z);//look at divide part
		}

		public Vector2 ScreenToNDC(Vector2 pos, int width, int height)
		{
			return ScreenToNDC((int)pos.X, (int)pos.Y, width, height);
		}

		public Vector2 ScreenToNDC(int x, int y, int width, int height)
		{
			Vector2 vec;

			vec.X = 2.0f * x / (float)width - 1;
			vec.Y = -(2.0f * y / (float)height - 1);
			return vec;
		}
		public Vector2 NDCToScreen(Vector2 pos, int width, int height)
		{
			return NDCToScreen(pos.X, pos.Y, width, height);
		}
		public Vector2 NDCToScreen(float x, float y, int width, int height)
		{
			Vector2 vec;

			vec.X = (x + 1) * width / 2f;
			vec.Y = -(y + 1) * height / 2f;
			return vec;
		}

		/// <summary>
		/// Sets up this camera with the specified Camera Mode
		/// </summary>
		/// <param name="mode">
		/// A <see cref="CamMode"/>
		/// </param>
		public void SetCameraMode(CamMode mode)
		{
			CameraMode = mode;
		}

		#endregion Public Methods

		#region Protected Methods

		/// <summary>
		/// Clamps the mouse rotation values
		/// </summary>
		protected void ClampMouseValues()
		{
			var newRot = Parent.transform.Rotation;
			if (newRot.Y >= 360) //360 degrees in radians (or something in radians)
				newRot.Y -= 360;
			if (newRot.Y <= -360)
				newRot.Y += 360;

			if (newRot.X >= 360) //360 degrees in radians (or something in radians)
				newRot.X -= 360;
			if (newRot.X <= -360)
				newRot.X += 360;
			Parent.transform.Rotation = newRot;
			/*if (MouseRotation.Y >= 6.28) //360 degrees in radians (or something in radians)
                MouseRotation.Y-= 6.28f;
            if (MouseRotation.Y <= -6.28)
                MouseRotation.Y += 6.28f;
            /*if (MouseRotation.Y >= 1.57) //90 degrees in radians
                        MouseRotation.Y = 1.57f;
                    if (MouseRotation.Y <= -1.57)
                        MouseRotation.Y = -1.57f;*/
		}

		/// <summary>
		/// Updates the Orientation Quaternion for this camera using the calculated Mouse Delta
		/// </summary>
		/// <param name="time">
		/// A <see cref="System.Double"/> containing the time since the last update
		/// </param>
		public void Rotate(float x, float y, float time = 1)
		{
			var newRot = Parent.transform.Rotation;
			newRot.X += (y * MouseXSensitivity * time);
			newRot.Y += (x * MouseYSensitivity * time);
			Parent.transform.Rotation = newRot;
			SetModelviewMatrix();
			//Console.WriteLine("Rotation={0}", MouseRotation);
			//ClampMouseValues();
			frustum?.Update(main.modelViewMatrix * main.projectionMatrix);
			//ResetMouse();
		}

		/// <summary>
		/// Updates the Position vector for this camera
		/// </summary>
		/// <param name="time">
		/// A <see cref="System.Double"/> containing the time since the last update
		/// </param>
		public void Move(float x, float y, float z, float time = 0.2f)
		{
			Movement.X = 0;
			Movement.Y = 0;
			Movement.Z = 0;
			if (x != 0)
			{
				Movement.X = x * time;
			}
			else if (y != 0)
			{
				Movement.Y = y * time;
			}
			else if (z != 0)
			{
				Movement.Z = z * time;
			}
			if (CameraMode == CamMode.FirstPerson)
			{
				Parent.transform.Position += Vector3.Transform(Movement, Quaternion.Inverse(Parent.ToQuaterion(Parent.transform.Rotation)));//Invert?
				Parent.transform.Position = new Vector3(Parent.transform.Position.X, 5, Parent.transform.Position.Z);
			}
			else
				Parent.transform.Position += Vector3.Transform(Movement, Quaternion.Inverse(Parent.ToQuaterion(Parent.transform.Rotation)));

			SetModelviewMatrix();
			SetProjectionMatrix();

			frustum.Update(Camera.main.modelViewMatrix * Camera.main.projectionMatrix);
			moved = true;
		}

		#endregion Protected Methods

		#endregion Members
	}

	public enum CamMode
	{
		FlightCamera,
		FirstPerson,
		NoClip
	}
}