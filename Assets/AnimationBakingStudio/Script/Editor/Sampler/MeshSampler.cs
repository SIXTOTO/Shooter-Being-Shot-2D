using UnityEngine;

namespace ABS
{
	public class MeshSampler : Sampler
	{
		private readonly MeshModel meshModel;

		private readonly MeshAnimation animation;

		private readonly Vector3 modelBaseSize; // for dynamic simple shadow
		private readonly Vector3 simpleShadowBaseScale;

		public MeshSampler(Model model, MeshAnimation animation, Studio studio) : base(model, studio)
		{
			this.animation = animation;

			meshModel = model as MeshModel;

			if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
			{
				modelBaseSize = model.GetSize();
				simpleShadowBaseScale = studio.shadow.obj.transform.localScale;
			}
		}

		protected override void ClearAllFrames()
		{
			animation.selectedFrames.Clear();
		}

		protected override float GetTimeForRatio(float ratio)
		{
			return meshModel.GetTimeForRatio(animation.clip, ratio);
		}

		protected override void AnimateModel(Frame frame)
		{
			meshModel.Animate(animation, frame);
		}

		protected override void AddFrame(Frame frame)
        {
			animation.selectedFrames.Add(frame);
        }

        protected override void OnCaptureFrame_()
        {
			if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
				ShadowHelper.ScaleSimpleShadowDynamically(modelBaseSize, simpleShadowBaseScale, meshModel, studio);
		}
	}
}
