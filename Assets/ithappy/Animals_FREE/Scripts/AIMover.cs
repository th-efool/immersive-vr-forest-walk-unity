using UnityEngine;
using Unity.XR.CoreUtils; // For XROrigin

namespace ithappy.Animals_FREE
{
	[RequireComponent(typeof(CreatureMover))]
	[DisallowMultipleComponent]
	public class AIMover : MonoBehaviour
	{
		[Header("Activation")]
		[SerializeField]
		private Transform m_Player;
		[SerializeField]
		private float m_ActivationDistance = 80f;
		[SerializeField]
		private float m_ActivationHysteresis = 5f;
		[SerializeField]
		private bool m_ToggleAnimatorWhenInactive = true;
		[SerializeField]
		private bool m_ToggleMoverWhenInactive = true;

		[Header("Wander")]
		[SerializeField]
		private float m_WanderRadius = 10f;
		[SerializeField]
		private Vector2 m_MoveDurationRange = new Vector2(2f, 5f);
		[SerializeField]
		private Vector2 m_PauseDurationRange = new Vector2(0.5f, 2f);
		[SerializeField]
		private bool m_ConfineToBounds = true;
		[SerializeField]
		private Transform m_BoundsCenter;
		[SerializeField]
		private float m_BoundsRadius = 15f;

		[Header("Pacing")]
		[SerializeField]
		private bool m_AllowRun = false;
		[SerializeField, Range(0f, 1f)]
		private float m_RunChance = 0.2f;

		private CreatureMover m_Mover;
		private Animator m_Animator;
		private Transform m_Transform;

		private Vector3 m_WanderCenter;
		private Vector3 m_Destination;
		private float m_StateTimer;
		private bool m_IsMoving;
		private bool m_RunThisBurst;

		private Vector2 m_Axis;
		private Vector3 m_Target;

		private const float k_ReachedDistance = 0.5f;
		private bool m_IsActiveByDistance = true;
		private float m_PlayerSearchTimer;
		private float m_ForcedRunTimer;
		private bool m_IsFleeing;

		private void Awake()
		{
			m_Mover = GetComponent<CreatureMover>();
			m_Animator = GetComponent<Animator>();
			m_Transform = transform;
			m_WanderCenter = (m_BoundsCenter != null) ? m_BoundsCenter.position : m_Transform.position;
			BeginPause();
		}

		private void OnEnable()
		{
			m_WanderCenter = (m_BoundsCenter != null) ? m_BoundsCenter.position : m_Transform.position;
		}

		private void Update()
		{
			UpdateActivation(Time.deltaTime);
			if (!m_IsActiveByDistance)
			{
				return;
			}

			if (m_ForcedRunTimer > 0f)
			{
				m_ForcedRunTimer -= Time.deltaTime;
				if (m_ForcedRunTimer <= 0f)
				{
					m_IsFleeing = false;
				}
			}

			TickState(Time.deltaTime);
			DriveMover();
		}

		private void UpdateActivation(float deltaTime)
		{
			if (m_Player == null)
			{
				m_PlayerSearchTimer -= deltaTime;
				if (m_PlayerSearchTimer <= 0f)
				{
					m_PlayerSearchTimer = 1.5f;
					// Prefer XR Origin camera if available
					var xrOrigin = FindObjectOfType<XROrigin>();
					if (xrOrigin != null && xrOrigin.Camera != null)
					{
						m_Player = xrOrigin.Camera.transform;
					}
					else
					{
						var mainCam = Camera.main;
						if (mainCam != null)
						{
							m_Player = mainCam.transform;
						}
						else
						{
							var tagged = GameObject.FindGameObjectWithTag("Player");
							if (tagged != null)
							{
								m_Player = tagged.transform;
							}
						}
					}
				}
			}

			if (m_Player == null)
			{
				// No reference to player: keep current state
				return;
			}

			var toPlayer = m_Player.position - m_Transform.position;
			toPlayer.y = 0f;
			var sqr = toPlayer.sqrMagnitude;
			var onDist = Mathf.Max(0f, m_ActivationDistance - m_ActivationHysteresis);
			var offDist = m_ActivationDistance + m_ActivationHysteresis;

			if (m_IsActiveByDistance && sqr > offDist * offDist)
			{
				ApplyActiveState(false);
			}
			else if (!m_IsActiveByDistance && sqr < onDist * onDist)
			{
				ApplyActiveState(true);
			}
		}

