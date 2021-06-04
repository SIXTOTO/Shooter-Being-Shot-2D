using UnityEngine;

namespace ABS.Demo
{
	public class SpriteHumanoid : MonoBehaviour
	{
		private Animator animator;

		private string lastTriggerCall = "walk";

		void Start()
		{
			animator = GetComponent<Animator>();
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				animator.SetTrigger("run");
				lastTriggerCall = "run";
			}
			else if (Input.GetKeyDown(KeyCode.S))
			{
				animator.SetTrigger("shoot");
				lastTriggerCall = "shoot";
			}
			else if (Input.GetKeyDown(KeyCode.W))
			{
				animator.SetTrigger("walk");
				lastTriggerCall = "walk";
			}
			else if (Input.GetKeyDown(KeyCode.D))
			{
				animator.SetTrigger("die");
				lastTriggerCall = "die";
			}
			else if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				animator.SetInteger("angle", 0);
				animator.SetTrigger(lastTriggerCall);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				animator.SetInteger("angle", 180);
				animator.SetTrigger(lastTriggerCall);
			}
		}
	}
}
