//Auto generated code

#pragma once

#include "CoreMinimal.h"
#include "LuaTable.h"
#include "SkillTable.generated.h"


UCLASS(Blueprintable)
class PROJECTZ_API USkillTable : public UObject
{
	GENERATED_BODY()
public:
	UFUNCTION(BlueprintCallable, Category = "Tables|SkillTable")
	bool GetTableItem(const FString& Key, UPARAM(ref) FLuaTable& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|SkillTable")
	bool GetAaa(const FString& Key, UPARAM(ref) int32& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|SkillTable")
	bool GetVvv(const FString& Key, UPARAM(ref) float& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|SkillTable")
	bool GetSkillName(const FString& Key, UPARAM(ref) FString& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|SkillTable")
	bool GetItem(const FString& Key, UPARAM(ref) FLuaTable& OutVal) const;

	UFUNCTION(BlueprintCallable, Category = "Tables|SkillTable")
	bool GetDsst(const FString& Key, UPARAM(ref) FLuaTable& OutVal) const;

};
