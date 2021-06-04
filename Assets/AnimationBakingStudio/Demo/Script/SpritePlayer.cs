using UnityEngine;

namespace ABS.Demo
{
	public class SpritePlayer : MonoBehaviour
	{
		private Animator animator;

		private string lastTriggerCall = "idle";

		void Start()
		{
			animator = GetComponent<Animator>();
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				animator.SetTrigger("forward");
				lastTriggerCall = "forward";
			}
			else if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				animator.SetTrigger("backward");
				lastTriggerCall = "backward";
			}
			else if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				animator.SetTrigger("left");
				lastTriggerCall = "left";
			}
			else if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				animator.SetTrigger("right");
				lastTriggerCall = "right";
			}
			else if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
			{
				animator.SetTrigger("idle");
				lastTriggerCall = "idle";
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
