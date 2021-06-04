using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class Batcher
	{
		private readonly List<Model> modelList;
		private readonly Studio studio;

		private CompletionCallback completion;

		public int ModelIndex { get; set; }

		public Baker CurrentBaker { get; set; }

		private string batchingFolderPath = "";

		private bool needPassingFinalUpdate = true;

		public Batcher(List<Model> modelList, Studio studio)
		{
			this.modelList = modelList;
			this.studio = studio;
		}

		public bool IsInProgress()
		{
			return (CurrentBaker != null);
		}

		public void Batch(CompletionCallback completion)
        {
			this.completion = completion;

			Debug.Assert(modelList.Count > 0);

			if (modelList.Count > 1)
            {
                int assetRootIndex = studio.path.directoryPath.IndexOf("Assets");
				string dirPath = studio.path.directoryPath.Substring(assetRootIndex);
                batchingFolderPath = Path.Combine(dirPath, PathHelper.MakeDateTimeString());
                Directory.CreateDirectory(batchingFolderPath);
            }

			ModelIndex = 0;

			EditorApplication.update -= UpdateState;
			EditorApplication.update += UpdateState;

			EditorUtility.DisplayProgressBar("Progress...", "Ready...", 0.0f);
		}

		private Model NextModel()
        {
			for (int i = ModelIndex; i < modelList.Count; ++i)
            {
				if (modelList[i] != null)
				{
					ModelIndex = i;
					return modelList[i];
                }
            }
			return null;
        }

		public void UpdateState()
		{
			try
			{
				if (CurrentBaker != null)
                {
                    if (CurrentBaker.IsInProgress())
					{
						CurrentBaker.UpdateState();

						if (CurrentBaker.IsCancelled)
							throw new Exception("Cancelled");
						return;
					}
					else
					{
						EditorUtility.ClearProgressBar();

						ModelIndex++;
					}
				}

				Model model = NextModel();
				if (model != null)
                {
					string sIndex = "";
					if (modelList.Count > 1)
						sIndex = ModelIndex.ToString().PadLeft((modelList.Count + 1).ToString().Length, '0');

					for (int i = 0; i < modelList.Count; ++i)
						modelList[i].gameObject.SetActive(false);
					model.gameObject.SetActive(true);

					if (Model.IsMeshModel(model))
                    {
						MeshModel meshModel = Model.AsMeshModel(model);
						List<MeshAnimation> validAnimations = meshModel.GetValidAnimations();

						if (validAnimations.Count > 0)
							CurrentBaker = new MeshBaker(model, validAnimations, studio, sIndex, batchingFolderPath);
						else
							CurrentBaker = new StaticBaker(model, studio, sIndex, batchingFolderPath);
					}
					else if (Model.IsParticleModel(model))
                    {
						CurrentBaker = new ParticleBaker(model, studio, sIndex, batchingFolderPath);
					}
				}
				else
				{
					EditorUtility.FocusProjectWindow();

					if (needPassingFinalUpdate)
					{
						needPassingFinalUpdate = false;
						return;
					}

					Finish();
				}
			}
			catch (Exception e)
            {
				Debug.LogException(e);
				Finish();
			}
		}

		public void Finish()
        {
			EditorApplication.update -= UpdateState;

			EditorUtility.ClearProgressBar();

			AssetDatabase.Refresh();

			CurrentBaker = null;

			completion();
		}
	}
}
