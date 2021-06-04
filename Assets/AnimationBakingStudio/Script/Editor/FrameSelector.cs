using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABS
{
    public class FrameSelector : ScriptableWizard
    {
        public static FrameSelector instance;

        private List<Frame> frames = null;
        private Studio studio = null;

        Vector2 whatPos = Vector2.zero;
        private const float LABEL_HEIGHT = 20.0f;

        void OnEnable()
        {
			minSize = new Vector2(400f, 300f);
            instance = this;
        }

        void OnDisable()
        {
            instance = null;

            if (AnimationPreviewer.instance != null)
                AnimationPreviewer.instance.Close();
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

                if (studio.samplings.Count == 0)
                    return;

                int texWidth = studio.samplings[0].tex.width;
                int texHeight = studio.samplings[0].tex.height;

                float padding = 10.0f;
                int colSize = Mathf.FloorToInt(Screen.width / (texWidth + padding));
                if (colSize < 1)
                    colSize = 1;

                if (studio.samplings.Count > 1)
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Select all"))
                    {
                        frames.Clear();
                        for (int i = 0; i < studio.samplings.Count; ++i)
                            frames.Add(new Frame(i, studio.samplings[i].time));
                    }

                    if (GUILayout.Button("Select each half"))
                    {
                        int modular = 0;
                        if (frames.Count >= 2)
                        {
                            if (frames[0].index == 0)
                                modular = 1;
                            else if (frames[0].index == 1)
                                modular = 0;
                        }

                        frames.Clear();
                        for (int i = 0; i < studio.samplings.Count; ++i)
                        {
                            if (i % 2 == modular)
                                frames.Add(new Frame(i, studio.samplings[i].time));
                        }
                    }

                    if (GUILayout.Button("Clear all"))
                        frames.Clear();

                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(padding);

                whatPos = GUILayout.BeginScrollView(whatPos);
                {
                    Rect rect = new Rect(padding, 0, texWidth, texHeight);

                    int col = 0;
                    int rowCount = 0;
                    for (int smpi = 0; smpi < studio.samplings.Count; ++smpi)
                    {
                        Sampling sampling = studio.samplings[smpi];

                        if (col >= colSize)
                        {
                            col = 0;
                            rowCount++;

                            rect.x = padding;
                            rect.y += texHeight + padding + LABEL_HEIGHT;

                            GUILayout.EndHorizontal();
                            GUILayout.Space(texHeight + padding);
                        }

                        if (col == 0)
                            GUILayout.BeginHorizontal();

                        if (GUI.Button(rect, ""))
                        {
                            if (frames.Count == 0)
                            {
                                frames.Add(new Frame(smpi, sampling.time));
                            }
                            else
                            {
                                bool exist = false;
                                foreach (Frame selectedFrame in frames)
                                {
                                    if (smpi == selectedFrame.index)
                                    {
                                        exist = true;
                                        break;
                                    }
                                }

                                int inserti = 0;
                                for (; inserti < frames.Count; ++inserti)
                                {
                                    if (smpi < frames[inserti].index)
                                        break;
                                }

                                if (exist)
                                    frames.Remove(new Frame(smpi, 0));
                                else
                                    frames.Insert(inserti, new Frame(smpi, sampling.time));
                            }
                        }

                        EditorGUI.DrawTextureTransparent(rect, sampling.tex);

                        foreach (Frame selectedFrame in frames)
                        {
                            if (selectedFrame.index == smpi)
                            {
                                DrawingHelper.StrokeRect(rect, Color.red, 2.0f);
                                break;
                            }
                        }

                        GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                        GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
                        GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, LABEL_HEIGHT),
                            smpi.ToString(), "ProgressBarBack");
                        GUI.contentColor = Color.white;
                        GUI.backgroundColor = Color.white;

                        col++;
                        rect.x += texWidth + padding;
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.Space(texHeight + padding);

                    GUILayout.Space(rowCount * 26);
                }
                GUILayout.EndScrollView();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Close();
            }
        }
    }
}
