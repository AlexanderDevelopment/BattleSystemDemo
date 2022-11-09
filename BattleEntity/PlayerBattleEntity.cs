using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OpenUnitySolutions.ScreenSystem;
using OpenUnitySolutions.Serialization;
using OpenUnitySolutions.UniTaskExtensions;
using Sirenix.OdinInspector;
using Toc.Battle;
using Toc.Battle.Ai;
using Toc.Damage;
using Toc.Extensions;
using Toc.PlayerProgression.Calculation;
using Toc.Scene;
using Toc.Screens.Enums;
using Toc.UI.Screens;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;


namespace Toc.PlayerProgression
{
	// TODO привести к работе с CommonNumber
	[Serializable]
	public class PlayerBattleEntity : BaseBattleEntity
	{
		[Inject]
		private readonly ScreenManager _screenManager;


		[SerializeField, AssetsOnly, Required]
		private PlayerClassesLeveling _playerClassesLeveling;


		[SerializeField]
		private PlayerClassTier _canSelectTier;


		[SerializeField, AssetsOnly, Required]
		private UnityDictionary<BattleScreenPlayerAction, Ability> _basicAbilities = new();


		public UnityDictionary<BattleScreenPlayerAction, int> ActivatedAbilities = new();


		private BattleScreen _battleScreen;


		[SerializeField, AssetsOnly, Required]
		private LocalizationStrings _localizationStrings;


		public override BattleSide Side
			=> BattleSide.Player;


		public PlayerClassTier CanSelectTier
			=> _canSelectTier;


		protected override bool CanBeResurrected
			=> _ressurectionCounter < 2;


		public override CommonNumber GetInitial(MainCharacteristic characteristic)
			=> characteristic.InitialValue;


		[Button]
		private void CheckPlayerClassUpdate()
		{
			bool needSelectClass = false;

			foreach (var leveling in _playerClassesLeveling.Leveling)
				if (Characteristics.Level.InitialValue >= leveling.Value)
					if (_canSelectTier != leveling.Key)
						_canSelectTier = leveling.Key;

			if (_canSelectTier != PlayerClassTier.None)
			{
				if (SelfState.PlayerClass == null)
					needSelectClass = true;
				else
				{
					if (SelfState.PlayerClass.Tier != _canSelectTier)
						needSelectClass = true;
				}
			}

			if (needSelectClass)
				ChoosePlayerClass();
		}


		public void Start()
		{
			Characteristics.Level.OnValueChanged.AddListener(CheckPlayerClassUpdate);
			TickActiveAbilities().ForgetWithHandler();
		}


		private async UniTask TickActiveAbilities()
		{
			while (true)
			{
				for (int i = 0; i < _activeAbilities.Count; i++)
				{
					if (_activeAbilities[i] == null)
						continue;

					if (!_activeAbilities[i].ReplaceAbility)
						continue;

					if (Characteristics.Level.InitialValue >= _activeAbilities[i].MinLevelToReplace)
					{
						_abilitiesToAdd.Add(_activeAbilities[i].ReplacedAbility);
						_abilitiesToRemove.Add(_activeAbilities[i]);
					}
				}
				
				for (int i = 0; i < _activeAbilities.Count; i++)
				{
					if (_activeAbilities[i] == null)
						continue;

					if (!_activeAbilities[i].RealTime)
						continue;

					if (!_activeAbilities[i].Activated)
						_activeAbilities[i].Activate();
					else
					{
						if (_activeAbilities[i].TimeSpent())
							_abilitiesToRemove.Add(_activeAbilities[i]);
					}
				}

				foreach (var ability in _abilitiesToRemove)
					_activeAbilities.Remove(ability);

				foreach (var ability in _abilitiesToAdd)
					_activeAbilities.Add(ability);

				if (_abilitiesToRemove.Count > 0)
					_abilitiesToRemove.Clear();

				if (_abilitiesToAdd.Count > 0)
					_abilitiesToAdd.Clear();

				foreach (var ability in _activeAbilities)
				{
					if (ability == null)
						continue;

					if (!ability.Activated)
						ability.Activate();
					else
						ability.TimeSpent();
				}

				await UniTask.Delay(1000);
			}
		}


