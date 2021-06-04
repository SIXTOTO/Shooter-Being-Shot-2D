using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    [DisallowMultipleComponent]
    public abstract class Model : MonoBehaviour
    {
        public bool isGroundPivot = false;

        public bool isSpecificSimpleShadow = false;
        public Vector2 simpleShadowScale = Vector3.one;

        public GameObject spritePrefab;
        public PrefabBuilder prefabBuilder;

        public string nameSuffix = "";

        private Dictionary<int, string> shaderBackup = new Dictionary<int, string>();

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public virtual Vector3 ComputedCenter
        {
            get
            {
                return GetPosition();
            }
        }

        public virtual Vector3 ComputedBottom
        {
            get
            {
                Vector3 position = GetPosition();

                float x = position.x;
                float y = isGroundPivot ? 0 : position.y;
                float z = position.z;

                return new Vector3(x, y, z);
            }
        }

        public Vector3 ComputedForward
        {
            get
            {
                return transform.forward;
            }
        }

        public Vector3 ComputedRight
        {
            get
            {
                return transform.right;
            }
        }

        public abstract Vector3 GetSize();
        public virtual Vector3 GetDynamicSize()
        {
            return GetSize();
        }

        public abstract Vector3 GetMinPos();
        public virtual Vector3 GetExactMinPos()
        {
            return GetMinPos();
        }

        public abstract Vector3 GetMaxPos();
        public virtual Vector3 GetExactMaxPos()
        {
            return GetMaxPos();
        }

        public Vector3 GetPivotPosition()
        {
            return isGroundPivot ? ComputedBottom : GetPosition();
        }

        public abstract bool IsReady();

        public abstract bool IsTileAvailable();

        public static bool IsMeshModel(Model model)
        {
            return (model is MeshModel);
        }

        public static MeshModel AsMeshModel(Model model)
        {
            return (model as MeshModel);
        }

        public static bool IsParticleModel(Model model)
        {
            return (model is ParticleModel);
        }

        public static ParticleModel AsParticleModel(Model model)
        {
            return (model as ParticleModel);
        }

        public abstract void ClearFrames();

        public virtual void DrawGizmoMore() { }

        void OnDrawGizmos()
        {
            if (GetSize().magnitude == 0.0f)
                return;

            Vector3 position = GetPosition();

            Gizmos.color = Color.yellow;
            float lineLength = GetSize().magnitude;
            Vector3 headPos = position + transform.forward * lineLength;
            Gizmos.DrawLine(position, headPos);
            float arrowLength = lineLength / 10.0f;
            Vector3 arrowEnd = headPos + (-transform.forward) * arrowLength;
            Vector3 arrowEnd1 = arrowEnd + transform.right * arrowLength;
            Vector3 arrowEnd2 = arrowEnd + (-transform.right) * arrowLength;
            Gizmos.DrawLine(headPos, arrowEnd1);
            Gizmos.DrawLine(headPos, arrowEnd2);

            Gizmos.color = Color.magenta;
            headPos = ComputedCenter + ComputedForward * lineLength;
            Gizmos.DrawLine(ComputedCenter, headPos);
            arrowEnd = headPos + (-ComputedForward) * arrowLength;
            arrowEnd1 = arrowEnd + ComputedRight * arrowLength;
            arrowEnd2 = arrowEnd + (-ComputedRight) * arrowLength;
            Gizmos.DrawLine(headPos, arrowEnd1);
            Gizmos.DrawLine(headPos, arrowEnd2);

            DrawGizmoMore();
        }

        public void Rotate(float angle)
        {
            transform.localRotation = Quaternion.identity;
            float angleDiff = Vector3.Angle(ComputedForward, transform.forward);
            transform.localRotation = Quaternion.Euler(new Vector3(0, angleDiff + angle, 0));
        }

        public void BackupAllShaders()
        {
            if (shaderBackup.Keys.Count > 0)
                shaderBackup.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.sharedMaterials)
                {
                    if (mtrl != null && mtrl.shader != null)
                    {
                        if (!shaderBackup.ContainsKey(mtrl.GetInstanceID()))
                            shaderBackup.Add(mtrl.GetInstanceID(), mtrl.shader.name);
                    }
                }
            }
        }

        public void ChangeAllShaders(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("shader is null");
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.sharedMaterials)
                {
                    if (mtrl != null && mtrl.shader != null)
                        mtrl.shader = shader;
                }
            }
        }

        public void RestoreAllShaders()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.sharedMaterials)
                {
                    if (mtrl != null && mtrl.shader != null)
                        mtrl.shader = Shader.Find(shaderBackup[mtrl.GetInstanceID()]);
                }
            }

            shaderBackup.Clear();
        }
    }
}
