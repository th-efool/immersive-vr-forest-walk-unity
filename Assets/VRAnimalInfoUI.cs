using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils; // For XROrigin camera auto-detect

public class VRAnimalInfoUI : MonoBehaviour
{
	[Header("Content")]
	[SerializeField]
	private string m_Title = "Animal";
	[SerializeField, TextArea(2, 6)]
	private string m_Description = "Description of this animal.";

	[Header("Layout")]
	[SerializeField]
	private Vector3 m_Offset = new Vector3(0f, 1.6f, 0f);
	[SerializeField]
	private Vector2 m_Size = new Vector2(450f, 220f);
	[SerializeField]
	private float m_UIScale = 0.0025f; // scales pixels to meters

	[Header("Behavior")]
	[SerializeField]
	private bool m_ShowOnStart = false;
	[SerializeField]
	private bool m_AutoResizeToText = true;
	[SerializeField]
	private float m_MinHeight = 140f;
	[SerializeField]
	private float m_MaxHeight = 700f;
	[SerializeField]
	private bool m_BillboardToCamera = true;
	[SerializeField]
	private Transform m_Camera;

	private Canvas m_Canvas;
	private RectTransform m_RootRect;
	private GameObject m_PanelGO;
	private Button m_ToggleButton;
	private TextMeshProUGUI m_TitleTMP;
	private TextMeshProUGUI m_DescriptionTMP;

	private void Awake()
	{
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

		BuildUIIfNeeded();
		ApplyContent();
		SetPanelActive(m_ShowOnStart);
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

		// Force non-mirrored scale regardless of parent negative scale
		EnsureNonMirrored(m_Canvas.transform);

		if (m_AutoResizeToText)
		{
			ResizeToContent();
		}
	}

	public void SetDescription(string title, string description)
	{
		m_Title = title;
		m_Description = description;
		ApplyContent();
	}

	public void Toggle()
	{
		if (m_PanelGO == null) return;
		SetPanelActive(!m_PanelGO.activeSelf);
	}

	private void SetPanelActive(bool active)
	{
		m_PanelGO.SetActive(active);
		// Update button label if present
		if (m_ToggleButton != null)
		{
			var label = m_ToggleButton.GetComponentInChildren<TextMeshProUGUI>();
			if (label != null)
			{
				label.text = active ? "Hide" : "ShowInfo";
			}
		}
	}

	private void ApplyContent()
	{
		if (m_TitleTMP != null) m_TitleTMP.text = m_Title;
		if (m_DescriptionTMP != null) m_DescriptionTMP.text = m_Description;
		if (m_AutoResizeToText)
		{
			ResizeToContent();
		}
	}

	private void ResizeToContent()
	{
		if (m_RootRect == null || m_TitleTMP == null || m_DescriptionTMP == null)
		{
			return;
		}

		// Calculate preferred text heights at current width
		float width = m_RootRect.sizeDelta.x;
		if (width <= 0f)
		{
			width = m_Size.x;
		}

		var titlePref = m_TitleTMP.GetPreferredValues(m_Title, width - 24f, 0f);
		var descPref = m_DescriptionTMP.GetPreferredValues(m_Description, width - 24f, 0f);

		float paddingTop = 12f;
		float paddingBottom = 12f;
		float titleSpacing = 10f;
		float buttonSpace = 60f; // space reserved for the button at bottom

		float needed = paddingTop + titlePref.y + titleSpacing + descPref.y + paddingBottom + buttonSpace;
		needed = Mathf.Clamp(needed, m_MinHeight, m_MaxHeight);

		m_RootRect.sizeDelta = new Vector2(width, needed);
	}

