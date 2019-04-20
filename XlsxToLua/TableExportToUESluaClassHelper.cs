using System;
using System.Collections.Generic;
using System.Text;

public class TableExportToUESluaClassHelper
{
    private static string _CPP_CLASS_INDENTATION_STRING = "\t";
    private static string _CPP_CLASS_PROPERTY_STRING = "UPROPERTY(BlueprintReadOnly)";

    public static bool ExportTableToUESluaClass(TableInfo tableInfo, out string errorString)
    {
        List<FieldInfo> allFieldInfo = tableInfo.GetAllClientFieldInfo();
        if (allFieldInfo.Count == 0)
        {
            errorString = "字段为空";
            return false;
        }

        string keyType = null;
        if (allFieldInfo[0].DataType == DataType.Int)
            keyType = "int32";
        else if (allFieldInfo[0].DataType == DataType.String)
            keyType = "const FString&";
        else
        {
            errorString = "主键类型只支持String和Int";
            return false;
        }

        allFieldInfo.RemoveAt(0);
        string fileName = string.Concat(char.ToUpper(tableInfo.TableName[0]), tableInfo.TableName.Substring(1), "Table");
        string className = null;
        // 若统一配置了前缀和后缀，需进行添加（但注意如果配置了前缀，上面生成的驼峰式类名首字母要改为大写）
        if (string.IsNullOrEmpty(AppValues.ExportCsvClassClassNamePrefix))
            className = string.Concat("U", fileName);
        else
            className = string.Concat("U", AppValues.ExportCsvClassClassNamePrefix, fileName);

        if (string.IsNullOrEmpty(AppValues.ExportCsvClassClassNamePostfix) == false)
            className = className + AppValues.ExportCsvClassClassNamePostfix;

        if (_ExportTableToUESluaHeader(tableInfo, allFieldInfo, keyType, className, fileName, out errorString) == false)
        {
            return false;
        }

        return _ExportTableToUESluaCode(tableInfo, allFieldInfo, keyType, className, fileName, out errorString);
    }


    private static bool _ExportTableToUESluaHeader(TableInfo tableInfo, List<FieldInfo> allFieldInfo, string keyType, string className, string fileName, out string errorString)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (AppValues.ExportUESluaExportAPIName == null)
        {
            errorString = "ExportUESluaExportAPIName 为空\n";
            return false;
        }
        stringBuilder.AppendLine("//Auto generated code").AppendLine();
        stringBuilder.AppendLine("#pragma once").AppendLine();
        stringBuilder.AppendLine("#include \"LuaTableBase.h\"");
        stringBuilder.AppendLine("#include \"LuaTable.h\"");
        ////////////////////////////////////////////////////////////////
        ///Ext include
        if (AppValues.ExportUESluaImport != null)
        {
            for (int i = 0; i < AppValues.ExportUESluaImport.Count; ++i)
            {
                string importString = AppValues.ExportUESluaImport[i];
                stringBuilder.AppendFormat("#include \"{0}\"", importString).AppendLine();
            }
        }
        ////////////////////////////////////////////////////////////////
        stringBuilder.AppendFormat("#include \"{0}.generated.h\"", fileName).AppendLine().AppendLine().AppendLine();

        stringBuilder.AppendLine("UCLASS(Blueprintable)");
        stringBuilder.AppendFormat("class {0} {1} : public ULuaTableBase", AppValues.ExportUESluaExportAPIName, className).AppendLine();

        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine(string.Concat(_CPP_CLASS_INDENTATION_STRING, "GENERATED_UCLASS_BODY()"));
        stringBuilder.AppendLine("public:");


        //////////////////////////////////////////////////////////
        ///Functions
        string UFunctionDef = string.Concat(_CPP_CLASS_INDENTATION_STRING, "UFUNCTION(BlueprintCallable, Category = \"Tables|", fileName, "\")");
        
        stringBuilder.AppendLine(UFunctionDef);
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
        stringBuilder.AppendFormat("static bool GetTableItem({0} Key, FLuaTable& OutVal);", keyType).AppendLine().AppendLine();


        StringBuilder feildBuilder = new StringBuilder();

        StringBuilder noneStaticFeildBuilder = new StringBuilder();
        
        noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
        noneStaticFeildBuilder.AppendFormat("virtual bool Initialize({0} Key) override;", keyType).AppendLine().AppendLine();

        noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
        noneStaticFeildBuilder.AppendLine("virtual bool LoadCache(ULangStringManager* langMgr) override;").AppendLine();


