using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
	public class MeshBaker : Baker
	{
        private readonly MeshModel meshModel = null;

        private readonly List<MeshAnimation> animations;
        public int AnimationIndex { get; set; }

        private string fileNameForModel = "";

        private readonly Vector3 simpleShadowBaseScale;
        private Vector3 modelBaseSizeForView; // for dynamic simple shadow

        private GameObject outRootPrefab = null;

        private bool wasColliderSetup = false;
        private IntegerBound[] frameCompactBounds = null;
        private Vector2[] frameCompactVectors = null; // vector ratio from compact area's center to texture area's center in trimmed texture

        private Texture2D[] frameNormalMapTextures = null;

        private List<AnimationTextureData> animationTextureDataList;
        private AnimationTextureData animTexData;
        private DirectionTextureData dirTexData;

        class LocationMapping
        {
            public Location location3d;
            public GameObject location2dObj;
            public IntegerVector[] frameLocationPositions;
            public Vector2[] frameRatioPositions;

            public LocationMapping(Location location3d, GameObject location2dObj)
            {
                this.location3d = location3d;
                this.location2dObj = location2dObj;
            }
        }
        private LocationMapping[] locationMappings = null;

        public MeshBaker(Model model, List<MeshAnimation> animations, Studio studio, string sIndex, string parentFolderPath)
            : base(model, studio, sIndex, parentFolderPath)
        {
            this.animations = animations;

            meshModel = model as MeshModel;

            if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
                simpleShadowBaseScale = studio.shadow.obj.transform.localScale;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginAnimation, OnBeginAnimation);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(BakingState.EndFrame, OnEndFrame);
            stateMachine.AddState(BakingState.EndView, OnEndView);
            stateMachine.AddState(BakingState.EndAnimation, OnEndAnimation);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void OnInitialize()
        {
            try
            {
                if (studio.view.rotationType == RotationType.Model)
                    CameraHelper.LocateMainCameraToModel(model, studio);

                ShadowHelper.LocateShadowToModel(model, studio);

                if (studio.shadow.type == ShadowType.TopDown)
                {
                    Camera camera;
                    GameObject fieldObj;
                    ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

                    CameraHelper.LookAtModel(camera.transform, model);
                    ShadowHelper.ScaleShadowField(camera, fieldObj);
                }
                else if (studio.shadow.type == ShadowType.Matte)
                {
                    ShadowHelper.ScaleMatteField(meshModel, studio.shadow.obj, studio.lit);
                }

                fileNameForModel = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

                if (studio.packing.on)
                {
                    if (DoesMakePrefab())
                    {
                        outRootPrefab = PrefabUtility.InstantiatePrefab(model.spritePrefab) as GameObject;
                        outRootPrefab.gameObject.SetActive(false);

                        if (studio.output.DoesMakeLocationPrefab())
                        {
                            Location[] locations = model.GetComponentsInChildren<Location>();
                            if (locations.Length > 0)
                            {
                                locationMappings = new LocationMapping[locations.Length];
                                for (int i = 0; i < locations.Length; ++i)
                                {
                                    Location loc = locations[i];
                                    GameObject locObj = PrefabUtility.InstantiatePrefab(studio.output.locationSpritePrefab) as GameObject;
                                    locObj.transform.parent = model.prefabBuilder.GetLocationsParent(outRootPrefab);
                                    locObj.transform.localPosition = Vector3.zero;
                                    locObj.transform.localRotation = Quaternion.identity;
                                    locObj.name += "_" + loc.locationName;
                                    locationMappings[i] = new LocationMapping(loc, locObj);
                                }
                            }
                        }

                        if (outRootPrefab != null && studio.output.animatorControllerMake && studio.output.normalMapMake)
                            animationTextureDataList = new List<AnimationTextureData>();
                    }
                }

                AnimationIndex = 0;

                stateMachine.ChangeState(BakingState.BeginAnimation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginAnimation()
        {
            try
            {
                MeshAnimation animation = animations[AnimationIndex];

                if (animation.selectedFrames.Count > 0)
                {
                    frames = new Frame[animation.selectedFrames.Count];
                    animation.selectedFrames.CopyTo(frames);
                }
                else
                {
                    frames = new Frame[studio.frame.size];
                    for (int i = 0; i < studio.frame.size; ++i)
                    {
                        float frameRatio = 0.0f;
                        if (i > 0 && i < studio.frame.size)
                            frameRatio = (float)i / (float)(studio.frame.size - 1);

                        float time = meshModel.GetTimeForRatio(animation.clip, frameRatio);
                        frames[i] = new Frame(i, time);
                    }
                }

                string animationName;
                if (meshModel.referenceController != null)
                    animationName = animation.stateName;
                else
                    animationName = animation.clip.name;

                fileBaseName = fileNameForModel + "_" + animationName;

                if (studio.packing.on && studio.output.animatorControllerMake)
                {
                    BuildAnimatorController();

                    if (outRootPrefab != null && studio.output.normalMapMake)
                    {
                        animTexData = new AnimationTextureData(animationName);
                        animationTextureDataList.Add(animTexData);
                    }
                }

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
                    if (studio.output.normalMapMake)
                        frameNormalMapTextures = new Texture2D[frames.Length];
                    framePivots = new IntegerVector[frames.Length];
                }

                studio.view.checkedSubViews[viewIndex].func(model);
                viewName = studio.view.checkedSubViews[viewIndex].name;

                modelBaseSizeForView = model.GetSize();

                if (outRootPrefab && model.prefabBuilder.GetBoxCollider2D(outRootPrefab) != null && studio.output.isCompactCollider)
                {
                    frameCompactBounds = new IntegerBound[frames.Length];
                    frameCompactVectors = new Vector2[frames.Length];
                }

                if (locationMappings != null)
                {
                    foreach (LocationMapping mapping in locationMappings)
                    {
                        mapping.frameLocationPositions = new IntegerVector[frames.Length];
                        mapping.frameRatioPositions = new Vector2[frames.Length];
                    }
                }

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
                meshModel.Animate(animations[AnimationIndex], frame);

                Vector3 pivotScreenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                pivot2D = new IntegerVector(pivotScreenPos);

                if (locationMappings != null)
                {
                    foreach (LocationMapping mapping in locationMappings)
                    {
                        Vector3 locationScreenPos = Camera.main.WorldToScreenPoint(mapping.location3d.transform.position);
                        mapping.frameLocationPositions[frameIndex] = new IntegerVector(locationScreenPos);
                    }
                }

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

                if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
                    ShadowHelper.ScaleSimpleShadowDynamically(modelBaseSizeForView, simpleShadowBaseScale, meshModel, studio);

                Texture2D modelTexture = CapturingHelper.CaptureModelManagingShadow(model, studio);
                Texture2D normalMapTexture = null;
                if (studio.output.normalMapMake)
                    normalMapTexture = CapturingHelper.CaptureModelForNormalMap(model, studio.output.isGrayscaleMap, studio.shadow.obj);

                IntegerBound texBound = new IntegerBound();
                IntegerBound compactBound = new IntegerBound();
                if (!TextureHelper.CalcTextureBound(modelTexture, pivot2D, texBound, compactBound))
                {
                    texBound.min.x = pivot2D.x - 1;
                    texBound.max.x = pivot2D.x + 1;
                    texBound.min.y = pivot2D.y - 1;
                    texBound.max.y = pivot2D.y + 1;
                }

                if (frameCompactBounds != null)
                    frameCompactBounds[frameIndex] = compactBound;

                if (studio.trimming.on)
                {
                    if (studio.trimming.isUnifiedForAllViews)
                    {
                        TextureHelper.MakeUnifiedBound(pivot2D, texBound, unifiedTexBound);
                    }
                    else
                    {
                        pivot2D.SubtractWithMargin(texBound.min, studio.trimming.margin);

                        modelTexture = TextureHelper.TrimTexture(modelTexture, texBound, studio.trimming.margin, EngineGlobal.CLEAR_COLOR32);
                        if (studio.output.normalMapMake)
                        {
                            Color32 defaultColor = (studio.output.isGrayscaleMap ? EngineGlobal.CLEAR_COLOR32 : EngineGlobal.NORMALMAP_COLOR32);
                            normalMapTexture = TextureHelper.TrimTexture(normalMapTexture, texBound, studio.trimming.margin, defaultColor, true);
                        }

                        if (frameCompactBounds != null)
                            CalcCompactVector(modelTexture, texBound);

                        if (locationMappings != null)
                        {
                            foreach (LocationMapping mapping in locationMappings)
                                mapping.frameLocationPositions[frameIndex].SubtractWithMargin(texBound.min, studio.trimming.margin);
                        }
                    }
                }

                if (studio.packing.on || studio.trimming.isUnifiedForAllViews)
                {
                    framePivots[frameIndex] = pivot2D;
                    frameModelTextures[frameIndex] = modelTexture;
                    if (studio.output.normalMapMake)
                        frameNormalMapTextures[frameIndex] = normalMapTexture;
                }
                else // !studio.packing.on && !studio.trim.isUnifiedSize
                {
                    BakeIndividually(modelTexture, pivot2D, viewName, frameIndex);
                    if (studio.output.normalMapMake)
                        BakeIndividually(normalMapTexture, pivot2D, viewName, frameIndex, true);
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
                    TrimToUnifiedSize(framePivots, frameModelTextures, frameNormalMapTextures);
                    BakeIndividually(framePivots, viewName, frameModelTextures, frameNormalMapTextures);
                }
                else if (studio.packing.on)
                {
                    if (studio.trimming.isUnifiedForAllViews)
                    {
                        TrimToUnifiedSize(framePivots, frameModelTextures, frameNormalMapTextures);

                        if (frameCompactBounds != null)
                        {
                            for (int i = 0; i < frameModelTextures.Length; ++i)
                                CalcCompactVector(frameModelTextures[i], unifiedTexBound);
                        }

                        if (locationMappings != null)
                        {
                            foreach (LocationMapping mapping in locationMappings)
                            {
                                for (int i = 0; i < frameModelTextures.Length; ++i)
                                    mapping.frameLocationPositions[i].SubtractWithMargin(unifiedTexBound.min, studio.trimming.margin);
                            }
                        }
                    }

                    Sprite[] sprites = BakeWithPacking(framePivots, viewName, frameModelTextures, frameNormalMapTextures);

                    if (locationMappings != null)
                    {
                        foreach (LocationMapping mapping in locationMappings)
                        {
                            Debug.Assert(frameModelTextures.Length == mapping.frameLocationPositions.Length);

                            for (int i = 0; i < frameModelTextures.Length; ++i)
                            {
                                Texture2D tex = frameModelTextures[i];
                                float locRatioX = (float)mapping.frameLocationPositions[i].x / (float)tex.width;
                                float locRatioY = (float)mapping.frameLocationPositions[i].y / (float)tex.height;
                                float pivotRatioX = (float)framePivots[i].x / (float)tex.width;
                                float pivotRatioY = (float)framePivots[i].y / (float)tex.height;
                                mapping.frameRatioPositions[i] = new Vector2(locRatioX - pivotRatioX, locRatioY - pivotRatioY);
                            }
                        }
                    }

                    if (studio.output.animationClipMake)
                    {
                        MeshAnimation animation = animations[AnimationIndex];
                        AnimationClip animClip = MakeAnimationClipsForView(animation.isLooping, sprites, viewName);

                        if (animClip != null && outRootPrefab != null)
                        {
                            BoxCollider2D collider = model.prefabBuilder.GetBoxCollider2D(outRootPrefab);
                            if (collider != null)
                                AddBoxColliderCurve(animClip, collider, sprites);

                            if (locationMappings != null)
                                AddLocationPositionCurve(animClip, sprites);

                            if (studio.output.animatorControllerMake && studio.output.normalMapMake)
                            {
                                dirTexData = new DirectionTextureData(studio.view.checkedSubViews[viewIndex].angle, modelAtlasTexture, normalMapAtlasTexture);
                                animTexData.directionTextureDataList.Add(dirTexData);
                            }
                        }
                    }
                }

                viewIndex++;

                if (viewIndex < studio.view.checkedSubViews.Count)
                    stateMachine.ChangeState(BakingState.BeginView);
                else
                    stateMachine.ChangeState(BakingState.EndAnimation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndAnimation()
        {
            try
            {
                EditorUtility.ClearProgressBar();

                AnimationIndex++;

                if (AnimationIndex < animations.Count)
                    stateMachine.ChangeState(BakingState.BeginAnimation);
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

                if (outRootPrefab != null)
                {
                    if (meshModel.referenceController != null && meshModel.outputController != null)
                        model.prefabBuilder.BindSpriteAndController(outRootPrefab, firstSprite, meshModel.outputController);
                    else
                        model.prefabBuilder.BindSprite(outRootPrefab, firstSprite);

                    if (studio.output.animatorControllerMake && studio.output.normalMapMake)
                    {
                        Shader shader = Shader.Find("Legacy Shaders/Transparent/Bumped Diffuse");
                        if (shader != null)
                        {
                            Material material = new Material(shader);
                            AssetDatabase.CreateAsset(material, folderPath + "/" + fileNameForModel + ".mat");
                            model.prefabBuilder.BindMaterialAndTextures(outRootPrefab, material, animationTextureDataList);
                        }
                    }

                    SaveAsPrefab(outRootPrefab, fileNameForModel);
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

            meshModel.Animate(animations[0], Frame.BEGIN);
        }

        private void BuildAnimatorController()
        {
            if (meshModel.referenceController == null)
                return;

            animatorStates = null;

            MeshAnimation animation = animations[AnimationIndex];

            if (meshModel.outputController == null)
            {
                string filePath = Path.Combine(folderPath, fileNameForModel + ".controller");
                meshModel.outputController = AnimatorController.CreateAnimatorControllerAtPath(filePath);
            }
            AnimatorStateMachine outStateMachine = meshModel.outputController.layers[0].stateMachine;

            AddParameterIfNotExist(meshModel.outputController, ANGLE_PARAM_NAME, AnimatorControllerParameterType.Int);
            foreach (AnimatorControllerParameter refParam in meshModel.referenceController.parameters)
                AddParameterIfNotExist(meshModel.outputController, refParam.name, refParam.type);

            AnimatorStateMachine refStateMachine = meshModel.referenceController.layers[0].stateMachine;

            AnimatorState refMainAnimState = FindState(refStateMachine, animation.stateName);
            Debug.Assert(refMainAnimState != null);

            animatorStates = new List<AnimatorState>();
            foreach (SubView subView in studio.view.checkedSubViews)
            {
                AnimatorStateMachine outSubStateMachine = GetOrCreateStateMachine(outStateMachine, subView.name, subView.angle);
                AnimatorState outStateForView = GetOrCreateState(outSubStateMachine, animation.stateName);
                CopyState(refMainAnimState, outStateForView);
                animatorStates.Add(outStateForView);
            }
            Debug.Assert(studio.view.checkedSubViews.Count == animatorStates.Count);

            bool hasAnyStateTransition = false;
            foreach (SubView subView in studio.view.checkedSubViews)
            {
                AnimatorStateMachine outViewStateMachine = FindStateMachine(outStateMachine, subView.name);
                Debug.Assert(outViewStateMachine != null);

                AnimatorState outMainAnimViewState = FindState(outViewStateMachine, animation.stateName);
                Debug.Assert(outMainAnimViewState != null);

                foreach (AnimatorTransition refEntryTransition in refStateMachine.entryTransitions)
                {
                    AnimatorState outDestState = FindState(outViewStateMachine, refEntryTransition.destinationState.name);
                    if (outDestState != null)
                        CopyOrCreateEntryTransition(outViewStateMachine, refEntryTransition, outDestState);
                }

                foreach (AnimatorStateTransition refAnyStateTransition in refStateMachine.anyStateTransitions)
                {
                    AnimatorState outDestState = FindState(outViewStateMachine, refAnyStateTransition.destinationState.name);
                    if (outDestState != null)
                    {
                        CopyOrCreateAnyStateTransition(outStateMachine, refAnyStateTransition, outDestState, subView.angle);
                        if (refAnyStateTransition.destinationState.name == animation.stateName)
                            hasAnyStateTransition = true;
                    }
                }

                foreach (ChildAnimatorState refChildState in refStateMachine.states)
                {
                    foreach (AnimatorStateTransition refTransition in refChildState.state.transitions)
                    {
                        AnimatorState outStartState = FindState(outViewStateMachine, refChildState.state.name);
                        if (outStartState == null)
                            continue;

                        if (refTransition.isExit)
                        {
                            CopyOrCreateExitTransition(refTransition, outStartState);
                        }
                        else
                        {
                            AnimatorState outDestState = FindState(outViewStateMachine, refTransition.destinationState.name);
                            if (outDestState != null)
                            {
                                CopyOrCreateTransition(refTransition, outStartState, outDestState);

                                foreach (AnimatorStateTransition refReverseTransition in refTransition.destinationState.transitions)
                                {
                                    if (refReverseTransition.destinationState == refMainAnimState)
                                        CopyOrCreateTransition(refReverseTransition, outDestState, outStartState);
                                }
                            }
                        }
                    }
                }
            }

            if (!hasAnyStateTransition)
            {
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

        private void CopyState(AnimatorState refState, AnimatorState outState)
        {
            outState.mirrorParameterActive = refState.mirrorParameterActive;
            outState.cycleOffsetParameterActive = refState.cycleOffsetParameterActive;
            outState.speedParameterActive = refState.speedParameterActive;
            outState.mirrorParameter = refState.mirrorParameter;
            outState.cycleOffsetParameter = refState.cycleOffsetParameter;
            outState.speedParameter = refState.speedParameter;
            outState.tag = refState.tag;
            outState.writeDefaultValues = refState.writeDefaultValues;
            outState.iKOnFeet = refState.iKOnFeet;
            outState.mirror = refState.mirror;
            outState.cycleOffset = refState.cycleOffset;
            outState.speed = refState.speed;
        }

        private void CopyOrCreateEntryTransition(AnimatorStateMachine outStateMachine, AnimatorTransition refTransition, AnimatorState outState)
        {
            AnimatorTransition outTransition = FindEntryTransition(outStateMachine, outState);
            if (outTransition == null)
                outTransition = outStateMachine.AddEntryTransition(outState);
            outTransition.solo = refTransition.solo;
            outTransition.mute = refTransition.mute;
            outTransition.isExit = refTransition.isExit;

            foreach (AnimatorCondition refCondition in refTransition.conditions)
                RemoveAllAndAddCondition(outTransition, refCondition.parameter, refCondition.mode, refCondition.threshold);
        }

        private AnimatorTransition FindEntryTransition(AnimatorStateMachine outStateMachine, AnimatorState outState)
        {
            foreach (AnimatorTransition outEntryTransition in outStateMachine.entryTransitions)
            {
                if (outEntryTransition.destinationState == outState)
                    return outEntryTransition;
            }
            return null;
        }

        private void CopyOrCreateAnyStateTransition(AnimatorStateMachine outStateMachine, AnimatorStateTransition refTransition, AnimatorState outState, int angle)
        {
            AnimatorStateTransition outTransition = FindAnyStateTransition(outStateMachine, outState);
            if (outTransition == null)
            {
                outTransition = outStateMachine.AddAnyStateTransition(outState);
                outTransition.AddCondition(AnimatorConditionMode.Equals, angle, ANGLE_PARAM_NAME);
            }
            CopyStateTransition(refTransition, outTransition);
        }

        private AnimatorStateTransition FindAnyStateTransition(AnimatorStateMachine outStateMachine, AnimatorState outState)
        {
            foreach (AnimatorStateTransition outAnyStateTransition in outStateMachine.anyStateTransitions)
            {
                if (outAnyStateTransition.destinationState == outState)
                    return outAnyStateTransition;
            }
            return null;
        }

        private void CopyOrCreateExitTransition(AnimatorStateTransition refTransition, AnimatorState outState)
        {
            AnimatorStateTransition outTransition = FindExitTransition(outState);
            if (outTransition == null)
                outTransition = outState.AddExitTransition();
            CopyStateTransition(refTransition, outTransition);
        }

        private AnimatorStateTransition FindExitTransition(AnimatorState outState)
        {
            foreach (AnimatorStateTransition outTransition in outState.transitions)
            {
                if (outTransition.isExit)
                    return outTransition;
            }
            return null;
        }

        private void CopyOrCreateTransition(AnimatorStateTransition refTransition, AnimatorState outStartState, AnimatorState outEndState)
        {
            AnimatorStateTransition outTransition = FindTransitionA2B(outStartState, outEndState);
            if (outTransition == null)
                outTransition = outStartState.AddTransition(outEndState);
            CopyStateTransition(refTransition, outTransition);
        }

        private void CopyStateTransition(AnimatorStateTransition refTransition, AnimatorStateTransition outTransition)
        {
            outTransition.solo = refTransition.solo;
            outTransition.mute = refTransition.mute;
            outTransition.isExit = refTransition.isExit;
            outTransition.duration = refTransition.duration;
            outTransition.offset = refTransition.offset;
            outTransition.interruptionSource = refTransition.interruptionSource;
            outTransition.orderedInterruption = refTransition.orderedInterruption;
            outTransition.exitTime = refTransition.exitTime;
            outTransition.hasExitTime = refTransition.hasExitTime;
            outTransition.hasFixedDuration = refTransition.hasFixedDuration;
            outTransition.canTransitionToSelf = refTransition.canTransitionToSelf;

            foreach (AnimatorCondition refCondition in refTransition.conditions)
                RemoveAllAndAddCondition(outTransition, refCondition.parameter, refCondition.mode, refCondition.threshold);
        }

        private AnimatorStateMachine GetOrCreateStateMachine(AnimatorStateMachine stateMachine, string stateMachineName, int angle)
        {
            AnimatorStateMachine subStateMachine = FindStateMachine(stateMachine, stateMachineName);
            if (subStateMachine == null)
                subStateMachine = stateMachine.AddStateMachine(stateMachineName, new Vector3(450 + Mathf.Cos(angle * Mathf.Deg2Rad) * 200, Mathf.Sin(angle * Mathf.Deg2Rad) * 150));
            return subStateMachine;
        }

        private AnimatorStateMachine FindStateMachine(AnimatorStateMachine stateMachine, string stateMachineName)
        {
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                if (childStateMachine.stateMachine.name == stateMachineName)
                    return childStateMachine.stateMachine;
            }
            return null;
        }

        private void CalcCompactVector(Texture2D tex, IntegerBound bound)
        {
            IntegerBound compactBound = frameCompactBounds[frameIndex];
            compactBound.min.SubtractWithMargin(bound.min, studio.trimming.margin);
            compactBound.max.SubtractWithMargin(bound.min, studio.trimming.margin);

            float textureCenterX = tex.width / 2f;
            float textureCenterY = tex.height / 2f;
            float compactCenterX = (float)(compactBound.min.x + compactBound.max.x) / 2f;
            float compactCenterY = (float)(compactBound.min.y + compactBound.max.y) / 2f;
            float ratioVectorX = (compactCenterX - textureCenterX) / tex.width;
            float ratioVectorY = (compactCenterY - textureCenterY) / tex.height;
            frameCompactVectors[frameIndex] = new Vector2(ratioVectorX, ratioVectorY);
        }

        private void AddBoxColliderCurve(AnimationClip animClip, BoxCollider2D collider, Sprite[] sprites)
        {
            Debug.Assert(framePivots != null && sprites.Length == framePivots.Length);

            AnimationCurve xSizeCurve = new AnimationCurve();
            AnimationCurve ySizeCurve = new AnimationCurve();
            AnimationCurve xOffsetCurve = new AnimationCurve();
            AnimationCurve yOffsetCurve = new AnimationCurve();

            for (int i = 0; i < sprites.Length; ++i)
            {
                float unitTime = 1f / animClip.frameRate;
                float time = studio.output.frameInterval * i * unitTime;

                Texture2D texture = frameModelTextures[i];

                // size curve
                Vector3 spriteSize = sprites[i].bounds.size;
                if (frameCompactBounds != null)
                {
                    int compactWidth = frameCompactBounds[i].max.x - frameCompactBounds[i].min.x + 1;
                    int compactHeight = frameCompactBounds[i].max.y - frameCompactBounds[i].min.y + 1;
                    float widthRatio = (float)compactWidth / (float)texture.width;
                    float heightRatio = (float)compactHeight / (float)texture.height;
                    spriteSize.x *= widthRatio;
                    spriteSize.y *= heightRatio;
                }
                xSizeCurve.AddKey(time, spriteSize.x);
                ySizeCurve.AddKey(time, spriteSize.y);

                // offset curve
                float xPivotRatio = (float)framePivots[i].x / (float)texture.width;
                float yPivotRatio = (float)framePivots[i].y / (float)texture.height;
                float xCenterRatio = 0.5f;
                float yCenterRatio = 0.5f;
                if (frameCompactVectors != null)
                {
                    xCenterRatio += frameCompactVectors[i].x;
                    yCenterRatio += frameCompactVectors[i].y;
                }
                float offsetX = (xCenterRatio - xPivotRatio) * sprites[i].bounds.size.x;
                float offsetY = (yCenterRatio - yPivotRatio) * sprites[i].bounds.size.y;
                xOffsetCurve.AddKey(time, offsetX);
                yOffsetCurve.AddKey(time, offsetY);

                // setup for firstSprite
                if (!wasColliderSetup && i == 0)
                {
                    wasColliderSetup = true;
                    collider.size = new Vector2(spriteSize.x, spriteSize.y);
                    collider.offset = new Vector2(offsetX, offsetY);
                }
            }

            string path = AnimationUtility.CalculateTransformPath(collider.transform, outRootPrefab.transform);

            EditorCurveBinding xSizeCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Size.x");
            EditorCurveBinding ySizeCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Size.y");
            EditorCurveBinding xOffsetCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Offset.x");
            EditorCurveBinding yOffsetCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Offset.y");

            AnimationUtility.SetEditorCurve(animClip, xSizeCurveBinding, xSizeCurve);
            AnimationUtility.SetEditorCurve(animClip, ySizeCurveBinding, ySizeCurve);
            AnimationUtility.SetEditorCurve(animClip, xOffsetCurveBinding, xOffsetCurve);
            AnimationUtility.SetEditorCurve(animClip, yOffsetCurveBinding, yOffsetCurve);
        }

        private void AddLocationPositionCurve(AnimationClip animClip, Sprite[] sprites)
        {
            foreach (LocationMapping mapping in locationMappings)
            {
                Debug.Assert(mapping.frameRatioPositions.Length == sprites.Length);

                AnimationCurve xCurve = new AnimationCurve();
                AnimationCurve yCurve = new AnimationCurve();

                for (int i = 0; i < sprites.Length; ++i)
                {
                    float unitTime = 1f / animClip.frameRate;
                    float time = studio.output.frameInterval * i * unitTime;

                    Vector3 spriteSize = sprites[i].bounds.size;
                    float x = spriteSize.x * mapping.frameRatioPositions[i].x;
                    float y = spriteSize.y * mapping.frameRatioPositions[i].y;

                    xCurve.AddKey(time, x);
                    yCurve.AddKey(time, y);

                    if (i == 0)
                        mapping.location2dObj.transform.localPosition = new Vector3(x, y);
                }

                string path = AnimationUtility.CalculateTransformPath(mapping.location2dObj.transform, outRootPrefab.transform);

                EditorCurveBinding xCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x");
                EditorCurveBinding yCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y");

                AnimationUtility.SetEditorCurve(animClip, xCurveBinding, xCurve);
                AnimationUtility.SetEditorCurve(animClip, yCurveBinding, yCurve);
            }
        }
    }
}
