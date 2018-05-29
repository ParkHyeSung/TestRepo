using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UniRx;
using Wolfship.Battle.ActorComponents;
using Wolfship.Battle.Actors;
using Wolfship.Battle.StageControllers;

namespace Wolfship.Battle {

	public class OperatingSystem : MonoBehaviour {

		private enum MessageType {

			NONE,

			BattleShipDurability20,
			BattleShipDurability40,
			BattleShipDurability60,
			BattleShipDurability80
		}

		private enum WaveMessageType {

			NONE,

			ParticularWaveStart,
			ParticularWaveEnd
		}

		private enum PriorityType {

			Wave = 1,
			Durability = 2,
			Enhance_Physical = 3,
			Enhance_CoreTech = 6,
			Enhance_Optical = 4,
			Enhance_ForceField = 5,
			Tutorial = 7
		}

		private readonly static KeyValuePair<MessageType, float>[] DurabilityMessages = {

			new KeyValuePair<MessageType, float>(MessageType.BattleShipDurability80, 0.8f),
			new KeyValuePair<MessageType, float>(MessageType.BattleShipDurability60, 0.6f),
			new KeyValuePair<MessageType, float>(MessageType.BattleShipDurability40, 0.4f),
			new KeyValuePair<MessageType, float>(MessageType.BattleShipDurability20, 0.2f)
		};

		private static bool TryGetBattleshipDurabilityMessage(float currentNormalized, float prevNormalized, out MessageType messageType) {

			messageType = MessageType.NONE;

			for (int i = 0; i < DurabilityMessages.Length; ++i) {

				float value = DurabilityMessages[i].Value;
				if (currentNormalized <= value && prevNormalized > value) {

					messageType = DurabilityMessages[i].Key;
					return true;
				}
			}

			return false;
		}

		#region Inspector Fields
		[SerializeField] private GameObject _panel = null;
		[SerializeField] private Image _portrait = null;
		[SerializeField] private Text _name = null;
		[SerializeField] private Text _string = null;

		[SerializeField] private List<int> _operatorNumber = new List<int>();
		[SerializeField] private List<string> _operatorName = new List<string>();

		[SerializeField] private float _operatorPanelLifeTime = 3.0f;
		[SerializeField] private bool _isStack = true;

		[SerializeField] private Color _physicalColor = new Color(255.0f / 255.0f, 70.0f / 255.0f, 0.0f / 255.0f);
		[SerializeField] private Color _opticalColor = new Color(0.0f / 255.0f, 180.0f / 255.0f, 255.0f / 255.0f);
		[SerializeField] private Color _forceFieldColor = new Color(240.0f / 255.0f, 70.0f / 255.0f, 130.0f / 255.0f);
		[SerializeField] private Color _operatorColor = new Color(120.0f / 255.0f, 130.0f / 255.0f, 230.0f / 255.0f);
		[SerializeField] private Color _coreTechColor = Color.cyan;
		#endregion

		#region MonoBehaviour messages
		private void Awake() {

			_panelRectTransform = _panel.GetComponent<RectTransform>();
			_operatingAudioSource = GetComponent<AudioSource>();
			_operatingVoices = new string[3];
		}

		private void LateUpdate() {

			if (_messageQueue.Count <= 0)
				return;

			if (!_isOperatingStringShow) {

				ShowPanel();
			}
		}
		#endregion

		#region Event Listeners
		private void OnBattleshipHitPointChanged(Health.HitPointChanged msg) {

			MessageType messageType;
			if (TryGetBattleshipDurabilityMessage(msg.CurrentNormalized, msg.PrevNormalized, out messageType))
				AddMessage(messageType);
		}

		private void OnWaveStarted(MissionStageController.WaveStarted msg) {

			MissionData missionData = _missionStageController.Data;
			AddMessage(WaveMessageType.ParticularWaveStart, missionData.UniqueId, missionData.Waves[msg.WaveIndex]);
		}

