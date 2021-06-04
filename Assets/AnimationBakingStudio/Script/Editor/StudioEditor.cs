using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace ABS
{
    [CustomEditor(typeof(Studio))]
    public class StudioEditor : Editor
    {
        private Studio studio = null;

        private ReorderableList modelReorderableList = null;

        private const string MODEL_INDEX_KEY = "ModelIndex";
        private int modelIndex = -1;
        private bool modelChanged = false;

        private Model SelectedModel
        {
            get
            {
                if (studio.model.list.Count > modelIndex && modelIndex >= 0)
                    return studio.model.list[modelIndex];
                return null;
            }
        }

        private int reservedModelIndex = -1;

        private MeshAnimation selectedAnimation = null;

        private float CurrentTurnAngle
        {
            get
            {
                return studio.view.baseTurnAngle + studio.appliedSubViewTurnAngle;
            }
        }

        private bool variationExcludingShadowBackup = false;
        private bool shadowWithoutModel = false;

        private Sampler sampler = null;

        private Batcher batcher = null;
        private List<Model> bakingModels = new List<Model>();

        private CameraClearFlags cameraClearFlagsBackup = CameraClearFlags.SolidColor;
        private Color cameraBackgroundColorBackup = lightGreenColor;

        private object hdrpCameraClearColorModeBackup;
        private object hdrpCameraBackgroundColorHdrBackup;

        private Dictionary<Model, bool> modelActivationBackup = new Dictionary<Model, bool>();

        private Texture2D previewTexture = null;

        private Texture arrowDownTexture = null;
        private Texture ArrowDownTexture
        {
            get
            {
                if (arrowDownTexture == null)
                    arrowDownTexture = AssetHelper.FindAsset<Texture>(EditorGlobal.GUI_FOLDER_NAME,
                        "ArrowDown" + (EditorGUIUtility.isProSkin ? "_pro" : ""));
                return arrowDownTexture;
            }
        }

        private Texture arrowRightTexture = null;
        private Texture ArrowRightTexture
        {
            get
            {
                if (arrowRightTexture == null)
                    arrowRightTexture = AssetHelper.FindAsset<Texture>(EditorGlobal.GUI_FOLDER_NAME,
                        "ArrowRight" + (EditorGUIUtility.isProSkin ? "_pro" : ""));
                return arrowRightTexture;
            }
        }

        private int editorY;
        private int editorWidth;
        private int editorHeight;

        private static readonly Color lightGreenColor = new Color32(0, 200, 0, 255);
        private static readonly Color darkGreenColor = new Color32(0, 50, 0, 255);

        private void SetModelByIndex(int index)
        {
            if (index >= 0 && studio.model.list.Count > index)
            {
                if (modelIndex != index && studio.model.list[index] != null)
                {
                    EditorPrefs.SetInt(MODEL_INDEX_KEY, index);
                    modelIndex = index;
                    modelChanged = true;
                    studio.samplings.Clear();
                    studio.frame.simulatedIndex = 0;
                    selectedAnimation = null;
                }
                modelReorderableList.index = index;

                CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
            }
        }

        void OnEnable()
        {
            studio = target as Studio;

            modelReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("model.list"))
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, new GUIContent("Models", "model list to bake"));
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = modelReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(
                        new Rect(rect.x + 100, rect.y, rect.width - 100, EditorGUIUtility.singleLineHeight),
                        element, GUIContent.none);
                    modelChanged = EditorGUI.EndChangeCheck();

                    if (modelChanged)
                    {
                        Model model = element.objectReferenceValue as Model;
                        if (model != null)
                            model.ClearFrames();
                        studio.samplings.Clear();
                        studio.frame.simulatedIndex = 0;
                    }

                    if (modelChanged && index == modelIndex)
                        CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                },

                onAddCallback = (ReorderableList l) => {
                    var index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                    element.objectReferenceValue = null;
                },

                onSelectCallback = (ReorderableList l) =>
                {
                    SetModelByIndex(l.index);
                },

                onRemoveCallback = (ReorderableList l) =>
                {
                    int index = l.index;
                    SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                    if (element.objectReferenceValue != null)
                        l.serializedProperty.DeleteArrayElementAtIndex(index);
                    l.serializedProperty.DeleteArrayElementAtIndex(index);

                    if (l.serializedProperty.arraySize > index)
                        reservedModelIndex = index;
                    else if (l.serializedProperty.arraySize > 0)
                        reservedModelIndex = l.serializedProperty.arraySize - 1;

                    studio.samplings.Clear();
                }
            };

            modelIndex = EditorPrefs.GetInt(MODEL_INDEX_KEY, -1);
            if (studio.model.list.Count > 0 && modelIndex < 0)
                modelIndex = 0;

            SetModelByIndex(modelIndex);

            ConfigurationWindow.UpdateGlobalVariables();
        }

        void OnDisable()
        {
            if (sampler != null)
                EditorApplication.update -= sampler.UpdateState;

            if (batcher != null)
                EditorApplication.update -= batcher.UpdateState;
        }

        public override void OnInspectorGUI()
        {
            if (studio == null)
                return;

            Rect rect = EditorGUILayout.BeginVertical(); EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                //editorY = (int)rect.y;
                editorY = 200;
                editorWidth = (int)rect.width;
            }

            int screenHeight = Screen.height;
#if UNITY_EDITOR_OSX
            screenHeight /= 2;
