using System;
using System.Collections.Generic;
using Toc.Damage;
using Toc.PlayerProgression;
using Toc.PlayerProgression.Calculation;


namespace Toc.Battle.Ai
{
	[Serializable]
	public abstract class BaseSideDiff<TDamage>
	{
		public BattleSide SourceSide;
		public BattleSide TargetSide;

		public CommonNumber Heal = (CommonNumber) 0;
		public TDamage Damage;

		public bool IsAutoAttack = false;
		
		public SideDeltaSet Deltas = new();
		public SideDeltaSet PassiveDeltas = new();
		public List<Item> ItemsToRemove = new();
		
		
		public bool IsOnActiveSide
			=> SourceSide == TargetSide;
	}
}
