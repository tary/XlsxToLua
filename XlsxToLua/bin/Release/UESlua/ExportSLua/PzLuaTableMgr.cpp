#include "PzLuaTableMgr.h"
#include "PzLuaStateMgr.h"
#include "LuaTable/LuaTableBase.h"



UPzLuaTableMgr* UPzLuaTableMgr::GetLuaTableMgr(const UObject* WorldContextObject)
{
	UPzLuaStateMgr* LuaStateMgr = UPzLuaStateMgr::GetLuaStateMgr(WorldContextObject);
	if (LuaStateMgr)
	{
		return LuaStateMgr->GetLuaTableMgr();
	}

	return nullptr;
}

void UPzLuaTableMgr::Clean()
{
	TableCaches.Empty();
	I18nTextMap.Empty();
	I18nStringMap.Empty();
}

bool UPzLuaTableMgr::GetLangString(const FString& Key, FString& OutValue)
{
	if (!I18nStringMap.Contains(Key))
	{
		I18nStringMap.Add(Key, Key);
	}

	FString* pExistString = I18nStringMap.Find(Key);
	if (pExistString)
	{
		OutValue = *pExistString;
	}

	return true;
}

bool UPzLuaTableMgr::GetLangString(const FString& Key, FText& OutValue)
{
	if (!I18nTextMap.Contains(Key))
	{
		I18nTextMap.Add(Key, FText::FromString(Key));
	}

	FText* pExistText = I18nTextMap.Find(Key);
	if (pExistText)
	{
		OutValue = *pExistText;
	}

	return true;
}