		[Button]
		public async UniTask ChoosePlayerClass()
		{
			var buttons = new Dictionary<DialogAction, string>
			{
				{ DialogAction.Ok, "Ok" },
			};

			DialogAction informationAction = await _screenManager.ShowDialogScreen(_localizationStrings.Strings[0].Value, buttons);

			PlayerClassHierarchyScreen playerClassHierarchyScreen = null;
			PlayerClass selectedPlayerClass = null;

			if (informationAction == DialogAction.Ok)
			{
				while (selectedPlayerClass == null)
				{
					playerClassHierarchyScreen = await _screenManager.ShowScreen<PlayerClassHierarchyScreen>();
					playerClassHierarchyScreen.CanSelectTier = _canSelectTier;
					playerClassHierarchyScreen.CurrentPlayerClass = SelfState.PlayerClass;
					playerClassHierarchyScreen.HighlightDrawedBlocks();

					selectedPlayerClass = await playerClassHierarchyScreen.WaitAction();

					await _screenManager.HideScreen<PlayerClassHierarchyScreen>();

					var chooseActionButtons = new Dictionary<DialogAction, string>
					{
						{ DialogAction.Ok, "Ok" },
						{ DialogAction.Cancel, "Cancel" },
					};

					DialogAction chooseClassAction = await _screenManager.ShowDialogScreen($"{_localizationStrings.Strings[1].Value} \"{selectedPlayerClass.Name.Value}\"?", chooseActionButtons);

					if (chooseClassAction == DialogAction.Ok)
					{
						DialogAction choosedClassInformationAction = await _screenManager.ShowDialogScreen($"{_localizationStrings.Strings[2].Value} \"{selectedPlayerClass.Name.Value}\"", buttons);

						if (choosedClassInformationAction == DialogAction.Ok)
							SelfState.SetPlayerClass(selectedPlayerClass);
					}
					else if (chooseClassAction == DialogAction.Cancel)
						selectedPlayerClass = null;
				}
			}
		}


		public override RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreBattleAction(IBattleEntityInfo self, IBattleEntityInfo opponent)
		{
			var selfDiffsRequest = new SideDiffRequest
			{
				SourceSide = BattleSide.Player,
				TargetSide = BattleSide.Player,
				Damage = OutcomingDamage.CreateEmpty(DamageType.None),
			};


			var opponentDiffsRequest = new SideDiffRequest
			{
				SourceSide = BattleSide.Player,
				TargetSide = BattleSide.Opponent,
				Damage = OutcomingDamage.CreateEmpty(DamageType.Pure),
			};

			foreach (var passiveAbility in PassiveAbilities)
				if (passiveAbility is CrusaderHealthPercentageDamageByRace ability)
					opponentDiffsRequest.Damage += UseCrusaderRaceDamage(opponent, ability);

			return new RoundDiffs<SideDiffRequest, OutcomingDamage>(selfDiffsRequest, opponentDiffsRequest);
		}


		public override RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreRoundAction(IBattleEntityInfo self, IBattleEntityInfo opponent)
		{
			var selfDiffsRequest = new SideDiffRequest
			{
				SourceSide = BattleSide.Player,
				TargetSide = BattleSide.Player,
				Damage = OutcomingDamage.CreateEmpty(DamageType.None),
			};


			var opponentDiffsRequest = new SideDiffRequest
			{
				SourceSide = BattleSide.Player,
				TargetSide = BattleSide.Opponent,
				Damage = OutcomingDamage.CreateEmpty(DamageType.Pure),
			};

			foreach (var passiveAbility in PassiveAbilities)
				if (passiveAbility is CrusaderHealthPercentageDamageByRace ability)
					opponentDiffsRequest.Damage += UseCrusaderRaceDamage(opponent, ability);

			return new RoundDiffs<SideDiffRequest, OutcomingDamage>(selfDiffsRequest, opponentDiffsRequest);
		}


