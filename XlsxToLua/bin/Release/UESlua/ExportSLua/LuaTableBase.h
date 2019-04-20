//Auto generated code

#pragma once

#include "CoreMinimal.h"
#include "LuaTable.h"
#include "LuaTableBase.generated.h"

class IFLangStringManager
{
public:
	virtual bool GetLangString(const FString& Key, FString& OutValue) = 0;
};

UCLASS(Abstract)
class WARFRAMEDEMO_API ULangStringManager : public UObject
{
	GENERATED_BODY()
public:
	virtual bool GetLangString(const FString& Key, FString& OutValue);
	virtual bool GetLangString(const FString& Key, FText& OutValue);
};


UCLASS(Abstract)
class WARFRAMEDEMO_API ULuaTableBase : public UObject
{
	GENERATED_UCLASS_BODY()
public:
	UFUNCTION(BlueprintCallable, Category = "Tables")
	virtual bool Initialize(int32 Key);
	UFUNCTION(BlueprintCallable, Category = "Tables")
	virtual bool LoadCache(ULangStringManager* langMgr);

	UPROPERTY(BlueprintReadOnly)
	FLuaTable _Row;


protected:
	inline bool initializeTemplate(int32 Key, const char* LuaTableName)
	{
		return GetRowTemplate(Key, LuaTableName, _Row.Table);
	}

	static bool GetRowTemplate(int32 Key, const char* LuaTableName, slua::LuaVar& OutResultLuaVar);
	static bool GetFieldTemplate(int32 Key, const char* LuaTableName, const char* LuaFieldName, slua::LuaVar& OutResultLuaVar);

	template<typename R>
	inline bool loadFieldTemplate(const char* LuaFieldName, R& OutResult) const
	{
		if (!_Row.Table.isValid() || _Row.Table.isNil()) return false;
		return loadFieldNoCheckTemplate(LuaFieldName, OutResult);
	}

	template<typename R>
	inline bool loadFieldNoCheckTemplate(const char* LuaFieldName, R& OutResult) const
	{
		return _Row.GetFromTable(LuaFieldName, OutResult);
	}

	inline bool loadLangField(const char* LuaFieldName, ULangStringManager* langMgr, FText& OutResult) const
	{
		if (!_Row.Table.isValid() || _Row.Table.isNil()) return false;
		return loadLangFieldNoCheck(LuaFieldName, langMgr, OutResult);
	}

	inline bool loadLangFieldNoCheck(const char* LuaFieldName, ULangStringManager* langMgr, FText& OutResult) const
	{
		FString OutID;
		if (_Row.GetFromTable(LuaFieldName, OutID))
		{
			return langMgr->GetLangString(OutID, OutResult);
		}

		return false;
	}
};
