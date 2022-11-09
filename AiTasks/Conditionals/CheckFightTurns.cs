using BehaviorDesigner.Runtime.Tasks;
using Toc.Battle;
using Toc.PlayerProgression;
using Toc.Utils;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Проверка количества ходов")]
	[TaskCategory("AI")]
	public class CheckFightTurns : BaseMonsterConditional
	{
		protected MonsterBattleEntity _monsterBattleEntity;


		public int CompareWith;


		public CompareOperator compareOperator;


		private TaskStatus Compare()
			=> CompareCondition.Compare(compareOperator, _monsterBattleEntity.FightTurns, CompareWith) ? TaskStatus.Success : TaskStatus.Failure;


		public override void OnStart()
			=> _monsterBattleEntity = GetComponent<MonsterBattleEntity>();


		public override TaskStatus OnUpdate()
			=> Compare();
	}
}
