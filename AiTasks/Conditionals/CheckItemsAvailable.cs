using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Проверка наличия предметов в инвентаре")]
	[TaskCategory("AI")]
	public class CheckItemsAvailable : BaseMonsterConditional
	{
		public BattleSide Side;


		public List<Item> ItemsToFind;

		
		public override TaskStatus OnUpdate()
			=> Check();

		
		private TaskStatus Check()
		{
			var targetInventory = State[Side].Inventory;

			foreach (var item in ItemsToFind)
			{
				if (item == null)
					continue;
				
				foreach (var inventoryItem in targetInventory.GetAllItems())
					if (item == inventoryItem)
						return TaskStatus.Success;
			}

			return TaskStatus.Failure;
		}
		
	}
}
