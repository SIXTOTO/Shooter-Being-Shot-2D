using UnityEngine;

namespace ABS
{
	public class ParticleSampler : Sampler
	{
		private readonly ParticleModel particleModel;

		private readonly Vector3 vecFromCameraToModel;

		public ParticleSampler(Model model, Studio studio): base(model, studio)
		{
			particleModel = model as ParticleModel;

			vecFromCameraToModel = model.transform.position - Camera.main.transform.position;
		}

		protected override void ClearAllFrames()
		{
			particleModel.selectedFrames.Clear();
		}

		protected override float GetTimeForRatio(float ratio)
        {
			return particleModel.GetTimeForRatio(ratio);
        }

		protected override void AnimateModel(Frame frame)
        {
			particleModel.Animate(frame);

            if (particleModel.IsCameraFollowing())
				DoCameraFollowing(particleModel, particleModel.transform.position, vecFromCameraToModel, studio);
        }

		public static void DoCameraFollowing(Model model, Vector3 modelPosition, Vector3 vecFromCameraToModel, Studio studio)
		{
			Camera.main.transform.position = modelPosition - vecFromCameraToModel;

			if (studio.shadow.type == ShadowType.TopDown)
				ShadowHelper.LocateShadowToModel(model, studio);

			if (studio.lit.cameraPositionFollow)
				studio.lit.com.transform.position = Camera.main.transform.position;
		}

		protected override void AddFrame(Frame frame)
		{
			particleModel.selectedFrames.Add(frame);
		}

		protected override void OnInitialize_()
		{
			if (studio.shadow.type == ShadowType.TopDown)
            {
				ObjectHelper.DeleteObject(EditorGlobal.DYNAMIC_SHADOW_NAME);
				studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.STATIC_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);

				ShadowHelper.LocateShadowToModel(particleModel, studio);

				Camera camera;
				GameObject fieldObj;
				ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

				CameraHelper.LookAtModel(camera.transform, particleModel);
				ShadowHelper.ScaleShadowField(camera, fieldObj);
			}
		}

		protected override void OnCaptureFrame_()
		{
			if (studio.shadow.type == ShadowType.TopDown)
            {
				Camera camera;
				GameObject fieldObj;
				ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);
				ShadowHelper.BakeStaticShadow(camera, fieldObj, particleModel, studio);
			}
		}

		protected override void Finish_()
		{
			if (studio.shadow.type == ShadowType.TopDown)
			{
				ObjectHelper.DeleteObject(EditorGlobal.STATIC_SHADOW_NAME);
				studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.DYNAMIC_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);

				ShadowHelper.LocateShadowToModel(particleModel, studio);
			}
		}
	}
}
