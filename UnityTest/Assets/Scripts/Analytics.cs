using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class Analytics : MonoBehaviour {

	[SerializeField]
	private Text _remoteValue = null;
	
	private Dictionary<string, object> _evnetData;

	private void Awake()
	{
		_evnetData = new Dictionary<string, object>();

		_evnetData.Add("name", "hyesung");
		_evnetData.Add("player_Level", "99");
		_evnetData.Add("Player_Tier", 1);
		_evnetData.Add("Player_Items", 30);

		TutorialCall();
	}

	private void Start()
	{
		// Add this class's updated settings handler to the RemoteSettings.Updated event.
		RemoteSettings.Updated += new RemoteSettings.UpdatedEventHandler(RemoteSettingsUpdated);
	}

	private void RemoteSettingsUpdated()
	{
		_remoteValue.text = RemoteSettings.GetInt("testValue").ToString();
	}

	public void EventCall()
	{
		AnalyticsResult result = AnalyticsEvent.LevelComplete("mision", 1, _evnetData);
		Debug.Log(result);
	}

	public void TutorialCall()
	{
		AnalyticsResult result = AnalyticsEvent.FirstInteraction();
		Debug.Log("frist" + result);

		result = AnalyticsEvent.TutorialStart("Beginner");
		Debug.Log("start" + result);

		result = AnalyticsEvent.TutorialStep(1, "Beginner");
		Debug.Log("step1" + result);

		result = AnalyticsEvent.TutorialStep(2, "Beginner");
		Debug.Log("step2" + result);

		result = AnalyticsEvent.TutorialStep(3, "Beginner");
		Debug.Log("step3" + result);

		result = AnalyticsEvent.TutorialComplete("Beginner");
		Debug.Log("complete" + result);
	}


	public void CustomCall()
	{
		AnalyticsResult result = AnalyticsEvent.Custom("customCall", _evnetData);
		Debug.Log(result);
	}

	public void ForceUpdate()
	{
		RemoteSettings.ForceUpdate();
	}
}
