using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Gets the names of all dialogue database elements of a specified type.")]
	public class GetAllLuaElements : FsmStateAction
	{

		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The type of elements to get")]
		public LuaElementTypeEnum elementType;

		[HutongGames.PlayMaker.TooltipAttribute("Include elements that were added at runtime, not just elements in the dialogue database. Only valid when using the Dialogue System's default Lua implementation. If you've switched to a different Lua, will get elements defined in database instead.")]
		public FsmBool allRuntimeElements = new FsmBool(true);

		[RequiredField]
		[UIHint(UIHint.Variable)]
		[VariableType(VariableType.Array)]
		[ArrayEditor(VariableType.String)]
		[HutongGames.PlayMaker.TooltipAttribute("Store the names of the elements in this string array")]
		public FsmArray storeStringArrayResult;

		public override void Reset()
		{
			elementType = LuaElementTypeEnum.Actors;
			storeStringArrayResult = null;
		}

		public override string ErrorCheck()
		{
			bool anyResultVariable = (storeStringArrayResult != null);
			return anyResultVariable ? base.ErrorCheck() : "Assign at least one store result variable.";
		}

		public override void OnEnter()
		{
			GetAndStore();
			Finish();
		}

		private void GetAndStore()
		{
			if (storeStringArrayResult == null || storeStringArrayResult.IsNone) return;
			storeStringArrayResult.Values = GetNames();
		}

		private object[] GetNames()
		{
			var list = new List<object>();
#if !(USE_NLUA || OVERRIDE_LUA) // Note: Currently only valid for built-in Lua implementation.
			if (allRuntimeElements != null && allRuntimeElements.Value == true)
			{
				Language.Lua.LuaTable assetTable = null;
				switch (elementType)
				{
					case LuaElementTypeEnum.Actors:
						assetTable = Lua.Environment.GetValue("Actor") as Language.Lua.LuaTable;
						foreach (var kvp in assetTable.Dict)
						{
							list.Add(kvp.Key.ToString());
						}
						break;
					case LuaElementTypeEnum.Items:
						assetTable = Lua.Environment.GetValue("Item") as Language.Lua.LuaTable;
						foreach (var kvp in assetTable.Dict)
						{
							var actorRecord = kvp.Value as Language.Lua.LuaTable;
							if (actorRecord.GetValue(DialogueSystemFields.IsItem).GetBooleanValue())
							{
								list.Add(kvp.Key.ToString());
							}
						}
						break;
					case LuaElementTypeEnum.Quests:
						list.AddRange(QuestLog.GetAllQuests(QuestState.Abandoned | QuestState.Active | QuestState.Failure | QuestState.Grantable | QuestState.Success | QuestState.Unassigned));
						break;
					case LuaElementTypeEnum.Locations:
						assetTable = Lua.Environment.GetValue("Location") as Language.Lua.LuaTable;
						foreach (var kvp in assetTable.Dict)
						{
							list.Add(kvp.Key.ToString());
						}
						break;
					case LuaElementTypeEnum.Variables:
						assetTable = Lua.Environment.GetValue("Variable") as Language.Lua.LuaTable;
						foreach (var kvp in assetTable.Dict)
                        {
							list.Add(kvp.Key.ToString());
                        }
						break;
				}
			}
			else
#endif
			{
				switch (elementType)
				{
					case LuaElementTypeEnum.Actors:
						foreach (var actor in DialogueManager.masterDatabase.actors)
						{
							list.Add(actor.Name);
						}
						break;
					case LuaElementTypeEnum.Items:
						foreach (var item in DialogueManager.masterDatabase.items)
						{
							if (item.IsItem) list.Add(item.Name);
						}
						break;
					case LuaElementTypeEnum.Quests:
						list.AddRange(QuestLog.GetAllQuests(QuestState.Abandoned | QuestState.Active | QuestState.Failure | QuestState.Grantable | QuestState.Success | QuestState.Unassigned));
						break;
					case LuaElementTypeEnum.Locations:
						foreach (var location in DialogueManager.masterDatabase.locations)
						{
							list.Add(location.Name);
						}
						break;
					case LuaElementTypeEnum.Variables:
						foreach (var variable in DialogueManager.masterDatabase.variables)
						{
							list.Add(variable.Name);
						}
						break;
				}
			}
			return list.ToArray();
		}

	}
}