#endif
            editorHeight = screenHeight - editorY - 100;

            if (sampler != null || batcher != null)
            {
                ProgressDrawer.DrawCapturingProgress(editorWidth, editorHeight, bakingModels, sampler, batcher);
                return;
            }

            GUI.changed = false;

            Undo.RecordObject(studio, "Studio");

            studio.isSamplingReady = true;
            studio.isBakingReady = true;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck(); // check any changes
            {
                DrawModelFields();

                bool noReadyModel = false;
                for (int i = 0; i < studio.model.list.Count; ++i)
                {
                    Model model = studio.model.list[i];
                    if (model != null)
                    {
                        if (studio.view.rotationType == RotationType.Camera)
                        {
                            if (Model.IsMeshModel(model))
                                Model.AsMeshModel(model).currentAngle = 0;
                            model.transform.rotation = Quaternion.identity;
                        }

                        if (model.IsReady())
                        {
                            noReadyModel = true;
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(string.Format("{0} at index {1} will not be baked because it is not ready.", model.name, i), MessageType.Warning);
                            studio.model.opened = true;
                        }
                    }
                }
                if (!noReadyModel)
                {
                    EditorGUILayout.HelpBox("No prepared model to capture!", MessageType.Error);
                    studio.isSamplingReady = false;
                    studio.isBakingReady = false;
                    studio.model.opened = true;
                }

                if (studio.model.list.Count > 0 && SelectedModel == null)
                    SetModelByIndex(0);

                EditorGUILayout.Space();

                DrawCameraFields();

                if (Camera.main == null)
                {
                    studio.isSamplingReady = false;
                    studio.isBakingReady = false;
                    studio.cam.opened = true;
                }

                EditorGUILayout.Space();

                DrawLightFields();

                EditorGUILayout.Space();

                DrawViewFields();

                if (studio.view.checkedSubViews.Count == 0)
                {
                    EditorGUILayout.HelpBox("No selected view!", MessageType.Error);
                    studio.isBakingReady = false;
                    studio.view.opened = true;
                }

                EditorGUILayout.Space();

                DrawShadowFields();

                EditorGUILayout.Space();

                DrawExtractionFields();

                if (studio.extraction.com == null)
                {
                    EditorGUILayout.HelpBox("No extractor!", MessageType.Error);
                    studio.isSamplingReady = false;
                    studio.isBakingReady = false;
                    studio.extraction.opened = true;
                }

                //EditorGUILayout.Space();

                //DrawVariationFields();

                EditorGUILayout.Space();

                DrawPreviewFields();

                EditorGUILayout.Space();

                DrawFrameFields();

                EditorGUILayout.Space();

                if (studio.isSamplingReady)
                    DrawSamplingFields();

                EditorGUILayout.Space();

                DrawTrimmingFields();

                EditorGUILayout.Space();

                DrawPackingFields();

                EditorGUILayout.Space();

                DrawOutputFields();

                EditorGUILayout.Space();
            }

            if (studio.preview.on)
            {
                if (EditorGUI.EndChangeCheck() | modelChanged || previewTexture == null)
                    UpdatePreviewTexture();
            }

            DrawPathFields();

            if (studio.path.directoryPath == null || studio.path.directoryPath.Length == 0 || !Directory.Exists(studio.path.directoryPath))
            {
                EditorGUILayout.HelpBox("Invalid directory!", MessageType.Error);
                studio.isBakingReady = false;
                studio.path.opened = true;
            }
            else
            {
                if (studio.path.directoryPath.IndexOf(Application.dataPath) < 0)
                {
                    EditorGUILayout.HelpBox(string.Format("{0} is out of Assets folder.", studio.path.directoryPath), MessageType.Error);
                    studio.isBakingReady = false;
                    studio.path.opened = true;
                }
            }

            EditorGUILayout.Space();

            if (studio.isBakingReady)
                DrawBakingFields();

            modelChanged = false;
        }

        private void DrawModelFields()
        {
            if (!DrawGroupOrPass("Model", ref studio.model.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();

            if (reservedModelIndex >= 0)
            {
                SetModelByIndex(reservedModelIndex);
                reservedModelIndex = -1;
            }

            Rect modelBoxRect = EditorGUILayout.BeginVertical();
            serializedObject.Update();
            modelReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        if (!modelBoxRect.Contains(Event.current.mousePosition))
                            break;
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (Event.current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (object draggedObj in DragAndDrop.objectReferences)
                            {
                                GameObject go = draggedObj as GameObject;
                                if (go == null)
                                    continue;

                                Model model = go.GetComponent<Model>();
                                if (model != null)
                                    studio.AddModel(model);
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }

            if (studio.model.list.Count > 0 && DrawingHelper.DrawMiddleButton("Clear all"))
                studio.model.list.Clear();

            if (SelectedModel != null && Model.IsMeshModel(SelectedModel))
            {
                MeshModel meshModel = Model.AsMeshModel(SelectedModel);

                List <MeshAnimation> validAnimations = meshModel.GetValidAnimations();
                if (validAnimations.Count > 0)
                {
                    GUIContent[] popupStrings = new GUIContent[validAnimations.Count];
                    for (int i = 0; i < validAnimations.Count; ++i)
                    {
                        MeshAnimation anim = validAnimations[i];
                        string stateName = (meshModel.referenceController != null) ? anim.stateName : anim.clip.name;
                        popupStrings[i] = new GUIContent(stateName);
                    }

                    if (studio.animPopupIndex >= popupStrings.Length)
                        studio.animPopupIndex = 0;

                    EditorGUI.BeginChangeCheck();
                    studio.animPopupIndex = EditorGUILayout.Popup(new GUIContent("Animation",
                        "active one of the selected model's animations"), studio.animPopupIndex, popupStrings);
                    if (EditorGUI.EndChangeCheck())
                    {
                        studio.samplings.Clear();
                        studio.frame.simulatedIndex = 0;
                    }
                    selectedAnimation = validAnimations[studio.animPopupIndex];
                }
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawCameraFields()
        {
            if (!DrawGroupOrPass("Camera", ref studio.cam.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            if (Camera.main != null)
            {
                EditorGUILayout.LabelField("Main Camera Exists.", EditorStyles.boldLabel);
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.red;
                labelStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField(new GUIContent("No Main Camera!",
                    "Camera that has 'MainCamera' tag doesn't exist or is inactive."), labelStyle);

                if (DrawingHelper.DrawMiddleButton("Create a Main Camera"))
                {
                    ObjectHelper.GetOrCreateObject("Main Camera", "Prefab", new Vector3(0, 100, 0));
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                }
            }

            bool isParticleModel = Model.IsParticleModel(SelectedModel);

            if (Camera.main != null)
            {
                string[] texts = new string[2] { "Orthographic", "Perspective" };

                CameraMode mode = isParticleModel ? CameraMode.Orthographic : studio.cam.mode;

                if (isParticleModel) GUI.enabled = false;
                mode = (CameraMode)GUILayout.Toolbar((int)mode, texts);
                if (isParticleModel) GUI.enabled = true;

                if (!isParticleModel)
                    studio.cam.mode = mode;

                Camera.main.orthographic = (mode == CameraMode.Orthographic);

                EditorGUI.indentLevel++;
                if (Camera.main.orthographic)
                    Camera.main.orthographicSize = EditorGUILayout.FloatField("Orthographic Size", Camera.main.orthographicSize);
                else
                    Camera.main.fieldOfView = EditorGUILayout.FloatField("Field Of View", Camera.main.fieldOfView);
                EditorGUI.indentLevel--;
            }

            if (SelectedModel != null && !isParticleModel)
            {
                EditorGUI.BeginChangeCheck();

                string[] texts = new string[2] { "Relative Distance", "Absolute Distance" };
                studio.cam.distanceType = (DistanceType)GUILayout.Toolbar((int)studio.cam.distanceType, texts);

                EditorGUI.indentLevel++;
                {
                    if (studio.cam.distanceType == DistanceType.Relative)
                        studio.cam.relativeDistance = EditorGUILayout.FloatField(new GUIContent("Distance",
                            "distance between model and main camera == this value * model's size"), studio.cam.relativeDistance);
                    else if (studio.cam.distanceType == DistanceType.Absolute)
                        studio.cam.absoluteDistance = EditorGUILayout.FloatField(new GUIContent("Distance",
                            "distance between model and main camera"), studio.cam.absoluteDistance);
                }
                EditorGUI.indentLevel--;

                if (EditorGUI.EndChangeCheck())
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawLightFields()
        {
            if (!DrawGroupOrPass("Light", ref studio.lit.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            studio.lit.com = EditorGUILayout.ObjectField("Directional Light", studio.lit.com, typeof(Light), true) as Light;
            if (studio.lit.com == null)
            {
                GameObject lightObj = GameObject.Find("Directional Light");
                if (lightObj == null)
                    lightObj = GameObject.Find("Directional light");
                if (lightObj != null)
                    studio.lit.com = lightObj.GetComponent<Light>();
            }

            if (studio.lit.com != null)
            {
                if (!studio.lit.cameraRotationFollow || studio.shadow.type == ShadowType.Matte)
                {
                    EditorGUI.indentLevel++;
                    if (studio.lit.cameraRotationFollow) GUI.enabled = false;
                    {
                        studio.lit.slopeAngle = EditorGUILayout.FloatField(new GUIContent("Slope Angle (10 ~ 90)",
                            "angle between ground and light's direction in X Axis"), studio.lit.slopeAngle);
                        studio.lit.slopeAngle = Mathf.Clamp(studio.lit.slopeAngle, 10f, 90f);
                        studio.lit.turnAngle = EditorGUILayout.FloatField(new GUIContent("Turn Angle",
                            "angle between z-forward and light's direction in Y axis"), studio.lit.turnAngle);
                        float turnAngle = studio.lit.turnAngle + 180f;
                        if (turnAngle > 360f)
                            turnAngle %= 360f;
                        studio.lit.com.transform.rotation = Quaternion.Euler(studio.lit.slopeAngle, turnAngle, 0);

                        if (DrawingHelper.DrawMiddleButton(new GUIContent("Look at Model", "rotates main camera toward model and modify two angles")))
                        {
                            CameraHelper.LookAtModel(studio.lit.com.transform, SelectedModel);
                            Vector3 camEulerAngles = studio.lit.com.transform.rotation.eulerAngles;
                            studio.lit.slopeAngle = camEulerAngles.x;
                            studio.lit.turnAngle = camEulerAngles.y;
                        }
                    }
                    if (studio.lit.cameraRotationFollow) GUI.enabled = true;
                    EditorGUI.indentLevel--;
                }

                studio.lit.cameraRotationFollow = EditorGUILayout.Toggle(new GUIContent("Follow Camera Rotation",
                    "makes light's direction the same as main camera's direction"), studio.lit.cameraRotationFollow);
                if (studio.lit.cameraRotationFollow)
                {
                    if (Camera.main != null)
                        studio.lit.com.transform.rotation = Camera.main.transform.rotation;
                }

                studio.lit.cameraPositionFollow = EditorGUILayout.Toggle(new GUIContent("Follow Camera Position",
                    "makes light's position the same as main camera's position"), studio.lit.cameraPositionFollow);
                if (studio.lit.cameraPositionFollow)
                {
                    if (Camera.main != null && studio.lit.com != null)
                        studio.lit.com.transform.position = Camera.main.transform.position;
                }
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawViewFields()
        {
            bool groupOpened = DrawGroupOrPass("View", ref studio.view.opened);

            bool baseTurnAngleChanged = false;
            if (groupOpened)
            {
                GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

                EditorGUI.BeginChangeCheck();
                {
                    string[] texts = new string[2] { "Camera Rotation", "Model Rotation" };
                    studio.view.rotationType = (RotationType)GUILayout.Toolbar((int)studio.view.rotationType, texts);
                }
                if (EditorGUI.EndChangeCheck() && studio.view.rotationType == RotationType.Model)
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio);

                EditorGUI.BeginChangeCheck();
                studio.view.slopeAngle = EditorGUILayout.FloatField(new GUIContent("View Slope Angle (0 ~ 90)",
                    "angle between ground and main camera's direction in X Axis"), studio.view.slopeAngle);
                studio.view.slopeAngle = Mathf.Clamp(studio.view.slopeAngle, 0f, 90f);
                bool slopeAngleChanged = EditorGUI.EndChangeCheck();

                if (studio.IsSideView())
                    studio.view.isTileVisible = false;
                else
                    DrawTileFields(ref slopeAngleChanged);

                if (slopeAngleChanged)
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);

                EditorGUI.BeginChangeCheck();
                studio.view.size = EditorGUILayout.IntField(new GUIContent("View Size",
                    "the number of scenes to capture"), studio.view.size);
                if (studio.view.size < 1)
                    studio.view.size = 1;
                bool viewSizeChanged = EditorGUI.EndChangeCheck();

                if (viewSizeChanged || studio.view.size != studio.view.subViewToggles.Length)
                {
                    SubViewToggle[] oldSubViewToggles = (SubViewToggle[])studio.view.subViewToggles.Clone();
                    studio.view.subViewToggles = new SubViewToggle[studio.view.size];
                    for (int i = 0; i < studio.view.subViewToggles.Length; ++i)
                        studio.view.subViewToggles[i] = new SubViewToggle(false);
                    MigrateViews(oldSubViewToggles);
                }

                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                studio.view.unitTurnAngle = 360f / studio.view.size;
                string label = string.Format("Base Angle (0 ~ {0})", (int)studio.view.unitTurnAngle);
                studio.view.baseTurnAngle = EditorGUILayout.FloatField(new GUIContent(label,
                    "initial angle from z-forward in Y axis"), studio.view.baseTurnAngle);
                studio.view.baseTurnAngle = Mathf.Clamp(studio.view.baseTurnAngle, 0, (int)studio.view.unitTurnAngle);
                baseTurnAngleChanged = EditorGUI.EndChangeCheck();
            }

            List<SubView> checkedSubViews = new List<SubView>();

            bool subViewChanged = false;
            for (int i = 0; i < studio.view.size; i++)
            {
                float subViewTurnAngle = studio.view.unitTurnAngle * i;
                float turnAngle = studio.view.baseTurnAngle + subViewTurnAngle;

                RotationCallback callback = (Model model) =>
                {
                    if (model == null)
                        return;

                    if (studio.view.rotationType == RotationType.Camera)
                    {
                        CameraHelper.LocateMainCameraToModel(model, studio, turnAngle);
                    }
                    else if (studio.view.rotationType == RotationType.Model)
                    {
                        if (Model.IsMeshModel(model))
                            Model.AsMeshModel(model).currentAngle = turnAngle;
                        model.Rotate(turnAngle);
                    }
                };

                int intTurnAngle = Mathf.RoundToInt(turnAngle);

                if (groupOpened)
                {
                    bool applied = DrawEachView(string.Format("{0}", intTurnAngle) + "º", studio.view.subViewToggles[i]);
                    if (applied)
                    {
                        studio.appliedSubViewTurnAngle = subViewTurnAngle;
                        callback(SelectedModel);
                    }
                    subViewChanged |= applied;
                }

                if (studio.view.subViewToggles[i].check)
                {
                    string viewName = (studio.view.subViewToggles[i].name.Length > 0) ? studio.view.subViewToggles[i].name : (intTurnAngle + "deg");
                    checkedSubViews.Add(new SubView(intTurnAngle, viewName, callback));
                }
            }

            studio.view.checkedSubViews = checkedSubViews;

            if (groupOpened)
            {
                EditorGUI.indentLevel--;

                if (studio.view.size > 1)
                    DrawViewSelectionButtons(studio.view.subViewToggles);

                if (baseTurnAngleChanged || subViewChanged)
                {
                    if (studio.view.rotationType == RotationType.Camera)
                        CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                    else if (studio.view.rotationType == RotationType.Model)
                        RotateAllModelsToAppliedSubView();
                }

                GUILayout.EndVertical(); // Group Box
            }
        }

        private void DrawTileFields(ref bool slopeAngleChanged)
        {
            EditorGUI.BeginChangeCheck();
            studio.view.isTileVisible = EditorGUILayout.Toggle(new GUIContent("Show Reference Tile",
                "This tile disappears when capturing."), studio.view.isTileVisible);
            bool isTileVisibleChanged = EditorGUI.EndChangeCheck();

            if (studio.view.isTileVisible)
            {
                EditorGUI.indentLevel++;

                studio.view.tileType = (TileType)EditorGUILayout.EnumPopup("Tile Type", studio.view.tileType);

                EditorGUI.BeginChangeCheck();
                studio.view.tileAspectRatio = EditorGUILayout.Vector2Field(new GUIContent("Aspect Ratio",
                    "ratio horizontal length by vertical length of tile"), studio.view.tileAspectRatio);
                bool aspectRatioChanged = EditorGUI.EndChangeCheck();

                if (studio.view.tileAspectRatio.x < 1f)
                    studio.view.tileAspectRatio.x = 1f;
                if (studio.view.tileAspectRatio.y < 1f)
                    studio.view.tileAspectRatio.y = 1f;
                if (studio.view.tileAspectRatio.x < studio.view.tileAspectRatio.y)
                    studio.view.tileAspectRatio.x = studio.view.tileAspectRatio.y;

                EditorGUI.indentLevel--;

                if (isTileVisibleChanged || slopeAngleChanged)
                {
                    studio.view.tileAspectRatio.x = studio.view.tileAspectRatio.y / Mathf.Sin(studio.view.slopeAngle * Mathf.Deg2Rad);
                }
                else if (aspectRatioChanged)
                {
                    studio.view.slopeAngle = Mathf.Asin(studio.view.tileAspectRatio.y / studio.view.tileAspectRatio.x) * Mathf.Rad2Deg;
                    slopeAngleChanged = true;
                }
            }

            if (SelectedModel != null)
            {
                if (studio.view.isTileVisible)
                {
                    if (studio.view.tileObj == null)
                        studio.view.tileObj = ObjectHelper.GetOrCreateObject(EditorGlobal.HELPER_TILES_NAME, EditorGlobal.TILE_FOLDER_NAME, Vector3.zero);

                    if (studio.view.tileObj != null)
                    {
                        Vector3 modelBottom = SelectedModel.ComputedBottom;
                        modelBottom.y -= 0.1f;
                        studio.view.tileObj.transform.position = modelBottom;
                    }

                    TileHelper.UpdateTileToModel(SelectedModel, studio);
                }
                else
                {
                    if (isTileVisibleChanged)
                        ObjectHelper.DeleteObject(EditorGlobal.HELPER_TILES_NAME);
                }
            }
        }

        private void MigrateViews(SubViewToggle[] oldSubViewToggles)
        {
            if (oldSubViewToggles.Length < studio.view.subViewToggles.Length)
            {
                for (int oldIndex = 0; oldIndex < oldSubViewToggles.Length; ++oldIndex)
                {
                    float ratio = (float)oldIndex / oldSubViewToggles.Length;
                    int newIndex = Mathf.FloorToInt(studio.view.subViewToggles.Length * ratio);
                    studio.view.subViewToggles[newIndex].name = oldSubViewToggles[oldIndex].name;

                    if (oldSubViewToggles[oldIndex].check)
                        studio.view.subViewToggles[newIndex].check = true;
                }
            }
            else if (oldSubViewToggles.Length > studio.view.subViewToggles.Length)
            {
                for (int newIndex = 0; newIndex < studio.view.subViewToggles.Length; ++newIndex)
                {
                    float ratio = (float)newIndex / studio.view.subViewToggles.Length;
                    int oldIndex = Mathf.FloorToInt(oldSubViewToggles.Length * ratio);
                    studio.view.subViewToggles[newIndex].name = oldSubViewToggles[oldIndex].name;

                    if (oldSubViewToggles[oldIndex].check)
                        studio.view.subViewToggles[newIndex].check = true;
                }
            }
        }

        private void DrawViewSelectionButtons(SubViewToggle[] subViewToggles)
        {
            EditorGUILayout.BeginHorizontal();
            if (DrawingHelper.DrawNarrowButton("Select all"))
            {
                for (int i = 0; i < subViewToggles.Length; i++)
                    subViewToggles[i].check = true;
            }
            if (DrawingHelper.DrawNarrowButton("Clear all"))
            {
                for (int i = 0; i < subViewToggles.Length; i++)
                    subViewToggles[i].check = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawEachView(string label, SubViewToggle subViewToggle)
        {
            bool applied = false;
            Rect rect = EditorGUILayout.BeginHorizontal();
            {
                subViewToggle.check = EditorGUILayout.Toggle(new GUIContent(label, "bakes model in this view"), subViewToggle.check);

                Rect textFieldRect = new Rect(rect.x + 50, rect.y, rect.width * 0.2f, EditorGlobal.NARROW_BUTTON_HEIGHT);
                if (!subViewToggle.check) GUI.enabled = false;
                {
                    EditorGUI.BeginChangeCheck();
                    subViewToggle.name = EditorGUI.TextField(textFieldRect, subViewToggle.name);
                    if (EditorGUI.EndChangeCheck())
                        PathHelper.CorrectPathString(ref subViewToggle.name);
                }
                if (!subViewToggle.check) GUI.enabled = true;

                if (DrawingHelper.DrawNarrowButton(new GUIContent("Apply", "rotates model or camera to show this view"), 60))
                    applied = true;
            }
            EditorGUILayout.EndHorizontal();

            return applied;
        }

        private void DrawShadowFields()
        {
            if (SelectedModel == null || Camera.main == null)
                return;

            if (!DrawGroupOrPass("Shadow", ref studio.shadow.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            studio.shadow.type = (ShadowType)EditorGUILayout.EnumPopup("Shadow Type", studio.shadow.type);
            bool shadowTypeChanged = EditorGUI.EndChangeCheck();

            if (shadowTypeChanged)
            {
                if (studio.shadow.type != ShadowType.None)
                {
                    studio.shadow.shadowOnly = shadowWithoutModel;
                    if (studio.variation.excludeShadow)
                    {
                        studio.shadow.shadowOnly = false;
                        shadowWithoutModel = false;
                    }
                }
                else
                {
                    shadowWithoutModel = studio.shadow.shadowOnly;
                    studio.shadow.shadowOnly = false;
                }
            }

            if (studio.shadow.type == ShadowType.Simple)
            {
                EditorGUI.indentLevel++;

                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObject(EditorGlobal.DYNAMIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorGlobal.STATIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorGlobal.MATTE_SHADOW_NAME);
                }

                if (studio.shadow.obj == null)
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.SIMPLE_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);

                if (SelectedModel.isSpecificSimpleShadow)
                    DrawSimpleShadowScaleField(ref SelectedModel.simpleShadowScale);
                else
                    DrawSimpleShadowScaleField(ref studio.shadow.simple.scale);

                EditorGUI.indentLevel++;
                SelectedModel.isSpecificSimpleShadow = EditorGUILayout.Toggle(new GUIContent("Model-Specific",
                    "uses model's own simple shadow scale"), SelectedModel.isSpecificSimpleShadow);
                EditorGUI.indentLevel--;

                studio.shadow.simple.isDynamicScale = EditorGUILayout.Toggle(new GUIContent("Dynamic Scale",
                    "auto-scales simple shadow when model is animating"), studio.shadow.simple.isDynamicScale);

                studio.shadow.simple.isSquareScale = EditorGUILayout.Toggle(new GUIContent("Square Scale",
                    "makes both horizontal and vertical scales equal"), studio.shadow.simple.isSquareScale);
                if (studio.shadow.simple.isSquareScale)
                {
                    Vector3 modelSize = SelectedModel.GetDynamicSize();
                    float ratio = modelSize.x / modelSize.z;
                    if (SelectedModel.isSpecificSimpleShadow)
                        SelectedModel.simpleShadowScale.y = SelectedModel.simpleShadowScale.x * ratio;
                    else
                        studio.shadow.simple.scale.y = studio.shadow.simple.scale.x * ratio;
                }

                DrawShadowOpacityField(studio.shadow.obj);

                DrawShadowOnlyField();

                ShadowHelper.ScaleSimpleShadow(SelectedModel, studio);

                EditorGUI.indentLevel--;
            }
            else if (studio.shadow.type == ShadowType.TopDown)
            {
                EditorGUI.indentLevel++;

                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObjectUnder(EditorGlobal.SIMPLE_SHADOW_NAME, SelectedModel.transform);
                    ObjectHelper.DeleteObject(EditorGlobal.STATIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorGlobal.MATTE_SHADOW_NAME);
                }

                if (studio.shadow.obj == null)
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.DYNAMIC_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);

                Camera camera;
                GameObject fieldObj;
                ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

                CameraHelper.LookAtModel(camera.transform, SelectedModel);
                ShadowHelper.ScaleShadowField(camera, fieldObj);

                DrawShadowOpacityField(fieldObj);

                DrawShadowOnlyField();

                EditorGUI.indentLevel--;
            }
            else if (studio.shadow.type == ShadowType.Matte)
            {
                EditorGUI.indentLevel++;

                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObjectUnder(EditorGlobal.SIMPLE_SHADOW_NAME, SelectedModel.transform);
                    ObjectHelper.DeleteObject(EditorGlobal.DYNAMIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorGlobal.STATIC_SHADOW_NAME);
                }

                if (studio.shadow.obj == null)
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorGlobal.MATTE_SHADOW_NAME, EditorGlobal.SHADOW_FOLDER_NAME, Vector3.zero);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);

                if (Model.IsMeshModel(SelectedModel))
                    ShadowHelper.ScaleMatteField(Model.AsMeshModel(SelectedModel), studio.shadow.obj, studio.lit);

                string message = "Rotate the directional light by adjusting 'Slope Angle' and 'Turn Angle'.";
                if (studio.lit.cameraRotationFollow)
                    message = "Rotate the directional light by adjusting 'Slope Angle' and 'Turn Angle' by turning off 'Follow Camera Rotation'.";
                EditorGUILayout.HelpBox(message, MessageType.Info);

                DrawShadowOpacityField(studio.shadow.obj);

                EditorGUILayout.HelpBox("Other shadow details is in Light component.", MessageType.Info);

                EditorGUI.indentLevel--;
            }
            else
            {
                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObjectUnder(EditorGlobal.SIMPLE_SHADOW_NAME, SelectedModel.transform);
                    ObjectHelper.DeleteObject(EditorGlobal.DYNAMIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorGlobal.STATIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorGlobal.MATTE_SHADOW_NAME);
                }

                studio.shadow.shadowOnly = false;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawSimpleShadowScaleField(ref Vector2 scale)
        {
            scale = EditorGUILayout.Vector2Field("Scale", scale);
            if (scale.x < 0.01f)
                scale.x = 0.01f;
            if (scale.y < 0.01f)
                scale.y = 0.01f;
        }

        private void DrawShadowOpacityField(GameObject shadowObj)
        {
            if (shadowObj == null)
                return;

            Renderer renderer = shadowObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.sharedMaterial.color;
                float opacity = EditorGUILayout.Slider("Opacity", color.a, 0, 1);
                color.a = Mathf.Clamp01(opacity);
                renderer.sharedMaterial.color = color;
            }
        }

        private void DrawShadowOnlyField()
        {
            if (studio.variation.on && studio.variation.excludeShadow) GUI.enabled = false;
            studio.shadow.shadowOnly = EditorGUILayout.Toggle(new GUIContent("Shadow Only",
                "captures only shadow excluding model's body"), studio.shadow.shadowOnly);
            if (studio.variation.on && studio.variation.excludeShadow) GUI.enabled = true;
        }

        private void DrawExtractionFields()
        {
            if (!DrawGroupOrPass("Extraction", ref studio.extraction.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            studio.extraction.com = EditorGUILayout.ObjectField(new GUIContent("Extractor",
                "extracts model's color from background"), studio.extraction.com, typeof(Extractor), false) as Extractor;
            if (studio.extraction.com == null)
            {
                GameObject prefab = AssetHelper.FindAsset<GameObject>("Extractor", "DefaultExtractor");
                if (prefab != null)
                    studio.extraction.com = prefab.GetComponent<DefaultExtractor>();
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawVariationFields()
        {
            if (!DrawGroupOrPass("Variation", ref studio.variation.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            studio.variation.on = EditorGUILayout.Toggle(new GUIContent("Apply Variation",
                "vary output image's all pixel colors"), studio.variation.on);
            bool variationOnChanged = EditorGUI.EndChangeCheck();

            if (variationOnChanged)
            {
                if (studio.variation.on)
                {
                    studio.variation.excludeShadow = variationExcludingShadowBackup;
                    if (studio.shadow.shadowOnly)
                    {
                        studio.variation.excludeShadow = false;
                        variationExcludingShadowBackup = false;
                    }
                }
                else
                {
                    variationExcludingShadowBackup = studio.variation.excludeShadow;
                    studio.variation.excludeShadow = false;
                }
            }

            if (studio.variation.on)
            {
                EditorGUI.indentLevel++;

                studio.variation.color = EditorGUILayout.ColorField("Color", studio.variation.color);
                studio.variation.colorBlendFactor = (BlendFactor)EditorGUILayout.EnumPopup(new GUIContent("Color Blend Factor",
                    "represents source factor of OpenGL blend function"), studio.variation.colorBlendFactor);
                studio.variation.imageBlendFactor = (BlendFactor)EditorGUILayout.EnumPopup(new GUIContent("Image Blend Factor",
                    "represents destination factor of OpenGL blend function"), studio.variation.imageBlendFactor);

                if (studio.shadow.type != ShadowType.None)
                {
                    if (studio.shadow.shadowOnly) GUI.enabled = false;
                    {
                        studio.variation.excludeShadow = EditorGUILayout.Toggle(new GUIContent("Exclude Shadow",
                            "does not apply variation to shadow"), studio.variation.excludeShadow);
                    }
                    if (studio.shadow.shadowOnly) GUI.enabled = true;
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawPreviewFields()
        {
            if (!DrawGroupOrPass("Preview", ref studio.preview.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            studio.preview.on = EditorGUILayout.Toggle(new GUIContent("Show Preview",
                "shows preview window at the bottom of this inspector"), studio.preview.on);
            bool anyChanged = EditorGUI.EndChangeCheck();

            if (studio.preview.on)
            {
                EditorGUI.indentLevel++;

                if (studio.output.normalMapMake)
                {
                    EditorGUI.BeginChangeCheck();
                    studio.preview.isNormalMap = EditorGUILayout.Toggle(new GUIContent("Normal Map",
                        "shows normal map preview"), studio.preview.isNormalMap);
                    anyChanged |= EditorGUI.EndChangeCheck();
                }

                studio.preview.backgroundType = (PreviewBackgroundType)EditorGUILayout.EnumPopup("Background Type", studio.preview.backgroundType);
                if (studio.preview.backgroundType == PreviewBackgroundType.SingleColor)
                {
                    EditorGUI.indentLevel++;
                    studio.preview.backgroundColor = EditorGUILayout.ColorField("Color", studio.preview.backgroundColor);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;

                if (anyChanged || DrawingHelper.DrawMiddleButton("Update Preview"))
                    UpdatePreviewTexture();

                EditorGUILayout.HelpBox("Studio gets slower because frequent preview updates occur.", MessageType.Warning);

                if (Model.IsParticleModel(SelectedModel))
                    EditorGUILayout.HelpBox("Particle Model is not supported.", MessageType.Info);
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawFrameFields()
        {
            if (!DrawGroupOrPass("Frame", ref studio.frame.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            Vector2 resoltuion = new Vector2(studio.frame.resolution.width, studio.frame.resolution.height);
            resoltuion = EditorGUILayout.Vector2Field(new GUIContent("Resolution",
                "output image's size before trimming"), resoltuion);
            studio.frame.resolution.width = Mathf.RoundToInt(resoltuion.x);
            studio.frame.resolution.height = Mathf.RoundToInt(resoltuion.y);
            if (studio.frame.resolution.width < 1)
                studio.frame.resolution.width = 1;
            if (studio.frame.resolution.height < 1)
                studio.frame.resolution.height = 1;

            EditorGUI.BeginChangeCheck();
            studio.frame.size = EditorGUILayout.IntField(new GUIContent("Frame Size",
                "the number of capturing for animations"), studio.frame.size);
            if (studio.frame.size < 1)
                studio.frame.size = 1;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Model model in studio.model.list)
                    model.ClearFrames();
                studio.samplings.Clear();
                studio.frame.simulatedIndex = 0;
            }

            if (Model.IsMeshModel(SelectedModel)) // SelectedModel is not null
            {
                if (selectedAnimation != null)
                {
                    EditorGUI.BeginChangeCheck();
                    string label = string.Format("Simulate (0~{0})", studio.frame.size - 1);
                    studio.frame.simulatedIndex = EditorGUILayout.IntSlider(new GUIContent(label,
                        "simulates model with active animation at specific frame"), studio.frame.simulatedIndex, 0, studio.frame.size - 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        float frameRatio = 0.0f;
                        if (studio.frame.simulatedIndex > 0 && studio.frame.simulatedIndex < studio.frame.size)
                            frameRatio = (float)studio.frame.simulatedIndex / (float)(studio.frame.size - 1);

                        MeshModel meshModel = Model.AsMeshModel(SelectedModel);
                        float frameTime = meshModel.GetTimeForRatio(selectedAnimation.clip, frameRatio);
                        meshModel.Animate(selectedAnimation, new Frame(studio.frame.simulatedIndex, frameTime));
                    }
                }
            }

            studio.frame.delay = EditorGUILayout.DoubleField(new GUIContent("Delay",
                "interval time of capturing between frames"), studio.frame.delay);
            if (studio.frame.delay < 0.0)
                studio.frame.delay = 0.0;

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawSamplingFields()
        {
            if (SelectedModel == null)
                return;

            if (!(Model.IsMeshModel(SelectedModel) && selectedAnimation != null) && !Model.IsParticleModel(SelectedModel))
                return;

            if (DrawingHelper.DrawWideButton(new GUIContent("Sample", "captures model with active animation")))
            {
                studio.frame.simulatedIndex = 0;

                HideSelectorAndViewer();

                HideAllModels();
                SelectedModel.gameObject.SetActive(true);
                SetUpCameraBeforeCapturing();
                TileHelper.HideAllTiles();

                if (Model.IsMeshModel(SelectedModel))
                    sampler = new MeshSampler(SelectedModel, selectedAnimation, studio);
                else if (Model.IsParticleModel(SelectedModel))
                    sampler = new ParticleSampler(SelectedModel, studio);

                sampler.SampleFrames(() =>
                {
                    RestoreAllModels();
                    SetUpCameraAfterCapturing();
                    TileHelper.UpdateTileToModel(SelectedModel, studio);

                    if (sampler.IsCancelled)
                        studio.samplings.Clear();
                    else
                        ShowSelectorAndPreviewer();

                    sampler = null;
                });
            }

            List<Frame> frames = null;
            if (Model.IsMeshModel(SelectedModel))
            {
                frames = selectedAnimation.selectedFrames;
            }
            else if (Model.IsParticleModel(SelectedModel))
            {
                ParticleModel particleModel = Model.AsParticleModel(SelectedModel);
                frames = particleModel.selectedFrames;
            }

            string fieldText = frames.Count + " frame(s) selected.";
            if (frames.Count == 0)
                fieldText += " -> All frames will be baked.";

            if (studio.samplings.Count > 0)
            {
                if (DrawingHelper.DrawWideButton(new GUIContent(fieldText, "selected frames only for active animation")))
                    ShowSelectorAndPreviewer();
            }
            else
            {
                GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);
                GUIStyle style = new GUIStyle("label");
                style.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField(fieldText, style);
                GUILayout.EndVertical(); // Group Box
            }
        }

        public void ShowSelectorAndPreviewer()
        {
            if (FrameSelector.instance != null || AnimationPreviewer.instance != null)
                return;

            List<Frame> frames = null;
            if (Model.IsMeshModel(SelectedModel))
            {
                if (selectedAnimation != null)
                    frames = selectedAnimation.selectedFrames;
            }
            else if (Model.IsParticleModel(SelectedModel))
            {
                ParticleModel particleModel = Model.AsParticleModel(SelectedModel);
                frames = particleModel.selectedFrames;
            }

            FrameSelector selector = ScriptableWizard.DisplayWizard<FrameSelector>("Frame Selector");
            if (selector != null)
                selector.SetInfo(frames, studio);

            AnimationPreviewer previewer = ScriptableWizard.DisplayWizard<AnimationPreviewer>("Animation Previewer");
            if (previewer != null)
            {
                if (Model.IsMeshModel(SelectedModel))
                {
                    if (selectedAnimation != null)
                        previewer.SetInfo(frames, studio);
                }
                else if (Model.IsParticleModel(SelectedModel))
                {
                    previewer.SetInfo(frames, studio);
                }
            }
        }

        public void HideSelectorAndViewer()
        {
            if (FrameSelector.instance != null)
                FrameSelector.instance.Close();

            if (AnimationPreviewer.instance != null)
                AnimationPreviewer.instance.Close();
        }

        private void DrawTrimmingFields()
        {
            if (!DrawGroupOrPass("Trimming", ref studio.trimming.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            studio.trimming.on = EditorGUILayout.Toggle(new GUIContent("Trim",
                "cuts output images fit to model's size"), studio.trimming.on);
            if (studio.trimming.on)
            {
                EditorGUI.indentLevel++;

                studio.trimming.margin = EditorGUILayout.IntField(new GUIContent("Margin",
                    "extra pixels around trimmed images"), studio.trimming.margin);
                if (studio.trimming.margin < 0)
                {
                    int absMargin = Mathf.Abs(studio.trimming.margin);
                    if (absMargin > studio.frame.resolution.width / 2)
                        studio.trimming.margin = -(studio.frame.resolution.width / 2);
                    if (absMargin > studio.frame.resolution.height / 2)
                        studio.trimming.margin = -(studio.frame.resolution.height / 2);
                }

                studio.trimming.isUnifiedForAllViews = EditorGUILayout.Toggle(new GUIContent("Unified for All Frames",
                    "cuts output images for an animation to same size"), studio.trimming.isUnifiedForAllViews);

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawPackingFields()
        {
            if (!DrawGroupOrPass("Packing", ref studio.packing.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            studio.packing.on = EditorGUILayout.Toggle(new GUIContent("Pack",
                "makes sprite sheets instead of individual image files"), studio.packing.on);

            if (studio.packing.on)
            {
                EditorGUI.indentLevel++;

                studio.packing.method = (PackingMethod)EditorGUILayout.EnumPopup(new GUIContent("Method",
                    "Optimized makes smallest sprite sheet and InOrder arranges sprites in order."), studio.packing.method);

                EditorGUI.indentLevel++;
                if (studio.packing.method == PackingMethod.Optimized)
                    studio.packing.maxAtlasSizeIndex = EditorGUILayout.Popup("Max Size", studio.packing.maxAtlasSizeIndex, studio.atlasSizes);
                else if (studio.packing.method == PackingMethod.InOrder)
                    studio.packing.minAtlasSizeIndex = EditorGUILayout.Popup("Min Size", studio.packing.minAtlasSizeIndex, studio.atlasSizes);
                EditorGUI.indentLevel--;

                studio.packing.padding = EditorGUILayout.IntField(new GUIContent("Padding",
                    "gap between sprites"), studio.packing.padding);
                if (studio.packing.padding < 0)
                    studio.packing.padding = 0;

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawOutputFields()
        {
            if (!DrawGroupOrPass("Output", ref studio.output.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            studio.output.animationClipMake = EditorGUILayout.Toggle(new GUIContent("Make Animation Clip",
                "generates sprite animation clips per model's animation"), studio.output.animationClipMake);
            if (studio.output.animationClipMake)
            {
                EditorGUI.indentLevel++;
                AnimationPreviewer.DrawFrameRateField(ref studio.output.frameRate);
                AnimationPreviewer.DrawIntervalField(ref studio.output.frameInterval);
                EditorGUI.indentLevel--;
            }

            studio.output.animatorControllerMake = EditorGUILayout.Toggle(new GUIContent("Make Animator Controller",
                "generates animator controllers per model"), studio.output.animatorControllerMake);

            studio.output.prefabMake = EditorGUILayout.Toggle(new GUIContent("Make Prefab",
                "instantiates objects per model"), studio.output.prefabMake);
            if (studio.output.prefabMake)
            {
                EditorGUI.indentLevel++;

                if (studio.output.prefabMake && Model.IsMeshModel(SelectedModel))
                {
                    EditorGUI.indentLevel++;

                    studio.output.isCompactCollider = EditorGUILayout.Toggle(new GUIContent("Compact Collider",
                        "makes the collider tightly fit to model's size if root prefab has a BoxCollider2D"), studio.output.isCompactCollider);

                    studio.output.locationPrefabMake = EditorGUILayout.Toggle(new GUIContent("Make Location Prefab",
                        "instantiates objects per location object in model hierarchy and add to output root prefab"), studio.output.locationPrefabMake);
                    if (studio.output.locationPrefabMake)
                    {
                        EditorGUI.indentLevel++;
                        studio.output.locationSpritePrefab = EditorGUILayout.ObjectField(new GUIContent("Location Sprite Prefab",
                            "output location sprite object to instantiate"), studio.output.locationSpritePrefab, typeof(GameObject), false) as GameObject;
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            studio.output.normalMapMake = EditorGUILayout.Toggle(new GUIContent("Make Normal Map",
                "generates normal maps"), studio.output.normalMapMake);

            if (studio.output.normalMapMake)
            {
                EditorGUI.indentLevel++;
                studio.output.isGrayscaleMap = EditorGUILayout.Toggle(new GUIContent("Grayscale Map",
                "generates grayscale maps"), studio.output.isGrayscaleMap);
                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawPathFields()
        {
            if (!DrawGroupOrPass("Path", ref studio.path.opened))
                return;

            GUILayout.BeginVertical(EditorGlobal.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            studio.path.fileNamePrefix = EditorGUILayout.TextField(new GUIContent("File Name Prefix",
                "concatenates before file's name"), studio.path.fileNamePrefix);
            if (EditorGUI.EndChangeCheck())
                PathHelper.CorrectPathString(ref studio.path.fileNamePrefix);

            studio.path.directoryPath = EditorGUILayout.TextField(new GUIContent("Output Directory",
                "root output folder to save output files"), studio.path.directoryPath);

            if (DrawingHelper.DrawMiddleButton("Choose Directory"))
            {
                studio.path.directoryPath = EditorUtility.SaveFolderPanel("Choose a directory", Application.dataPath, "Output");
                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawBakingFields()
        {
            if (SelectedModel != null && SelectedModel.IsReady())
            {
                if (Model.IsMeshModel(SelectedModel) && selectedAnimation != null && selectedAnimation.clip != null)
                {
                    if (DrawingHelper.DrawWideButton("Bake the selected animation"))
                    {
                        MeshModel meshModel = Model.AsMeshModel(SelectedModel);
                        List<MeshAnimation> animationsBackup = meshModel.animations;
                        List<MeshAnimation> tempAnimations = new List<MeshAnimation>();
                        tempAnimations.Add(selectedAnimation);
                        meshModel.animations = tempAnimations;

                        bakingModels.Clear();
                        bakingModels.Add(SelectedModel);

                        BakeModels(() =>
                        {
                            meshModel.animations = animationsBackup;
                        });
                    }
                }

                if (DrawingHelper.DrawWideButton("Bake the selected model"))
                {
                    bakingModels.Clear();
                    bakingModels.Add(SelectedModel);

                    BakeModels();
                }
            }

            int meshModelCount = 0;
            int particleModelCount = 0;
            foreach (Model model in studio.model.list)
            {
                if (model == null)
                    continue;
                if (Model.IsMeshModel(model))
                    meshModelCount++;
                else if (Model.IsParticleModel(model))
                    particleModelCount++;
            }

            if (meshModelCount > 0 && particleModelCount > 0)
            {
                EditorGUILayout.HelpBox("Can't bake heterogeneous models at once.", MessageType.Info);
            }
            else
            {
                if (DrawingHelper.DrawWideButton("Bake all models"))
                {
                    bakingModels.Clear();
                    foreach (Model model in studio.model.list)
                    {
                        if (model != null && model.IsReady())
                            bakingModels.Add(model);
                    }
                    Debug.Assert(bakingModels.Count > 0);

                    BakeModels();
                }
            }
        }

        private void BakeModels(CompletionCallback completion = null)
        {
            studio.frame.simulatedIndex = 0;
            HideSelectorAndViewer();

            HideAllModels();
            SetUpCameraBeforeCapturing();
            TileHelper.HideAllTiles();

            batcher = new Batcher(bakingModels, studio);
            batcher.Batch(() =>
            {
                batcher = null;

                EditorUtility.SetDirty(studio.gameObject);

                RestoreAllModels();
                SetUpCameraAfterCapturing();

                if (studio.view.rotationType == RotationType.Model)
                    RotateAllModelsToAppliedSubView();

                CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);
                if (Model.IsMeshModel(SelectedModel))
                    ShadowHelper.ScaleMatteField(Model.AsMeshModel(SelectedModel), studio.shadow.obj, studio.lit);

                TileHelper.UpdateTileToModel(SelectedModel, studio);

                if (completion != null)
                    completion();
            });
        }

        private bool DrawGroupOrPass(string name, ref bool opened)
        {
            Rect groupRect = EditorGUILayout.BeginVertical(); EditorGUILayout.EndVertical();

            Texture arrowTex = opened ? ArrowDownTexture : ArrowRightTexture;
            if (arrowTex != null)
            {
                float plusY = opened ? 10 : 5;
                Rect arrowRect = new Rect(groupRect.x - arrowTex.width - 2, groupRect.y + plusY, arrowTex.width, arrowTex.height);
                GUI.DrawTexture(arrowRect, arrowTex);

                if (Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.mousePosition.x >= arrowRect.x && Event.current.mousePosition.x <= arrowRect.x + arrowRect.width &&
                        Event.current.mousePosition.y >= arrowRect.y && Event.current.mousePosition.y <= arrowRect.y + arrowRect.height)
                    {
                        opened = !opened;
                        Event.current.Use();
                    }
                }
            }
            else
            {
                opened = true;
            }

            if (opened)
            {
                return true;
            }
            else
            {
                Rect labelRect = EditorGUILayout.BeginVertical();
                {
                    DrawingHelper.FillRect(labelRect, EditorGUIUtility.isProSkin ? darkGreenColor : lightGreenColor);

                    GUIStyle headerStyle = new GUIStyle();
                    headerStyle.alignment = TextAnchor.MiddleCenter;
                    headerStyle.fontSize = 12;
                    headerStyle.fontStyle = FontStyle.Bold;
                    headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                    EditorGUILayout.LabelField(name, headerStyle);

                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (Event.current.mousePosition.x >= labelRect.x && Event.current.mousePosition.x <= labelRect.x + labelRect.width &&
                            Event.current.mousePosition.y >= labelRect.y && Event.current.mousePosition.y <= labelRect.y + labelRect.height)
                        {
                            opened = true;
                            Event.current.Use();
                        }
                    }
                }
                EditorGUILayout.EndVertical();

                return false;
            }
        }

        private void RotateAllModelsToAppliedSubView()
        {
            foreach (Model model in studio.model.list)
            {
                if (model == null)
                    continue;

                if (Model.IsMeshModel(model))
                    Model.AsMeshModel(model).currentAngle = CurrentTurnAngle;

                model.Rotate(CurrentTurnAngle);
            }
        }


        private void HideAllModels()
        {
            Model[] allModels = Resources.FindObjectsOfTypeAll<Model>();
            modelActivationBackup.Clear();
            foreach (Model model in allModels)
            {
                if (!modelActivationBackup.ContainsKey(model))
                {
                    modelActivationBackup.Add(model, model.gameObject.activeSelf);
                    model.gameObject.SetActive(false);
                }
            }
        }

        private void RestoreAllModels()
        {
            foreach (KeyValuePair<Model, bool> pair in modelActivationBackup)
                pair.Key.gameObject.SetActive(pair.Value);
        }

        private void SetUpCameraBeforeCapturing()
        {
            cameraClearFlagsBackup = Camera.main.clearFlags;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            cameraClearFlagsBackup = Camera.main.clearFlags;
            cameraBackgroundColorBackup = Camera.main.backgroundColor;
            Camera.main.targetTexture = new RenderTexture(studio.frame.resolution.width, studio.frame.resolution.height, 24, RenderTextureFormat.ARGB32);

            Type hdrpCameraType = Extractor.GetHdrpCameraType();
            if (hdrpCameraType != null)
            {
                Component hdrpCameraComponent = Camera.main.gameObject.GetComponent(hdrpCameraType);
                if (hdrpCameraComponent != null)
                {
                    FieldInfo hdrpCameraClearColorModeField = hdrpCameraType.GetField("clearColorMode");
                    if (hdrpCameraClearColorModeField != null)
                    {
                        hdrpCameraClearColorModeBackup = hdrpCameraClearColorModeField.GetValue(hdrpCameraComponent);

                        Type hdrpClearColorModeEnumA = Type.GetType(
                            "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData+ClearColorMode, " +
                            "Unity.RenderPipelines.HighDefinition.Runtime",
                            false, true
                        );
                        Type hdrpClearColorModeEnumB = Type.GetType(
                            "UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData+ClearColorMode, " +
                            "Unity.RenderPipelines.HighDefinition.Runtime",
                            false, true
                        );
                        Type hdrpClearColorModeEnum = hdrpClearColorModeEnumA ?? hdrpClearColorModeEnumB;

                        object hdrpClearColorMode = null;
                        if (hdrpClearColorModeEnum != null)
                        {
                            try
                            {
                                hdrpClearColorMode = Enum.Parse(hdrpClearColorModeEnum, "Color");
                            }
                            catch (Exception) { }

                            if (hdrpClearColorMode == null)
                            {
                                try
                                {
                                    hdrpClearColorMode = Enum.Parse(hdrpClearColorModeEnum, "BackgroundColor");
                                }
                                catch (Exception) { }
                            }
                        }

                        hdrpCameraClearColorModeField.SetValue(hdrpCameraComponent, hdrpClearColorMode);
                    }

                    FieldInfo hdrpCameraBackgroundColorHdrField = hdrpCameraType.GetField("backgroundColorHDR");
                    if (hdrpCameraBackgroundColorHdrField != null)
                        hdrpCameraBackgroundColorHdrBackup = hdrpCameraBackgroundColorHdrField.GetValue(hdrpCameraComponent);
                }
            }
        }

        private void SetUpCameraAfterCapturing()
        {
            Camera.main.clearFlags = cameraClearFlagsBackup;
            Camera.main.backgroundColor = cameraBackgroundColorBackup;
            Camera.main.targetTexture = null;

            Type hdrpCameraType = Extractor.GetHdrpCameraType();
            if (hdrpCameraType != null)
            {
                Component hdrpCameraComponent = Camera.main.gameObject.GetComponent(hdrpCameraType);
                if (hdrpCameraComponent != null)
                {
                    FieldInfo hdrpCameraClearColorModeField = hdrpCameraType.GetField("clearColorMode");
                    if (hdrpCameraClearColorModeField != null)
                        hdrpCameraClearColorModeField.SetValue(hdrpCameraComponent, hdrpCameraClearColorModeBackup);

                    FieldInfo hdrpCameraBackgroundColorHdrField = hdrpCameraType.GetField("backgroundColorHDR");
                    if (hdrpCameraBackgroundColorHdrField != null)
                        hdrpCameraBackgroundColorHdrField.SetValue(hdrpCameraComponent, hdrpCameraBackgroundColorHdrBackup);
                }
            }
        }

        private void UpdatePreviewTexture()
        {
            if (SelectedModel == null)
                return;
            if (sampler != null || batcher != null)
                return;

            HideAllModels();
            SelectedModel.gameObject.SetActive(true);
            SetUpCameraBeforeCapturing();
            TileHelper.HideAllTiles();

            previewTexture = CapturingHelper.CaptureModelManagingShadow(SelectedModel, studio);

            Texture2D normalMapTexture = null;
            if (studio.output.normalMapMake && studio.preview.isNormalMap)
                normalMapTexture = CapturingHelper.CaptureModelForNormalMap(SelectedModel, studio.output.isGrayscaleMap, studio.shadow.obj);

            if (studio.trimming.on)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(SelectedModel.GetPivotPosition());
                IntegerVector pivot2D = new IntegerVector(screenPos);

                IntegerBound texBound = new IntegerBound();
                if (!TextureHelper.CalcTextureBound(previewTexture, pivot2D, texBound))
                {
                    texBound.min.x = pivot2D.x - 1;
                    texBound.max.x = pivot2D.x + 1;
                    texBound.min.y = pivot2D.y - 1;
                    texBound.max.y = pivot2D.y + 1;
                }

                pivot2D.SubtractWithMargin(texBound.min, studio.trimming.margin);

                previewTexture = TextureHelper.TrimTexture(previewTexture, texBound, studio.trimming.margin, EngineGlobal.CLEAR_COLOR32);
                if (normalMapTexture != null)
                {
                    Color32 defaultColor = (studio.output.isGrayscaleMap ? EngineGlobal.CLEAR_COLOR32 : EngineGlobal.NORMALMAP_COLOR32);
                    normalMapTexture = TextureHelper.TrimTexture(normalMapTexture, texBound, studio.trimming.margin, defaultColor, studio.output.normalMapMake);
                }
            }

            if (normalMapTexture != null)
                previewTexture = normalMapTexture;

            RestoreAllModels();
            SetUpCameraAfterCapturing();
            TileHelper.UpdateTileToModel(SelectedModel, studio);
        }

        public override bool HasPreviewGUI()
        {
            if (studio == null)
                return false;
            if (sampler != null || batcher != null)
                return false;
            return (studio.preview.on && Model.IsMeshModel(SelectedModel));
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Preview");
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (rect.width <= 1 || rect.height <= 1)
                return;

            if (previewTexture == null)
                return;

            Rect scaledRect = PreviewHelper.ScalePreviewRect(previewTexture, rect);
            Texture2D scaledTex = TextureHelper.ScaleTexture(previewTexture, (int)scaledRect.width, (int)scaledRect.height);

            if (studio.preview.backgroundType == PreviewBackgroundType.Checker)
            {
                EditorGUI.DrawTextureTransparent(scaledRect, scaledTex);
            }
            else if (studio.preview.backgroundType == PreviewBackgroundType.SingleColor)
            {
                EditorGUI.DrawRect(scaledRect, studio.preview.backgroundColor);
                GUI.DrawTexture(scaledRect, scaledTex);
            }

            EditorGUI.LabelField(rect, string.Format("{0} X {1}", previewTexture.width, previewTexture.height), EditorStyles.whiteLabel);
        }
    }
}