	private void BuildUIIfNeeded()
	{
		if (m_Canvas != null) return;

		var root = new GameObject("VRAnimalInfoUI_Canvas");
		root.transform.SetParent(transform, false);
		root.transform.localPosition = m_Offset;
		root.transform.localRotation = Quaternion.identity;

		m_Canvas = root.AddComponent<Canvas>();
		m_Canvas.renderMode = RenderMode.WorldSpace;
		m_Canvas.sortingOrder = 20;

		var scaler = root.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
		scaler.scaleFactor = 1f;

		root.AddComponent<GraphicRaycaster>();
		root.AddComponent<TrackedDeviceGraphicRaycaster>();

		m_RootRect = m_Canvas.GetComponent<RectTransform>();
		m_RootRect.sizeDelta = m_Size;
		m_RootRect.localScale = Vector3.one * Mathf.Abs(m_UIScale);

		// Panel background
		m_PanelGO = new GameObject("Panel");
		m_PanelGO.transform.SetParent(root.transform, false);
		var panelRT = m_PanelGO.AddComponent<RectTransform>();
		panelRT.anchorMin = new Vector2(0f, 0f);
		panelRT.anchorMax = new Vector2(1f, 1f);
		panelRT.offsetMin = new Vector2(12f, 12f);
		panelRT.offsetMax = new Vector2(-12f, -12f);
		var img = m_PanelGO.AddComponent<Image>();
		img.color = new Color(0f, 0f, 0f, 0.55f);

		// Title text
		var titleGO = new GameObject("Title");
		titleGO.transform.SetParent(m_PanelGO.transform, false);
		var titleRT = titleGO.AddComponent<RectTransform>();
		titleRT.anchorMin = new Vector2(0f, 1f);
		titleRT.anchorMax = new Vector2(1f, 1f);
		titleRT.pivot = new Vector2(0.5f, 1f);
		titleRT.sizeDelta = new Vector2(0f, 48f);
		titleRT.anchoredPosition = new Vector2(0f, -10f);
		m_TitleTMP = titleGO.AddComponent<TextMeshProUGUI>();
		m_TitleTMP.fontSize = 36f;
		m_TitleTMP.enableWordWrapping = false;
		m_TitleTMP.alignment = TextAlignmentOptions.Center;

		// Description text
		var descGO = new GameObject("Description");
		descGO.transform.SetParent(m_PanelGO.transform, false);
		var descRT = descGO.AddComponent<RectTransform>();
		descRT.anchorMin = new Vector2(0f, 0f);
		descRT.anchorMax = new Vector2(1f, 1f);
		descRT.offsetMin = new Vector2(12f, 12f);
		descRT.offsetMax = new Vector2(-12f, -60f);
		m_DescriptionTMP = descGO.AddComponent<TextMeshProUGUI>();
		m_DescriptionTMP.fontSize = 26f;
		m_DescriptionTMP.enableWordWrapping = true;
		m_DescriptionTMP.alignment = TextAlignmentOptions.TopLeft;
		m_DescriptionTMP.overflowMode = TextOverflowModes.Overflow;

		// Toggle button (outside panel so it's clickable even when panel is hidden)
		var btnGO = new GameObject("ToggleButton");
		btnGO.transform.SetParent(root.transform, false);
		var btnRT = btnGO.AddComponent<RectTransform>();
		btnRT.anchorMin = new Vector2(0.5f, 0f);
		btnRT.anchorMax = new Vector2(0.5f, 0f);
		btnRT.pivot = new Vector2(0.5f, 0.5f);
		btnRT.sizeDelta = new Vector2(160f, 42f);
		btnRT.anchoredPosition = new Vector2(0f, 22f);
		var btnImg = btnGO.AddComponent<Image>();
		btnImg.color = new Color(1f, 1f, 1f, 0.85f);
		m_ToggleButton = btnGO.AddComponent<Button>();
		m_ToggleButton.targetGraphic = btnImg;
		m_ToggleButton.onClick.AddListener(Toggle);

		var btnTextGO = new GameObject("Text");
		btnTextGO.transform.SetParent(btnGO.transform, false);
		var btnTextRT = btnTextGO.AddComponent<RectTransform>();
		btnTextRT.anchorMin = new Vector2(0f, 0f);
		btnTextRT.anchorMax = new Vector2(1f, 1f);
		btnTextRT.offsetMin = Vector2.zero;
		btnTextRT.offsetMax = Vector2.zero;
		var btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
		btnTMP.text = "Show";
		btnTMP.alignment = TextAlignmentOptions.Center;
		btnTMP.fontSize = 24f;
		btnTMP.color = Color.black;
	}

	private void EnsureNonMirrored(Transform t)
	{
		if (t == null) return;
		var ls = t.localScale;
		var s = t.lossyScale;
		var detSign = Mathf.Sign(s.x * s.y * s.z);
		if (detSign < 0f)
		{
			// Flip X locally to correct handedness; preserve magnitude
			t.localScale = new Vector3(-ls.x, ls.y, ls.z);
		}
	}
}


