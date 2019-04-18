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
	virtual bool InitializeLoad(int32 Key);

	UPROPERTY(BlueprintReadOnly)
	FLuaTable _Row;


protected:
	inline bool initializeLoadTemplate(int32 Key, const char* LuaTableName)
	{
		return GetRowTemplate(Key, LuaTableName, _Row.Table);
	}

	static bool GetRowTemplate(int32 Key, const char* LuaTableName, slua::LuaVar& OutResultLuaVar);
	static bool GetFieldTemplate(int32 Key, const char* LuaTableName, const char* LuaFieldName, slua::LuaVar& OutResultLuaVar);

	template<typename R>
	inline bool loadFieldTemplate(const char* LuaFieldName, R& OutResult) const
	{
		if (!_Row.Table.isValid() || _Row.Table.isNil()) return false;
		return _Row.GetFromTable(LuaFieldName, OutResult);
	}
};
