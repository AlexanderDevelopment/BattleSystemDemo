using System.Collections.Generic;
using Toc.Animations;
using Toc.Damage;
using Toc.Scene.Animations;


namespace Toc.Battle.Ai
{
	public class SideDiffResult : BaseSideDiff<IncomingDamage>
	{
		public bool IsKilled = false;
		public List<TriggerAnimationInfo> Animations = new();
	}
}
