using System.Collections;
using System.Collections.Generic;
using Pie.ExtensionMethods.UnityEngine;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfship
{
	public class ToolTip : MonoBehaviour
	{
		[SerializeField] private Text _text = null;

		#region monoBehaviour Messages
		private void Awake()
		{
			_screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
			_rectTransform = GetComponent<RectTransform>();
			Hide();
		}
		#endregion

		public void Set(string text, Transform parent)
		{
			_text.text = text;
			SetTransform(parent);
		}

		public void Show()
		{
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		private void SetTransform(Transform transform)
		{
			Vector3 pos = transform.position;
			Vector3 relativePos = transform.root.InverseTransformPoint(transform.position);
			Vector3 offset = -relativePos.normalized;

			float isRight = Mathf.Clamp01(relativePos.x);
			float isTop = Mathf.Clamp01(relativePos.y);
			relativePos.x = isRight;
			relativePos.y = isTop;

			_rectTransform.Reset();
			_rectTransform.pivot = relativePos;
			_rectTransform.position = pos + offset;
		}


		private Vector3 _screenCenter;
		private RectTransform _rectTransform;
	}
}
