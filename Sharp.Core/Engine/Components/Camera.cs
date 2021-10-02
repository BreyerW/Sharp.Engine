using Sharp.Core;
using SharpAsset;
using System;
using System.Collections.Generic;
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
                //if (main != null)
                //	Material.BindGlobalProperty("camOrtho", main.orthoMatrix);
            }
        }
        public Matrix4x4 ViewMatrix
        {
            get
            {
                return Parent.transform.ModelMatrix.Inverted();
            }
            internal set
            {
                Parent.transform.ModelMatrix = value;
                if (main is not null)
                {
                    Material.BindGlobalProperty("camView", main.Parent.transform.ModelMatrix);
                    Material.BindGlobalProperty("camWorldView", main.Parent.transform.ModelMatrix.Inverted());
                }
            }
        }

        public bool moved = false;

        #region Constructors

        static Camera()
        {
        }

        public Camera(Entity p) : base(p)
        {
            Speed = 50.0f;
            //TargetPosition =  new Vector3();
            //TargetOrientation = new Quaternion();
            MouseRotation = new Vector2(0, 0);
            Movement = new Vector3(0, 0, 0);
            //MouseLookEnabled = mouseLook;

            AspectRatio = 1f;
            FieldOfView = 110f;
            ZNear = 0.2f;
            ZFar = 5000f;
            Material.BindGlobalProperty("camNearFar", new Vector2(ZNear, ZFar));
            SetProjectionMatrix();
            cullingTags.SetTag("Nothing");
        }

        #endregion Constructors

        #region Members

        #region Properties

        public float MouseYSensitivity = 1f;
        public float MouseXSensitivity = 1f;
        public Entity pivot;
        public Vector2 MouseRotation;
        public Vector3 Movement;
        public BitMask cullingTags = new(0);
        public float Speed { get; set; }
        public float Acceleration { get; set; }
        public float ZNear { get; set; }
        public float ZFar { get; set; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        private int width;
        private int height;
        public int Width
        {
            set
            {
                if (value == width) return;
                width = value;
                OnDimensionChanged?.Invoke(this);
            }
            get => width;
        }
        public int Height
        {
            set
            {
                if (value == height) return;
                height = value;
                OnDimensionChanged?.Invoke(this);
            }
            get => height;
        }
        public Action<Camera> OnDimensionChanged;
        public CamMode CameraMode = CamMode.Flight;

        #endregion Properties

        #region Public Methods


        public void SetProjectionMatrix()
        {
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView((float)(FieldOfView * NumericsExtensions.Deg2Rad), AspectRatio, ZNear, ZFar);

        }

        public void SetOrthoMatrix(int width, int height/*int left, int right, int bottom, int top*/)
        {
            OrthoMatrix = Matrix4x4.CreateOrthographicOffCenter(300, 0, 70, 0, -1, 1); //Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1, 1); //Matrix4x4.CreateOrthographic(width, height, -1, 1); //
        }

        public void SetModelviewMatrix()
        {
            var translationMatrix = Matrix4x4.CreateTranslation(-Parent.transform.Position);
            var angles = Parent.transform.Rotation * NumericsExtensions.Deg2Rad;

            var rotationMatrix = Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationZ(angles.Z);
            //ViewMatrix = rotationMatrix * translationMatrix * Matrix4x4.CreateTranslation(0, 0, -20); //orbit
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

            vec.X = 2.0f * x / (float)width - 1;//screentondc
            vec.Y = -(2.0f * y / (float)height - 1);
            vec.Z = time;
            vec.W = 1.0f;

            var viewInv = ViewMatrix;//.Inverted();
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
        public Vector3 NDCToWorld(Vector3 vec)
        {
            var viewInv = ViewMatrix;
            var projInv = main.projectionMatrix.Inverted();
            var newVec = new Vector4(vec, 1f).Transform(projInv * viewInv);

            if (newVec.W > 0.000001f || newVec.W < -0.000001f)
            {
                newVec.X /= newVec.W;
                newVec.Y /= newVec.W;
                newVec.Z /= newVec.W;
            }
            return new Vector3(newVec.X, newVec.Y, newVec.Z);
        }
        public Vector3 WorldToScreen(Vector3 pos, int width, int height)
        {
            var pos4 = Vector4.Transform(new Vector4(pos, 1), ViewMatrix * projectionMatrix);
            var NDCSpace = new Vector3(pos4.X, pos4.Y, pos4.Z) / pos4.W;
            return new Vector3((NDCSpace.X + 1) * (width / (2f)), (NDCSpace.Y + 1) * (height / (2f)), NDCSpace.Z);//look at divide part
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


            if (pivot is not null)
            {
                var angles = Parent.transform.Rotation * NumericsExtensions.Deg2Rad;
                var origRotateMatrix = Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationX(angles.X);
                var localPos = Vector3.Transform(pivot.transform.Position - Parent.transform.Position, origRotateMatrix);
                var newAngles = newRot * NumericsExtensions.Deg2Rad;

                var rotationMatrix = Matrix4x4.CreateRotationY(newAngles.Y) * Matrix4x4.CreateRotationX(newAngles.X);
                ViewMatrix = rotationMatrix;
                var pos = Vector3.Transform(localPos, ViewMatrix);
                var newCameraPos = pivot.transform.Position - pos;
                ViewMatrix = Matrix4x4.CreateTranslation(-newCameraPos) * rotationMatrix;
                Parent.transform.Position = newCameraPos;
            }
            else
                SetModelviewMatrix();
            Parent.transform.Rotation = newRot;
            //Console.WriteLine("Rotation={0}", MouseRotation);
            //ClampMouseValues();
            //ResetMouse();
        }

        /// <summary>
        /// Updates the Position vector for this camera
        /// </summary>
        /// <param name="time">
        /// A <see cref="System.Double"/> containing the time since the last update
        /// </param>
        public void Move(Vector3 axis, float distance, float time = 0.2f)
        {
            Parent.transform.Position += distance * axis.Transform(Quaternion.Inverse(Parent.ToQuaterion(Parent.transform.Rotation)));

            SetModelviewMatrix();
            SetProjectionMatrix();

            moved = true;
        }

        #endregion Protected Methods

        #endregion Members
    }

    public enum CamMode
    {
        Flight,
        Orbit
    }
}