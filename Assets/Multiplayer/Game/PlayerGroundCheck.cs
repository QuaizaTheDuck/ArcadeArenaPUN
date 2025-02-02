using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
	PlayerController playerController;

	private bool isGrounded = false;

	void Awake()
	{
		playerController = GetComponentInParent<PlayerController>();
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.gameObject == playerController.gameObject)
			return;

		isGrounded = true;
	}

	void OnTriggerExit(Collider other)
	{
		if (other.gameObject == playerController.gameObject)
			return;

		isGrounded = false;
	}

	void OnTriggerStay(Collider other)
	{
		if (other.gameObject == playerController.gameObject)
			return;

		isGrounded = true;
	}

	public bool getGroundCheck()
	{
		return isGrounded;
	}
}