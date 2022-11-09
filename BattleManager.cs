using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using OpenUnitySolutions.ScreenSystem;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Toc.Animations;
using Toc.Damage;
using Toc.PlayerProgression;
using Toc.Scene;
using Toc.Scene.PlayerLogic;
using Toc.UI.Screens;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Zenject;


namespace Toc.Battle.Ai
{
	public class BattleManager : SerializedMonoBehaviour
	{
		[Inject]
		private readonly ScreenManager _screenManager;


		[Inject]
		private readonly GameState _state;


		[Inject]
		private readonly Player _player;


		[Inject]
		private readonly AnimationRouter _animations;


		[OdinSerialize, SceneObjectsOnly]
		private Dictionary<BattleSide, IBattleEntity> _battlers;


		private BattleScreen _battleScreen;


		public readonly List<RoundDiffs<SideDiffResult, IncomingDamage>> DiffLogs = new();


		public readonly UnityEvent OnRoundEnd = new();


		[Button]
		public void SetupBattlers(IBattleEntity player, IBattleEntity opponent)
		{
			_battlers = new Dictionary<BattleSide, IBattleEntity>
			{
				{ BattleSide.Player, player },
				{ BattleSide.Opponent, opponent },
			};

			_state.SetupOpponent(opponent);
		}


		[Button]
		public async UniTask<RoundResult> Fight(IBattleEntity player, IBattleEntity opponent)
		{
			await _screenManager.HideScreen<PlayerScreen>();
			DiffLogs.Clear();

			//lisen sides events
			player.AttackEvent.AddListener(() => opponent.PlayAnimation(_animations.GetHit));
			opponent.AttackEvent.AddListener(() => player.PlayAnimation(_animations.GetHit));

			Debug.Log("===[ Начинается сражение ]===");

			_battleScreen = await _screenManager.ShowScreen<BattleScreen>();

			_battlers[BattleSide.Player].OnFightStart();
			_battlers[BattleSide.Opponent].OnFightStart();

			await DoStageActionAsync(BattleStageAction.PreBattleAction, BattleSide.Player);
			await DoStageActionAsync(BattleStageAction.PreBattleAction, BattleSide.Opponent);


			var lastRoundResult = RoundResult.None;
			while (lastRoundResult == RoundResult.None)
			{
				_battlers[BattleSide.Player].OnRoundStart();
				_battlers[BattleSide.Opponent].OnRoundStart();
				
				#if UNITY_EDITOR
				if (CheatMenu.PlayerAutoWinFight)
				{
					_battleScreen.HideButtons();
					lastRoundResult = RoundResult.PlayerWin;
				}
				else if (CheatMenu.EnemyAutoWinFight)
				{
					_battleScreen.HideButtons();
					lastRoundResult = RoundResult.OpponentWin;
				}
				else
					#endif
					lastRoundResult = await HandleNextRound();
				
				OnRoundEnd.Invoke();
				_battlers[BattleSide.Player].OnRoundEnd();
				_battlers[BattleSide.Opponent].OnRoundEnd();
			}
			
			if (lastRoundResult == RoundResult.PlayerWin)
				await HandleBattleResult<BattleScreenWin>();
			else if (lastRoundResult == RoundResult.OpponentWin)
            {
				await HandleBattleResult<BattleScreenLose>();
                var scene = SceneManager.GetActiveScene();
				SceneManager.LoadScene(scene.name);
            }
			else if (lastRoundResult == RoundResult.PlayerEscape)
			{
				await HandleBattleResult<BattleScreenEscaped>();
				_player.PlayerEscapeFromBattle.Invoke();
			}
			

			Debug.Log($"Результат сражения: [{lastRoundResult}]");

			await _screenManager.HideScreen<BattleScreen>();
			await _screenManager.ShowScreen<PlayerScreen>();
			_battlers = null;
			_state.ClearOpponent();

			return lastRoundResult;
		}


