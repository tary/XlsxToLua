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
        stringBuilder.AppendLine("#include \"CoreMinimal.h\"");
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
        stringBuilder.AppendFormat("class {0} {1} : public UObject", AppValues.ExportUESluaExportAPIName, className).AppendLine();

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

        feildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine(_CPP_CLASS_PROPERTY_STRING);
        feildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine("FLuaTable _Row;");


        StringBuilder noneStaticFeildBuilder = new StringBuilder();

        noneStaticFeildBuilder.AppendLine(UFunctionDef);
        noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
        noneStaticFeildBuilder.AppendFormat("bool InitializeLoad({0} Key);", keyType).AppendLine();


        //bool GetNAME(TYPE& OutVal);
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {

            if (_IsSupportBP(fieldInfo.DataType))
            {
                stringBuilder.AppendLine(UFunctionDef);
            }

            stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
            string valTypeName = _GetUESluaClassTableStringDataType(fieldInfo.DataType);
            string feildName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            stringBuilder.AppendFormat("static bool Get{0}({1} Key, {2}& OutVal);", feildName, keyType, valTypeName);
            stringBuilder.AppendLine();

            if (_IsSupportBP(fieldInfo.DataType))
            {
                noneStaticFeildBuilder.AppendLine(UFunctionDef);
            }

            if(_IsLocalCache(fieldInfo.DataType))
                noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("bool Load{0}();", feildName).AppendLine();
            else
                noneStaticFeildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("bool Load{0}({1}& OutVal);", feildName, valTypeName).AppendLine();

            if(_IsLocalCache(fieldInfo.DataType))
            {
                if (_IsSupportBP(fieldInfo.DataType))
                {
                    feildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine(_CPP_CLASS_PROPERTY_STRING);
                }
                feildBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat("{0} {1};", valTypeName, feildName).AppendLine();
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


    private static bool _ExportTableToUESluaCode(TableInfo tableInfo, List<FieldInfo> allFieldInfo, string keyType, string className, string fileName, out string errorString)
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.AppendFormat("#include \"{0}.h\"", fileName).AppendLine();
        stringBuilder.AppendLine("#include \"LuaState.h\"").AppendLine().AppendLine();

        //////////////////////////////////////////////////////////
        ///Construct
        stringBuilder.AppendFormat("{0}::{0}(const FObjectInitializer& ObjectInitializer)", className).AppendLine();
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine(": Super(ObjectInitializer)");
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            string defaultVal = null;
            string feildName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            switch (fieldInfo.DataType)
            {
                case DataType.Int:
                case DataType.Long:
                    defaultVal = "0";
                    break;
                case DataType.Float:
                    defaultVal = "0.f";
                    break;
                case DataType.Bool:
                    defaultVal = "false";
                    break;
                default:
                    break;
            }

            if (defaultVal != null)
            {
                stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendFormat(", {0}({1})", feildName, defaultVal).AppendLine();
            }
        }
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING).AppendLine("{ }").AppendLine();

        //////////////////////////////////////////////////////////
        ///Functions
        ///
        
        stringBuilder.AppendFormat("bool {0}::InitializeLoad({1} Key)", className, keyType).AppendLine();
        stringBuilder.Append("{");
        {
            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("slua::LuaState* mainState = slua::LuaState::get();");
            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("if (!mainState) { return false; }");
            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendFormat("slua::LuaVar ResultLuaVar = mainState->call(\"GetTableItem\", \"{0}\", Key);", tableInfo.TableName).AppendLine();

            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }").AppendLine();

            stringBuilder.Append(_GetIndentation(1)).AppendLine("_Row.Table = ResultLuaVar;");

            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("return true;");
        }
        stringBuilder.AppendLine("}").AppendLine();

        stringBuilder.AppendFormat("bool {0}::GetTableItem({1} Key, FLuaTable& OutVal)", className, keyType).AppendLine();
        stringBuilder.AppendLine("{");
        {
            //mainState->call("GetTableItem", {0}, Key);
            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("slua::LuaState* mainState = slua::LuaState::get();");
            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("if (!mainState) { return false; }");
            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendFormat("slua::LuaVar ResultLuaVar = mainState->call(\"GetTableItem\", \"{0}\", Key);", tableInfo.TableName).AppendLine();

            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }").AppendLine();

            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("OutVal.Table = ResultLuaVar;");

            stringBuilder.Append(_GetIndentation(1));
            stringBuilder.AppendLine("return true;");
        }
        stringBuilder.AppendLine("}").AppendLine();

        StringBuilder noneStaticFeildBuilder = new StringBuilder();

        //bool GetNAME(TYPE& OutVal);
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            string valTypeName = _GetUESluaClassTableStringDataType(fieldInfo.DataType);
            string feildName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            if (_IsLocalCache(fieldInfo.DataType))
                noneStaticFeildBuilder.AppendFormat("bool {0}::Load{1}()", className, feildName).AppendLine();
            else
                noneStaticFeildBuilder.AppendFormat("bool {0}::Load{1}({2}& OutVal)", className, feildName, valTypeName).AppendLine();

            noneStaticFeildBuilder.AppendLine("{");
            {
                noneStaticFeildBuilder.Append(_GetIndentation(1)).AppendLine("if (!_Row.Table.isValid() || _Row.Table.isNil()) return false;");

                if (_IsLocalCache(fieldInfo.DataType))
                    noneStaticFeildBuilder.Append(_GetIndentation(1)).AppendFormat("return _Row.GetFromTable(\"{0}\", {1});", fieldInfo.FieldName, feildName).AppendLine();
                else
                    noneStaticFeildBuilder.Append(_GetIndentation(1)).AppendFormat("return _Row.GetFromTable(\"{0}\", OutVal);", fieldInfo.FieldName).AppendLine();
            }
            noneStaticFeildBuilder.AppendLine("}").AppendLine();


            stringBuilder.AppendFormat("bool {0}::Get{1}({2} Key, {3}& OutVal)", className, feildName, keyType, valTypeName);
            stringBuilder.AppendLine("{");
            {
                stringBuilder.Append(_GetIndentation(1)).AppendLine("slua::LuaState* mainState = slua::LuaState::get();");

                stringBuilder.Append(_GetIndentation(1)).AppendLine("if (!mainState) { return false; }");

                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendFormat("slua::LuaVar ResultLuaVar = mainState->call(\"GetTableItemField\", \"{0}\", Key, \"{1}\");", fieldInfo.TableName, fieldInfo.FieldName).AppendLine();

                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendLine("if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }").AppendLine();

                string checkFunc = null;
                string castFunc = null;
                if (_GetUESluaValueFuncString(fieldInfo.DataType, out checkFunc, out castFunc))
                {
                    stringBuilder.Append(_GetIndentation(1));
                    stringBuilder.AppendFormat("if (!ResultLuaVar.{0}()) {{ return false; }}", checkFunc).AppendLine();

                    stringBuilder.Append(_GetIndentation(1));

                    if (fieldInfo.DataType == DataType.Lang || fieldInfo.DataType == DataType.String)
                        stringBuilder.AppendFormat("OutVal = UTF8_TO_TCHAR(ResultLuaVar.{0}());", castFunc).AppendLine();
                    else
                        stringBuilder.AppendFormat("OutVal = ResultLuaVar.{0}();", castFunc).AppendLine();
                }
                else
                {
                    stringBuilder.Append(_GetIndentation(1));
                    stringBuilder.AppendLine("OutVal.Table = ResultLuaVar;");
                }

                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendLine("return true;");
            }
            stringBuilder.AppendLine("}").AppendLine();
        }

        stringBuilder.AppendLine().Append(noneStaticFeildBuilder).AppendLine();
        

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

    private static bool _IsLocalCache(DataType dataType)
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
            default:
                return false;
        }
    }
    private static string _GetUESluaClassTableStringDataType(DataType dataType)
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
                return "FString";
            default:
                return "FLuaTable";
        }
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
                checkFunc = "isInt() && !ResultLuaVar.isNumber";
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
