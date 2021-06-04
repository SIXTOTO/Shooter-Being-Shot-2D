using UnityEngine;

namespace ABS.Demo
{
	public class SpriteParticleBuilder : PrefabBuilder
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
    }
}
