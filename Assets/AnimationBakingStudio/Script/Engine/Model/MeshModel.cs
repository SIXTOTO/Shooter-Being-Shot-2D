using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace ABS
{
    [Serializable]
    public class MeshAnimation
    {
        public AnimationClip clip;
        public int stateIndex;
        public string stateName;
        public bool isLooping = true;
        public AnimationCustomizer customizer;
        [NonSerialized]
        public List<Frame> selectedFrames = new List<Frame>();
    }

    public enum PivotType
    {
        Bottom = 0,
        Center
    }

    public class MeshModel : Model
    {
        public Renderer mainRenderer;

        public PivotType pivotType = PivotType.Bottom;

        public Transform rootBoneObj = null;
        public bool isFixingToOrigin = false;
        public bool isFixingToGround = false;

        public List<MeshAnimation> animations = new List<MeshAnimation>();

#if UNITY_EDITOR
        public AnimatorController referenceController;
        public AnimatorController outputController;
#endif

        public float currentAngle = 0.0f; // for humanoid model of 2018 or newer

        public override Vector3 ComputedCenter
        {
            get
            {
                Vector3 position = GetPosition();

                float x = position.x;
                float z = position.z;

                float y = 0;
                if (pivotType == PivotType.Center)
                    y = position.y;
                else if (pivotType == PivotType.Bottom)
                    y = position.y + GetSize().y / 2.0f;

                return new Vector3(x, y, z);
            }
        }

        public override Vector3 ComputedBottom
        {
            get
            {
                Vector3 position = GetPosition();

                float x = position.x;
                float z = position.z;

                float y = 0;
                if (!isGroundPivot)
                {
                    if (pivotType == PivotType.Center)
                        y = position.y - GetSize().y / 2.0f;
                    else if (pivotType == PivotType.Bottom)
                        y = position.y;
                }

                return new Vector3(x, y, z);
            }
        }

        public override Vector3 GetSize()
        {
            return mainRenderer != null ? mainRenderer.bounds.size : Vector3.one;
        }

        public override Vector3 GetDynamicSize()
        {
            if (mainRenderer != null)
            {
                SkinnedMeshRenderer skinnedMeshRndr = mainRenderer as SkinnedMeshRenderer;
                if (skinnedMeshRndr != null)
                    return skinnedMeshRndr.localBounds.size;
            }
            return Vector3.one;
        }

        public override Vector3 GetMinPos()
        {
            return mainRenderer != null ? mainRenderer.bounds.min : Vector3.zero;
        }

        public override Vector3 GetExactMinPos()
        {
            if (mainRenderer != null)
            {
                SkinnedMeshRenderer skinnedMeshRndr = mainRenderer as SkinnedMeshRenderer;
                if (skinnedMeshRndr != null)
                    return skinnedMeshRndr.localBounds.min;
            }
            return Vector3.zero;
        }

        public override Vector3 GetMaxPos()
        {
            return mainRenderer != null ? mainRenderer.bounds.max : Vector3.zero;
        }

        public override Vector3 GetExactMaxPos()
        {
            if (mainRenderer != null)
            {
                SkinnedMeshRenderer skinnedMeshRndr = mainRenderer as SkinnedMeshRenderer;
                if (skinnedMeshRndr != null)
                    return skinnedMeshRndr.localBounds.max;
            }
            return Vector3.zero;
        }

        public static Renderer FindBiggestRenderer(GameObject rootObject)
        {
            Renderer biggestRenderer = null;

            Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Dictionary<float, Renderer> dic = new Dictionary<float, Renderer>();
                foreach (Renderer rndr in renderers)
                {
                    if (!dic.ContainsKey(rndr.bounds.size.sqrMagnitude))
                        dic.Add(rndr.bounds.size.sqrMagnitude, rndr);
                }
                var sortedDic = dic.OrderByDescending(obj => obj.Key);
                KeyValuePair<float, Renderer> biggest = sortedDic.ElementAt(0);
                biggestRenderer = biggest.Value;
            }

            return biggestRenderer;
        }

        public void AddAnimation(MeshAnimation anim)
        {
            bool assigned = false;
            for (int i = 0; i < animations.Count; ++i)
            {
                if (animations[i] == null || animations[i].clip == null)
                {
                    animations[i] = anim;
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
                animations.Add(anim);
        }

        public List<MeshAnimation> GetValidAnimations()
        {
            List<MeshAnimation> validAnimations = new List<MeshAnimation>();
            foreach (MeshAnimation anim in animations)
            {
                if (anim.clip != null)
                    validAnimations.Add(anim);
            }

            return validAnimations;
        }

        public override bool IsReady()
        {
            return (mainRenderer != null);
        }

        public override bool IsTileAvailable()
        {
            return true;
        }

        public bool IsFixingToOrigin()
        {
            return rootBoneObj != null && isFixingToOrigin;
        }

        public bool IsSkinnedModel()
        {
            if (mainRenderer == null)
                return false;
            return (mainRenderer is SkinnedMeshRenderer);
        }

        public override void ClearFrames()
        {
            foreach (MeshAnimation animation in animations)
                animation.selectedFrames.Clear();
        }

        public float GetTimeForRatio(AnimationClip clip, float ratio)
        {
            if (clip == null)
                return 0f;
            return clip.length * ratio;
        }

        public void Animate(MeshAnimation animation, Frame frame)
        {
            if (animation == null || animation.clip == null)
                return;

            Animator animator = GetComponentInChildren<Animator>();
            bool isHumanoid2018 = (animator != null); // for 2018 or newer

            Vector3 originalPosition = transform.position;
            Vector3 movedVector = Vector3.zero - originalPosition;
            if (isHumanoid2018)
            {
                transform.position = Vector3.zero;
                Camera.main.transform.Translate(movedVector);
            }

            animation.clip.SampleAnimation(gameObject, frame.time);

            if (animation.customizer != null)
                animation.customizer.UpdateFrame(frame);

            if (isHumanoid2018 && currentAngle > float.Epsilon)
                Rotate(currentAngle);

            if (IsFixingToOrigin())
            {
                float bottomY = 0.0f;
                if (isFixingToGround)
                {
                    bottomY = float.MaxValue;
                    Transform[] transforms = rootBoneObj.GetComponentsInChildren<Transform>();
                    foreach (Transform trsf in transforms)
                        bottomY = Mathf.Min(trsf.position.y, bottomY);
                }

                Vector3 translation = new Vector3(ComputedBottom.x - rootBoneObj.position.x,
                                                  ComputedBottom.y - bottomY,
                                                  ComputedBottom.z - rootBoneObj.position.z);
                rootBoneObj.Translate(translation, Space.World);
            }

            if (isHumanoid2018)
            {
                transform.position = originalPosition;
                Camera.main.transform.Translate(-movedVector);
            }
        }

        public override void DrawGizmoMore()
        {
            float magnitude = GetSize().magnitude;

            Gizmos.color = Color.yellow;
            float sphereRadius = magnitude / 20.0f;
            Gizmos.DrawSphere(GetPosition(), sphereRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(ComputedCenter, sphereRadius);

            Gizmos.color = Color.cyan;
            sphereRadius = magnitude / 40.0f;
            Gizmos.DrawSphere(GetPivotPosition(), sphereRadius);
        }
    }
}
