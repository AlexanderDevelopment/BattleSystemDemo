using System.Collections.Generic;
using Toc.Damage;
using Toc.PlayerProgression;
using UnityEngine;


namespace Toc.Battle.Ai
{
	public class SideDiffRequest : BaseSideDiff<OutcomingDamage>
	{
		public List<Ability> ActivatedAbilities;

		public bool IsEscaped;


		public void CalculateDeltaHealthDiffForTarget(BattleSide side, GameState state, ScaleRouter scales)
		{
			if (Damage == null)
				Damage = OutcomingDamage.CreateEmpty(DamageType.None);
			
			if (this.Deltas == null || !this.Deltas.Contains(scales.Health))
				return;

			var currentHealth = state[side].Scales[scales.Health].Value;
			var requestedHealth = this.Deltas[scales.Health]
				.EvaluateDelta(currentHealth, state);

			var absDelta = requestedHealth - currentHealth;
				
			if (this.PassiveDeltas != null && this.PassiveDeltas.Contains(scales.Health))
			{
				var ignoreArmorHealth = this.PassiveDeltas[scales.Health]
					.EvaluateDelta(currentHealth, state);
					
				var absIgnoreDelta = ignoreArmorHealth - currentHealth;
				absDelta += absIgnoreDelta;
			}

			if (absDelta == 0)
				return;
			
			if (absDelta > 0)
				this.Heal = absDelta;
			else
			{
				var damage = -absDelta;
				
				if (Damage.DamageType == DamageType.None)
					Damage = new OutcomingDamage(DamageType.Physical, damage, CommonNumber.Zero);
				else
					Damage += damage;
				
				Debug.LogWarning("You using deltas to deal damage, it's deprecated. Use Outcome/Income Damage system insted");
			}
		}
	}
}
