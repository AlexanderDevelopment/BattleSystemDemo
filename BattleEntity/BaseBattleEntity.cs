using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using OpenUnitySolutions.Serialization;
using Sirenix.OdinInspector;
using Toc.Animations;
using Toc.Battle;
using Toc.Battle.Ai;
using Toc.Damage;
using Toc.PlayerProgression.Calculation;
using Toc.Scene.Animations;
using Toc.Scripts.Data.SO;
using Toc.Scripts.UI.FloatingText;
using Toc.UI.Screens;
using Toc.Utils;
using UnityEngine;
using UnityEngine.Events;
using Zenject;
using AnimationInfo = Toc.Animations.AnimationInfo;
using Random = UnityEngine.Random;


namespace Toc.PlayerProgression
{
	public abstract class BaseBattleEntity : MonoBehaviour, IBattleEntity
	{
		public abstract BattleSide Side { get; }


		protected abstract bool CanBeResurrected { get; }


		public abstract RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreBattleAction(IBattleEntityInfo player, IBattleEntityInfo opponent);


		public abstract RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreRoundAction(IBattleEntityInfo player, IBattleEntityInfo opponent);


		public abstract UniTask<RoundDiffs<SideDiffRequest, OutcomingDamage>> MakeAction(IBattleEntityInfo player, IBattleEntityInfo opponent);


		public abstract RoundDiffs<SideDiffRequest, OutcomingDamage> MakePostRoundAction(IBattleEntityInfo player, IBattleEntityInfo opponent);


		public abstract CommonNumber GetInitial(MainCharacteristic characteristic);


		public CharacteristicRouter Characteristics
			=> _characteristics;


		[Inject]
		protected readonly ScaleRouter _scales;
		
		
		[Inject]
		protected readonly AnimationRouter _animations;


		[SerializeField, AssetsOnly]
		[Inject]
		[BoxGroup(CommonGroupNames.Di)]
		protected readonly CharacteristicRouter _characteristics;


		[Inject]
		[ShowInInspector]
		[BoxGroup(CommonGroupNames.Di)]
		protected readonly GameState _state;

		[Inject]
		protected readonly BattleStats _battleStats;


		[SerializeField]
		protected AnimationOperator _animatorController;


		[SerializeField]
		protected FloatingBattleTextProvider _floatingBattleTextProvider;


		[SerializeField]
		protected List<Ability> _activeAbilities = new();


		protected List<Ability> _abilitiesToRemove = new();


		protected List<Ability> _abilitiesToAdd = new();
		
		
		[SerializeField]
		protected List<PassiveAbility> _passiveAbilities = new();


		protected int _ressurectionCounter = 0;


		public bool IsInCombat { get; private set; }


		public int FightTurns { get; protected set; }


		public List<PassiveAbility> PassiveAbilities
			=> _passiveAbilities;
		
		
		public EntityState SelfState
			=> _state[Side];


		public UnityEvent AttackEvent { get; } = new();


		public CommonNumber MaxHealth
			=> SelfState.Scales[_scales.Health].MaxValue;


		public CommonNumber CurrentHealth
			=> SelfState.Scales[_scales.Health].Value;


		public bool IsDead
			=> SelfState.Scales[_scales.Health].IsMin;


		public bool IsAlive
			=> !IsDead;


		public virtual void OnFightStart()
		{
			IsInCombat = true;
			EnableFloatingBattleText();
			FightTurns = 0;
			_animatorController.SetCombat(true);
			_ressurectionCounter = 0;
		}


		public virtual void OnFightEnd()
		{
			FightTurns = 0;
			IsInCombat = false;
			DisableFloatingBattleText();
			_animatorController.SetCombat(false);
		}


		public virtual void OnRoundStart()
		{
		}


		public virtual void OnRoundEnd()
		{
			FightTurns++;
		}


		protected virtual void OnResurrection()
		{
		}


		public async UniTask PlayAnimation(TriggerAnimationInfo animationInfo)
			=> await _animatorController.PlayTrigger(animationInfo);


		private void EnableFloatingBattleText()
			=> SelfState.Scales[_scales.Health].HemorrhageTick.AddListener(_floatingBattleTextProvider.SpawnFloatingText);


		private void DisableFloatingBattleText()
			=> SelfState.Scales[_scales.Health].HemorrhageTick.RemoveListener(_floatingBattleTextProvider.SpawnFloatingText);


		public void InvokeAttackEvent()
			=> AttackEvent.Invoke();


		public virtual RoundDiffs<SideDiffRequest, OutcomingDamage> MakeAutoAttack(IBattleEntityInfo player, IBattleEntityInfo opponent)
		{
			var selfResults = new SideDiffRequest
			{
				SourceSide = Side,
				TargetSide = Side,
				IsAutoAttack = true,
				Damage = OutcomingDamage.CreateEmpty(DamageType.None),
			};

			var otherResult = new SideDiffRequest
			{
				SourceSide = Side,
				TargetSide = Side.Inverse(),
				IsAutoAttack = true,
				Damage = new OutcomingDamage(_battleStats.AutoAttackDamage, Side, _state),
			};
			
			selfResults.CalculateDeltaHealthDiffForTarget(Side, _state, _scales);
			otherResult.CalculateDeltaHealthDiffForTarget(Side.Inverse(), _state, _scales);

			if (Side == BattleSide.Player)
				return new RoundDiffs<SideDiffRequest, OutcomingDamage>(selfResults, otherResult);
			
			return new RoundDiffs<SideDiffRequest, OutcomingDamage>(otherResult, selfResults);
		}
		

