using System;
using System.Collections.Generic;
using System.Text;

public class TableExportToUESluaClassHelper
{
    private static string _CPP_CLASS_INDENTATION_STRING = "\t";

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
        stringBuilder.AppendLine(string.Concat(_CPP_CLASS_INDENTATION_STRING, "GENERATED_BODY()"));
        stringBuilder.AppendLine("public:");


        //////////////////////////////////////////////////////////
        ///Functions
        string UFunctionDef = string.Concat(_CPP_CLASS_INDENTATION_STRING, "UFUNCTION(BlueprintCallable, Category = \"Tables|", fileName, "\")");

        stringBuilder.AppendLine(UFunctionDef);
        stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
        stringBuilder.AppendFormat("static bool GetTableItem({0} Key, FLuaTable& OutVal);", keyType).AppendLine().AppendLine();

        //bool GetNAME(TYPE& OutVal);
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            stringBuilder.AppendLine(UFunctionDef);

            stringBuilder.Append(_CPP_CLASS_INDENTATION_STRING);
            string valTypeName = _GetUESluaClassTableStringDataType(fieldInfo.DataType);
            string feilName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            stringBuilder.AppendFormat("static bool Get{0}({1} Key, {2}& OutVal);", feilName, keyType, valTypeName);
            stringBuilder.AppendLine();
        }

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
        ///Functions
        ///

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
        //bool GetNAME(TYPE& OutVal);
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            string valTypeName = _GetUESluaClassTableStringDataType(fieldInfo.DataType);
            string feilName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            stringBuilder.AppendFormat("bool {0}::Get{1}({2} Key, {3}& OutVal)", className, feilName, keyType, valTypeName);
            stringBuilder.AppendLine("{");
            {
                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendLine("slua::LuaState* mainState = slua::LuaState::get();");

                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendLine("if (!mainState) { return false; }");
                stringBuilder.Append(_GetIndentation(1));

                //mainState->call("GetTableItemField", {0}, Key, FeildName);
                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendFormat("slua::LuaVar ResultLuaVar = mainState->call(\"GetTableItemField\", \"{0}\", Key, \"{1}\");", fieldInfo.TableName, fieldInfo.FieldName).AppendLine();

                stringBuilder.Append(_GetIndentation(1));
                stringBuilder.AppendLine("if (!ResultLuaVar.isValid() || ResultLuaVar.isNil()) { return false; }").AppendLine();

                string checkFunc = null;
                string castFunc = null;
                if (_GetUESluaValueFuncString(fieldInfo.DataType, out checkFunc, out castFunc))
                {
                    stringBuilder.Append(_GetIndentation(1));
                    //stringBuilder.AppendFormat("if (!ResultLuaVar.{0}()) { return false; }", checkFunc);
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

    private static string _GetUESluaClassTableStringDataType(DataType dataType)
    {
        switch (dataType)
        {
            case DataType.Int:
                return "int32";
            case DataType.Long:
                return "int32";
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

    private static bool _GetUESluaValueFuncString(DataType dataType, out string checkFunc, out string castFunc)
    {
        switch (dataType)
        {
            case DataType.Int:
            case DataType.Long:
                checkFunc = "isInt";
                castFunc = "asInt";
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
