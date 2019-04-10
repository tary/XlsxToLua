#include "TaskTable.h"
#include "LuaState.h"


bool UTaskTable::GetTableItem(const FString& Key, FLuaTable& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
	slua::LuaVar ResultLuaVar = mainState->call("GetTableItem", "Task", Key);
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	OutVal.Table = ResultLuaVar;
	return true;
}

bool UTaskTable::GetAaa(const FString& Key, int32& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Task", Key, "aaa");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isInt()) { return false; }
	OutVal = ResultLuaVar.asInt();
	return true;
}

bool UTaskTable::GetVvv(const FString& Key, float& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Task", Key, "vvv");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isNumber()) { return false; }
	OutVal = ResultLuaVar.asFloat();
	return true;
}

bool UTaskTable::GetTaskTitle(const FString& Key, FString& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Task", Key, "taskTitle");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isString()) { return false; }
	OutVal = ResultLuaVar.asString();
	return true;
}

bool UTaskTable::GetTaskName(const FString& Key, FString& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Task", Key, "taskName");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	if (!ResultLuaVar.isString()) { return false; }
	OutVal = ResultLuaVar.asString();
	return true;
}

bool UTaskTable::GetItem(const FString& Key, FLuaTable& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Task", Key, "item");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	OutVal.Table = ResultLuaVar;
	return true;
}

bool UTaskTable::GetDsst(const FString& Key, FLuaTable& OutVal) const
{
	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState) { return false; }
		slua::LuaVar ResultLuaVar = mainState->call("GetTableItemField", "Task", Key, "dsst");
	if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }

	OutVal.Table = ResultLuaVar;
	return true;
}

