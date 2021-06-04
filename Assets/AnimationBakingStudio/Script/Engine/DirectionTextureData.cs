using System;
using UnityEngine;

namespace ABS
{
	[Serializable]
	public class DirectionTextureData
	{
		public int angle;
		public Texture2D modelTexture;
		public Texture2D normalMapTexture;

		public DirectionTextureData()
        {
			angle = 0;
			modelTexture = null;
			normalMapTexture = null;
		}

		public DirectionTextureData(int angle, Texture2D modelTexture, Texture2D normalMapTexture)
		{
			this.angle = angle;
			this.modelTexture = modelTexture;
			this.normalMapTexture = normalMapTexture;
		}
	}
}
