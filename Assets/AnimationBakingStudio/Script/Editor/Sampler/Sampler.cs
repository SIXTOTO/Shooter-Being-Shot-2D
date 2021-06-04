using System;
using UnityEngine;
using UnityEditor;

namespace ABS
{
    public abstract class Sampler
    {
        private readonly Model model = null;
        protected readonly Studio studio = null;

        public CompletionCallback completion;

        public enum SamplingState
        {
            Initialize = 0,
            BeginFrame,
            CaptureFrame,
            EndFrame,
            Finalize
        }
        private StateMachine<SamplingState> stateMachine;

        private int frameIndex = 0;
        private float frameTime = 0.0f;

        private const int MIN_LENGTH = 200;

        private IntegerVector pivot2D;
        private IntegerBound unifiedTexBound;

        private double prevTime = 0.0;

        public bool IsCancelled { get; set; }

        public Sampler(Model model, Studio studio)
        {
            this.model = model;
            this.studio = studio;
        }

        public bool IsInProgress()
        {
            return (stateMachine != null);
        }

        public void SampleFrames(CompletionCallback completion)
        {
            this.completion = completion;

            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;

            stateMachine = new StateMachine<SamplingState>();
            stateMachine.AddState(SamplingState.Initialize, OnInitialize);
            stateMachine.AddState(SamplingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(SamplingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(SamplingState.EndFrame, OnEndFrame);
            stateMachine.AddState(SamplingState.Finalize, OnFinalize);

            stateMachine.ChangeState(SamplingState.Initialize);
        }

        public void UpdateState()
		{
            if (stateMachine != null)
                stateMachine.Update();
        }

        protected abstract void ClearAllFrames();

        protected abstract float GetTimeForRatio(float ratio);

        protected abstract void AnimateModel(Frame frame);

        protected abstract void AddFrame(Frame frame);

        protected virtual void OnInitialize_() { }
        protected virtual void OnCaptureFrame_() { }
        protected virtual void Finish_() { }

        public void OnInitialize()
        {
            try
            {
                OnInitialize_();

                AnimateModel(Frame.BEGIN);

                frameIndex = 0;
                frameTime = 0.0f;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                pivot2D = new IntegerVector(screenPos);

                unifiedTexBound = new IntegerBound();

                studio.samplings.Clear();
                ClearAllFrames();

                stateMachine.ChangeState(SamplingState.BeginFrame);
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
                int shownCurrFrameNumber = frameIndex + 1;
                float progress = (float)shownCurrFrameNumber / studio.frame.size;

                IsCancelled = EditorUtility.DisplayCancelableProgressBar("Sampling...", "Frame: " + shownCurrFrameNumber + " (" + ((int)(progress * 100f)) + "%)", progress);
                if (IsCancelled)
                    throw new Exception("Cancelled");

                float frameRatio = 0.0f;
                if (frameIndex > 0 && frameIndex < studio.frame.size)
                    frameRatio = (float)frameIndex / (float)(studio.frame.size - 1);

                frameTime = GetTimeForRatio(frameRatio);

                AnimateModel(new Frame(frameIndex, frameTime));

                stateMachine.ChangeState(SamplingState.CaptureFrame);
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

                OnCaptureFrame_();

                Texture2D tex = CapturingHelper.CaptureModelManagingShadow(model, studio);

                studio.samplings.Add(new Sampling(tex, frameTime));

                IntegerBound texBound = new IntegerBound();
                if (!TextureHelper.CalcTextureBound(tex, pivot2D, texBound))
                {
                    texBound.min.x = pivot2D.x - 1;
                    texBound.max.x = pivot2D.x + 1;
                    texBound.min.y = pivot2D.y - 1;
                    texBound.max.y = pivot2D.y + 1;
                }

                TextureHelper.MakeUnifiedBound(pivot2D, texBound, unifiedTexBound);

                stateMachine.ChangeState(SamplingState.EndFrame);
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

                if (frameIndex < studio.frame.size)
                    stateMachine.ChangeState(SamplingState.BeginFrame);
                else
                    stateMachine.ChangeState(SamplingState.Finalize);
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

                TrimAll();

                for (int i = 0; i < studio.samplings.Count; i++)
                    AddFrame(new Frame(i, studio.samplings[i].time));
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

        private void TrimAll()
        {
            try
            {
                foreach (Sampling sample in studio.samplings)
                {
                    Texture2D trimTex = TextureHelper.TrimTexture(sample.tex, unifiedTexBound, 5, EngineGlobal.CLEAR_COLOR32);

                    if (trimTex.width >= trimTex.height && trimTex.width > MIN_LENGTH)
                    {
                        float ratio = (float)trimTex.height / (float)trimTex.width;
                        int newTrimWidth = MIN_LENGTH;
                        int newTrimHeight = Mathf.RoundToInt(newTrimWidth * ratio);
                        sample.tex = TextureHelper.ScaleTexture(trimTex, newTrimWidth, newTrimHeight);
                    }
                    else if (trimTex.width < trimTex.height && trimTex.height > MIN_LENGTH)
                    {
                        float ratio = (float)trimTex.width / (float)trimTex.height;
                        int newTrimHeight = MIN_LENGTH;
                        int newTrimWidth = Mathf.RoundToInt(newTrimHeight * ratio);
                        sample.tex = TextureHelper.ScaleTexture(trimTex, newTrimWidth, newTrimHeight);
                    }
                    else
                    {
                        sample.tex = trimTex;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Finish()
        {
            stateMachine = null;

            EditorApplication.update -= UpdateState;

            EditorUtility.ClearProgressBar();

            Finish_();

            AnimateModel(Frame.BEGIN);

            completion();
        }
    }
}
