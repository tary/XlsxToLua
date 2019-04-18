//Auto generated code

#pragma once

#include "CoreMinimal.h"
#include "LuaTable.h"
#include "LuaTableBase.generated.h"


UCLASS(Abstract)
class WARFRAMEDEMO_API ULuaTableBase : public UObject
{
	GENERATED_UCLASS_BODY()
public:
	UFUNCTION(BlueprintCallable, Category = "Tables")
	virtual bool Initialize(int32 Key);
	UFUNCTION(BlueprintCallable, Category = "Tables")
	virtual bool LoadCache();

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
};