		private void ApplyActiveState(bool active)
		{
			m_IsActiveByDistance = active;

			if (!active)
			{
				// Stop motion and animation work when far
				if (m_Mover != null)
				{
					var zeroAxis = Vector2.zero;
					var look = m_Transform.position + m_Transform.forward * 5f;
					m_Mover.SetInput(in zeroAxis, in look, false, false);
					if (m_ToggleMoverWhenInactive)
					{
						m_Mover.enabled = false;
					}
				}
				if (m_Animator != null && m_ToggleAnimatorWhenInactive)
				{
					m_Animator.enabled = false;
				}
			}
			else
			{
				if (m_Mover != null)
				{
					m_Mover.enabled = true;
				}
				if (m_Animator != null)
				{
					m_Animator.enabled = true;
				}
			}
		}

		private void TickState(float deltaTime)
		{
			m_StateTimer -= deltaTime;
			if (m_StateTimer > 0f)
			{
				return;
			}

			if (!m_IsMoving)
			{
				BeginMove();
			}
			else
			{
				BeginPause();
			}
		}

		private void BeginMove()
		{
			if (!m_IsFleeing)
			{
				m_Destination = PickRandomDestination();
				m_RunThisBurst = m_AllowRun && Random.value < m_RunChance;
			}
			m_IsMoving = true;
			m_StateTimer = Random.Range(m_MoveDurationRange.x, m_MoveDurationRange.y);
		}

		private void BeginPause()
		{
			m_IsMoving = false;
			m_RunThisBurst = false;
			m_Axis = Vector2.zero;
			m_StateTimer = Random.Range(m_PauseDurationRange.x, m_PauseDurationRange.y);
		}

		private Vector3 PickRandomDestination()
		{
			var center = (m_BoundsCenter != null) ? m_BoundsCenter.position : m_WanderCenter;
			var dir = Random.insideUnitCircle.normalized; // XZ plane
			var dist = Random.Range(0.25f * m_WanderRadius, m_WanderRadius);
			var offset = new Vector3(dir.x, 0f, dir.y) * dist;
			var dest = center + offset;

			if (m_ConfineToBounds)
			{
				var to = dest - center;
				to.y = 0f;
				if (to.sqrMagnitude > m_BoundsRadius * m_BoundsRadius)
				{
					to = to.normalized * m_BoundsRadius;
					dest = center + to;
				}
			}

			return dest;
		}

		private void DriveMover()
		{
			if (m_Mover == null)
			{
				return;
			}

			if (m_IsMoving)
			{
				var to = m_Destination - m_Transform.position;
				to.y = 0f;

				if (to.sqrMagnitude <= k_ReachedDistance * k_ReachedDistance)
				{
					BeginPause();
				}
				else
				{
					// Provide a forward look target for CreatureMover (used to derive forward frame)
					m_Target = m_Transform.position + to.normalized * 5f;
					m_Axis = new Vector2(0f, 1f); // forward in the computed frame
					m_Mover.SetInput(in m_Axis, in m_Target, in m_RunThisBurst, false);
				}
			}
			else
			{
				m_Target = m_Transform.position + m_Transform.forward * 5f;
				m_Axis = Vector2.zero;
				m_Mover.SetInput(in m_Axis, in m_Target, false, false);
			}
		}

		public void SetWanderCenter(Vector3 center)
		{
			m_WanderCenter = center;
		}

		public void TriggerRunAway(Vector3 fromPosition, float duration = 3f, float distance = 12f)
		{
			var away = (m_Transform.position - fromPosition);
			away.y = 0f;
			if (away.sqrMagnitude < 0.0001f)
			{
				away = m_Transform.forward;
			}
			away = away.normalized * Mathf.Max(2f, distance);
			m_Destination = m_Transform.position + away;
			m_IsMoving = true;
			m_RunThisBurst = true;
			m_IsFleeing = true;
			m_ForcedRunTimer = Mathf.Max(0.5f, duration);
			m_StateTimer = m_ForcedRunTimer;
		}

		public void TriggerRunAway(Transform fromTransform, float duration = 3f, float distance = 12f)
		{
			if (fromTransform == null)
			{
				TriggerRunAway(m_Transform.position - m_Transform.forward * distance, duration, distance);
				return;
			}
			TriggerRunAway(fromTransform.position, duration, distance);
		}
	}
}