		protected virtual void TakeDamage(IncomingDamage damage)
		{
			if (damage.HandledDamage == 0)
				return;

			SelfState.Scales[_scales.Health].Value -= damage.HandledDamage;

			if (damage.IsCritical)
				Debug.Log($"В {name} прилетел критический урон");
		}


		public virtual void Heal(CommonNumber amount)
		{
			if (amount <= 0)
				return;

			SelfState.Scales[_scales.Health].Value += amount;
			Debug.Log($"{name} восстановил {amount} единиц здоровья");
		}


		protected RoundDiffs<SideDiffRequest, OutcomingDamage> MakeEmptyAction(BattleSide source)
		{
			var playerResults = new SideDiffRequest
			{
				SourceSide = source,
				TargetSide = BattleSide.Player,
			};

			var opponentResults = new SideDiffRequest
			{
				SourceSide = source,
				TargetSide = BattleSide.Opponent,
			};

			return new RoundDiffs<SideDiffRequest, OutcomingDamage>(playerResults, opponentResults);
		}


		private DodgeResult CalculateDodge(CommonNumber successfulDodgeMultiplier)
		{
			var roll = (CommonNumber)Random.Range(0, 100);
			var dodge = _characteristics.DodgeChance.EvaluateForSide(Side, _state);

			if (roll > dodge)
				return DodgeResult.Failed();

			return DodgeResult.Successful(successfulDodgeMultiplier);
		}


		public virtual SideDiffResult RecalculateRequestedDiff(SideDiffRequest diff)
		{
			if (diff.Damage == null)
				diff.Damage = OutcomingDamage.CreateEmpty(DamageType.None);

			DodgeResult dodge;

			if (diff.Damage.DamageType != DamageType.None && diff.Damage.VariantedValue != 0)
			{
				var successfulDodgeDamageMultiplier = (CommonNumber) 0;
				dodge = CalculateDodge(successfulDodgeDamageMultiplier);

				Debug.Log($"Уворот: {dodge.IsSuccessfullyDodged}, dmg *= {dodge.Multiplier}");
			}
			else
				dodge = DodgeResult.NotTried();

			// TODO обрабатывать Pure-урон и Default-урон по-разному

			var damage = new IncomingDamage(diff.Damage, dodge);
			var heal = diff.Heal;

			bool isKilled = CurrentHealth + heal - damage.HandledDamage <= 0;

			if (isKilled && CanBeResurrected)
				foreach (var passiveAbility in PassiveAbilities)
					if (passiveAbility is ForsakenResurrection resurrection)
					{
						heal += MaxHealth * resurrection.GetRecoveryHealthPercent(_ressurectionCounter);
						isKilled = false;

						// TODO анимация воскрешения
						OnResurrection();
					}



			if (diff.Damage.IsCritical)
				diff.Deltas.Add(diff.TargetSide, _scales.Health, _battleStats.HemmorhageDelta);


			var result = new SideDiffResult()
			{
				SourceSide = diff.SourceSide,
				TargetSide = diff.TargetSide,
				Damage = damage,
				Heal = heal,
				IsKilled = isKilled,
				Deltas = diff.Deltas,
				PassiveDeltas = diff.PassiveDeltas,
				ItemsToRemove = diff.ItemsToRemove,
			};

			if (result.IsOnActiveSide && diff.IsAutoAttack)
				result.Animations.Add(_animations.Attack);
			
			if (result.IsOnActiveSide && diff.ActivatedAbilities != null)
				foreach (var ability in diff.ActivatedAbilities)
					result.Animations.Add(ability.TriggerAnimation);
			else
			{
				if (result.Damage.Dodge.IsSuccessfullyDodged)
					result.Animations.Add(_animations.Dodge);

				if (!result.IsKilled && result.Heal > 0)
					result.Animations.Add(_animations.Heal);
			}


			if (result.IsKilled)
			{
				Debug.Log($"{name} был убит...");
				
				if (SelfState.ResurrectionTimes > 0)
				{
					SelfState.ResurrectionTimes--;
					SelfState.Scales[_scales.Health].Value = (CommonNumber)1;
					result.Damage = IncomingDamage.CreateEmpty();
					result.IsKilled = false;
					result.Animations.Add(_animations.Death);
					result.Animations.Add(_animations.GetUp);
					
					Debug.Log("... но ожил");
				}
				else
					result.Animations.Add(_animations.Death);
				
			}

			return result;
		}


		public void ApplyDiffToState(SideDiffResult diff)
		{
			if (diff.Deltas != null)
			{
				if (diff.Deltas.CharacteristicDeltas != null)
					SelfState.Characteristics.ApplyDeltas(diff.SourceSide, diff.Deltas.CharacteristicDeltas);

				if (diff.Deltas.RecurrentScaleDeltas != null)
					SelfState.ApplyRecurrentDeltas(diff.SourceSide, diff.Deltas.RecurrentScaleDeltas);
			}

			Heal(diff.Heal);
			TakeDamage(diff.Damage);

			if (diff.ItemsToRemove != null)
				RemoveItemsFromInventory(diff.ItemsToRemove);

			if (diff.Damage.HandledDamage != 0)
				_floatingBattleTextProvider.SpawnFloatingText(diff.Damage.HandledDamage, diff.Damage.IsCritical);
		}


		private void RemoveItemsFromInventory(List<Item> items)
		{
			foreach (var item in items)
			{
				if (item != null)
					_state[Side].Inventory.RemoveItem(item);
			}
		}
	}
}
