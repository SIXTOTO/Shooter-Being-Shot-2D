using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
    [CustomEditor(typeof(MeshModel)), CanEditMultipleObjects]
    public class MeshModelEditor : ModelEditor
    {
        private MeshModel model = null;

        private UnityEditorInternal.ReorderableList animReorderableList = null;

        private int animIndex = -1;

        private MeshAnimation SelectedAnimation
        {
            get
            {
                if (model.animations.Count > animIndex && animIndex >= 0)
                    return model.animations[animIndex];
                return null;
            }
        }

        private void SetAnimationByIndex(int index)
        {
            if (index >= 0 && model.animations.Count > index)
            {
                if (animIndex != index && model.animations[index] != null)
                {
                    EditorPrefs.SetInt(model.GetInstanceID().ToString(), index);
                    animIndex = index;
                }
                animReorderableList.index = index;
            }
        }

        private int reservedAnimIndex = -1;

        private AnimatorStateMachine refStateMachine = null;

        private static Texture loopMarkTexture = null;
        private static Texture LoopMarkTexture
        {
            get
            {
                if (loopMarkTexture == null)
                    loopMarkTexture = AssetHelper.FindAsset<Texture>("GUI", "LoopMark");
                return loopMarkTexture;
            }
        }

        void OnEnable()
        {
            model = target as MeshModel;

            animReorderableList = new UnityEditorInternal.ReorderableList(serializedObject, serializedObject.FindProperty("animations"))
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, new GUIContent("Animations", "animation list to bake"));
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = animReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;

                    const float LEFT_MARGIN = 50;
                    float rectWidth = rect.width - LEFT_MARGIN;
                    float popupWidth = rect.width * 0.4f;
                    const float CHECKBOX_WIDTH = 15;

                    SerializedProperty clipProperty = element.FindPropertyRelative("clip");
                    float animClipWidth = rectWidth - popupWidth - CHECKBOX_WIDTH;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(
                        new Rect(rect.x + LEFT_MARGIN, rect.y, animClipWidth, EditorGUIUtility.singleLineHeight),
                        clipProperty, GUIContent.none);
                    if (EditorGUI.EndChangeCheck())
                        model.animations[index].selectedFrames.Clear();

                    if (model.referenceController != null && refStateMachine != null && refStateMachine.states.Length > 0 && clipProperty.objectReferenceValue != null)
                    {
                        string[] stateNames = new string[refStateMachine.states.Length];
                        for (int i = 0; i < refStateMachine.states.Length; ++i)
                            stateNames[i] = refStateMachine.states[i].state.name;

                        SerializedProperty stateIndexProperty = element.FindPropertyRelative("stateIndex");
                        stateIndexProperty.intValue =
                            EditorGUI.Popup(new Rect(rect.x + LEFT_MARGIN + animClipWidth + 5, rect.y, popupWidth - CHECKBOX_WIDTH - 10, EditorGUIUtility.singleLineHeight),
                                            stateIndexProperty.intValue, stateNames);

                        SerializedProperty stateNameProperty = element.FindPropertyRelative("stateName");
                        if (stateNames.Length > stateIndexProperty.intValue)
                            stateNameProperty.stringValue = stateNames[stateIndexProperty.intValue];
                    }

                    SerializedProperty loopingProperty = element.FindPropertyRelative("isLooping");
                    loopingProperty.boolValue =
                        GUI.Toggle(new Rect(rect.x + rect.width - CHECKBOX_WIDTH - 10, rect.y, CHECKBOX_WIDTH + 10, EditorGUIUtility.singleLineHeight),
                                   loopingProperty.boolValue, new GUIContent(LoopMarkTexture, "generates looping animation clip"), GUI.skin.button);
                },

                onAddCallback = (UnityEditorInternal.ReorderableList l) => {
                    var index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("clip").objectReferenceValue = null;
                },

                onSelectCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    SetAnimationByIndex(l.index);
                },

                onRemoveCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    int index = l.index;
                    UnityEditorInternal.ReorderableList.defaultBehaviours.DoRemoveButton(l);

                    if (l.serializedProperty.arraySize > index)
                        reservedAnimIndex = index;
                    else if (l.serializedProperty.arraySize > 0)
                        reservedAnimIndex = l.serializedProperty.arraySize - 1;
                }
            };

            animIndex = EditorPrefs.GetInt(model.GetInstanceID().ToString(), -1);
            if (model.animations.Count > 0 && animIndex < 0)
                animIndex = 0;

            SetAnimationByIndex(animIndex);
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (targets != null && targets.Length > 1)
                OnInspectorGUI_Multi();
            else if (model != null)
                OnInspectorGUI_Single();
        }

        private void OnInspectorGUI_Single()
        {
            Undo.RecordObject(model, "Mesh Model");

            bool isAnyChanged = false;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            model.mainRenderer = EditorGUILayout.ObjectField(new GUIContent("Main Renderer",
                "the most important and biggest renderer in hierarchy"), model.mainRenderer, typeof(Renderer), true) as Renderer;
            bool mainRendererChanged = EditorGUI.EndChangeCheck();

            if (model.mainRenderer == null)
                model.mainRenderer = MeshModel.FindBiggestRenderer(model.gameObject);

            if (model.IsSkinnedModel())
            {
                EditorGUILayout.Space();

                if (mainRendererChanged)
                    model.pivotType = PivotType.Bottom;

                bool isPivotTypeChanged;
                model.pivotType = DrawPivotTypeField(model, out isPivotTypeChanged);
                if (isPivotTypeChanged)
                    UpdateSceneWindow();
            }
            else
            {
                model.pivotType = PivotType.Center;
            }

            if (model.isFixingToOrigin && model.isFixingToGround) GUI.enabled = false;
            {
                bool isGroundPivotChanged;
                model.isGroundPivot = DrawGroundPivotField(model, out isGroundPivotChanged);
                if (isGroundPivotChanged)
                    UpdateSceneWindow();
            }
            if (model.isFixingToOrigin && model.isFixingToGround) GUI.enabled = true;

            if (model.IsSkinnedModel())
            {
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();

                model.rootBoneObj = EditorGUILayout.ObjectField(new GUIContent("Root Bone",
                    "root bone object to fix body"), model.rootBoneObj, typeof(Transform), true) as Transform;

                if (model.rootBoneObj == null) GUI.enabled = false;
                EditorGUI.indentLevel++;
                {
                    model.isFixingToOrigin = DrawFixToOriginField(model, out isAnyChanged);
                    if (model.isFixingToOrigin)
                    {
                        EditorGUI.indentLevel++;
                        if (model.isGroundPivot) GUI.enabled = false;
                        model.isFixingToGround = DrawFixToGroundField(model, out isAnyChanged);
                        if (model.isGroundPivot) GUI.enabled = true;
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
                if (model.rootBoneObj == null) GUI.enabled = true;

                if (EditorGUI.EndChangeCheck())
                    model.Animate(SelectedAnimation, Frame.BEGIN);
            }

            EditorGUILayout.Space();

            if (reservedAnimIndex >= 0)
            {
                SetAnimationByIndex(reservedAnimIndex);
                reservedAnimIndex = -1;
            }

            Rect animBoxRect = EditorGUILayout.BeginVertical();
            serializedObject.Update();
            animReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        if (!animBoxRect.Contains(Event.current.mousePosition))
                            break;
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (Event.current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (object draggedObj in DragAndDrop.objectReferences)
                            {
                                AnimationClip clip = draggedObj as AnimationClip;
                                if (clip != null)
                                {
                                    MeshAnimation anim = new MeshAnimation();
                                    anim.clip = clip;
                                    anim.selectedFrames = new List<Frame>();
                                    model.AddAnimation(anim);
                                }
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }

            if (model.animations.Count > 0 && DrawingHelper.DrawMiddleButton("Clear all"))
                model.animations.Clear();

            EditorGUILayout.Space();

            DrawReferenceControllerField();
            if (model.referenceController != null)
            {
                EditorGUI.indentLevel++;
                model.outputController = EditorGUILayout.ObjectField(new GUIContent("Output Controller",
                    "controller to which a baker saves animation states related to the animation list's animations"),
                    model.outputController, typeof(AnimatorController), false) as AnimatorController;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            model.spritePrefab = DrawSpritePrefabField(model, out isAnyChanged);
            if (model.spritePrefab != null)
            {
                EditorGUI.indentLevel++;
                model.prefabBuilder = DrawPrefabBuilderField(model, out isAnyChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            if (SelectedAnimation != null)
            {
                SelectedAnimation.customizer = EditorGUILayout.ObjectField(new GUIContent("Customizer",
                    "component that customizes this model at every frames in the selected animation"),
                    SelectedAnimation.customizer, typeof(AnimationCustomizer), true) as AnimationCustomizer;
            }

            EditorGUILayout.Space();

            bool isNameSuffixChanged;
            model.nameSuffix = DrawModelNameSuffix(model, out isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref model.nameSuffix);

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add to the model list"))
                AddToModelList(model);
        }

        protected void OnInspectorGUI_Multi()
        {
            EditorGUILayout.HelpBox("Displayed information is of the first selected model,\nbut any change affects all selected models.", MessageType.Info);

            MeshModel[] models = new MeshModel[targets.Length];

            for (int i = 0; i < models.Length; ++i)
                models[i] = targets[i] as MeshModel;

            MeshModel firstModel = models[0];

            bool isAllSkinnedModel = true;
            foreach (MeshModel model in models)
                isAllSkinnedModel &= model.IsSkinnedModel();

            bool isAllNotFixingToGround = true;
            foreach (MeshModel model in models)
                isAllNotFixingToGround &= (!model.isFixingToOrigin || !model.isFixingToGround);

            PivotType pivotType = PivotType.Bottom;
            bool isPivotTypeChanged = false;
            bool isGroundPivot = false;
            bool isGroundPivotChanged = false;

            if (isAllSkinnedModel || isAllNotFixingToGround)
            {
                EditorGUILayout.Space();

                if (isAllSkinnedModel)
                    pivotType = DrawPivotTypeField(firstModel, out isPivotTypeChanged);

                if (isAllNotFixingToGround)
                    isGroundPivot = DrawGroundPivotField(firstModel, out isGroundPivotChanged);
            }

            bool hasAllRootBone = true;
            foreach (MeshModel model in models)
                hasAllRootBone &= (model.rootBoneObj != null);

            bool isFixingToOrigin = false;
            bool isFixingToOriginChanged = false;

            bool isAllFixingToOrigin = true;
            bool isAllNotGroundPivot = true;
            bool isFixingToGround = false;
            bool isFixingToGroundChanged = false;

            if (isAllSkinnedModel && hasAllRootBone)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Root Bone");

                EditorGUI.indentLevel++;
                {
                    isFixingToOrigin = DrawFixToOriginField(firstModel, out isFixingToOriginChanged);

                    foreach (MeshModel model in models)
                        isAllFixingToOrigin &= model.isFixingToOrigin;
                    foreach (MeshModel model in models)
                        isAllNotGroundPivot &= !model.isGroundPivot;

                    if (isAllFixingToOrigin && isAllNotGroundPivot)
                    {
                        EditorGUI.indentLevel++;
                        isFixingToGround = DrawFixToGroundField(firstModel, out isFixingToGroundChanged);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isSpritePrefabChanged;
            GameObject spritePrefab = DrawSpritePrefabField(firstModel, out isSpritePrefabChanged);

            bool hasAllSpritePrefab = true;
            foreach (MeshModel model in models)
                hasAllSpritePrefab &= (model.spritePrefab != null);

            PrefabBuilder prefabBuilder = null;
            bool isPrefabBuilderChanged = false;
            if (hasAllSpritePrefab)
            {
                EditorGUI.indentLevel++;
                prefabBuilder = DrawPrefabBuilderField(model, out isPrefabBuilderChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isNameSuffixChanged;
            string nameSuffix = DrawModelNameSuffix(firstModel, out isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref nameSuffix);

            if (isPivotTypeChanged || isGroundPivotChanged || isFixingToOriginChanged || isFixingToGroundChanged ||
                isSpritePrefabChanged || isPrefabBuilderChanged || isNameSuffixChanged)
            {
                foreach (MeshModel model in models)
                {
                    Undo.RecordObject(model, "Mesh Model");

                    if (isAllSkinnedModel && isPivotTypeChanged)
                        model.pivotType = pivotType;
                    if (isAllNotFixingToGround && isGroundPivotChanged)
                        model.isGroundPivot = isGroundPivot;

                    if (isAllSkinnedModel && hasAllRootBone)
                    {
                        if (isFixingToOriginChanged)
                            model.isFixingToOrigin = isFixingToOrigin;
                        if (isAllFixingToOrigin && isAllNotGroundPivot && isFixingToGroundChanged)
                            model.isFixingToGround = isFixingToGround;
                    }

                    if (isSpritePrefabChanged)
                        model.spritePrefab = spritePrefab;

                    if (hasAllSpritePrefab && isPrefabBuilderChanged)
                        model.prefabBuilder = prefabBuilder;

                    if (isNameSuffixChanged)
                        model.nameSuffix = nameSuffix;
                }
            }

            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
                return;

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add all to the model list"))
            {
                foreach (MeshModel model in models)
                    AddToModelList(model);
            }
        }

        private PivotType DrawPivotTypeField(MeshModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            PivotType pivotType = (PivotType)EditorGUILayout.EnumPopup(new GUIContent("Pivot Type",
                "type of the root object's world position"), model.pivotType);
            isChanged = EditorGUI.EndChangeCheck();

            return pivotType;
        }

        private void UpdateSceneWindow()
        {
            EditorWindow sceneWindow = EditorWindow.GetWindow<SceneView>();
            sceneWindow.Repaint();
        }

        private bool DrawFixToOriginField(MeshModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isFixingToOrigin = EditorGUILayout.Toggle(new GUIContent("Fix to Origin",
                "animates in place"), model.isFixingToOrigin);
            isChanged = EditorGUI.EndChangeCheck();

            return isFixingToOrigin;
        }

        private bool DrawFixToGroundField(MeshModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isFixingToGround = EditorGUILayout.Toggle(new GUIContent("Fix to Ground",
                "animates on ground, preventing from floating"), model.isFixingToGround);
            isChanged = EditorGUI.EndChangeCheck();

            return isFixingToGround;
        }

        private void DrawReferenceControllerField()
        {
            model.referenceController = EditorGUILayout.ObjectField(new GUIContent("Reference Controller",
                "guide controller to build an output controller"),
                model.referenceController, typeof(AnimatorController), false) as AnimatorController;

            if (model.referenceController != null)
            {
                if (model.referenceController.layers.Length > 1)
                {
                    Debug.LogError("Reference controller in which has layers more than 1 is not supported.");
                    model.referenceController = null;
                }
                else if (model.referenceController.layers[0].stateMachine.stateMachines.Length > 0)
                {
                    Debug.LogError("Reference controller in which has any sub machine is not supported.");
                    model.referenceController = null;
                }

                refStateMachine = model.referenceController.layers[0].stateMachine;
                if (refStateMachine.states.Length == 0)
                {
                    Debug.LogError("Reference controller in which has no state is not supported.");
                    model.referenceController = null;
                }
            }
        }
    }
}
