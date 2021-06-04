using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
	public class ParticleBaker : Baker
    {
        private readonly ParticleModel particleModel = null;

        private Vector3 vecFromCameraToModel;

        private AnimatorController outputController;

        public ParticleBaker(Model model, Studio studio, string sIndex, string parentFolderPath)
            : base(model, studio, sIndex, parentFolderPath)
        {
            particleModel = model as ParticleModel;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(BakingState.EndFrame, OnEndFrame);
            stateMachine.AddState(BakingState.EndView, OnEndView);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void OnInitialize()
        {
            try
            {
                if (studio.view.rotationType == RotationType.Model)
                    CameraHelper.LocateMainCameraToModel(model, studio);

                if(studio.shadow.type == ShadowType.TopDown)
                {
                    ObjectHelper.DeleteObject(EditorGlobal.DYNAMIC_SHADOW_NAME);
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.STATIC_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);

                    ShadowHelper.LocateShadowToModel(model, studio);

                    Camera camera;
                    GameObject fieldObj;
                    ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

                    CameraHelper.LookAtModel(camera.transform, model);
                    ShadowHelper.ScaleShadowField(camera, fieldObj);
                }
                else
                {
                    ShadowHelper.LocateShadowToModel(model, studio);
                }

                particleModel.Animate(Frame.BEGIN); // important for particle simulation

                if (particleModel.selectedFrames.Count > 0)
                {
                    frames = new Frame[particleModel.selectedFrames.Count];
                    particleModel.selectedFrames.CopyTo(frames);
                }
                else
                {
                    frames = new Frame[studio.frame.size];
                    for (int i = 0; i < studio.frame.size; ++i)
                    {
                        float frameRatio = 0.0f;
                        if (i > 0 && i < studio.frame.size)
                            frameRatio = (float)i / (float)(studio.frame.size - 1);

                        float time = particleModel.GetTimeForRatio(frameRatio);
                        frames[i] = new Frame(i, time);
                    }
                }

                fileBaseName = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

                if (studio.packing.on && studio.output.animatorControllerMake)
                    BuildAnimatorController();

                viewIndex = 0;

                stateMachine.ChangeState(BakingState.BeginView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginView()
        {
            try
            {
                if (studio.trimming.isUnifiedForAllViews)
                    unifiedTexBound = new IntegerBound();

                if (studio.packing.on || studio.trimming.isUnifiedForAllViews)
                {
                    frameModelTextures = new Texture2D[frames.Length];
                    framePivots = new IntegerVector[frames.Length];
                }

                studio.view.checkedSubViews[viewIndex].func(model);
                viewName = studio.view.checkedSubViews[viewIndex].name;

                vecFromCameraToModel = model.transform.position - Camera.main.transform.position;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                pivot2D = new IntegerVector(screenPos);

                frameIndex = 0;

                stateMachine.ChangeState(BakingState.BeginFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginFrame()
        {
            try
            {
                int shownCurrFrameIndex = frameIndex + 1;
                float progress = (float)(viewIndex * frames.Length + shownCurrFrameIndex) / (studio.view.checkedSubViews.Count * frames.Length);

                if (studio.view.checkedSubViews.Count == 0)
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", "Frame: " + shownCurrFrameIndex + " (" + ((int)(progress * 100f)) + "%)", progress);
                else
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", "View: " + viewName + " | Frame: " + shownCurrFrameIndex + " (" + ((int)(progress * 100f)) + "%)", progress);

                if (IsCancelled)
                    throw new Exception("Cancelled");

                Frame frame = frames[frameIndex];
                particleModel.Animate(frame);

                if (particleModel.IsCameraFollowing())
                    ParticleSampler.DoCameraFollowing(particleModel, particleModel.transform.position, vecFromCameraToModel, studio);

                stateMachine.ChangeState(BakingState.CaptureFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnCaptureFrame()
        {
            try
            {
                double deltaTime = EditorApplication.timeSinceStartup - prevTime;
                if (deltaTime < studio.frame.delay)
                    return;
                prevTime = EditorApplication.timeSinceStartup;

                if (studio.shadow.type == ShadowType.TopDown)
                {
                    Camera camera;
                    GameObject fieldObj;
                    ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);
                    ShadowHelper.BakeStaticShadow(camera, fieldObj, particleModel, studio);
                }

                Texture2D tex = CapturingHelper.CaptureModelManagingShadow(model, studio);

                IntegerBound texBound = new IntegerBound();
                if (!TextureHelper.CalcTextureBound(tex, pivot2D, texBound))
                {
                    texBound.min.x = pivot2D.x - 1;
                    texBound.max.x = pivot2D.x + 1;
                    texBound.min.y = pivot2D.y - 1;
                    texBound.max.y = pivot2D.y + 1;
                }

                if (studio.trimming.on)
                {
                    if (studio.trimming.isUnifiedForAllViews)
                    {
                        TextureHelper.MakeUnifiedBound(pivot2D, texBound, unifiedTexBound);
                    }
                    else
                    {
                        pivot2D.SubtractWithMargin(texBound.min, studio.trimming.margin);
                        tex = TextureHelper.TrimTexture(tex, texBound, studio.trimming.margin, EngineGlobal.CLEAR_COLOR32);
                    }
                }

                if (studio.packing.on || studio.trimming.isUnifiedForAllViews)
                {
                    frameModelTextures[frameIndex] = tex;
                    framePivots[frameIndex] = pivot2D;
                }
                else // !studio.packing.on && !studio.trim.isUnifiedSize
                {
                    BakeIndividually(tex, pivot2D, viewName, frameIndex);
                }

                stateMachine.ChangeState(BakingState.EndFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndFrame()
        {
            try
            {
                frameIndex++;

                if (frameIndex < frames.Length)
                    stateMachine.ChangeState(BakingState.BeginFrame);
                else
                    stateMachine.ChangeState(BakingState.EndView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndView()
        {
            try
            {
                if (!studio.packing.on && studio.trimming.isUnifiedForAllViews)
                {
                    TrimToUnifiedSize(framePivots, frameModelTextures);
                    BakeIndividually(framePivots, viewName, frameModelTextures);
                }
                else if (studio.packing.on)
                {
                    Sprite[] sprites = null;
                    if (!studio.trimming.isUnifiedForAllViews)
                    {
                        // trimmed or not
                        sprites = BakeWithPacking(framePivots, viewName, frameModelTextures);
                    }
                    else
                    {
                        TrimToUnifiedSize(framePivots, frameModelTextures);
                        sprites = BakeWithPacking(framePivots, viewName, frameModelTextures);
                    }

                    if (studio.output.animationClipMake)
                        MakeAnimationClipsForView(particleModel.isLooping, sprites, viewName);
                }

                particleModel.Animate(Frame.BEGIN);

                viewIndex++;

                if (viewIndex < studio.view.checkedSubViews.Count)
                    stateMachine.ChangeState(BakingState.BeginView);
                else
                    stateMachine.ChangeState(BakingState.Finalize);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnFinalize()
        {
            try
            {
                stateMachine = null;

                if (DoesMakePrefab())
                {
                    GameObject obj = PrefabUtility.InstantiatePrefab(model.spritePrefab) as GameObject;
                    if (obj != null)
                    {
                        if (firstSprite != null)
                        {
                            if (outputController != null)
                                model.prefabBuilder.BindSpriteAndController(obj, firstSprite, outputController);
                            else
                                model.prefabBuilder.BindSprite(obj, firstSprite);
                        }

                        SaveAsPrefab(obj, fileBaseName);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                Finish();
            }
        }

        public override void Finish()
        {
            stateMachine = null;

            if (studio.shadow.type == ShadowType.TopDown)
            {
                ObjectHelper.DeleteObject(EditorGlobal.STATIC_SHADOW_NAME);
                studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.DYNAMIC_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);
            }
        }

        private void BuildAnimatorController()
        {
            animatorStates = null;

            string filePath = Path.Combine(folderPath, particleModel.name + ".controller");
            outputController = AnimatorController.CreateAnimatorControllerAtPath(filePath);

            AnimatorStateMachine stateMachine = outputController.layers[0].stateMachine;

            AddParameterIfNotExist(outputController, ANGLE_PARAM_NAME, AnimatorControllerParameterType.Int);

            animatorStates = new List<AnimatorState>();
            foreach (SubView subView in studio.view.checkedSubViews)
            {
                AnimatorState state = GetOrCreateState(stateMachine, subView.name);
                animatorStates.Add(state);
            }

            for (int ai = 0; ai < animatorStates.Count; ++ai)
            {
                for (int bi = 0; bi < animatorStates.Count; ++bi)
                {
                    if (ai == bi)
                        continue;

                    AddDirectionTransitionA2BIfNotExist(animatorStates[ai], animatorStates[bi], studio.view.checkedSubViews[bi].angle);
                }
            }
        }
    }
}
