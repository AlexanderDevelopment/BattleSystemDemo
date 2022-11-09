using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Удалить предметы")]
	[TaskCategory("AI")]
	public class RemoveItems : BaseMonsterAction
	{
		public BattleSide Side;


		public List<Item> Items;

		
		public override TaskStatus OnUpdate()
		{
			UpdateRemovalList();
			return TaskStatus.Success;
		}
		
		
		private void UpdateRemovalList()
		{
			foreach (var item in Items)
			{
				if (item == null)
					continue;

				if (Side == BattleSide.Opponent)
					Proxy.MonsterItemsToRemove.Add(item);
				else if (Side == BattleSide.Player)
					Proxy.PlayerItemsToRemove.Add(item);
			}
		}
	}
}
