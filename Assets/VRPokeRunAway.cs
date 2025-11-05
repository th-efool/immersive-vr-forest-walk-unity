using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using ithappy.Animals_FREE; // AIMover

// World-space UI button (with hover feedback) placed at an offset above the animal.
// Uses XR UI (TrackedDeviceGraphicRaycaster + XRUIInputModule) so rays/pokes highlight and click.
public class VRPokeRunAway : MonoBehaviour
{
	[Header("Target")]
	[SerializeField]
	private AIMover m_AIMover;

	[Header("UI Layout")]
	[SerializeField]
	private Vector3 m_Offset = new Vector3(0f, 1.4f, 0f);
	[SerializeField]
	private Vector2 m_Size = new Vector2(140f, 56f);
	[SerializeField]
	private float m_UIScale = 0.0025f;
	[SerializeField]
	private bool m_BillboardToCamera = true;
	[SerializeField]
	private Transform m_Camera;

	[Header("Button Visuals")]
	[SerializeField]
	private string m_Label = "Run!";
	[SerializeField]
	private Color m_Normal = new Color(1f, 0.9f, 0.4f, 0.9f);
	[SerializeField]
	private Color m_Highlight = new Color(1f, 0.95f, 0.6f, 1f);
	[SerializeField]
	private Color m_Pressed = new Color(0.95f, 0.75f, 0.35f, 1f);

	[Header("Flee Settings")]
	[SerializeField]
	private float m_FleeDuration = 3.5f;
	[SerializeField]
	private float m_FleeDistance = 14f;

	private Canvas m_Canvas;
	private RectTransform m_RootRect;
	private Button m_Button;

	private void Awake()
	{
		if (m_AIMover == null)
		{
			m_AIMover = GetComponentInParent<AIMover>();
		}

		if (m_Camera == null)
		{
			var xr = FindObjectOfType<XROrigin>();
			if (xr != null && xr.Camera != null)
			{
				m_Camera = xr.Camera.transform;
			}
			else if (Camera.main != null)
			{
				m_Camera = Camera.main.transform;
			}
		}

		BuildUI();
	}

	private void LateUpdate()
	{
		if (m_Canvas == null)
		{
			return;
		}

		m_Canvas.transform.position = transform.position + m_Offset;
		if (m_BillboardToCamera && m_Camera != null)
		{
			var toCam = m_Camera.position - m_Canvas.transform.position;
			toCam.y = 0f;
			if (toCam.sqrMagnitude > 0.0001f)
			{
				m_Canvas.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
				// Force a 180° yaw so the face points the expected way in your setup
				m_Canvas.transform.Rotate(Vector3.up, 180f, Space.World);
			}
		}

		EnsureNonMirrored(m_Canvas.transform);
	}

	private void BuildUI()
	{
		var root = new GameObject("RunAwayButton_Canvas");
		root.transform.SetParent(transform, false);
		root.transform.localPosition = m_Offset;
		root.transform.localRotation = Quaternion.identity;

        m_Canvas = root.AddComponent<Canvas>();
		m_Canvas.renderMode = RenderMode.WorldSpace;
		m_Canvas.sortingOrder = 25;

		var scaler = root.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
		scaler.scaleFactor = 1f;

		root.AddComponent<GraphicRaycaster>();
		root.AddComponent<TrackedDeviceGraphicRaycaster>();

		m_RootRect = m_Canvas.GetComponent<RectTransform>();
		m_RootRect.sizeDelta = m_Size;
		m_RootRect.localScale = Vector3.one * Mathf.Abs(m_UIScale);

		var btnGO = new GameObject("Button");
		btnGO.transform.SetParent(root.transform, false);
		var btnRT = btnGO.AddComponent<RectTransform>();
		btnRT.anchorMin = new Vector2(0f, 0f);
		btnRT.anchorMax = new Vector2(1f, 1f);
		btnRT.offsetMin = Vector2.zero;
		btnRT.offsetMax = Vector2.zero;

		var img = btnGO.AddComponent<Image>();
		img.color = m_Normal;

		m_Button = btnGO.AddComponent<Button>();
		var colors = m_Button.colors;
		colors.normalColor = m_Normal;
		colors.highlightedColor = m_Highlight;
		colors.pressedColor = m_Pressed;
		colors.selectedColor = m_Highlight;
		colors.colorMultiplier = 1f;
		m_Button.colors = colors;
		m_Button.onClick.AddListener(OnClicked);

		var textGO = new GameObject("Text");
		textGO.transform.SetParent(btnGO.transform, false);
		var textRT = textGO.AddComponent<RectTransform>();
		textRT.anchorMin = new Vector2(0f, 0f);
		textRT.anchorMax = new Vector2(1f, 1f);
		textRT.offsetMin = Vector2.zero;
		textRT.offsetMax = Vector2.zero;
		var tmp = textGO.AddComponent<TextMeshProUGUI>();
		tmp.text = m_Label;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.fontSize = 40f;
		tmp.color = Color.black;
	}

	private void EnsureNonMirrored(Transform t)
	{
		if (t == null) return;
		var ls = t.localScale;
		var s = t.lossyScale;
		var detSign = Mathf.Sign(s.x * s.y * s.z);
		if (detSign < 0f)
		{
			t.localScale = new Vector3(-ls.x, ls.y, ls.z);
		}
	}

	private void OnClicked()
	{
		if (m_AIMover == null)
		{
			m_AIMover = GetComponentInParent<AIMover>();
			if (m_AIMover == null) return;
		}

		Transform source = m_Camera != null ? m_Camera : (Camera.main != null ? Camera.main.transform : transform);
		m_AIMover.TriggerRunAway(source, m_FleeDuration, m_FleeDistance);
	}
}