		private async UniTask<RoundResult> HandleNextRound()
		{
			_battleScreen.UpdateHealth(_battlers);


			Debug.Log("===[ 1. Пассивные способности перед раундом ]===");
			
			var result = await DoStageActionAsync(BattleStageAction.PreRoundAction, BattleSide.Player);
			if (result != RoundResult.None)
				return result;
			
			result = await DoStageActionAsync(BattleStageAction.PreRoundAction, BattleSide.Opponent);
			if (result != RoundResult.None)
				return result;


			Debug.Log("===[ 2. Основное действие игрока ]===");
			
			result = await DoStageActionAsync(BattleStageAction.MainAsyncAction, BattleSide.Player);
			if (result != RoundResult.None)
				return result;


			Debug.Log("===[ 3. Основное действие противника ]===");
			
			result = await DoStageActionAsync(BattleStageAction.MainAsyncAction, BattleSide.Opponent);
			if (result != RoundResult.None)
				return result;

			await UniTask.Delay(200, ignoreTimeScale: false);

			Debug.Log("===[ 4. Авто-атака противника ]===");
			
			result = await DoStageActionAsync(BattleStageAction.AutoAttack, BattleSide.Opponent);
			if (result != RoundResult.None)
				return result;	
			
			
			Debug.Log("===[ 5. Авто-атака игрока ]===");
			
			result = await DoStageActionAsync(BattleStageAction.AutoAttack, BattleSide.Player);
			if (result != RoundResult.None)
				return result;
			

			Debug.Log("===[ 6. Переодические эффекты, пассивные способности после раунда ]===");
			
			result = await DoStageActionAsync(BattleStageAction.PostRoundAction, BattleSide.Opponent);
			if (result != RoundResult.None)
				return result;
			
			result = await DoStageActionAsync(BattleStageAction.PostRoundAction, BattleSide.Player);
			if (result != RoundResult.None)
				return result;

			await ApplyDeltas();

			return GetRoundResult(false);
		}


		private async UniTask ApplyDeltas()
		{
			_state.TickDeltaSteps();

			var playerAdditionalAnimations = new List<TriggerAnimationInfo>();
			var opponentAdditionalAnimations = new List<TriggerAnimationInfo>();
			
			if (_battlers[BattleSide.Player].IsDead)
				playerAdditionalAnimations.Add(_animations.Death);
			
			if (_battlers[BattleSide.Opponent].IsDead)
				opponentAdditionalAnimations.Add(_animations.Death);

			await PlayAnimations(playerAdditionalAnimations, opponentAdditionalAnimations);

		}


		private async UniTask HandleDiffs(RoundDiffs<SideDiffRequest, OutcomingDamage> diffs)
		{
			var playerDiffs = RecalculateRequestedDiff(diffs, BattleSide.Player);
			var opponentDiffs = RecalculateRequestedDiff(diffs, BattleSide.Opponent);
			ApplyDiffs(playerDiffs, opponentDiffs);

			await PlayAnimations(playerDiffs.Animations, opponentDiffs.Animations);
		}


		private SideDiffResult RecalculateRequestedDiff(RoundDiffs<SideDiffRequest, OutcomingDamage> diffRequest, BattleSide side)
			=> _battlers[side].RecalculateRequestedDiff(diffRequest[side]);


		private async UniTask<RoundResult> DoStageActionAsync(BattleStageAction stage, BattleSide side)
		{
			var actionDiffs = stage switch
			{
				BattleStageAction.PreBattleAction => GetPreBattleActionsFromSide(side),
				BattleStageAction.PreRoundAction => GetPreRoundActionsFromSide(side),
				BattleStageAction.MainAsyncAction => await GetActionsFromSide(side),
				BattleStageAction.AutoAttack => GetAutoAttackFromSide(side),
				BattleStageAction.PostRoundAction => GetPostRoundActionsFromSide(side),
				_ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
			};
			
			await HandleDiffs(actionDiffs);

			return GetRoundResult(actionDiffs[BattleSide.Player].IsEscaped);
		}

		
		private RoundDiffs<SideDiffRequest, OutcomingDamage> GetPreBattleActionsFromSide(BattleSide side)
			=> _battlers[side].MakePreBattleAction(_battlers[BattleSide.Player], _battlers[BattleSide.Opponent]);

		
		private RoundDiffs<SideDiffRequest, OutcomingDamage> GetPreRoundActionsFromSide(BattleSide side)
			=> _battlers[side].MakePreRoundAction(_battlers[BattleSide.Player], _battlers[BattleSide.Opponent]);

		
		private async UniTask<RoundDiffs<SideDiffRequest, OutcomingDamage>> GetActionsFromSide(BattleSide side)
			=> await _battlers[side].MakeAction(_battlers[BattleSide.Player], _battlers[BattleSide.Opponent]);

		
		private RoundDiffs<SideDiffRequest, OutcomingDamage> GetAutoAttackFromSide(BattleSide side)
			=> _battlers[side].MakeAutoAttack(_battlers[BattleSide.Player], _battlers[BattleSide.Opponent]);
		