        //bool GetNAME(TYPE& OutVal);
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {

            if (_IsSupportBP(fieldInfo.DataType))
            {
                stringBuilder.AppendLine(UFunctionDef);
            }

            stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
            string valTypeName = _GetUECppDataTypeString(fieldInfo.DataType, fieldInfo.JsonDetailType);
            string fieldName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            if (fieldInfo.DataType == DataType.Lang)
                stringBuilder.AppendFormat("static bool Get{0}LangID({1} Key, FString& OutVal);", fieldName, keyType);
            else
                stringBuilder.AppendFormat("static bool Get{0}({1} Key, {2}& OutVal);", fieldName, keyType, valTypeName);
            stringBuilder.AppendLine();

            if (_IsSupportBP(fieldInfo.DataType))
            {
                noneStaticFeildBuilder.AppendLine(UFunctionDef);
            }

            if(_IsLocalCache(fieldInfo.DataType, fieldInfo.JsonDetailType))
                noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("bool Load{0}(ULangStringManager* langMgr);", fieldName).AppendLine();
            else
                noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("bool Load{0}(ULangStringManager* langMgr, {1}& OutVal);", fieldName, valTypeName).AppendLine();

            if(_IsLocalCache(fieldInfo.DataType, fieldInfo.JsonDetailType))
            {
                if (_IsSupportBP(fieldInfo.DataType))
                {
                    feildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine(_CPP_CLASS_PROPERTY_STRING);
                }
                feildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("{0} {1};", valTypeName, fieldName).AppendLine();
            }        
        }

        stringBuilder.AppendLine();
        stringBuilder.Append(noneStaticFeildBuilder);

        stringBuilder.AppendLine();
        stringBuilder.Append(feildBuilder);

        // 闭合类定义
        stringBuilder.AppendLine("};");

