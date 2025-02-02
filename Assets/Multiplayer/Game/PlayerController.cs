using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks //, IDamageable
{

	[SerializeField] GameObject cameraHolder;

	[SerializeField] float mouseSensitivity, smoothTime;

	float verticalLookRotation;
	bool grounded;
	Rigidbody rb;

	PhotonView PV;
	PlayerManager playerManager;

	// Nasze zmienne
	// DEBUG
	[SerializeField] TextMeshProUGUI speedCounter;

	[Header("Max Movement Speeds")]
	[SerializeField] private float currentMaxSpeed;
	[SerializeField] float maxYSpeed;
	[SerializeField] float maxGroundSpeed;
	[SerializeField] float maxAirSpeed;
	[SerializeField] float maxDashSpeed;
	[SerializeField] float maxDashYSpeed;
	[SerializeField] float maxSwingSpeed;
	[Header("Movement Stats")]
	[SerializeField] float jumpForce;

	[Header("Unasinged")]
	[SerializeField] Transform playerCamera;
	[SerializeField] Transform horizontalDirection;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		PV = GetComponent<PhotonView>();
	}

	void Start()
	{
		if (PV.IsMine)
		{
		}
		else
		{
			Destroy(GetComponentInChildren<Camera>().gameObject);
			Destroy(rb);
		}
	}

	void FixedUpdate()
	{
		if (!PV.IsMine)
			return;
		Move();
		Look();
		Jump();
		LimitSpeed();

		// Debug #TODO - DELETE ME IN PRODUCTION
		speedCounter.text = $"{rb.velocity.magnitude.ToString("F1")} / {currentMaxSpeed.ToString("F1")}";
	}
	void Look()
	{
		transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

		verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

		cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
	}

	void Move()
	{
		Vector3 moveDir = (horizontalDirection.right * Input.GetAxisRaw("Horizontal") + horizontalDirection.forward * Input.GetAxisRaw("Vertical")).normalized;
		rb.AddForce(10f * currentMaxSpeed * moveDir, ForceMode.Force);
	}
	public void LimitSpeed()
	{
		Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

		// limit velocity if needed  
		if (flatVel.magnitude > currentMaxSpeed)
		{
			Vector3 limitedVel = flatVel.normalized * currentMaxSpeed;
			rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
		}


		// limit y vel
		if (maxYSpeed != 0 && rb.velocity.y > maxYSpeed)
			rb.velocity = new Vector3(rb.velocity.x, maxYSpeed, rb.velocity.z);
	}

	void Jump()
	{
		if (Input.GetKeyDown(KeyCode.Space) && grounded)
		{
			//reset y velocity
			rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
			rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
		}
	}

	public void SetGroundedState(bool _grounded)
	{
		grounded = _grounded;
	}




}