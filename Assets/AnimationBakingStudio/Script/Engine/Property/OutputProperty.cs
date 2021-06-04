using System;
using UnityEngine;

namespace ABS
{
	[Serializable]
	public class OutputProperty : PropertyBase
	{
		public bool animationClipMake = true;
		public int frameRate = 20;
		public int frameInterval = 1;
		public bool animatorControllerMake = false;
		public bool prefabMake = false;
		public bool isCompactCollider = false;
		public bool locationPrefabMake = false;
		public GameObject locationSpritePrefab;
		public bool normalMapMake = false;
		public bool isGrayscaleMap = false;

		public bool DoesMakeLocationPrefab()
        {
			return locationPrefabMake && locationSpritePrefab != null;
		}
	}
}
