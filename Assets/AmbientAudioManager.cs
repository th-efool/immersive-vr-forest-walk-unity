using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Unity.XR.CoreUtils; // XROrigin auto-detect

public class AmbientAudioManager : MonoBehaviour
{
	[Header("Player / Activation")]
	[SerializeField]
	private Transform m_Player;
	[SerializeField]
	private float m_ActivationDistance = 120f;
	[SerializeField]
	private float m_ActivationHysteresis = 10f;

	[Header("Output / Spatial Settings")]
	[SerializeField]
	private AudioMixerGroup m_OutputMixer;
	[SerializeField, Range(0f, 1f)]
	private float m_SpatialBlend = 1f;
	[SerializeField]
	private float m_MinDistance = 5f;
	[SerializeField]
	private float m_MaxDistance = 80f;
	[SerializeField]
	private AudioRolloffMode m_RolloffMode = AudioRolloffMode.Logarithmic;
	[SerializeField]
	private float m_Spread = 0f;
	[SerializeField]
	private float m_DopplerLevel = 0f;

	[Header("Looping Bed (wind, far forest, etc.)")]
	[SerializeField]
	private AudioClip[] m_LoopClips;
	[SerializeField, Range(0f, 1f)]
	private float m_LoopVolume = 0.35f;
	[SerializeField]
	private Vector2 m_LoopPitchRange = new Vector2(0.97f, 1.03f);
	[SerializeField]
	private float m_LoopCrossfadeSeconds = 3f;
	[SerializeField]
	private Vector2 m_LoopRetainSecondsRange = new Vector2(20f, 60f);

	[Header("One-Shots (birds, leaves, twigs, insects)")]
	[SerializeField]
	private AudioClip[] m_OneShotClips;
	[SerializeField]
	private Vector2 m_OneShotIntervalRange = new Vector2(2.5f, 7.5f);
	[SerializeField]
	private Vector2 m_SpawnRadiusRange = new Vector2(8f, 25f);
	[SerializeField, Range(0f, 1f)]
	private Vector2 m_OneShotVolumeRange = new Vector2(0.12f, 0.35f);
	[SerializeField]
	private Vector2 m_OneShotPitchRange = new Vector2(0.92f, 1.08f);
	[SerializeField]
	private int m_MaxSimultaneousOneShots = 6;

	[Header("Subtle Modulation (bed movement)")]
	[SerializeField]
	private float m_VolumeLfoAmount = 0.05f;
	[SerializeField]
	private Vector2 m_VolumeLfoRateRange = new Vector2(0.02f, 0.08f);

	private AudioSource m_LoopA;
	private AudioSource m_LoopB;
	private bool m_LoopAActive = true;
	private float m_LoopSwapTimer;
	private float m_LoopRetainSeconds;
	private float m_LfoPhase;
	private float m_LfoRate;

	private readonly List<AudioSource> m_OneShotPool = new List<AudioSource>();
	private float m_OneShotTimer;
	private bool m_IsActiveByDistance = true;
	private float m_PlayerSearchTimer;

	private void Awake()
	{
		CreateOrGetLoopSources();
		PrimeLoopClip(true);
		m_OneShotTimer = Random.Range(m_OneShotIntervalRange.x, m_OneShotIntervalRange.y);
		m_LfoRate = Random.Range(m_VolumeLfoRateRange.x, m_VolumeLfoRateRange.y);
	}

	private void Update()
	{
		UpdateActivation(Time.deltaTime);
		if (!m_IsActiveByDistance)
		{
			return;
		}

		UpdateLoopBed(Time.deltaTime);
		UpdateOneShots(Time.deltaTime);
	}

	private void UpdateActivation(float deltaTime)
	{
		if (m_Player == null)
		{
			m_PlayerSearchTimer -= deltaTime;
			if (m_PlayerSearchTimer <= 0f)
			{
				m_PlayerSearchTimer = 1.5f;
				var xr = FindObjectOfType<XROrigin>();
				if (xr != null && xr.Camera != null)
				{
					m_Player = xr.Camera.transform;
				}
				else if (Camera.main != null)
				{
					m_Player = Camera.main.transform;
				}
				else
				{
					var tagged = GameObject.FindGameObjectWithTag("Player");
					if (tagged != null) m_Player = tagged.transform;
				}
			}
		}

		if (m_Player == null)
		{
			return;
		}

		var toPlayer = m_Player.position - transform.position;
		toPlayer.y = 0f;
		var sqr = toPlayer.sqrMagnitude;
		var onDist = Mathf.Max(0f, m_ActivationDistance - m_ActivationHysteresis);
		var offDist = m_ActivationDistance + m_ActivationHysteresis;

		if (m_IsActiveByDistance && sqr > offDist * offDist)
		{
			SetActiveAudio(false);
		}
		else if (!m_IsActiveByDistance && sqr < onDist * onDist)
		{
			SetActiveAudio(true);
		}
	}

	private void SetActiveAudio(bool active)
	{
		m_IsActiveByDistance = active;
		if (m_LoopA != null) m_LoopA.mute = !active;
		if (m_LoopB != null) m_LoopB.mute = !active;
		for (int i = 0; i < m_OneShotPool.Count; i++)
		{
			if (m_OneShotPool[i] != null) m_OneShotPool[i].mute = !active;
		}
	}

