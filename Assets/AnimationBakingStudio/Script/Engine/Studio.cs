using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    [DisallowMultipleComponent]
    public class Studio : MonoBehaviour
    {
        public ModelProperty model = new ModelProperty();
        public CameraProperty cam = new CameraProperty();
        public LightProperty lit = new LightProperty();
        public ViewProperty view = new ViewProperty();
        public ShadowProperty shadow = new ShadowProperty();
        public ExtractionProperty extraction = new ExtractionProperty();
        public VariationProperty variation = new VariationProperty();
        public PreviewProperty preview = new PreviewProperty();
        public FrameProperty frame = new FrameProperty();
        public TrimmingProperty trimming = new TrimmingProperty();
        public PackingProperty packing = new PackingProperty();
        public OutputProperty output = new OutputProperty();
        public PathProperty path = new PathProperty();

        public float appliedSubViewTurnAngle = 0;

        public int animPopupIndex = 0;

        [NonSerialized]
        public string[] atlasSizes = new string[] { "128", "256", "512", "1024", "2048", "4096", "8192" };

        [NonSerialized]
        public List<Sampling> samplings = new List<Sampling>();

        [NonSerialized]
        public bool isSamplingReady = false;

        [NonSerialized]
        public bool isBakingReady = false;

        public bool IsTopView()
        {
            return (view.slopeAngle == 90f);
        }

        public bool IsSideView()
        {
            return (view.slopeAngle == 0f);
        }

        public void AddModel(Model model)
        {
            bool assigned = false;
            for (int i = 0; i < this.model.list.Count; ++i)
            {
                if (this.model.list[i] == null)
                {
                    this.model.list[i] = model;
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
                this.model.list.Add(model);
        }
    }
}
