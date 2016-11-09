using System;
using System.Drawing;
using OpenTK;
using Sharp.Editor.Views;

namespace Sharp.Editor
{
    public static class Manipulators
    {
        public static readonly Color selectedColor = Color.FromArgb(255, 128, 16);
        public static readonly Color xColor = ColorTranslator.FromHtml("#FFAA0000");
        public static readonly Color yColor = ColorTranslator.FromHtml("#FF00AA00");
        public static readonly Color zColor = ColorTranslator.FromHtml("#FF0000AA");

        internal static int selectedAxisId = 0;
        internal static float? rotAngleOrigin;
        internal static Vector3? relativeOrigin;
        internal static Vector3? planeOrigin;
        internal static Vector3? rotVectSource;
        public static void DrawCombinedGizmos(Vector3 pos)
        {
            DrawCombinedGizmos(pos, (selectedAxisId == 1 ? selectedColor : xColor), (selectedAxisId == 2 ? selectedColor : yColor), (selectedAxisId == 3 ? selectedColor : zColor), (selectedAxisId == 4 ? selectedColor : xColor), (selectedAxisId == 5 ? selectedColor : yColor), (selectedAxisId == 6 ? selectedColor : zColor), (selectedAxisId == 7 ? selectedColor : xColor), (selectedAxisId == 8 ? selectedColor : yColor), (selectedAxisId == 9 ? selectedColor : zColor), 3f);
        }
        public static void DrawCombinedGizmos(Vector3 pos, Color xColor, Color yColor, Color zColor, Color xRotColor, Color yRotColor, Color zRotColor, Color xScaleColor, Color yScaleColor, Color zScaleColor, float thickness = 5f)
        {
            float scale = (Camera.main.entityObject.Position - pos).Length / 25f;
            DrawHelper.DrawTranslationGizmo(thickness, scale, xColor, yColor, zColor);
            DrawHelper.DrawRotationGizmo(thickness, scale, xRotColor, yRotColor, zRotColor);
            DrawHelper.DrawScaleGizmo(thickness, scale, xScaleColor, yScaleColor, zScaleColor);
        }
        //public static (Color, Color, Color) ComputeColorId() {

        //}
        public static void Reset()
        {
            rotVectSource = null;
            rotAngleOrigin = null;
            planeOrigin = null;
            relativeOrigin = null;
        }
        private static Vector3 GetAxis()
        {
            if (selectedAxisId == 1 || selectedAxisId == 4 || selectedAxisId == 7)
            {
                return Vector3.UnitX;
            }
            else if (selectedAxisId == 2 || selectedAxisId == 5 || selectedAxisId == 8)
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
                v = Vector3.TransformVector(v, entity.ModelMatrix.ClearTranslation().ClearScale()).Normalized();
            var len = ComputeLength(ref ray, entity.Position);
            if (!relativeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                relativeOrigin = (planeOrigin - entity.Position) * (1f / (0.1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix)));
            }
            var newPos = ray.origin + ray.direction * len;
            var newOrigin = newPos - relativeOrigin.Value * (0.1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix));
            var delta = newOrigin - entity.Position;
            var lenOnAxis = Vector3.Dot(delta, v);
            delta = v * lenOnAxis;
            entity.Position += delta;
        }
        public static void HandleRotation(Entity entity, ref Ray ray)
        {
            var v = GetAxis();
            if (!SceneView.globalMode)
            {
                //entity.SetModelMatrix();
                v = Vector3.TransformVector(v, entity.ModelMatrix).Normalized();
            }
            var plane = BuildPlane(entity.Position, v);
            var intersectPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
            var len = ComputeLength(ref ray, entity.Position);
            if (!rotVectSource.HasValue)
            {
                rotVectSource = (ray.origin + ray.direction * len - entity.Position).Normalized();
                rotAngleOrigin = ComputeAngleOnPlane(entity, ref ray, ref intersectPlane);
            }
            var angle = ComputeAngleOnPlane(entity, ref ray, ref intersectPlane);
            var rotAxisLocalSpace = Vector4.Transform(new Vector4(intersectPlane.Xyz, 0f), entity.ModelMatrix.Inverted()).Normalized();
            var deltaRot = Matrix4.CreateFromAxisAngle(rotAxisLocalSpace.Xyz, angle - rotAngleOrigin.Value);
            //entity.ModelMatrix = deltaRot * entity.ModelMatrix;
            entity.Rotation = Entity.rotationMatrixToEulerAngles(deltaRot * entity.ModelMatrix) * (180.0f / MathHelper.Pi);
            rotAngleOrigin = angle;
        }
        public static void HandleScale(Entity entity, ref Ray ray)
        {
            var v = GetAxis();
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
            entity.Scale = scale;
        }
        private static float GetUniform(Vector3 pos, Matrix4 mat)
        {
            var trf = new Vector4(pos, 1f);
            trf = Vector4.Transform(trf, mat);
            return trf.W;
        }
        private static System.Numerics.Plane BuildPlane(Vector3 pos, Vector3 normal)
        {
            System.Numerics.Vector4 baseForPlane = System.Numerics.Vector4.Zero;
            normal.Normalize();
            baseForPlane.W = Vector3.Dot(normal, pos);
            baseForPlane.X = normal.X;
            baseForPlane.Y = normal.Y;
            baseForPlane.Z = normal.Z;
            return new System.Numerics.Plane(baseForPlane);
        }
        private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
        {
            var len = ray.IntersectPlane(ref plane);
            var localPos = (ray.origin + ray.direction * len - entity.Position).Normalized();
            var perpendicularVect = Vector3.Cross(rotVectSource.Value, plane.Xyz).Normalized();
            var angle = Vector3.CalculateAngle(localPos, rotVectSource.Value);//(float)Math.Acos(MathHelper.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

            return angle *= (Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f;
        }
        private static double AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            var angle = Math.Atan2(vector1.Y, vector1.X) - Math.Atan2(vector2.Y, vector2.X);// * (180 / Math.PI);
            if (angle < 0)
            {
                //angle = angle + MathHelper.TwoPi;
            }
            return angle;
        }
        private static float ComputeLength(ref Ray ray, Vector3 pos)
        {
            var plane = BuildPlane(pos, -ray.direction);
            var intersectPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
            return ray.IntersectPlane(ref intersectPlane);
        }
    }
}

