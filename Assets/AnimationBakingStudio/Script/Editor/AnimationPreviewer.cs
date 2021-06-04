using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABS
{
    public class AnimationPreviewer : ScriptableWizard
    {
        public static AnimationPreviewer instance;

        private List<Frame> frames = null;
        private Studio studio = null;

        private bool playing = false;
        private bool looping = true;
        private int indexOfselectedFrames = 0;
        private int frameNumber = 0;
        private float nextFrameTime = 0f;
        private float nextSpriteTime = 0f;

        const float MIN_EDITOR_WIDTH = 220f;

        public void OnEnable()
        {
            instance = this;

            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;

            Reset();
            playing = true;
        }

        public void OnDisable()
        {
            instance = null;

            EditorApplication.update -= UpdateState;

            if (FrameSelector.instance != null)
                FrameSelector.instance.Close();
        }

        public void SetInfo(List<Frame> frames, Studio studio)
        {
            this.frames = frames;
            this.studio = studio;
        }

        void OnGUI()
        {
            try
            {
                if (frames == null || studio == null)
                    return;

                if (studio.samplings.Count == 0 || frames.Count == 0)
                    return;

                const float BUTTOM_HEIGHT = 18f;
                const float MENU_HEIGHT = 60f;
                const float MARGIN = 2f;

                int spriteWidth = studio.samplings[0].tex.width;
                int spriteHeight = studio.samplings[0].tex.height;

                const float FRAME_LABEL_WIDTH = 30f;

                float editorWidth = Mathf.Max(spriteWidth + (MARGIN + FRAME_LABEL_WIDTH) * 2f, MIN_EDITOR_WIDTH);

                position = new Rect(position.x, position.y, editorWidth, MENU_HEIGHT + (float)spriteHeight + MARGIN);

                EditorGUI.BeginChangeCheck();
                {
                    DrawFrameRateField(ref studio.output.frameRate);
                    DrawIntervalField(ref studio.output.frameInterval);
                }
                if (EditorGUI.EndChangeCheck())
                    Reset();

                const float BUTTONS_Y = MENU_HEIGHT - 20f;

                Rect playRect = new Rect(MARGIN, BUTTONS_Y, editorWidth / 2f - MARGIN, BUTTOM_HEIGHT);
                EditorGUI.BeginChangeCheck();
                playing = GUI.Toggle(playRect, playing, "Play", GUI.skin.button);
                if (EditorGUI.EndChangeCheck())
                {
                    if (playing)
                    {
                        Reset();
                        playing = true;
                    }
                    else
                    {
                        playing = false;
                    }
                }

                Rect loopRect = new Rect(MARGIN + editorWidth / 2f, BUTTONS_Y, editorWidth / 2f - MARGIN, BUTTOM_HEIGHT);
                looping = GUI.Toggle(loopRect, looping, "Loop", GUI.skin.button);

                float spritePosX = MARGIN + FRAME_LABEL_WIDTH;
                if (spriteWidth < editorWidth)
                    spritePosX = (editorWidth - spriteWidth) / 2f;
                float spritePosY = MENU_HEIGHT;

                Rect spriteRect = new Rect(spritePosX, spritePosY, spriteWidth, spriteHeight);
                EditorGUI.DrawTextureTransparent(spriteRect, studio.samplings[frameNumber].tex);
                DrawingHelper.StrokeRect(spriteRect, Color.black, 1f);

                Rect frameLabelRect = new Rect(1.0f, spritePosY, FRAME_LABEL_WIDTH, 15f);
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                EditorGUI.LabelField(frameLabelRect, frameNumber.ToString(), labelStyle);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.update -= UpdateState;
                Close();
            }
        }

        static public void DrawFrameRateField(ref int frameRate)
        {
            frameRate = EditorGUILayout.IntField(new GUIContent("Frame Rate",
                "how many model updates per second in output animation clip"), frameRate, GUILayout.Width(MIN_EDITOR_WIDTH - 10f));
            if (frameRate <= 0)
                frameRate = 1;
        }

        static public void DrawIntervalField(ref int frameInterval)
        {
            frameInterval = EditorGUILayout.IntField(new GUIContent("Interval",
                "distance between two frames in output animation clip"), frameInterval);
            if (frameInterval <= 0)
                frameInterval = 1;
        }

        public void UpdateState()
        {
            try
            {
                if (frames.Count == 0)
                    return;

                if (!playing)
                    return;

                if (Time.realtimeSinceStartup > nextFrameTime)
                {
                    float unitTime = 1f / studio.output.frameRate;
                    nextFrameTime += unitTime;

                    if (Time.realtimeSinceStartup > nextSpriteTime)
                    {
                        indexOfselectedFrames = (indexOfselectedFrames + 1) % frames.Count;
                        frameNumber = frames[indexOfselectedFrames].index;
                        Repaint();

                        if (indexOfselectedFrames >= frames.Count - 1)
                        {
                            if (!looping)
                            {
                                playing = false;
                                return;
                            }
                        }

                        nextSpriteTime += unitTime * studio.output.frameInterval;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.update -= UpdateState;
                Close();
            }
        }

        private void Reset()
        {
            indexOfselectedFrames = 0;
            frameNumber = 0;
            nextFrameTime = Time.realtimeSinceStartup;
            nextSpriteTime = Time.realtimeSinceStartup;
        }
    }
}
