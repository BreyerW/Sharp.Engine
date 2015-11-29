using System;
using OpenTK;

namespace Sharp
{
	[Serializable]
	public struct BoundingBox /*: IEquatable<BoundingBox>*/
	{
		#region Public Fields

		public Vector3 Min;
		public Vector3 Max;

		#endregion Public Fields

		public BoundingBox(Vector3 min, Vector3 max)
		{
			this.Min = min;
			this.Max = max;
		}
		public Vector3[] GetCorners()
		{
			return new Vector3[] {
				new Vector3(this.Min.X, this.Max.Y, this.Max.Z), 
				new Vector3(this.Max.X, this.Max.Y, this.Max.Z),
				new Vector3(this.Max.X, this.Min.Y, this.Max.Z), 
				new Vector3(this.Min.X, this.Min.Y, this.Max.Z), 
				new Vector3(this.Min.X, this.Max.Y, this.Min.Z),
				new Vector3(this.Max.X, this.Max.Y, this.Min.Z),
				new Vector3(this.Max.X, this.Min.Y, this.Min.Z),
				new Vector3(this.Min.X, this.Min.Y, this.Min.Z)
			};
		}


		public override int GetHashCode()
		{
			return this.Min.GetHashCode() + this.Max.GetHashCode();
		}
		public Vector3 getPositiveVertex( Vector3 normal, Matrix4 modelMatrix )
		{
			Vector3 positiveVertex = Min;// add /scale

			if( normal.X >= 0.0f ) positiveVertex.X =Max.X;
			if( normal.Y >= 0.0f ) positiveVertex.Y =Max.Y;
			if( normal.Z >= 0.0f ) positiveVertex.Z =Max.Z;

			return Vector3.TransformPosition(positiveVertex, modelMatrix);
		}

		public Vector3 getNegativeVertex( Vector3 normal, Matrix4 modelMatrix )
		{
			Vector3 negativeVertex =Max;

			if( normal.X >= 0.0f ) negativeVertex.X =Min.X;
			if( normal.Y >= 0.0f ) negativeVertex.Y =Min.Y;
			if( normal.Z >= 0.0f ) negativeVertex.Z =Min.Z;

			return Vector3.TransformPosition(negativeVertex, modelMatrix);
		}

	}
}   