		private void OnWaveEnded(MissionStageController.WaveEnded msg) {

			MissionData missionData = _missionStageController.Data;

			AddMessage(WaveMessageType.ParticularWaveEnd, missionData.UniqueId, missionData.Waves[msg.WaveIndex]);
		}

		private void OnEnhance(BattleshipEnhanceManagement.SlotCountChanged msg) {

			int level = msg.Slot.Count;
			var levelStr = msg.Slot.MaxCount > level ? level.ToString() : "Max";

			Action<string, PriorityType, Func<string, string>> addMessage = (key, priority, format) => {

				OperatingStringData data = GetOperatingData(key);
				AddMessage(priority, format(data.OperatingString), data.Voice);
			};

			switch (msg.Slot.Data.UniqueId) {

				case "Battleship_PhysicalTurret":
					addMessage(
							"Enhance_Physical",
							PriorityType.Enhance_Physical,
							fmt => string.Format(
									fmt,
									levelStr,
									msg.Slot.Data.PhysicalDamage * 100.0f * level,
									msg.Slot.Data.PhysicalAttackSpeed * 100.0f * level));
					break;

				case "Battleship_CoreTech_OpticalTurret":
				case "Battleship_OpticalTurret":
					addMessage(
							"Enhance_Optical",
							PriorityType.Enhance_Optical,
							fmt => string.Format(
									fmt,
									levelStr,
									msg.Slot.Data.OpticalDamage * 100.0f * level,
									msg.Slot.Data.OpticalAttackSpeed * 100.0f * level));
					break;

				case "Battleship_CoreTech_PowerStone":
				case "Battleship_PowerStone":
					addMessage(
						"Enhance_ForceField",
						PriorityType.Enhance_ForceField,
						fmt => string.Format(
								fmt,
								levelStr,
								msg.Slot.Data.PowerStoneSensorRange * level));
					break;

				case "Battleship_CoreTech":
					addMessage(
							"Enhance_CoreTech",
							PriorityType.Enhance_CoreTech,
							fmt => string.Format(
									fmt,
									levelStr,
									msg.Slot.Data.MaxCoreEnergy * 100.0f * level,
									msg.Slot.Data.GenerateCoreValue * level));
					break;

				default:
					throw new Exception("이름이 없는 인헨스 입니다.");
			}
		}

		private void OnOperateString(TutorialStageController.OperateString msg) {

			if (_operatorPanelLifeTime == 0.0f)
				HidePanel();

			_operatorPanelLifeTime = msg.ShowTime;

			OperatingStringData data = GetOperatingData(msg.OperStringID);
			AddMessage(PriorityType.Tutorial, data.OperatingString, data.Voice);
		}
		#endregion

		public void Init(StageManager stageManager, MissionStageController missionStageController, Battleship battleship) {

			if (stageManager == null)
				throw new ArgumentNullException("stageManager");

			if (missionStageController == null)
				throw new ArgumentNullException("missionStageController");

			if (battleship == null)
				throw new ArgumentNullException("battleship");

			_missionStageController = missionStageController;
			_battleship = battleship;

			SetOperator(missionStageController.Data.OperatorNumber);
			RegisterListeners();

			_originPanelWidth = _panelRectTransform.sizeDelta.x;

			InitPanel();
		}

		public void InitForTutorial(StageManager stageManager, TutorialStageController tutorialStageController, Battleship battleship) {

			if (stageManager == null)
				throw new ArgumentNullException("stageManager");

			if (tutorialStageController == null)
				throw new ArgumentNullException("missionStageController");

			if (battleship == null)
				throw new ArgumentNullException("battleship");

			_tutorialStageController = tutorialStageController;
			_battleship = battleship;
			_operatorPanelLifeTime = 0.0f;

			SetOperator(tutorialStageController.Data.OperatorNumber);
			TutorialRegisterListeners();

			_originPanelWidth = _panelRectTransform.sizeDelta.x;

			InitPanel();
		}

