using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class ProgressDrawer
	{
        private static Texture checkMarkTexture = null;
        private static Texture CheckMarkTexture
        {
            get
            {
                if (checkMarkTexture == null)
                    checkMarkTexture = AssetHelper.FindAsset<Texture>(EditorGlobal.GUI_FOLDER_NAME, "CheckMark");
                return checkMarkTexture;
            }
        }

        private static readonly Color lightGrayColor = new Color32(170, 170, 170, 255);
        private static readonly Color darkGrayColor = new Color32(90, 90, 90, 255);

        private enum FieldState
        {
            None, Completed, Baking
        }

        public static void DrawCapturingProgress(int editorWidth, int editorHeight, List<Model> bakingModels, Sampler sampler, Batcher batcher)
        {
            EditorGUILayout.HelpBox("Don't do anything while capturing..", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUIStyle titleStyle = new GUIStyle();
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            if (sampler != null)
            {
                EditorGUILayout.LabelField("Sampling..", titleStyle);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            else if (batcher != null)
            {
                DrawBakingProgress(editorWidth, editorHeight, titleStyle, bakingModels, batcher);
            }

            EditorGUILayout.Space();
            if (DrawingHelper.DrawMiddleButton("Cancel"))
            {
                if (sampler != null)
                {
                    sampler.IsCancelled = true;
                    sampler.Finish();
                }
                else if (batcher != null)
                {
                    Baker baker = batcher.CurrentBaker;
                    if (baker != null)
                    {
                        baker.IsCancelled = true;
                        baker.Finish();
                    }
                    batcher.Finish();
                }
            }
            EditorGUILayout.Space();
        }

        private static void DrawBakingProgress(int editorWidth, int editorHeight, GUIStyle titleStyle, List<Model> bakingModels, Batcher batcher)
        {
            const float HELP_BOX_HEIGHT = 54; // help box + two spaces
            const float TITLE_HEIGHT = 30; // title label + two spaces
            const float BOTTOM_AREA_HEIGHT = 34; // cancel button + two spaces
            float maxBodyAreaHeight = editorHeight - HELP_BOX_HEIGHT - TITLE_HEIGHT - BOTTOM_AREA_HEIGHT;

            float FIELD_HEIGHT = 16;

            float computedBodyHeight = 0;
            for (int i = 0; i < bakingModels.Count; ++i)
            {
                computedBodyHeight += FIELD_HEIGHT;

                Model model = bakingModels[i];
                if (Model.IsMeshModel(model))
                    computedBodyHeight += Model.AsMeshModel(model).GetValidAnimations().Count * FIELD_HEIGHT;
            }

            float bodyAreaHeight = Mathf.Min(computedBodyHeight, maxBodyAreaHeight);

            Rect titleRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(string.Format("Baking.. ({0}/{1})", batcher.ModelIndex, bakingModels.Count), titleStyle, GUILayout.Height(TITLE_HEIGHT + bodyAreaHeight));
            EditorGUILayout.EndVertical();

            float bodyY = titleRect.y + TITLE_HEIGHT;

            int firstModelIndex = 0;
            if (computedBodyHeight > maxBodyAreaHeight)
            {
                float simulatedBodyHeight = 0;
                for (int i = 0; i < bakingModels.Count; ++i)
                {
                    float totalModelHeight = FIELD_HEIGHT;

                    Model model = bakingModels[i];
                    if (Model.IsMeshModel(model))
                        totalModelHeight += Model.AsMeshModel(model).GetValidAnimations().Count * FIELD_HEIGHT;

                    float thresholdHeight = totalModelHeight + FIELD_HEIGHT * 2; // extra two for baking one & post ellipsis;

                    if (simulatedBodyHeight + thresholdHeight < maxBodyAreaHeight)
                    {
                        simulatedBodyHeight += totalModelHeight;
                    }
                    else
                    {
                        if (i > batcher.ModelIndex)
                            break;
                        firstModelIndex++;

                        if (firstModelIndex == 1)
                            simulatedBodyHeight += FIELD_HEIGHT; // for pre ellipsis
                    }
                }
            }
            Debug.Assert(firstModelIndex < bakingModels.Count);

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            { // progress bar
                int totalTargetCount = 0;
                int ongoingTargetCount = 0;
                for (int i = 0; i < bakingModels.Count; ++i)
                {
                    Model model = bakingModels[i];
                    if (Model.IsMeshModel(model))
                    {
                        int validAnimationCount = Model.AsMeshModel(model).GetValidAnimations().Count;
                        if (validAnimationCount > 0)
                        {
                            totalTargetCount += validAnimationCount;
                            if (i < batcher.ModelIndex)
                            {
                                ongoingTargetCount += validAnimationCount;
                            }
                            else if (i == batcher.ModelIndex)
                            {
                                MeshBaker animationBaker = batcher.CurrentBaker as MeshBaker;
                                if (animationBaker != null)
                                    ongoingTargetCount += animationBaker.AnimationIndex;
                            }
                        }
                        else
                        {
                            totalTargetCount++;
                            if (i <= batcher.ModelIndex)
                                ongoingTargetCount++;
                        }
                    }
                    else
                    {
                        totalTargetCount++;
                        if (i <= batcher.ModelIndex)
                            ongoingTargetCount++;
                    }
                }

                float progress = (float)ongoingTargetCount / (float)totalTargetCount;

                const float PROGRESS_BAR_WIDTH = 10;

                Rect progressBarBgRect = new Rect(editorWidth, bodyY, PROGRESS_BAR_WIDTH, bodyAreaHeight);
                DrawingHelper.FillRect(progressBarBgRect, EditorGUIUtility.isProSkin ? lightGrayColor : darkGrayColor);

                Rect progressBarFgRect = new Rect(editorWidth, bodyY, PROGRESS_BAR_WIDTH, bodyAreaHeight * progress);
                DrawingHelper.FillRect(progressBarFgRect, EditorGUIUtility.isProSkin ? Color.white : Color.black);

                labelStyle.alignment = TextAnchor.MiddleRight;
                float progressLabelY = bodyY + bodyAreaHeight * progress - (FIELD_HEIGHT / 2);
                Rect progressLabelRect = new Rect(14, progressLabelY, editorWidth - 17, FIELD_HEIGHT);
                EditorGUI.LabelField(progressLabelRect, string.Format("{0:0}%", progress * 100), labelStyle);
            }

            labelStyle.alignment = TextAnchor.MiddleLeft;

            float FIELD_X = 14;
            float bodyHeight = 0;

            if (firstModelIndex > 0)
            {
                Rect ellipsisRect = new Rect(FIELD_X, bodyY + bodyHeight, editorWidth, FIELD_HEIGHT);
                EditorGUI.indentLevel++; EditorGUI.indentLevel++;
                EditorGUI.LabelField(ellipsisRect, "...", labelStyle);
                EditorGUI.indentLevel--; EditorGUI.indentLevel--;
                bodyHeight += FIELD_HEIGHT;
            }

            for (int mi = firstModelIndex; mi < bakingModels.Count; ++mi)
            {
                if (bodyHeight + FIELD_HEIGHT > maxBodyAreaHeight)
                    return;

                Model model = bakingModels[mi];

                Rect modelRect = new Rect(FIELD_X, bodyY + bodyHeight, editorWidth, FIELD_HEIGHT);
                bodyHeight += FIELD_HEIGHT;

                FieldState state = FieldState.None;
                if (mi < batcher.ModelIndex)
                    state = FieldState.Completed;
                else if (mi == batcher.ModelIndex)
                    state = FieldState.Baking;

                bool elliptical = false;
                if (bodyHeight + FIELD_HEIGHT > maxBodyAreaHeight)
                {
                    if (mi < bakingModels.Count - 1)
                        elliptical = true;
                }

                string labelText;
                if (elliptical)
                {
                    labelText = "...";
                }
                else
                {
                    labelText = mi.ToString() + ". " + model.name;
                    if (model.nameSuffix.Length > 0)
                        labelText += model.nameSuffix;
                }

                labelStyle.fontStyle = GetLabelFontStyle(state);

                EditorGUI.indentLevel++;
                EditorGUI.LabelField(modelRect, labelText, labelStyle);
                EditorGUI.indentLevel--;

                if (state == FieldState.Completed && !elliptical)
                    DrawCheckMark(modelRect);

                if (Model.IsMeshModel(model))
                {
                    MeshModel meshModel = Model.AsMeshModel(model);
                    List<MeshAnimation> validAnimations = meshModel.GetValidAnimations();

                    for (int ai = 0; ai < validAnimations.Count; ++ai)
                    {
                        if (bodyHeight + FIELD_HEIGHT > maxBodyAreaHeight)
                            return;

                        MeshAnimation anim = validAnimations[ai];

                        MeshBaker animationBaker = batcher.CurrentBaker as MeshBaker;

                        Rect animationRect = new Rect(FIELD_X, bodyY + bodyHeight, editorWidth, FIELD_HEIGHT);
                        bodyHeight += FIELD_HEIGHT;

                        state = FieldState.None;
                        if (mi < batcher.ModelIndex || (mi == batcher.ModelIndex && (animationBaker != null && ai < animationBaker.AnimationIndex)))
                            state = FieldState.Completed;
                        else if (mi == batcher.ModelIndex && (animationBaker != null && ai == animationBaker.AnimationIndex))
                            state = FieldState.Baking;

                        elliptical = false;
                        if (bodyHeight + FIELD_HEIGHT > maxBodyAreaHeight)
                        {
                            if (mi < bakingModels.Count - 1 || ai < validAnimations.Count - 1)
                                elliptical = true;
                        }

                        if (elliptical)
                            labelText = "...";
                        else
                            labelText = (meshModel.referenceController != null) ? anim.stateName : anim.clip.name;

                        labelStyle.fontStyle = GetLabelFontStyle(state);

                        EditorGUI.indentLevel++; EditorGUI.indentLevel++;
                        EditorGUI.LabelField(animationRect, labelText, labelStyle);
                        EditorGUI.indentLevel--; EditorGUI.indentLevel--;

                        if (state == FieldState.Completed && !elliptical)
                            DrawCheckMark(animationRect, 10);
                    }
                }
            }
        }

        private static FontStyle GetLabelFontStyle(FieldState state)
        {
            switch (state)
            {
                case FieldState.Completed:
                    return FontStyle.Italic;
                case FieldState.Baking:
                    return FontStyle.Bold;
                default:
                    return FontStyle.Normal;
            }
        }

        private static void DrawCheckMark(Rect labelRect, int leftMargin = 0)
        {
            Rect checkMarkRect = new Rect(labelRect.x + leftMargin, labelRect.y + 3, CheckMarkTexture.width, CheckMarkTexture.height);
            GUI.DrawTexture(checkMarkRect, CheckMarkTexture);
        }
    }
}
