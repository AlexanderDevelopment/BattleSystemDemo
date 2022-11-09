using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Toc.Battle.Ai;
using UnityEngine;
using BehaviorDesigner.Runtime;
using Toc.Battle;
using Toc.Damage;
using Toc.PlayerProgression.Calculation;
using UnityEngine.Serialization;


namespace Toc.PlayerProgression
{
	public class MonsterBattleEntity : BaseBattleEntity
	{
		[SerializeField, ChildGameObjectsOnly, Required]
		private BehaviorTree _tree;


		[FormerlySerializedAs("_monsterTreeDecisionsHolder")]
		[SerializeField, ChildGameObjectsOnly, Required]
		private DecisionTreeProxy _treeProxy;


		[SerializeField, AssetsOnly, Required]
		private EnemyData _sourceEnemy;


		[SerializeField, SceneObjectsOnly, Required]
		private BehaviorTree _behaviourTree;


		//TODO Use MMFeedbacks if need advanced functional
		[SerializeField]
		private ParticleSystem _fog;


		public override BattleSide Side
			=> BattleSide.Opponent;
		
		
		protected override bool CanBeResurrected
			=> false;
		

		public Ability BasicAttackAbility
			=> _sourceEnemy.BasicAttackAbility;


		public EnemyRace Race
			=> _sourceEnemy.EnemyRace;


		public EnemyData Source
			=> _sourceEnemy;


		public override CommonNumber GetInitial(MainCharacteristic characteristic)
			=> _sourceEnemy.GetInitial(characteristic);


		public void Awake()
		{
			_behaviourTree.ExternalBehavior = _sourceEnemy.ExternalBehaviorTree;
		}


		private SideDeltaSet GetSideDeltaSet(DecisionTreeProxy result, TargetSide side)
		{
			SideDeltaSet sideDeltaSet = new SideDeltaSet();

			foreach (var ability in result.ActivatedAbilities)
			{
				if (ability == null)
					continue;

				if (ability.Deltas == null)
					continue;

				if (ability.Deltas[side] == null)
					continue;

				if (ability.Deltas[side].CharacteristicDeltas != null && ability.Deltas[side].CharacteristicDeltas.Count > 0)
					foreach (var delta in ability.Deltas[side].CharacteristicDeltas)
						sideDeltaSet.Add(BattleSide.Opponent, delta.Key, delta.Value);

				if (ability.Deltas[side].ScaleDeltas != null && ability.Deltas[side].ScaleDeltas.Count > 0)
					foreach (var delta in ability.Deltas[side].ScaleDeltas)
						sideDeltaSet.Add(BattleSide.Opponent, delta.Key, delta.Value);

				if (ability.Deltas[side].RecurrentScaleDeltas != null && ability.Deltas[side].RecurrentScaleDeltas.Count > 0)
					foreach (var delta in ability.Deltas[side].RecurrentScaleDeltas)
						sideDeltaSet.Add(BattleSide.Opponent, delta.Key, delta.Value);
			}

			return sideDeltaSet;
		}

		public override RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreBattleAction(IBattleEntityInfo player, IBattleEntityInfo self)
			=> MakeEmptyAction(Side);
		
		
		public override RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreRoundAction(IBattleEntityInfo player, IBattleEntityInfo self)
			=> MakeEmptyAction(Side);
		

		public override async UniTask<RoundDiffs<SideDiffRequest, OutcomingDamage>> MakeAction(IBattleEntityInfo player, IBattleEntityInfo self)
		{
			Debug.Log("Противник думает...");

			var result = await GetTreeResults();

			OutcomingDamage damageToPlayer = null;
			foreach (var ability in result.ActivatedAbilities)
			{
				var deltas = ability.Deltas[TargetSide.Other];
				if (deltas == null)
					continue;
				
				if (deltas.Damage == null)
					continue;

				if (damageToPlayer == null)
					damageToPlayer = new OutcomingDamage(deltas.Damage, BattleSide.Opponent, _state);
				else
					damageToPlayer += deltas.Damage.EvaluateUnvariantedDamage(BattleSide.Opponent, _state);
			}
			
			if (damageToPlayer == null)
				damageToPlayer = OutcomingDamage.CreateEmpty(DamageType.None);
			
			// TODO считать урон новым способом
			
			var selfResults = new SideDiffRequest
			{
				SourceSide = BattleSide.Opponent,
				TargetSide = BattleSide.Opponent,
				Damage = OutcomingDamage.CreateEmpty(DamageType.None),
				ActivatedAbilities = result.ActivatedAbilities,
				Deltas = GetSideDeltaSet(result, TargetSide.Self),
				ItemsToRemove = _treeProxy.MonsterItemsToRemove,
			};

			var playerResults = new SideDiffRequest
			{
				SourceSide = BattleSide.Opponent,
				TargetSide = BattleSide.Player,
				Damage = damageToPlayer,
				Deltas = GetSideDeltaSet(result, TargetSide.Other),
				ItemsToRemove = _treeProxy.PlayerItemsToRemove,
			};
			
			selfResults.CalculateDeltaHealthDiffForTarget(BattleSide.Opponent, _state, _scales);
			playerResults.CalculateDeltaHealthDiffForTarget(BattleSide.Player, _state, _scales);
			

			return new RoundDiffs<SideDiffRequest, OutcomingDamage>(playerResults, selfResults);
		}

		public override RoundDiffs<SideDiffRequest, OutcomingDamage> MakePostRoundAction(IBattleEntityInfo player, IBattleEntityInfo self)
			=> MakeEmptyAction(Side);


		// TODO В идеале возвращать не проксю, а DTO: покся теперь двухсторонняя
		private async UniTask<DecisionTreeProxy> GetTreeResults()
		{
			_tree.EnableBehavior();

			await UniTask.WaitForFixedUpdate();

			return _treeProxy;
		}


		public override void OnFightStart()
		{
			base.OnFightStart();
			
			SelfState.ResurrectionTimes = _sourceEnemy.EnemyRace.ResurrectionTimes;
			_fog.Stop();
		}


		public override void OnRoundEnd()
		{
			base.OnRoundEnd();
			_treeProxy.ResetDecision();
		}


		public void SetSource(EnemyData source)
			=> _sourceEnemy = source;
	}
}
