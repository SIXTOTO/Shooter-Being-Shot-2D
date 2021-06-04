using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public enum RotationType : int
    {
        Camera = 0,
        Model = 1
    }

    public enum TileType
    {
        Square,
        Hexagon
    }

    [Serializable]
    public class SubViewToggle
    {
        public bool check;
        public string name;

        public SubViewToggle(bool toggle = true)
        {
            this.check = toggle;
            name = "";
        }
    }

    public delegate void RotationCallback(Model model);

    [Serializable]
    public class SubView
    {
        public int angle;
        public string name;
        public RotationCallback func;

        public SubView(int angle, string name, RotationCallback func)
        {
            this.angle = angle;
            this.name = name;
            this.func = func;
        }
    }

    [Serializable]
    public class ViewProperty : PropertyBase
    {
        public RotationType rotationType = RotationType.Camera;

        public float slopeAngle = 30;

        public bool isTileVisible = false;
        public TileType tileType = TileType.Square;
        public Vector2 tileAspectRatio = new Vector2(2.0f, 1.0f);
        public GameObject tileObj = null;

        public const int VIEW_INITIAL_SIZE = 4;
        public int size = VIEW_INITIAL_SIZE;
        public SubViewToggle[] subViewToggles = new SubViewToggle[VIEW_INITIAL_SIZE];
        public float baseTurnAngle = 0;
        public float unitTurnAngle = 0;

        public List<SubView> checkedSubViews = new List<SubView>();

        public ViewProperty()
        {
            for (int i = 0; i < subViewToggles.Length; ++i)
                subViewToggles[i] = new SubViewToggle();
        }
    }
}
