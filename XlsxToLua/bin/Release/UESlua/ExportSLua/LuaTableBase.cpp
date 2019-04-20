#include "LuaTableBase.h"
#include "LuaState.h"

const char* LUA_GET_TABLE_FUNC = "GetTableItem";
const char* LUA_GET_TABLE_FIELD_FUNC = "GetTableItemField";

bool ULangStringManager::GetLangString(const FString& Key, FString& OutValue)
{
	OutValue = Key;
	return true;
}

bool ULangStringManager::GetLangString(const FString& Key, FText& OutValue)
{
	OutValue.FromString(Key);
	return true;
}

ULuaTableBase::ULuaTableBase(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
}

bool ULuaTableBase::Initialize(int32 Key)
{
	return false;
}


bool ULuaTableBase::LoadCache(ULangStringManager* langMgr)
{
	return false;
}

bool ULuaTableBase::GetRowTemplate(int32 Key, const char* LuaTableName, slua::LuaVar& OutResultLuaVar)
{
	if (nullptr == LuaTableName)
	{
		return false;
	}

	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState)
	{
		return false;
	}

	OutResultLuaVar = mainState->call(LUA_GET_TABLE_FUNC, LuaTableName, Key);
	return (OutResultLuaVar.isValid() && !OutResultLuaVar.isNil());
}

bool ULuaTableBase::GetFieldTemplate(int32 Key, const char* LuaTableName, const char* LuaFieldName, slua::LuaVar& OutResultLuaVar)
{
	if (nullptr == LuaFieldName || nullptr == LuaTableName)
	{
		return false;
	}

	slua::LuaState* mainState = slua::LuaState::get();
	if (!mainState)
	{
		return false;
	}
	
	OutResultLuaVar = mainState->call(LUA_GET_TABLE_FIELD_FUNC, LuaTableName, Key, LuaFieldName);
	return (OutResultLuaVar.isValid() && !OutResultLuaVar.isNil());
}
