using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wolfship.Battle.UI;
using Debug = UnityEngine.Debug;

namespace Wolfship
{
	public class IToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] protected ToolTip _toolTip = null;

		#region MonoBehaviourMessages
		protected virtual void Update()
		{
			if (!IsToolTipActive)
				return;

			if (!_isPointerEnter)
				return;
			
			_timer -= Time.deltaTime;
			Debug.Log("<color=yellow>ToolTip OnPointerEnter: </color>" + _timer);

			if (_timer <= 0.0f && _isPointerEnter)
			{
				_isPointerEnter = false;
				_toolTip.Set(_toolTipText, transform);
				_toolTip.Show();
			}
		}
		#endregion

		#region UI Event
		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			if (!IsToolTipActive)
				return;

			_isPointerEnter = true;
			_timer = _toolTipDelay;
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			if (!IsToolTipActive)
				return;

			_isPointerEnter = false;
			_toolTip.Hide();
			Debug.Log("<color=yellow>ToolTip OnPointerExit: </color>" + _timer);
		}
		#endregion

		public bool IsToolTipActive
		{
			get;
			set;
		}

		protected string _toolTipText = "툴팁 스트링을 세팅하지 않았습니다.";

		private float _toolTipDelay = 0.5f;
		private float _timer;
		private bool _isPointerEnter;
	}
}

