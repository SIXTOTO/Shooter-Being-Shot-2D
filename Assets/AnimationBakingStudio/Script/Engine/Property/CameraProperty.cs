using System;

namespace ABS
{
	public enum CameraMode : int
    {
		Orthographic = 0,
		Perspective = 1
    }

	public enum DistanceType : int
	{
		Relative = 0,
		Absolute = 1
	}

	[Serializable]
	public class CameraProperty : PropertyBase
	{
		public CameraMode mode = CameraMode.Orthographic;
		public DistanceType distanceType = DistanceType.Relative;
		public float absoluteDistance = 5;
		public float relativeDistance = 2;
	}
}
