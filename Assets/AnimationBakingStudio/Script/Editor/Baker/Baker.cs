using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
    public abstract class Baker
    {
        protected enum BakingState
        {
            Initialize,
            BeginAnimation,
            BeginView,
            BeginFrame,
            CaptureFrame,
            EndView,
            EndFrame,
            EndAnimation,
            Finalize
        }
        protected StateMachine<BakingState> stateMachine = null;

        protected readonly Model model;
        protected readonly Studio studio;

        protected string sIndex = "";

        protected string parentFolderPath = "";

        public readonly string modelName;

        protected string fileBaseName = "";
        protected string folderPath = "";

        protected int viewIndex = -1;
        protected string viewName;

        protected Frame[] frames = null;
        protected int frameIndex = -1;

        protected IntegerVector pivot2D;
        protected IntegerBound unifiedTexBound;

        protected IntegerVector[] framePivots = null;
        protected Texture2D[] frameModelTextures = null;

        protected List<AnimatorState> animatorStates = null;

        protected Sprite firstSprite;

        protected Texture2D modelAtlasTexture;
        protected Texture2D normalMapAtlasTexture;

        protected double prevTime = 0.0;

        public bool IsCancelled { get; set; }

        public Baker(Model model, Studio studio, string sIndex, string parentFolderPath)
        {
            this.model = model;
            this.studio = studio;
            this.sIndex = sIndex;
            this.parentFolderPath = parentFolderPath;

            modelName = model.nameSuffix.Length > 0 ? model.name + model.nameSuffix : model.name;
        }

        public bool IsInProgress()
        {
            return (stateMachine != null);
        }

        public void UpdateState()
        {
            stateMachine.Update();
        }

        public abstract void Finish();

        protected void BuildFolderPathAndCreate(string fileName)
        {
            string folderName = "";

            if (parentFolderPath.Length > 0)
            {
                if (sIndex.Length > 0)
                    folderName += sIndex + "_";
                folderName += fileName;
            }
            else
            {
                int assetRootIndex = studio.path.directoryPath.IndexOf("Assets");
                parentFolderPath = studio.path.directoryPath.Substring(assetRootIndex);

                folderName += fileName + "_" + PathHelper.MakeDateTimeString();
            }

            folderPath = Path.Combine(parentFolderPath, folderName);
            Directory.CreateDirectory(folderPath);
        }

        protected string BuildFileBaseName()
        {
            string fileName = "";
            if (studio.path.fileNamePrefix.Length > 0)
                fileName += studio.path.fileNamePrefix;
            fileName += modelName;
            return fileName;
        }

        protected void TrimToUnifiedSize(IntegerVector[] pivots, Texture2D[] modelTextures, Texture2D[] normalMapTextures = null)
        {
            try
            {
                Debug.Assert(studio.trimming.isUnifiedForAllViews);

                for (int i = 0; i < modelTextures.Length; ++i)
                {
                    pivots[i].SubtractWithMargin(unifiedTexBound.min, studio.trimming.margin);

                    modelTextures[i] = TextureHelper.TrimTexture(modelTextures[i], unifiedTexBound, studio.trimming.margin, EngineGlobal.CLEAR_COLOR32);
                    if (studio.output.normalMapMake)
                    {
                        Color32 defaultColor = (studio.output.isGrayscaleMap ? EngineGlobal.CLEAR_COLOR32 : EngineGlobal.NORMALMAP_COLOR32);
                        normalMapTextures[i] = TextureHelper.TrimTexture(normalMapTextures[i], unifiedTexBound, studio.trimming.margin, defaultColor, studio.output.normalMapMake);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected void BakeIndividually(IntegerVector[] pivots, string subName, Texture2D[] modelTextures, Texture2D[] normalMapTextures = null)
        {
            try
            {
                for (int i = 0; i < modelTextures.Length; i++)
                {
                    BakeIndividually(modelTextures[i], pivots[i], subName, i);
                    if (studio.output.normalMapMake)
                        BakeIndividually(normalMapTextures[i], pivots[i], subName, i, true);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected void BakeIndividually(Texture2D tex, IntegerVector pivot, string subName, int frame, bool isNormalMap = false)
        {
            try
            {
                string detailName = frame.ToString().PadLeft((frames.Length - 1).ToString().Length, '0');
                BakeIndividually(tex, pivot, subName, detailName, isNormalMap);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected void BakeIndividually(Texture2D tex, IntegerVector pivot, string subName, string detailName, bool isNormalMap = false)
        {
            try
            {
                string fileFullName = fileBaseName;
                if (subName.Length > 0)
                    fileFullName += "_" + subName;
                if (detailName.Length > 0)
                    fileFullName += "_" + detailName;
                if (isNormalMap)
                    fileFullName += "_normal";

                string filePath = TextureHelper.SaveTexture(folderPath, fileFullName, tex);

                AssetDatabase.ImportAsset(filePath);

                TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                if (texImporter != null)
                {
                    texImporter.textureType = (isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Sprite);
                    texImporter.spriteImportMode = SpriteImportMode.Multiple;

                    SpriteMetaData[] metaData = new SpriteMetaData[1];
                    metaData[0].name = "0";
                    metaData[0].rect = new Rect(0.0f, 0.0f, (float)tex.width, (float)tex.height);
                    metaData[0].alignment = (int)SpriteAlignment.Custom;
                    metaData[0].pivot = new Vector2((float)pivot.x / (float)tex.width,
                                                    (float)pivot.y / (float)tex.height);

                    texImporter.spritesheet = metaData;

                    AssetDatabase.ImportAsset(filePath);
                }

                if (firstSprite == null)
                    firstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected Sprite[] BakeWithPacking(IntegerVector[] pivots, string subName, Texture2D[] modelTextures, Texture2D[] normalMapTextures = null)
        {
            try
            {
                string[] spriteNames = new string[modelTextures.Length];
                for (int i = 0; i < modelTextures.Length; ++i)
                    spriteNames[i] = i.ToString();
                return BakeWithPacking(pivots, subName, spriteNames, modelTextures, normalMapTextures);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected Sprite[] BakeWithPacking(IntegerVector[] pivots, string subName, string[] spriteNames, Texture2D[] modelTextures, Texture2D[] normalMapTextures = null)
        {
            Debug.Assert(modelTextures.Length == pivots.Length);
            Debug.Assert(modelTextures.Length == spriteNames.Length);

            try
            {
                int atlasLength = 64;
                if (studio.packing.method == PackingMethod.Optimized)
                {
                    if (!int.TryParse(studio.atlasSizes[studio.packing.maxAtlasSizeIndex], out atlasLength))
                        atlasLength = 4096;
                }
                else if (studio.packing.method == PackingMethod.InOrder)
                {
                    if (!int.TryParse(studio.atlasSizes[studio.packing.minAtlasSizeIndex], out atlasLength))
                        atlasLength = 256;
                }

                Rect[] atlasRects = null;

                if (studio.packing.method == PackingMethod.Optimized)
                {
                    modelAtlasTexture = new Texture2D(atlasLength, atlasLength, TextureFormat.ARGB32, false);
                    atlasRects = modelAtlasTexture.PackTextures(modelTextures, studio.packing.padding, atlasLength);
                    for (int i = 0; i < atlasRects.Length; i++)
                    {
                        Texture2D tex = modelTextures[i];
                        float newX = atlasRects[i].x * modelAtlasTexture.width;
                        float newY = atlasRects[i].y * modelAtlasTexture.height;
                        atlasRects[i] = new Rect(newX, newY, (float)tex.width, (float)tex.height);
                    }

                    if (studio.output.normalMapMake)
                    {
                        normalMapAtlasTexture = new Texture2D(atlasLength, atlasLength, TextureFormat.ARGB32, false);
                        normalMapAtlasTexture.PackTextures(normalMapTextures, studio.packing.padding, atlasLength);

                        if (!studio.output.isGrayscaleMap)
                        {
                            Color32[] normapMapPixels = normalMapAtlasTexture.GetPixels32();
                            Color32[] basePixels = Enumerable.Repeat(EngineGlobal.NORMALMAP_COLOR32, normapMapPixels.Length).ToArray();
                            for (int i = 0; i < basePixels.Length; i++)
                            {
                                Color32 pixel = normapMapPixels[i];
                                if (pixel.a > 0)
                                    basePixels[i] = pixel;
                            }

                            normalMapAtlasTexture = new Texture2D(normalMapAtlasTexture.width, normalMapAtlasTexture.height, TextureFormat.ARGB32, false);
                            normalMapAtlasTexture.SetPixels32(basePixels);
                            normalMapAtlasTexture.Apply();
                        }
                    }
                }
                else if (studio.packing.method == PackingMethod.InOrder)
                {
                    MakeAtlasInOrder(modelTextures, ref modelAtlasTexture, ref atlasLength, ref atlasRects, EngineGlobal.CLEAR_COLOR32);
                    if (studio.output.normalMapMake)
                    {
                        Color32 defaultColor = (studio.output.isGrayscaleMap ? EngineGlobal.CLEAR_COLOR32 : EngineGlobal.NORMALMAP_COLOR32);
                        MakeAtlasInOrder(normalMapTextures, ref normalMapAtlasTexture, ref atlasLength, ref atlasRects, defaultColor);
                    }
                }

                string fileFullName = fileBaseName;
                if (subName.Length > 0)
                    fileFullName += "_" + subName;

                string modelAtlasFilePath = SaveAtlasAndSetMetaData(fileFullName, ref modelAtlasTexture, atlasLength, atlasRects, modelTextures, pivots, spriteNames);
                if (studio.output.normalMapMake && normalMapAtlasTexture != null)
                    SaveAtlasAndSetMetaData(fileFullName, ref normalMapAtlasTexture, atlasLength, atlasRects, normalMapTextures, pivots, spriteNames, true);

                Sprite[] modelSprites = AssetDatabase.LoadAllAssetsAtPath(modelAtlasFilePath).OfType<Sprite>().ToArray();
                if (firstSprite == null)
                    firstSprite = modelSprites[0];

                return modelSprites;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void MakeAtlasInOrder(Texture2D[] textures, ref Texture2D atlasTexture, ref int atlasLength, ref Rect[] atlasRects, Color32 defaultColor)
        {
            int maxSpriteWidth = int.MinValue;
            int maxSpriteHeight = int.MinValue;
            foreach (Texture2D tex in textures)
            {
                maxSpriteWidth = Mathf.Max(tex.width, maxSpriteWidth);
                maxSpriteHeight = Mathf.Max(tex.height, maxSpriteHeight);
            }

            while (atlasLength < maxSpriteWidth || atlasLength < maxSpriteHeight)
                atlasLength *= 2;

            int atlasWidth = atlasLength;
            int atlasHeight = atlasLength;

            while (true)
            {
                atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false);
                Color32[] atlasPixels = Enumerable.Repeat(defaultColor, atlasWidth * atlasHeight).ToArray();

                atlasRects = new Rect[textures.Length];
                int originY = atlasHeight - maxSpriteHeight;

                bool needMultiply = false;

                int atlasRectIndex = 0;
                int currX = 0, currY = originY;
                foreach (Texture2D tex in textures)
                {
                    if (currX + tex.width > atlasWidth)
                    {
                        if (currY - maxSpriteHeight < 0)
                        {
                            needMultiply = true;
                            break;
                        }
                        currX = 0;
                        currY -= (maxSpriteHeight + studio.packing.padding);
                    }
                    WriteSpriteToAtlas(tex, atlasPixels, currX, currY, atlasTexture.width);
                    atlasRects[atlasRectIndex++] = new Rect(currX, currY, tex.width, tex.height);
                    currX += (tex.width + studio.packing.padding);
                }

                if (needMultiply)
                {
                    if (atlasWidth == atlasHeight)
                        atlasWidth *= 2;
                    else // atlasWidth > atlasHeight
                        atlasHeight *= 2;

                    if (atlasWidth > 8192)
                    {
                        Debug.Log("Output sprite sheet size is bigger than 8192 X 8192");
                        return;
                    }
                }
                else
                {
                    atlasLength = atlasWidth;
                    atlasTexture.SetPixels32(atlasPixels);
                    atlasTexture.Apply();
                    break;
                }
            }
        }

        private void WriteSpriteToAtlas(Texture2D spriteTex, Color32[] atlasPixels, int atlasStartX, int atlasStartY, int atlasWidth)
        {
            Color32[] spritePixels = spriteTex.GetPixels32();

            for (int i = 0; i < spriteTex.width * spriteTex.height; ++i)
            {
                int x = i % spriteTex.width;
                int y = i / spriteTex.width;
                int atlasIndex = (atlasStartY + y) * atlasWidth + (atlasStartX + x);
                if (atlasIndex < atlasPixels.Length)
                    atlasPixels[atlasIndex] = spritePixels[i];
            }
        }

        private string SaveAtlasAndSetMetaData(string fileName, ref Texture2D atlasTexture, int atlasLength, Rect[] atlasRects, Texture2D[] textures, IntegerVector[] pivots, string[] spriteNames, bool isNormalMap = false)
        {
            if (isNormalMap)
                fileName += "_normal";

            string filePath = TextureHelper.SaveTexture(folderPath, fileName, atlasTexture);
            AssetDatabase.ImportAsset(filePath);

            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
            if (textureImporter != null)
            {
                textureImporter.textureType = (isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Sprite);
                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.maxTextureSize = atlasLength;

                SpriteMetaData[] metaData = new SpriteMetaData[textures.Length];
                for (int i = 0; i < textures.Length; i++)
                {
                    string name = spriteNames[i].PadLeft((spriteNames.Length - 1).ToString().Length, '0');
                    metaData[i].name = name;
                    metaData[i].rect = atlasRects[i];
                    metaData[i].alignment = (int)SpriteAlignment.Custom;
                    metaData[i].pivot = new Vector2((float)pivots[i].x / (float)textures[i].width,
                                                    (float)pivots[i].y / (float)textures[i].height);
                }
                textureImporter.spritesheet = metaData;

                AssetDatabase.ImportAsset(filePath);

                atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            }

            return filePath;
        }

        protected const string ANGLE_PARAM_NAME = "angle";

        protected void AddParameterIfNotExist(AnimatorController controller, string paramName, AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Trigger)
        {
            if (!HasParameter(controller, paramName, paramType))
                controller.AddParameter(paramName, paramType);
        }

        protected bool HasParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Trigger)
        {
            foreach (AnimatorControllerParameter param in controller.parameters)
            {
                if (param.name == paramName && param.type == paramType)
                    return true;
            }
            return false;
        }

        protected AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string stateName)
        {
            AnimatorState state = FindState(stateMachine, stateName);
            if (state == null)
                state = stateMachine.AddState(stateName);
            return state;
        }

        protected AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                    return childState.state;
            }
            return null;
        }

        protected void AddDirectionTransitionA2BIfNotExist(AnimatorState stateA, AnimatorState stateB, int angle)
        {
            AnimatorStateTransition transition = FindTransitionA2B(stateA, stateB);
            if (transition == null)
                transition = stateA.AddTransition(stateB);
            RemoveAllAndAddCondition(transition, ANGLE_PARAM_NAME, AnimatorConditionMode.Equals, angle);
        }

        protected AnimatorStateTransition FindTransitionA2B(AnimatorState stateA, AnimatorState stateB)
        {
            foreach (AnimatorStateTransition transition in stateA.transitions)
            {
                if (transition.destinationState == stateB)
                    return transition;
            }
            return null;
        }

        protected void RemoveAllAndAddCondition(AnimatorTransitionBase transition, string paramName, AnimatorConditionMode mode, float threshold)
        {
            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == paramName)
                    transition.RemoveCondition(condition);
            }
            transition.AddCondition(mode, threshold, paramName);
        }

        protected AnimationClip MakeAnimationClipsForView(bool isLooping, Sprite[] sprites, string viewName)
        {
            AnimationClip animClip = new AnimationClip
            {
                frameRate = studio.output.frameRate
            };

            if (isLooping)
            {
                AnimationClipSettings animClipSettings = AnimationUtility.GetAnimationClipSettings(animClip);
                animClipSettings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(animClip, animClipSettings);
            }

            EditorCurveBinding spriteCurveBinding;
            if (model.prefabBuilder != null)
                spriteCurveBinding = model.prefabBuilder.MakeSpriteCurveBinding();
            else
                spriteCurveBinding = PrefabBuilder.GetDefaultSpriteCurveBinding();

            ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteKeyFrames[i] = new ObjectReferenceKeyframe();
                float unitTime = 1f / animClip.frameRate;
                spriteKeyFrames[i].time = studio.output.frameInterval * i * unitTime;
                spriteKeyFrames[i].value = sprites[i];
            }

            AnimationUtility.SetObjectReferenceCurve(animClip, spriteCurveBinding, spriteKeyFrames);

            string clipFilePath = Path.Combine(folderPath, fileBaseName + "_" + viewName + ".anim");
            AssetDatabase.CreateAsset(animClip, clipFilePath);

            if (animatorStates != null && studio.view.checkedSubViews.Count == animatorStates.Count)
            {
                for (int i = 0; i < studio.view.checkedSubViews.Count; ++i)
                {
                    if (studio.view.checkedSubViews[i].name == viewName)
                    {
                        animatorStates[i].motion = animClip;
                        break;
                    }
                }
            }

            return animClip;
        }

        protected bool DoesMakePrefab()
        {
            return studio.output.prefabMake && model.spritePrefab != null && model.prefabBuilder != null;
        }

        protected void SaveAsPrefab(GameObject obj, string prefabName)
        {
            obj.SetActive(true);

            string prefabPath = folderPath + "/" + prefabName + ".prefab";
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
#else
            PrefabUtility.CreatePrefab(prefabPath, obj);
#endif
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