		public void Clear() {

			TimerManager.Instance.ClearTimer(_panelLifeTimerHandle);

			UnregisterListeners();
			TutorialUnregisterListeners();

			InitPanel();

			_messageQueue.Clear();

			_operatingDict = null;
			_missionStageController = null;
			_battleship = null;
			_isOperatingStringShow = false;
		}

		private void SetOperator(int operatorNumber) {

			if (_operatorNumber.Count != _operatorName.Count)
				throw new Exception("오퍼레이터 넘버와 오퍼레이터 이름의 개수가 다릅니다.");

			var list = new List<int>();
			int index = _operatorNumber.Count - 1;

			while (index >= 0) {

				operatorNumber -= _operatorNumber[index];

				if (operatorNumber < 0) {

					operatorNumber += _operatorNumber[index];
				}
				else {

					list.Add(_operatorNumber[index]);
				}
				index--;
			}

			index = UnityEngine.Random.Range(0, 99999) % list.Count;

			_operName = _operatorName[_operatorNumber.IndexOf(list[index])];
			_operatingDict = OperatingStringTable.Instance.GetOperatingData(_operName);

			_nameStr = _operName;

			switch (_operName) {

				case "James":
					_operatorPortrait = Resources.Load<Sprite>("OperatingPortraits/oper_cha_james");
					SetWaveVoice("James");
					break;
				case "Marion":
					_operatorPortrait = Resources.Load<Sprite>("OperatingPortraits/oper_cha_marion");
					SetWaveVoice("Marion");
					break;
				case "Fred":
					_operatorPortrait = Resources.Load<Sprite>("OperatingPortraits/oper_cha_frederick");
					SetWaveVoice("James");
					break;
				default:
					throw new Exception("오퍼레이터 포트레이트가 없는 이름입니다. 확인해 주세요.");
			}

			_opticalEnhancePotrait = Resources.Load<Sprite>("OperatingPortraits/oper_mod_optical");
			_physicalEnhacePotrait = Resources.Load<Sprite>("OperatingPortraits/oper_mod_physical");
			_forceFieldEnhancePotrait = Resources.Load<Sprite>("OperatingPortraits/oper_mod_f_field");

			_portrait.sprite = _operatorPortrait;
		}

		private void RegisterListeners() {

			_missionStageController.OnWaveStarted.AddListener(OnWaveStarted);
			_missionStageController.OnWaveEnded.AddListener(OnWaveEnded);

			_battleship.GetComponent<BattleshipEnhanceManagement>().OnSlotCountChanged.AddListener(OnEnhance);
			_battleship.GetComponent<Health>().OnHitPointChanged.AddListener(OnBattleshipHitPointChanged);
		}

		private void UnregisterListeners() {

			if (_missionStageController != null) {

				_missionStageController.OnWaveStarted.RemoveListener(OnWaveStarted);
				_missionStageController.OnWaveEnded.RemoveListener(OnWaveEnded);
			}

			if (_battleship != null) {

				_battleship.GetComponent<Health>().OnHitPointChanged.RemoveListener(OnBattleshipHitPointChanged);
				_battleship.GetComponent<BattleshipEnhanceManagement>().OnSlotCountChanged.RemoveListener(OnEnhance);
			}
		}

		private void TutorialRegisterListeners() {

			_tutorialStageController.OnOperateString.AddListener(OnOperateString);

			Health health = _battleship.GetComponent<Health>();
			health.OnHitPointChanged.AddListener(OnBattleshipHitPointChanged);
		}

		private void TutorialUnregisterListeners() {

			if (_tutorialStageController != null) {


			}

			if (_battleship != null) {

				Health health = _battleship.GetComponent<Health>();
				health.OnHitPointChanged.RemoveListener(OnBattleshipHitPointChanged);
			}
		}

