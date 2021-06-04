using System;
using System.Collections.Generic;

namespace ABS
{
	[Serializable]
	public class AnimationTextureData
	{
		public string stateName;
		public List<DirectionTextureData> directionTextureDataList;

		public AnimationTextureData()
        {
			stateName = "";
			directionTextureDataList = new List<DirectionTextureData>();
		}

		public AnimationTextureData(string stateName)
		{
			this.stateName = stateName;
			directionTextureDataList = new List<DirectionTextureData>();
		}
	}
}