		private CommonNumber UseCrusaderRaceDamage(IBattleEntityInfo opponent, CrusaderHealthPercentageDamageByRace ability)
		{
			if (opponent is not MonsterBattleEntity monster)
				return (CommonNumber)0;

			if (!ability.EnemyRaces.Contains(monster.Race))
				return (CommonNumber)0;

			var result = opponent.MaxHealth * ability.DamagePercent;

			if (ability.WorkOneTime)
				PassiveAbilities.Remove(ability);

			return result;
		}


		public override async UniTask<RoundDiffs<SideDiffRequest, OutcomingDamage>> MakeAction(IBattleEntityInfo self, IBattleEntityInfo opponent)
		{
			var playerAction = await _battleScreen.GetPlayerAction();
			var playerAbility = _basicAbilities[playerAction];

			// FIXME паверап захардкожен

			if (ActivatedAbilities.ContainsKey(BattleScreenPlayerAction.PowerUp))
			{
				if (ActivatedAbilities[BattleScreenPlayerAction.PowerUp] > 1)
					ActivatedAbilities[BattleScreenPlayerAction.PowerUp]--;
				else
				{
					ActivatedAbilities.Remove(BattleScreenPlayerAction.PowerUp);
					_battleScreen.EnablePowerUpAbility();
				}
			}

			if (playerAction == BattleScreenPlayerAction.PowerUp)
			{
				// FIXME тут идет подсчет длительности паверапа по дельте, это неправильно
				var abilitySoDeltas = playerAbility.Deltas[TargetSide.Other].CharacteristicDeltas;

				if (abilitySoDeltas != null)
					foreach (var abilitySoDelta in abilitySoDeltas)
						if (!ActivatedAbilities.ContainsKey(playerAction))
							ActivatedAbilities.Add(playerAction, abilitySoDelta.Value.RoundsBeforeEnd);
						else
							ActivatedAbilities[BattleScreenPlayerAction.PowerUp] = abilitySoDelta.Value.RoundsBeforeEnd;

				_battleScreen.DisablePowerUpAbility();
			}

			_battleScreen.HideButtons();

			var selfPassiveDeltas = new SideDeltaSet();
			var otherPassiveDeltas = new SideDeltaSet();

			if (playerAction == BattleScreenPlayerAction.EscapeFromBattle)
			{
				var selfDiffsRequest = new SideDiffRequest
				{
					SourceSide = BattleSide.Player,
					IsEscaped = RollEscape(_characteristics.EscapeFromBattleChance.EvaluateForSide(BattleSide.Player, _state)),
					TargetSide = BattleSide.Player,
					Damage = OutcomingDamage.CreateEmpty(DamageType.None),
				};

				var opponentDiffsRequest = new SideDiffRequest
				{
					SourceSide = BattleSide.Player,
					TargetSide = BattleSide.Opponent,
					Damage = OutcomingDamage.CreateEmpty(DamageType.None),
				};

				return new RoundDiffs<SideDiffRequest, OutcomingDamage>(selfDiffsRequest, opponentDiffsRequest);
			}
			else
			{
				foreach (var passiveAbility in PassiveAbilities.Where(passiveAbility => passiveAbility != null))
				{
					if (passiveAbility is not CharacteristicPassiveAbility characteristicPassiveAbility)
						continue;

					selfPassiveDeltas.AddRangeForCreator(BattleSide.Player, characteristicPassiveAbility.Deltas[TargetSide.Self]);
					otherPassiveDeltas.AddRangeForCreator(BattleSide.Player, characteristicPassiveAbility.Deltas[TargetSide.Other]);
				}

				var selfDiffsRequest = new SideDiffRequest
				{
					SourceSide = BattleSide.Player,
					TargetSide = BattleSide.Player,
					ActivatedAbilities = new List<Ability> { playerAbility },
					Damage = OutcomingDamage.CreateEmpty(DamageType.None),
					Deltas = playerAbility.Deltas[TargetSide.Self].GetCopyForCreator(BattleSide.Player),
					PassiveDeltas = selfPassiveDeltas,
				};

				var opponentDiffsRequest = new SideDiffRequest
				{
					SourceSide = BattleSide.Player,
					TargetSide = BattleSide.Opponent,
					Damage = new OutcomingDamage(playerAbility.Deltas[TargetSide.Other].Damage, BattleSide.Player, _state),
					Deltas = playerAbility.Deltas[TargetSide.Other].GetCopyForCreator(BattleSide.Player),
					PassiveDeltas = otherPassiveDeltas,
				};

				selfDiffsRequest.CalculateDeltaHealthDiffForTarget(BattleSide.Player, _state, _scales);
				opponentDiffsRequest.CalculateDeltaHealthDiffForTarget(BattleSide.Opponent, _state, _scales);

				return new RoundDiffs<SideDiffRequest, OutcomingDamage>(selfDiffsRequest, opponentDiffsRequest);
			}
		}