		private void AddMessage(MessageType type) {

			OperatingStringData message;
			switch (type) {

			case MessageType.BattleShipDurability20: message = GetOperatingData("durability20"); break;
			case MessageType.BattleShipDurability40: message = GetOperatingData("durability40"); break;
			case MessageType.BattleShipDurability60: message = GetOperatingData("durability60"); break;
			case MessageType.BattleShipDurability80: message = GetOperatingData("durability80"); break;

			default:
				throw new ArgumentException("유효한 MessageType 값이 아닙니다.", "type");
			}

			AddMessage(PriorityType.Durability, message.OperatingString, message.Voice);
		}

		private void AddMessage(WaveMessageType type, string missionId, string waveId) {

			string key;
			switch (type) {

			case WaveMessageType.ParticularWaveStart: key = "ws_"; break;
			case WaveMessageType.ParticularWaveEnd: key = "we_"; break;

			default:
				throw new ArgumentException("유효한 WaveMessageType 값이 아닙니다.", "type");
			}

			key += missionId + "_" + waveId;
			if (!_operatingDict.ContainsKey(key))
				return;

			OperatingStringData message = GetOperatingData(key);
			AddMessage(PriorityType.Wave, message.OperatingString, message.Voice);
		}

		private void AddMessage(Tuple<PriorityType, string, string> message) {

			AddMessage(message.Item1, message.Item2, message.Item3);
		}

		private void AddMessage(PriorityType priority, string text, string voice) {

			if (!_isStack) {

				_messageQueue.Clear();
				_isOperatingStringShow = false;
			}

			if (priority == PriorityType.Enhance_ForceField ||
				priority == PriorityType.Enhance_Optical ||
				priority == PriorityType.Enhance_Physical ||
				priority == PriorityType.Enhance_CoreTech) {

				for (int i = 0; i < _messageQueue.Count; ++i) {

					if (_messageQueue[i].Item1 == priority) {

						_messageQueue[i] = Tuple.Create(priority, text, voice);
						return;
					}
				}
			}
			else {

				for (int i = 0; i < _messageQueue.Count; ++i) {

					if (_messageQueue[i].Item1 > priority) {

						_messageQueue.Insert(i, Tuple.Create(priority, text, voice));
						return;
					}
				}
			}

			_messageQueue.Add(Tuple.Create(priority, text, voice));
		}

		private void InitPanel() {

			_portrait.sprite = _operatorPortrait;
			_nameStr = _operName;
			_name.color = _operatorColor;

			_panel.SetActive(false);
		}

