using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	public abstract class CharacteristicCondition : ComparisionCondition
	{
		public TargetCharacteristic Characteristic;
		

		protected override CommonNumber EvaluateCurrentValue()
			=> Characteristic.Evaluate(BattleSide.Opponent, State);
	}
}