		private void ApplyPassiveAbilitiesToActionDiffRequest(SideDiffRequest playerDiff, SideDiffRequest opponentDiff)
		{
			foreach (var passiveAbility in _passiveAbilities)
			{
				opponentDiff.Damage.UnvariantedValue = passiveAbility.CalculateAdditionalDamageSelf(_state[BattleSide.Player].Scales[_scales.Health], opponentDiff.Damage.UnvariantedValue); // Berserker

				opponentDiff.Damage.UnvariantedValue =
					passiveAbility.CalculateAdditionalDamageOther(_state[BattleSide.Opponent].Scales[_scales.Health], opponentDiff.Damage.UnvariantedValue); // Ripper, DarkBlade

				opponentDiff.Damage.UnvariantedValue = passiveAbility.CalculateDoubleDamage(opponentDiff.Damage.UnvariantedValue); // RoyalAssassin

				opponentDiff.Damage += passiveAbility.CalculateReflectAttack(playerDiff.Damage.UnvariantedValue); // Paladin

				playerDiff.Heal += passiveAbility.CalculateAdditionalHeal(opponentDiff.Damage.UnvariantedValue); // Bloodsucker
			}
		}


		public override RoundDiffs<SideDiffRequest, OutcomingDamage> MakePostRoundAction(IBattleEntityInfo self, IBattleEntityInfo opponent)
		{
			var selfDiffsRequest = new SideDiffRequest
			{
				SourceSide = BattleSide.Player,
				TargetSide = BattleSide.Player,
				Damage = OutcomingDamage.CreateEmpty(DamageType.None),
			};

			var opponentDiffsRequest = new SideDiffRequest
			{
				SourceSide = BattleSide.Player,
				TargetSide = BattleSide.Opponent,
				Damage = OutcomingDamage.CreateEmpty(DamageType.None),
			};


			foreach (var passiveAbility in PassiveAbilities)
				opponentDiffsRequest.Damage += passiveAbility.CalculateAdditionalDamageRoundEnd(_state[BattleSide.Opponent].Scales[_scales.Health]); // DarkLord

			return new RoundDiffs<SideDiffRequest, OutcomingDamage>(selfDiffsRequest, opponentDiffsRequest);
		}


		protected override void OnResurrection()
		{
			if (_ressurectionCounter < 2)
				_ressurectionCounter++;
			else
				throw new Exception("Can't resurrect third time");
		}


		public override void OnRoundStart()
		{
			_battleScreen = _screenManager.GetScreen<BattleScreen>();
			_battleScreen.ShowButtons();
		}



		public bool RollAvoid(CommonNumber avoidanceChance)
		{
			var roll = Random.Range(0, 100);
			
			#if UNITY_EDITOR
			if (CheatMenu.AlwaysAvoid)
				avoidanceChance = (CommonNumber) 100;
			#endif

			return roll <= avoidanceChance;
		}


		public bool RollEscape(CommonNumber escapeChance)
		{
			var roll = Random.Range(0, 100);
			
			#if UNITY_EDITOR
			if (CheatMenu.AlwaysEscape)
				escapeChance = (CommonNumber) 100;
			#endif

			if (roll >= escapeChance)
				Debug.Log("Escape is failed");

			return roll <= escapeChance;
		}
	}
}
