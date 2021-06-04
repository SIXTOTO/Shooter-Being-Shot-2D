using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public class ParticleModel : Model
    {
        public ParticleSystem mainParticleSystem;

        public AnimationClip animationClip;
        public float animStartTime;
        public bool isCameraFollowing = false;

        public bool isPrewarm = false;
        public bool isLooping = false;

        public bool targetChecked;

        public Vector3 maxSize = Vector3.one;
        public Vector3 minPos = Vector3.zero;
        public Vector3 maxPos = Vector3.zero;

        [NonSerialized]
        public List<Frame> selectedFrames = new List<Frame>();

        public bool TrySetMainParticleSystem()
        {
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            if (particleSystems.Length > 0)
            {
                mainParticleSystem = particleSystems[0];
                return true;
            }

            return false;
        }

        public void CheckModel()
        {
            if (targetChecked)
                return;
            targetChecked = true;

            const int FRAME_SIZE = 10;
            for (int i = 0; i < FRAME_SIZE; ++i)
            {
                float frameRatio = (float)i / (float)FRAME_SIZE;
                float frameTime = GetTimeForRatio(frameRatio);
                Animate(new Frame(i, frameTime));

                ParticleSystemRenderer[] particleSystemRenderers = mainParticleSystem.GetComponentsInChildren<ParticleSystemRenderer>();
                foreach (ParticleSystemRenderer renderer in particleSystemRenderers)
                {
                    if (maxSize.magnitude < renderer.bounds.size.magnitude)
                    {
                        Vector3 targetSize = renderer.bounds.size;
                        maxSize = new Vector3(targetSize.x, targetSize.y, targetSize.z);
                    }

                    minPos = new Vector3
                    (
                        Mathf.Min(minPos.x, renderer.bounds.min.x),
                        Mathf.Min(minPos.y, renderer.bounds.min.y),
                        Mathf.Min(minPos.z, renderer.bounds.min.z)
                    );

                    maxPos = new Vector3
                    (
                        Mathf.Max(maxPos.x, renderer.bounds.max.x),
                        Mathf.Max(maxPos.y, renderer.bounds.max.y),
                        Mathf.Max(maxPos.z, renderer.bounds.max.z)
                    );
                }
            }
        }

        public override Vector3 GetSize()
        {
            return maxSize;
        }

        public override Vector3 GetMinPos()
        {
            return minPos;
        }

        public override Vector3 GetMaxPos()
        {
            return maxPos;
        }

        public float GetTimeForRatio(float ratio)
        {
            if (mainParticleSystem == null)
                return 0f;
            return mainParticleSystem.main.duration * ratio;
        }

        public void Animate(Frame frame)
        {
            if (mainParticleSystem == null)
                return;

            if (frame.index == 0)
            {
                if (isPrewarm)
                    mainParticleSystem.Simulate(mainParticleSystem.main.duration, true, true);
                else
                    mainParticleSystem.Simulate(0, true, true);
            }
            else
            {
                if (isPrewarm)
                {
                    mainParticleSystem.Simulate(frame.time, true, true);
                }
                else
                {
                    float period = frame.time - mainParticleSystem.time;
                    mainParticleSystem.Simulate(period, true, false);
                }
            }

            if (animationClip != null)
            {
                float animEndTime = animStartTime + animationClip.length;
                if (frame.time >= animStartTime && frame.time <= animEndTime)
                    animationClip.SampleAnimation(gameObject, frame.time - animStartTime);
            }
        }

        public override bool IsReady()
        {
            return (mainParticleSystem != null && targetChecked);
        }

        public override bool IsTileAvailable()
        {
            return false;
        }

        public bool IsCameraFollowing()
        {
            return animationClip != null && isCameraFollowing;
        }

        public override void ClearFrames()
        {
            selectedFrames.Clear();
        }
    }
}