        if (Utils.SaveUESluaClassFile(tableInfo.TableName, fileName + ".h", stringBuilder.ToString()) == true)
        {
            errorString = null;
            return true;
        }
        else
        {
            errorString = "保存csv对应UESlua头文件失败\n";
            return false;
        }
    }

    //获得构造函数专用的默认值
    private static string _getDefaultValForCtr(DataType ty)
    {
        switch (ty)
        {
            case DataType.Int:
            case DataType.Long:
                return "0";
            case DataType.Float:
                return "0.f";
            case DataType.Bool:
                return "false";
            default:
                break;
        }

        return null;
    }

    private static bool _ExportTableToUESluaCode(TableInfo tableInfo, List<FieldInfo> allFieldInfo, string keyType, string className, string fileName, out string errorString)
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.AppendFormat("#include \"{0}.h\"", fileName).AppendLine();
        stringBuilder.AppendLine("#include \"LuaState.h\"").AppendLine().AppendLine();

        string tableNameNS = string.Concat("LuaTable", tableInfo.TableName);

        stringBuilder.AppendFormat("namespace {0}", tableNameNS).AppendLine();
        stringBuilder.AppendLine("{");
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("static const char* NAME = \"{0}\";", tableInfo.TableName).AppendLine();
        stringBuilder.AppendLine("}").AppendLine();

        tableNameNS = string.Concat(tableNameNS, "::NAME");


        //////////////////////////////////////////////////////////
        ///Construct
        stringBuilder.AppendFormat("{0}::{0}(const FObjectInitializer& ObjectInitializer)", className).AppendLine();
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine(": Super(ObjectInitializer)");
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            string feildName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            {
                string defaultVal = _getDefaultValForCtr(fieldInfo.DataType); ;
                if (defaultVal != null)
                {
                    stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat(", {0}({1})", feildName, defaultVal).AppendLine();
                }
            }
        }
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine("{ }").AppendLine();

        //////////////////////////////////////////////////////////
        ///Functions
        ///
        
        stringBuilder.AppendFormat("bool {0}::Initialize({1} Key)", className, keyType).AppendLine();
        stringBuilder.AppendLine("{");
        {
            stringBuilder.Append(_GetIndentation(1)).AppendFormat("return initializeTemplate(Key, {0});", tableNameNS).AppendLine();
        }
        stringBuilder.AppendLine("}").AppendLine();

        stringBuilder.AppendFormat("bool {0}::GetTableItem({1} Key, FLuaTable& OutVal)", className, keyType).AppendLine();
        stringBuilder.AppendLine("{");
        {
            stringBuilder.Append(_GetIndentation(1)).AppendFormat("return GetRowTemplate(Key, {0}, OutVal.Table);", tableNameNS).AppendLine();
        }
        stringBuilder.AppendLine("}").AppendLine();

        //非静态函数
        StringBuilder noneStaticFeildBuilder = new StringBuilder();
        //LoadCache内逻辑
        StringBuilder loadFeildBuilder = new StringBuilder();

        //bool GetNAME(TYPE& OutVal);
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            string valTypeName = _GetUECppDataTypeString(fieldInfo.DataType, fieldInfo.JsonDetailType);
            string feildName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));
            string luaFieldName = (fieldInfo.DataType == DataType.Lang) ? string.Concat(AppValues.LUA_LANG_ID_PREFIX, fieldInfo.FieldName) : fieldInfo.FieldName;

            if (_IsLocalCache(fieldInfo.DataType, fieldInfo.JsonDetailType))
                noneStaticFeildBuilder.AppendFormat("bool {0}::Load{1}(ULangStringManager* langMgr)", className, feildName).AppendLine();
            else
                noneStaticFeildBuilder.AppendFormat("bool {0}::Load{1}(ULangStringManager* langMgr, {2}& OutVal)", className, feildName, valTypeName).AppendLine();

            noneStaticFeildBuilder.AppendLine("{");
            {
                if (_IsLocalCache(fieldInfo.DataType, fieldInfo.JsonDetailType))
                {
                    if (fieldInfo.DataType == DataType.Lang)
                        noneStaticFeildBuilder.Append(_GetIndentation(1)).AppendFormat("return loadLangField(\"{0}\", langMgr, {1});", luaFieldName, feildName).AppendLine();
                    else
                        noneStaticFeildBuilder.Append(_GetIndentation(1)).AppendFormat("return loadFieldTemplate(\"{0}\", {1});", luaFieldName, feildName).AppendLine();
                }
                else
                    noneStaticFeildBuilder.Append(_GetIndentation(1)).AppendFormat("return loadFieldTemplate(\"{0}\", OutVal);", luaFieldName).AppendLine();
            }
            noneStaticFeildBuilder.AppendLine("}").AppendLine();

            if (_IsLocalCache(fieldInfo.DataType, fieldInfo.JsonDetailType))
            {
                if (fieldInfo.DataType == DataType.Lang)
                    loadFeildBuilder.Append(_GetIndentation(1)).AppendFormat("if (!loadLangFieldNoCheck(\"{0}\", langMgr, {1}))", luaFieldName, feildName).AppendLine();
                else
                    loadFeildBuilder.Append(_GetIndentation(1)).AppendFormat("if (!loadFieldNoCheckTemplate(\"{0}\", {1}))", luaFieldName, feildName).AppendLine();

                loadFeildBuilder.Append(_GetIndentation(1)).AppendLine("{");
                loadFeildBuilder.Append(_GetIndentation(2)).AppendLine("return false;");
                loadFeildBuilder.Append(_GetIndentation(1)).AppendLine("}").AppendLine();
            }


            if(fieldInfo.DataType == DataType.Lang)
                stringBuilder.AppendFormat("bool {0}::Get{1}LangID({2} Key, FString& OutVal)", className, feildName, keyType);
            else
                stringBuilder.AppendFormat("bool {0}::Get{1}({2} Key, {3}& OutVal)", className, feildName, keyType, valTypeName);
            stringBuilder.AppendLine("{");
            {
                stringBuilder.Append(_GetIndentation(1)).AppendLine("slua::LuaVar ResultLuaVar;");
                stringBuilder.Append(_GetIndentation(1)).AppendFormat("if (!GetFieldTemplate(Key, {0}, \"{1}\", ResultLuaVar))", tableNameNS, fieldInfo.FieldName).AppendLine();
                stringBuilder.Append(_GetIndentation(1)).AppendLine("{");
                stringBuilder.Append(_GetIndentation(2)).AppendLine("return false;");
                stringBuilder.Append(_GetIndentation(1)).AppendLine("}");

                string checkFunc = null;
                string castFunc = null;
                if (fieldInfo.DataType == DataType.Json && fieldInfo.JsonDetailType != null)
                {
                    stringBuilder.Append(_GetIndentation(1)).AppendLine("return slua::getFromVar(ResultLuaVar, OutVal);");
                }
                else
                {
                    if (_GetUESluaValueFuncString(fieldInfo.DataType, out checkFunc, out castFunc))
                    {
                        stringBuilder.Append(_GetIndentation(1));
                        stringBuilder.AppendFormat("if (!ResultLuaVar.{0}()) {{ return false; }}", checkFunc).AppendLine();

                        stringBuilder.Append(_GetIndentation(1));

                        switch (fieldInfo.DataType)
                        {
                            case DataType.Lang:
                            case DataType.String:
                                stringBuilder.AppendFormat("OutVal = UTF8_TO_TCHAR(ResultLuaVar.{0}());", castFunc).AppendLine();
                                break;
                            default:
                                stringBuilder.AppendFormat("OutVal = ResultLuaVar.{0}();", castFunc).AppendLine();
                                break;
                        }

                    }
                    else
                    {
                        stringBuilder.Append(_GetIndentation(1));
                        stringBuilder.AppendLine("OutVal.Table = ResultLuaVar;");
                    }

                    stringBuilder.Append(_GetIndentation(1));
                    stringBuilder.AppendLine("return true;");
                }
            }
            stringBuilder.AppendLine("}").AppendLine();
        }

        stringBuilder.AppendLine().Append(noneStaticFeildBuilder);

        stringBuilder.AppendFormat("bool {0}::LoadCache(ULangStringManager* langMgr)", className).AppendLine();
        stringBuilder.AppendLine("{");
        {
            stringBuilder.Append(_GetIndentation(1)).AppendLine("if (!_Row.Table.isValid() || _Row.Table.isNil()) return false;").AppendLine();
            stringBuilder.Append(loadFeildBuilder);
            stringBuilder.Append(_GetIndentation(1)).AppendLine("return true;");
        }
        stringBuilder.AppendLine("}").AppendLine();


        if (Utils.SaveUESluaClassFile(tableInfo.TableName, fileName + ".cpp", stringBuilder.ToString()) == true)
        {
            errorString = null;
            return true;
        }
        else
        {
            errorString = "保存csv对应UESlua头文件失败\n";
            return false;
        }
    }

    private static bool _IsLocalCache(DataType dataType, JsonDetail JsonDetailType)
    {
        switch (dataType)
        {
            case DataType.Int:
            case DataType.Long:
            case DataType.Float:
            case DataType.Bool:
            case DataType.String:
            case DataType.Lang:
                return true;
            case DataType.Json:
                return JsonDetailType != null;
            default:
                return false;
        }
    }

    private static string _getUECppJsonTypeString(JsonDetail jsonDetail)
    {
        if (null == jsonDetail) return null;

        switch (jsonDetail.ContentType)
        {
            case DataType.Array:
                return string.Concat("TArray<", _GetUECppDataTypeString(jsonDetail.ValueType, null), ">");
            case DataType.Dict:
                return string.Concat("TMap<", _GetUECppDataTypeString(jsonDetail.KeyType, null), ", ", _GetUECppDataTypeString(jsonDetail.ValueType, null), ">");
            default:
                break;
        }

        return null;
    }

    private static string _GetUECppDataTypeString(DataType dataType, JsonDetail JsonDetailType)
    {
        switch (dataType)
        {
            case DataType.Int:
                return "int32";
            case DataType.Long:
                return "int64";
            case DataType.Float:
                return "float";
            case DataType.Bool:
                return "bool";
            case DataType.String:
                return "FString";
            case DataType.Lang:
                return "FText";
            case DataType.Json:
                {
                    string tyStr = _getUECppJsonTypeString(JsonDetailType);
                    if (tyStr != null)
                        return tyStr;
                    break;
                }                
            default:
                break;
        }

        return "FLuaTable";
    }

    private static bool _IsSupportBP(DataType dataType)
    {
        switch (dataType)
        {
            case DataType.Long:
                return false;
            default:
                return true;
        }
    }

    private static bool _GetUESluaValueFuncString(DataType dataType, out string checkFunc, out string castFunc)
    {
        switch (dataType)
        {
            case DataType.Int:
                checkFunc = "isInt";
                castFunc = "asInt";
                break;
            case DataType.Long:
                checkFunc = "isInt";
                castFunc = "asInt64";
                break;
            case DataType.Float:
                checkFunc = "isNumber";
                castFunc = "asFloat";
                break;
            case DataType.Bool:
                checkFunc = "isBool";
                castFunc = "asBool";
                break;
            case DataType.String:
            case DataType.Lang:
                checkFunc = "isString";
                castFunc = "asString";
                break;
            default:
                checkFunc = null;
                castFunc = null;
                return false;
        }

        return true;
    }

    private static string _GetIndentation(int level)
    {
        if(level == 1)
        {
            return _CPP_CLASS_INDENTATION_STRING;
        }

        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < level; ++i)
            stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING);

        return stringBuilder.ToString();
    }
}