		private void ShowPanel() {

			UISoundManager.Instance.Play("UI_OperatorNotice");
			//TODO 2016-10-07 HyeSung : 여기서 보이스 출력, 이전 보이스가 있으면 정지

			_isOperatingStringShow = true;

			_string.text = _messageQueue[0].Item2;

			float lifeTime = _operatorPanelLifeTime;
			string keyStr = _messageQueue[0].Item1.ToString();
			switch (keyStr) {

				case "Enhance_Physical":
					_portrait.sprite = _physicalEnhacePotrait;
					_nameStr = StringTable.Instance.GetText(keyStr);
					_name.color = _physicalColor;
					break;

				case "Enhance_CoreTech":
					_portrait.sprite = _physicalEnhacePotrait;
					_nameStr = StringTable.Instance.GetText(keyStr);
					_name.color = _coreTechColor;
					break;

				case "Enhance_Optical":
					_portrait.sprite = _opticalEnhancePotrait;
					_nameStr = StringTable.Instance.GetText(keyStr);
					_name.color = _opticalColor;
					break;

				case "Enhance_ForceField":
					_portrait.sprite = _forceFieldEnhancePotrait;
					_nameStr = StringTable.Instance.GetText(keyStr);
					_name.color = _forceFieldColor;
					break;

				case "Wave":
					int index = (UnityEngine.Random.Range(0, 10000) % 2) + 1;
					//NOTE hyesung 2018.05.14 : 보이스 제거
					//StartCoroutine(PlayVoiceCoroutine(_operatingVoices[index]));
					break;

				case "Tutorial":
					//NOTE hyesung 2018.05.14 : 보이스 제거
					//StartCoroutine(PlayVoiceCoroutine(_messageQueue[0].Item3));
					break;

				default:
					break;
			}

			float stringHeight = Mathf.Max(_string.preferredHeight + 53.0f, _portrait.rectTransform.sizeDelta.y); // 64.0f
			_panel.SetActive(true);

			Sequence portraitSeq = DOTween.Sequence().SetUpdate(true);
			portraitSeq.Append(_portrait.DOColor(Color.black, 0.08f));
			portraitSeq.Append(_portrait.DOColor(Color.white, 0.08f));
			portraitSeq.Append(_panelRectTransform.DOSizeDelta(new Vector2(_originPanelWidth, stringHeight), 0.3f).ChangeStartValue(Vector2.zero));
			portraitSeq.Insert(0.2f, _name.DOText(_nameStr, 0.2f).SetRelative().SetEase(Ease.Linear).ChangeStartValue(""));
			portraitSeq.Insert(0.2f, _string.DOText(_messageQueue[0].Item2, 0.7f).SetRelative().SetEase(Ease.Linear).ChangeStartValue(""));

			if (_messageQueue[0].Item1 == PriorityType.Durability){

				//NOTE hyesung 2018.05.14 : 보이스 제거
				//StartCoroutine(PlayVoiceCoroutine(_operatingVoices[0]));
				_panelRectTransform.DOShakePosition(1.5f, 12.0f, 23, 10.0f).SetEase(Ease.OutExpo).SetAutoKill(true);
			}

			_messageQueue.RemoveAt(0);
			TimerManager.Instance.SetTimer(ref _panelLifeTimerHandle, HidePanel, lifeTime);
		}

		private void HidePanel() {

			InitPanel();

			_isOperatingStringShow = false;
		}

		private void SetWaveVoice(string musicPath) {

			for (int i = 1; i < 4; ++i) {

				_operatingVoices[i - 1] = "Operator_" + musicPath + "_" + i.ToString();
			}
		}

		private IEnumerator PlayVoiceCoroutine(string voicePath) {

			if (_operatingAudioSource.clip != null)
				Resources.UnloadAsset(_operatingAudioSource.clip);

			if (string.IsNullOrEmpty(voicePath))
				throw new ArgumentException("빈 문자열일 수 없습니다.", "voicePath");

			var request = Resources.LoadAsync<AudioClip>(Constants.VoiceRoot + voicePath);
			yield return request;

			if (request.asset == null)
				throw new FileNotFoundException(Constants.VoiceRoot + voicePath + " 파일을 찾을 수 없습니다.");

			_operatingAudioSource.clip = request.asset as AudioClip;
			_operatingAudioSource.Play();
		}

		private OperatingStringData GetOperatingData(string key) {

			List<OperatingStringData> strList;
			if (!_operatingDict.TryGetValue(key, out strList))
				throw new ArgumentException(key + "와 일치하는 Situation 값이 없습니다.", "key");

			return _operatingDict[key][UnityEngine.Random.Range(0, strList.Count)];
		}

		private RectTransform _panelRectTransform;
		private AudioSource _operatingAudioSource;

		private Dictionary<string, List<OperatingStringData>> _operatingDict;

		private string[] _operatingVoices;
		private MissionStageController _missionStageController;
		private TutorialStageController _tutorialStageController;

		private Battleship _battleship;

		private Sprite _physicalEnhacePotrait;
		private Sprite _opticalEnhancePotrait;
		private Sprite _forceFieldEnhancePotrait;
		private Sprite _operatorPortrait;

		private readonly List<Tuple<PriorityType, string, string>> _messageQueue = new List<Tuple<PriorityType, string, string>>();

		private TimerHandle _panelLifeTimerHandle;
		private bool _isOperatingStringShow = false;

		private string _operName;
		private string _nameStr;

		private float _originPanelWidth;
	}
}
