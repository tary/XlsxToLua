//Auto generated code

#pragma once

#include "CoreMinimal.h"
#include "LuaTable.h"
#include "TaskTable.generated.h"


UCLASS(Blueprintable)
class PROJECTZ_API UTaskTable : public UObject
{
	GENERATED_BODY()
public:
	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetTableItem(const FString& Key, UPARAM(ref) FLuaTable& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetAaa(const FString& Key, UPARAM(ref) int32& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetVvv(const FString& Key, UPARAM(ref) float& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetTaskTitle(const FString& Key, UPARAM(ref) FString& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetTaskName(const FString& Key, UPARAM(ref) FString& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetItem(const FString& Key, UPARAM(ref) FLuaTable& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|TaskTable")
	bool GetDsst(const FString& Key, UPARAM(ref) FLuaTable& OutVal) const;

};
