using UnityEngine;

namespace ABS
{
    public class CameraHelper
    {
        public static void LocateMainCameraToModel(Model model, Studio studio, float turnAngle = 0f)
        {
            if (Camera.main == null || model == null)
                return;

            Vector3 dirModelToCamera = new Vector3(0, Mathf.Sin(studio.view.slopeAngle * Mathf.Deg2Rad), Mathf.Cos(studio.view.slopeAngle * Mathf.Deg2Rad));
            float distModelToCamera = 500;

            if (Model.IsMeshModel(model))
            {
                if (studio.cam.distanceType == DistanceType.Relative)
                    distModelToCamera = model.GetSize().magnitude * studio.cam.relativeDistance;
                else if (studio.cam.distanceType == DistanceType.Absolute)
                    distModelToCamera = studio.cam.absoluteDistance;
            }

            Camera.main.transform.position = model.ComputedCenter + dirModelToCamera * distModelToCamera;
            Camera.main.transform.rotation = Quaternion.LookRotation(-dirModelToCamera);

            if (studio.view.rotationType == RotationType.Camera && turnAngle > float.Epsilon)
                Camera.main.transform.RotateAround(model.GetPosition(), Vector3.down, turnAngle);

            Camera.main.farClipPlane = distModelToCamera * 2;

            if (studio.lit.com != null)
            {
                if (studio.lit.cameraPositionFollow)
                    studio.lit.com.transform.position = Camera.main.transform.position;
                if (studio.lit.cameraRotationFollow)
                    studio.lit.com.transform.rotation = Camera.main.transform.rotation;
            }
        }

        public static void LookAtModel(Transform transf, Model model)
        {
            if (transf == null || model == null)
                return;

            Vector3 dirToModel = model.ComputedCenter - transf.position;
            Vector3 rightDir = Vector3.Cross(dirToModel, Vector3.up);
            transf.rotation = Quaternion.LookRotation(dirToModel.normalized, Vector3.Cross(dirToModel, rightDir));
        }
    }
}
