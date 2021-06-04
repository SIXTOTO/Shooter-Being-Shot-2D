using System;

namespace ABS
{
	[Serializable]
	public class Resolution
	{
		public int width;
		public int height;

		public Resolution(int width, int height)
        {
			this.width = width;
			this.height = height;
		}

		public Resolution(float width, float height)
		{
			this.width = (int)width;
			this.height = (int)height;
		}
	}
}