		private RoundDiffs<SideDiffRequest, OutcomingDamage> GetPostRoundActionsFromSide(BattleSide side)
			=> _battlers[side].MakePostRoundAction(_battlers[BattleSide.Player], _battlers[BattleSide.Opponent]);

		
		private void ApplyDiffs(SideDiffResult playerDiff, SideDiffResult opponentDiff)
		{
			#if UNITY_EDITOR
			if (CheatMenu.PlayerInvincible)
				playerDiff.Damage = IncomingDamage.CreateEmpty();

			if (CheatMenu.EnemyInvincible)
				opponentDiff.Damage = IncomingDamage.CreateEmpty();

			if (CheatMenu.PlayerOneAttackKill)
			{
				var unstoppableOutcomingDamage = new OutcomingDamage(DamageType.Pure, (CommonNumber)10000, (CommonNumber)0);
				var noDodge = DodgeResult.NotTried();
				opponentDiff.Damage = new IncomingDamage(unstoppableOutcomingDamage, noDodge);
			}
			#endif

			DiffLogs.Add(new(playerDiff, opponentDiff));

			_battlers[BattleSide.Player].ApplyDiffToState(playerDiff);
			_battlers[BattleSide.Opponent].ApplyDiffToState(opponentDiff);

			_battleScreen.UpdateHealth(_battlers);
		}


		private async UniTask PlayAnimations(List<TriggerAnimationInfo> playerAnimations, List<TriggerAnimationInfo> opponentAnimations)
		{
			var applyToPlayerTask = ApplyAnimationsForSide(playerAnimations, BattleSide.Player);
			var applyToOpponentTask = ApplyAnimationsForSide(opponentAnimations, BattleSide.Opponent);

			await UniTask.WhenAll(applyToPlayerTask, applyToOpponentTask);
		}


		private async UniTask ApplyAnimationsForSide(List<TriggerAnimationInfo> animations, BattleSide side)
		{
			foreach (var animationInfo in animations)
				await _battlers[side].PlayAnimation(animationInfo);
		}


		private RoundResult GetRoundResult(bool didPlayerEscaped)
		{
			var player = _battlers[BattleSide.Player];
			var opponent = _battlers[BattleSide.Opponent];

			if (player.IsAlive)
			{
				if (didPlayerEscaped)
					return RoundResult.PlayerEscape;
				
				if (opponent.IsAlive)
					return RoundResult.None;
				
				return RoundResult.PlayerWin;
			}
			return RoundResult.OpponentWin;
		}


		private async UniTask HandleBattleResult<TResultScreen>()
			where TResultScreen : BattleScreenResult
		{
			_battlers[BattleSide.Player].OnFightEnd();
			_battlers[BattleSide.Opponent].OnFightEnd();
			_battlers[BattleSide.Player].AttackEvent.RemoveAllListeners();
			_battlers[BattleSide.Opponent].AttackEvent.RemoveAllListeners();
			_battleScreen.HideScales();

			var battleResultScreen = await _screenManager.ShowScreen<TResultScreen>();
			await battleResultScreen.GetPlayerAction();
			await _screenManager.HideScreen<TResultScreen>();
		}
	}
}
