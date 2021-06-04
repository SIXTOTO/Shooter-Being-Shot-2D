using UnityEngine;

namespace ABS
{
    public class TileHelper
    {
        public static void UpdateTileToModel(Model model, Studio studio)
        {
            if (model == null || studio.view.tileObj == null)
                return;

            Transform squareTransform = studio.view.tileObj.transform.Find("Square");
            Transform hexagonTransform = studio.view.tileObj.transform.Find("Hexagon");
            if (squareTransform == null || hexagonTransform == null)
                return;

            GameObject squareTileObj = squareTransform.gameObject, hexagonTileObj = hexagonTransform.gameObject;

            if (!model.IsTileAvailable() || !studio.view.isTileVisible)
            {
                squareTileObj.SetActive(false);
                hexagonTileObj.SetActive(false);
                return;
            }

            if (studio.view.tileType == TileType.Square)
            {
                UpdateTile(model, squareTileObj, studio.view.tileType, studio.view.baseTurnAngle);
                hexagonTileObj.SetActive(false);
            }
            else if (studio.view.tileType == TileType.Hexagon)
            {
                UpdateTile(model, hexagonTileObj, studio.view.tileType, studio.view.baseTurnAngle);
                squareTileObj.SetActive(false);
            }
        }

        private static void UpdateTile(Model model, GameObject tileObj, TileType gridType, float baseAngle = 0)
        {
            if (model == null)
                return;

            Renderer tileRenderer = tileObj.GetComponent<Renderer>();
            if (tileRenderer == null)
                return;

            tileObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            float animMaxLength = Mathf.Max(model.GetDynamicSize().x, model.GetDynamicSize().z);
            float tileLength = 0.0f;
            if (gridType == TileType.Square)
                tileLength = (tileRenderer.bounds.size.x / 2.0f) * (1.0f / Mathf.Sqrt(2.0f)) * 2.0f;
            else if (gridType == TileType.Hexagon)
                tileLength = (tileRenderer.bounds.size.x / 2.0f) * (Mathf.Sqrt(3.0f) / 2.0f) * 1.5f;

            float diffRatio = animMaxLength / tileLength;
            tileObj.transform.localScale = new Vector3
            (
                tileObj.transform.localScale.x * diffRatio,
                1.0f,
                tileObj.transform.localScale.z * diffRatio
            );

            tileObj.transform.rotation = Quaternion.identity;
            tileObj.transform.RotateAround(tileObj.transform.position, Vector3.up, baseAngle);

            tileObj.SetActive(true);
        }

        public static void HideAllTiles()
        {
            GameObject tilesObj = GameObject.Find(EditorGlobal.HELPER_TILES_NAME);
            if (tilesObj == null)
                return;

            Transform squareTransform = tilesObj.transform.Find("Square");
            Transform hexagonTransform = tilesObj.transform.Find("Hexagon");
            if (squareTransform == null || hexagonTransform == null)
                return;

            squareTransform.gameObject.SetActive(false);
            hexagonTransform.gameObject.SetActive(false);
        }
    }
}
