#pragma once

#include "CoreMinimal.h"
#include "LuaTable/LuaTableBase.h"
#include "PzLuaTableMgr.generated.h"

class ULuaTableBase;


USTRUCT()
struct FLuaTableCacheMap
{
	GENERATED_BODY()
	UPROPERTY()
	TMap<int32, ULuaTableBase*> LuaTables;
};

UCLASS(BlueprintType)
class WARFRAMEDEMO_API UPzLuaTableMgr : public ULangStringManager
{
	GENERATED_BODY()
public:
	static UPzLuaTableMgr* GetLuaTableMgr(const UObject* WorldContextObject);

	void Clean();

	/************************************************************************/
	/*
	//通过CTX(UObject)获得World
	UTaskTable* pT = UPzLuaTableMgr::GetLuaTableMgr(CTX)->TGetTableRow<UTaskTable>(22);
	*/
	/************************************************************************/
	template<class T>
	T* TGetTableRow(int32 Key)
	{
		if (!TableCaches.Contains(T::StaticClass()))
		{
			TableCaches.Add(T::StaticClass(), FLuaTableCacheMap());
		}
		FLuaTableCacheMap* pMapStruct = TableCaches.Find(T::StaticClass());
		if (nullptr == pMapStruct)
		{
			return nullptr;
		}

		FLuaTableCacheMap& RefMapStruct = *pMapStruct;

		ULuaTableBase** ppTableRow = RefMapStruct.LuaTables.Find(Key);
		if (ppTableRow && *ppTableRow)
		{
			return Cast<T>(*ppTableRow);
		}

		T* pTagbleRow = NewObject<T>(this);

		if (!pTagbleRow->IsA<ULuaTableBase>())
		{
			return nullptr;
		}
		ULuaTableBase* pTagbleRowBase = Cast<ULuaTableBase>(pTagbleRow);
		if (pTagbleRowBase && pTagbleRowBase->Initialize(Key))
		{
			pTagbleRowBase->LoadCache(this);
			RefMapStruct.LuaTables.Add(Key, pTagbleRow);
			return pTagbleRow;
		}

		return nullptr;
	}
	
	virtual bool GetLangString(const FString& Key, FString& OutValue) override;


	virtual bool GetLangString(const FString& Key, FText& OutValue) override;

private:
	UPROPERTY()
	TMap<UClass*, FLuaTableCacheMap> TableCaches;

	UPROPERTY()
	TMap<FString, FString> I18nStringMap;
	TMap<FString, FText> I18nTextMap;
};
