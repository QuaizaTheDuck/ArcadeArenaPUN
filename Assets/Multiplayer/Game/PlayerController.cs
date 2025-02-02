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
	[SerializeField] private GameObject cameraHolder;

	[SerializeField] private float mouseSensitivity, smoothTime;

	private float verticalLookRotation;

	private Rigidbody rb;

	private PhotonView PV;
	private PlayerManager playerManager;

	// Nasze zmienne
	// DEBUG
	[SerializeField] private TextMeshProUGUI speedCounter;

	[Header("State")]
	private PlayerGroundCheck groundCheck;
	[SerializeField] private MovementState state = MovementState.isAirborn;
	[SerializeField] private MovementState lastState;
	private enum MovementState
	{
		isDashing,
		isSwinging,
		isGrounded,
		isAirborn
	}

	[Header("Max Movement Speeds")]
	[SerializeField] private float currentMaxSpeed;
	[SerializeField] private float maxYSpeed;
	[SerializeField] private float maxGroundSpeed;
	[SerializeField] private float maxAirSpeed;
	[SerializeField] private float maxDashSpeed;
	[SerializeField] private float maxDashYSpeed;
	[SerializeField] private float maxSwingSpeed;

	[Header("Movement Stats")]
	[SerializeField] private float jumpForce;
	[SerializeField] private float speedLerpMultiplier;

	[Header("Dash Stats")]
	public float dashForce;
	public float dashUpwardForce;
	public float dashDuration;
	public float dashCooldown;
	public float dashCdTimer;

	[Header("Camera")]
	[SerializeField] private int currentFov = 85;
	[SerializeField] private int defaultFov = 85;
	[SerializeField] private int dashingFov = 85;
	[SerializeField] private int swingingFov = 85;

	[Header("Unasigned")]
	[SerializeField] private Transform playerCamera;
	[SerializeField] private Transform horizontalDirection;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		PV = GetComponent<PhotonView>();
		groundCheck = GetComponentInChildren<PlayerGroundCheck>();
	}

	private void Start()
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

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) && state == MovementState.isGrounded)
		{
			Jump();
		}

		if (dashCdTimer > 0)
			dashCdTimer -= Time.deltaTime;
		else if (Input.GetKeyDown(KeyCode.LeftShift))
			Dash();
	}

	private void FixedUpdate()
	{
		if (!PV.IsMine)
			return;

		StateHandler();

		if (!(state == MovementState.isSwinging || state == MovementState.isDashing))
		{
			if (groundCheck.getGroundCheck())
				state = MovementState.isGrounded;
			else
				state = MovementState.isAirborn;
		}

		if (state == MovementState.isGrounded)
			rb.drag = 5f;
		else
			rb.drag = 0f;

		Look();
		Move();
		LimitSpeed();

		// Debug #TODO - DELETE ME IN PRODUCTION
		speedCounter.text = $"{rb.velocity.magnitude.ToString("F1")} / {currentMaxSpeed.ToString("F1")}";
	}

	private void LateUpdate()
	{
		Look();
	}

	#region State Handling
	private float desiredCurrentMaxSpeed;
	private float lastDesiredCurrentMaxSpeed;
	[SerializeField] private bool keepMomentum;

	private void StateHandler()
	{
		switch (state)
		{
			case MovementState.isGrounded:
				desiredCurrentMaxSpeed = maxGroundSpeed;
				if (currentFov != defaultFov)
					setFov(defaultFov);
				break;

			case MovementState.isAirborn:
				desiredCurrentMaxSpeed = maxAirSpeed;
				if (currentFov != defaultFov)
					setFov(defaultFov);
				break;

			case MovementState.isDashing:
				desiredCurrentMaxSpeed = maxDashSpeed;
				if (currentFov != dashingFov)
					setFov(dashingFov);
				break;

			case MovementState.isSwinging:
				desiredCurrentMaxSpeed = maxSwingSpeed;
				if (currentFov != swingingFov)
					setFov(swingingFov);
				break;

			default:
				break;
		}

		bool desiredMoveSpeedHasChanged = desiredCurrentMaxSpeed != lastDesiredCurrentMaxSpeed;
		// LerpMaxSpeed after x state
		if (lastState == MovementState.isDashing) keepMomentum = true;

		if (desiredMoveSpeedHasChanged)
		{
			if (keepMomentum)
			{
				StopAllCoroutines();
				StartCoroutine(SmoothlyLerpMoveSpeed());
			}
			else
			{
				StopAllCoroutines();
				currentMaxSpeed = desiredCurrentMaxSpeed;
			}
		}

		lastDesiredCurrentMaxSpeed = desiredCurrentMaxSpeed;
		lastState = state;

		if (Mathf.Abs(desiredCurrentMaxSpeed - currentMaxSpeed) < 0.1f) keepMomentum = false;
	}

	private IEnumerator SmoothlyLerpMoveSpeed()
	{
		float time = 0;
		float difference = Mathf.Abs(desiredCurrentMaxSpeed - currentMaxSpeed);
		float startValue = currentMaxSpeed;

		while (time < difference)
		{
			currentMaxSpeed = Mathf.Lerp(startValue, desiredCurrentMaxSpeed, time / difference);

			time += Time.deltaTime * speedLerpMultiplier;

			yield return null;
		}

		currentMaxSpeed = desiredCurrentMaxSpeed;
	}

	public void setFov(int fov)
	{
		currentFov = fov;
		// playerLook.DoFov(currentFov);
	}
	#endregion State Handling

	#region Basic Movement
	private void Look()
	{
		transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

		verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

		cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
	}

	private void Move()
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

	private void Jump()
	{
		//reset y velocity
		rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
	}
	#endregion Basic Movement

	#region Dash
	private Vector3 delayedForceToApply;

	public void Dash()
	{
		if (dashCdTimer > 0) return;
		else dashCdTimer = dashCooldown;

		state = MovementState.isDashing;
		maxYSpeed = maxDashYSpeed;

		Transform forwardT = playerCamera;

		Vector3 direction = GetDirection(playerCamera);

		Vector3 forceToApply = direction * dashForce + horizontalDirection.up * dashUpwardForce;

		delayedForceToApply = forceToApply;
		Invoke(nameof(DelayedDashForce), 0.025f);

		Invoke(nameof(ResetDash), dashDuration);
	}

	private void DelayedDashForce()
	{
		rb.velocity = Vector3.zero;
		rb.AddForce(delayedForceToApply, ForceMode.Impulse);
	}

	private void ResetDash()
	{
		state = MovementState.isAirborn;
		maxYSpeed = 0;
	}

	private Vector3 GetDirection(Transform forwardT)
	{
		float horizontalInput = Input.GetAxisRaw("Horizontal");
		float verticalInput = Input.GetAxisRaw("Vertical");

		Vector3 direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;

		if (verticalInput == 0 && horizontalInput == 0)
			direction = forwardT.forward;

		return direction.normalized;
	}
	#endregion Dash
}