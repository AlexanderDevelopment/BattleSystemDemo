using System;
using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;
using Toc.PlayerProgression.Enums;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Модифицировать характеристику предмета")]
	[TaskCategory("AI")]
	public class ModifyItemCharacteristic : BaseMonsterAction
	{
		public ItemCharacteristic _itemCharacteristic;


		public MathOperator MathOperator;


		public CommonNumber ModifyWith;
		

		public override TaskStatus OnUpdate()
		{
			Modify();
			
			return TaskStatus.Success;
		}
		
		
		private void Modify()
		{
			// FIXME не применяется к реальному значению, и вообще подозрительно
			var result = Proxy.ArmorValue.CalculateOperator(MathOperator, ModifyWith);
		}
	}
}
