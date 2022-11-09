using Cysharp.Threading.Tasks;
using Toc.Animations;
using Toc.Battle.Ai;
using Toc.Damage;
using Toc.Scene.Animations;
using UnityEngine.Events;


namespace Toc.PlayerProgression
{
	public interface IBattleEntity : IBattleEntityInfo, IInitialStateSource
	{
		UnityEvent AttackEvent { get;}


		bool IsInCombat { get;}


		RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreBattleAction(IBattleEntityInfo player, IBattleEntityInfo opponent);
		
		
		RoundDiffs<SideDiffRequest, OutcomingDamage> MakePreRoundAction(IBattleEntityInfo player, IBattleEntityInfo opponent);
		

		UniTask<RoundDiffs<SideDiffRequest, OutcomingDamage>> MakeAction(IBattleEntityInfo player, IBattleEntityInfo opponent);

		
		RoundDiffs<SideDiffRequest, OutcomingDamage> MakeAutoAttack(IBattleEntityInfo player, IBattleEntityInfo opponent);
		
		
		RoundDiffs<SideDiffRequest, OutcomingDamage> MakePostRoundAction(IBattleEntityInfo player, IBattleEntityInfo opponent);
		

		SideDiffResult RecalculateRequestedDiff(SideDiffRequest diff);


		void ApplyDiffToState(SideDiffResult diff);


		public void OnFightStart();
		
		
		public void OnFightEnd();
		
		
		public void OnRoundStart();
		
		
		public void OnRoundEnd();


		UniTask PlayAnimation(TriggerAnimationInfo animationType);
	}
}