	private void UpdateLoopBed(float deltaTime)
	{
		if ((m_LoopClips == null || m_LoopClips.Length == 0) || m_LoopA == null || m_LoopB == null)
		{
			return;
		}

		// Subtle volume LFO
		m_LfoPhase += deltaTime * m_LfoRate * Mathf.PI * 2f;
		var lfo = Mathf.Sin(m_LfoPhase) * m_VolumeLfoAmount;
		var baseVol = m_LoopVolume;

		AudioSource active = m_LoopAActive ? m_LoopA : m_LoopB;
		AudioSource inactive = m_LoopAActive ? m_LoopB : m_LoopA;
		active.volume = Mathf.Clamp01(baseVol + lfo);
		inactive.volume = Mathf.Clamp01(baseVol + lfo);

		m_LoopSwapTimer -= deltaTime;
		if (m_LoopSwapTimer <= 0f)
		{
			SwapLoopClip();
		}

		// Crossfade if both playing different clips
		if (m_LoopCrossfadeSeconds > 0.01f && inactive.isPlaying)
		{
			// Fade the inactive out over time; when reaches low level, stop it
			inactive.volume = Mathf.MoveTowards(inactive.volume, 0f, deltaTime / m_LoopCrossfadeSeconds);
			if (inactive.volume <= 0.001f)
			{
				inactive.Stop();
			}
		}
	}

	private void SwapLoopClip()
	{
		if (m_LoopClips == null || m_LoopClips.Length == 0) return;

		AudioSource active = m_LoopAActive ? m_LoopA : m_LoopB;
		AudioSource inactive = m_LoopAActive ? m_LoopB : m_LoopA;

		// Choose a different clip than the currently active, if possible
		AudioClip next = m_LoopClips[Random.Range(0, m_LoopClips.Length)];
		if (active.clip != null && m_LoopClips.Length > 1)
		{
			int safety = 8;
			while (next == active.clip && safety-- > 0)
			{
				next = m_LoopClips[Random.Range(0, m_LoopClips.Length)];
			}
		}

		inactive.clip = next;
		inactive.pitch = Random.Range(m_LoopPitchRange.x, m_LoopPitchRange.y);
		inactive.volume = 0f;
		inactive.loop = true;
		inactive.Play();

		m_LoopAActive = !m_LoopAActive;
		m_LoopRetainSeconds = Random.Range(m_LoopRetainSecondsRange.x, m_LoopRetainSecondsRange.y);
		m_LoopSwapTimer = m_LoopRetainSeconds;
	}

	private void UpdateOneShots(float deltaTime)
	{
		if (m_OneShotClips == null || m_OneShotClips.Length == 0) return;

		m_OneShotTimer -= deltaTime;
		if (m_OneShotTimer > 0f) return;

		m_OneShotTimer = Random.Range(m_OneShotIntervalRange.x, m_OneShotIntervalRange.y);

		// Limit concurrency
		int playing = 0;
		for (int i = 0; i < m_OneShotPool.Count; i++)
		{
			if (m_OneShotPool[i] != null && m_OneShotPool[i].isPlaying) playing++;
		}
		if (playing >= m_MaxSimultaneousOneShots) return;

		var src = GetFreeOneShotSource();
		if (src == null) return;

		var clip = m_OneShotClips[Random.Range(0, m_OneShotClips.Length)];
		var vol = Random.Range(m_OneShotVolumeRange.x, m_OneShotVolumeRange.y);
		var pit = Random.Range(m_OneShotPitchRange.x, m_OneShotPitchRange.y);
		var r = Random.Range(m_SpawnRadiusRange.x, m_SpawnRadiusRange.y);
		var dir = Random.insideUnitCircle.normalized;
		var pos = transform.position + new Vector3(dir.x, 0f, dir.y) * r;

		src.transform.position = pos;
		src.clip = clip;
		src.volume = vol;
		src.pitch = pit;
		src.spatialBlend = m_SpatialBlend;
		src.minDistance = m_MinDistance;
		src.maxDistance = m_MaxDistance;
		src.rolloffMode = m_RolloffMode;
		src.spread = m_Spread;
		src.dopplerLevel = m_DopplerLevel;
		src.outputAudioMixerGroup = m_OutputMixer;
		src.Play();
	}

	private void CreateOrGetLoopSources()
	{
		m_LoopA = CreateChildSource("Loop_A");
		m_LoopB = CreateChildSource("Loop_B");
	}

	private void PrimeLoopClip(bool immediate)
	{
		if (m_LoopClips == null || m_LoopClips.Length == 0) return;
		m_LoopA.clip = m_LoopClips[Random.Range(0, m_LoopClips.Length)];
		m_LoopA.loop = true;
		m_LoopA.pitch = Random.Range(m_LoopPitchRange.x, m_LoopPitchRange.y);
		m_LoopA.volume = m_LoopVolume;
		m_LoopA.Play();
		m_LoopB.Stop();
		m_LoopB.clip = null;
		m_LoopAActive = true;
		m_LoopRetainSeconds = Random.Range(m_LoopRetainSecondsRange.x, m_LoopRetainSecondsRange.y);
		m_LoopSwapTimer = immediate ? 0.5f : m_LoopRetainSeconds;
	}

	private AudioSource GetFreeOneShotSource()
	{
		for (int i = 0; i < m_OneShotPool.Count; i++)
		{
			var s = m_OneShotPool[i];
			if (s != null && !s.isPlaying) return s;
		}

		var created = CreateChildSource("OneShot_");
		m_OneShotPool.Add(created);
		return created;
	}

	private AudioSource CreateChildSource(string namePrefix)
	{
		var go = new GameObject(namePrefix);
		go.transform.parent = transform;
		go.transform.localPosition = Vector3.zero;
		var src = go.AddComponent<AudioSource>();
		src.playOnAwake = false;
		src.loop = false;
		src.spatialBlend = m_SpatialBlend;
		src.minDistance = m_MinDistance;
		src.maxDistance = m_MaxDistance;
		src.rolloffMode = m_RolloffMode;
		src.spread = m_Spread;
		src.dopplerLevel = m_DopplerLevel;
		src.outputAudioMixerGroup = m_OutputMixer;
		return src;
	}
}


