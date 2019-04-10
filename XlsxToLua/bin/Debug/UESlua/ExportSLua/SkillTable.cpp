#include "SkillTable.h"
#include "LuaState.h"


bool USkillTable::GetTableItem(const FString& Key, FLuaTable& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
	slua::LuaVar ResultLuaVar = mainState->call("GetTableItem", "Skill", Key);
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	OutVal.Table = ResultLuaVar;
	return true;
}

bool USkillTable::GetAaa(const FString& Key, int32& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Skill", Key, "aaa");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isInt()) { return false; }
	OutVal = ResultLuaVar.asInt();
	return true;
}

bool USkillTable::GetVvv(const FString& Key, float& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Skill", Key, "vvv");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isNumber()) { return false; }
	OutVal = ResultLuaVar.asFloat();
	return true;
}

bool USkillTable::GetSkillName(const FString& Key, FString& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Skill", Key, "SkillName");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isString()) { return false; }
	OutVal = ResultLuaVar.asString();
	return true;
}

bool USkillTable::GetItem(const FString& Key, FLuaTable& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Skill", Key, "item");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	OutVal.Table = ResultLuaVar;
	return true;
}

bool USkillTable::GetDsst(const FString& Key, FLuaTable& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Skill", Key, "dsst");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	OutVal.Table = ResultLuaVar;
	return true;
}

