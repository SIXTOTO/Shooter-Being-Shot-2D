using UnityEngine;

namespace ABS.Demo
{
    public class SpriteHumanoidBuilder : PrefabBuilder
    {
#if UNITY_EDITOR
        public override void BindSpriteAndController(GameObject rootObject, Sprite firstSprite, UnityEditor.Animations.AnimatorController controller)
        {
            SpriteRenderer renderer = rootObject.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite == null)
                renderer.sprite = firstSprite;

            Animator animator = rootObject.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = controller;
        }
#endif

        public override void BindSprite(GameObject rootObject, Sprite firstSprite)
        {
            SpriteRenderer renderer = rootObject.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite == null)
                renderer.sprite = firstSprite;
        }

        public override BoxCollider2D GetBoxCollider2D(GameObject rootObject)
        {
            return rootObject.GetComponent<BoxCollider2D>();
        }
    }
}