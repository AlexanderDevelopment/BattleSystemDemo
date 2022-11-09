using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Сравниваем значение CurrentHealth у монстра")]
	[TaskCategory("AI")]
	public class CompareMonsterCurrentHealth : ComparisionCondition
	{
		protected override CommonNumber EvaluateCurrentValue()
			=> Proxy.MonsterBattleEntity.CurrentHealth;
		
	}
}