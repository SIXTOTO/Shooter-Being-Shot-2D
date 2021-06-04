using System;
using UnityEngine;

namespace ABS
{
	[Serializable]
	public class SimpleShadowProperty
	{
		public bool isDynamicScale = false;
		public Vector2 scale = Vector3.one;
		public bool isSquareScale = false;
	}
}
