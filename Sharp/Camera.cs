using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Sharp
{
	public class Camera: Component
	{
		public static Camera main;
		public Frustum frustum;
		public Matrix4 projectionMatrix;
		public Matrix4 modelViewMatrix;
		public bool moved=false;

		#region Constructors
		public Camera()
		{
			Speed = 50.0f;
			TargetPosition =  new Vector3();
			//TargetOrientation = new Quaternion();
			MouseRotation = new Vector2(0, 0);
			Movement = new Vector3(0, 0, 0);
			//MouseLookEnabled = mouseLook;

			AspectRatio = 1f;
			FieldOfView = 75;
			ZNear = 0.1f;
			ZFar = 1000f;
			//Orientation = TargetOrientation;
			SetProjectionMatrix ();
		}

		#endregion

		#region Members

		#region Properties

		//public Quaternion Orientation { get; set; }

		public Vector3 TargetPosition { get; set; }
		//public Quaternion TargetOrientation { get; set; }
		//public Quaternion TargetOrientationY { get; set; }

public float MouseYSensitivity=1f;
public float MouseXSensitivity=1f;

public Vector2 MouseRotation;
public Vector3 Movement;

public float Speed { get; set; }
public float Acceleration { get; set; }

public float ZNear { get; set; }
public float ZFar { get; set; }
public float FieldOfView { get; set; }
public float AspectRatio { get; set; }

public CamMode CameraMode=CamMode.FlightCamera;

#endregion

#region Public Methods
public void Update()
{

	if (TargetPosition !=entityObject.position)
	{
		entityObject.position = Vector3.Lerp(entityObject.position, TargetPosition, 1);
	}
}

public void SetProjectionMatrix()
{
	projectionMatrix  = Matrix4.CreatePerspectiveFieldOfView((float)(FieldOfView * Math.PI / 180.0), AspectRatio, ZNear, ZFar);
}

public void SetModelviewMatrix()
{
	var translationMatrix = Matrix4.CreateTranslation(-entityObject.position);
			var rotationMatrix = Matrix4.CreateFromQuaternion(ToQuaterion(entityObject.rotation));
			//modelViewMatrix = rotationMatrix*translationMatrix; orbit 
			modelViewMatrix = translationMatrix*rotationMatrix; //pan
}


		public static Matrix4 billboard(Vector3 position, Vector3 cameraPos, Vector3 cameraUp) {
			Vector3 look =Vector3.Normalize(cameraPos - position);
			Vector3 right =Vector3.Cross(cameraUp, look);
			Vector3 up2 = Vector3.Cross(look, right);
			Matrix4 transform=new Matrix4();
			transform.M11 = right.X;
			transform.M12 = right.Y;
			transform.M13 = right.Z;
			transform.M14 = 0.0f;
			transform.M21 = up2.X;
			transform.M22 = up2.Y;
			transform.M23 = up2.Z;
			transform.M24 = 0.0f;
			transform.M31 = look.X;
			transform.M32 = look.Y;
			transform.M33 = look.Z;
			transform.M34 = 0.0f;
			transform.M41 =position.X;
			transform.M42 = position.Y;
			transform.M43 = position.Z;
			transform.M44 = 1.0f;
			// Uncomment this line to translate the position as well
			// (without it, it's just a rotation)
			//transform[3] = vec4(position, 0);
			return transform;
		}

		private static bool WithinEpsilon(float a, float b)
		{
			float num = a - b;
			return ((-1.401298E-45f <= num) && (num <= float.Epsilon));
		}
		public Vector3 ScreenToWorld(int x, int y, int width, int height,float time=-1f)
		{
			Vector4 vec;

			vec.X = 2.0f * x / (float)width - 1;
			vec.Y = -(2.0f * y / (float)height - 1);
			vec.Z = time;
			vec.W = 1.0f;

			Matrix4 viewInv = modelViewMatrix.Inverted ();
			Matrix4 projInv = Camera.main.projectionMatrix.Inverted();

			Vector4.Transform(ref vec, ref projInv, out vec);
			Vector4.Transform(ref vec, ref viewInv, out vec);

			if (vec.W > 0.000001f || vec.W < -0.000001f)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}

			return vec.Xyz;
		}
		public Vector3 WorldToScreen( Vector3 pos, int width, int height) {
			
			var pos4 = Vector4.Transform (new Vector4 (pos, 1),modelViewMatrix*projectionMatrix);

			var NDCSpace=pos4.Xyz/pos4.W;
			return new Vector3(NDCSpace.X*(width/(2f)),NDCSpace.Y*(height/(2f)),NDCSpace.Z);//look at divide part
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
		public Quaternion ToQuaterion(Vector3 angles) {
			// Assuming the angles are in radians.
			angles *= MathHelper.Pi / 180f;

			return Quaternion.FromMatrix (Matrix3.CreateRotationX (angles.Y) * Matrix3.CreateRotationY (angles.X) * Matrix3.CreateRotationZ (angles.Z));
		}
		public Vector3 ToEuler(Quaternion q) 
		{ 
			float sqw = q.W*q.W;
			float sqx = q.X*q.X;
			float sqy = q.Y*q.Y;
			float sqz = q.Z*q.Z;
			float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
			float test = q.X*q.Y + q.Z*q.W;
				if (test > 0.499*unit) { // singularity at north pole
				return new Vector3 (2f * (float)Math.Atan2(q.X,q.W),MathHelper.Pi/2,0)*180f/MathHelper.Pi;
				}
				if (test < -0.499*unit) { // singularity at south pole
				return new Vector3 (-2f * (float)Math.Atan2(q.X,q.W),-MathHelper.Pi/2,0)*180f/MathHelper.Pi;
				}
			return new Vector3 ((float)Math.Atan2 (2 * q.Y * q.W - 2 * q.X * q.Z, sqx - sqy - sqz + sqw),
				(float)Math.Asin (2 * test / unit),
				(float)Math.Atan2 (2 * q.X * q.W - 2 * q.Y * q.Z, -sqx + sqy - sqz + sqw))*180f/MathHelper.Pi;

		}  
#endregion

#region Protected Methods

/// <summary>
/// Clamps the mouse rotation values
/// </summary>
protected void ClampMouseValues()
{
			if (entityObject.rotation.Y >=360) //360 degrees in radians (or something in radians)
				entityObject.rotation.Y-= 360;
			if (entityObject.rotation.Y <= -360)
				entityObject.rotation.Y += 360;
			
			if (entityObject.rotation.X >=360) //360 degrees in radians (or something in radians)
				entityObject.rotation.X-= 360;
			if (entityObject.rotation.X <= -360)
				entityObject.rotation.X += 360;
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
public void Rotate(float x, float y, float time=1)
{
			entityObject.rotation.X +=(x * MouseXSensitivity * time);
			entityObject.rotation.Y +=(y * MouseYSensitivity * time);
	//Console.WriteLine("Rotation={0}", MouseRotation);
	//ClampMouseValues();

	SetModelviewMatrix ();
			if(frustum!=null)
	frustum.Update (Camera.main.modelViewMatrix*Camera.main.projectionMatrix);
	//ResetMouse();

}

/// <summary>
/// Updates the Position vector for this camera
/// </summary>
/// <param name="time">
/// A <see cref="System.Double"/> containing the time since the last update
/// </param>
public void Move(float x, float y,float z, float time=1f)
{
	Movement.X = 0;
	Movement.Y = 0;
	Movement.Z = 0;
	if (x != 0) {
		Movement.X = x*time;
	}
	else if (y != 0) {
		Movement.Y = y*time;
	}else if (z != 0) {
		Movement.Z = z*time;
	}
	if (CameraMode == CamMode.FirstPerson)
	{
				TargetPosition += Vector3.Transform(Movement, Quaternion.Invert(ToQuaterion(entityObject.rotation)));
		TargetPosition = new Vector3(TargetPosition.X, 5, TargetPosition.Z);
	}
	else
				TargetPosition += Vector3.Transform(Movement, Quaternion.Invert(ToQuaterion(entityObject.rotation)));
	if (CameraMode != CamMode.FlightCamera)
		entityObject.position = TargetPosition;
	
	SetModelviewMatrix ();
	SetProjectionMatrix ();
	
	frustum.Update (Camera.main.modelViewMatrix*Camera.main.projectionMatrix);
	moved = true;
}

#endregion

#endregion

}
	public enum CamMode
	{
		FlightCamera,
		FirstPerson,
		NoClip
	}
}