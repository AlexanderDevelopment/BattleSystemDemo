using System;
using System.Collections.Generic;
using Toc.PlayerProgression;
using UnityEngine;
using Zenject;


[Serializable]
public class DecisionTreeProxy : MonoBehaviour
{
	[Inject]
	private readonly GameState _state;


	public List<Ability> ActivatedAbilities = new ();


	public List<Item> MonsterItemsToRemove = new ();
	
	
	public List<Item> PlayerItemsToRemove = new ();


	public CommonNumber ArmorValue;


	private MonsterBattleEntity _monsterBattleEntity;
	

	public GameState GameState
		=> _state;


	public MonsterBattleEntity MonsterBattleEntity
		=> _monsterBattleEntity;
	

	private void Awake()
	{
		// TODO выставить на сцене?..
		_monsterBattleEntity = GetComponent<MonsterBattleEntity>();
	}


	public void ResetDecision()
	{
		ActivatedAbilities.Clear();
		MonsterItemsToRemove.Clear();
		PlayerItemsToRemove.Clear();
	}
}
