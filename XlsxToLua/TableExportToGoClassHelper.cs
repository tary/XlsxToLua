using System;
using System.Collections.Generic;
using System.Text;

public class TableExportToGoClassHelper
{
    private static string _GO_CLASS_INDENTATION_STRING = "\t";

    public static bool ExportTableToGoClass(TableInfo tableInfo, out string errorString)
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
            keyType = "string";
        else
        {
            errorString = "主键类型只支持String和Int";
            return false;
        }

        //allFieldInfo.RemoveAt(0);
        string fileName = string.Concat(char.ToUpper(tableInfo.TableName[0]), tableInfo.TableName.Substring(1));
        string className = null;
        // 若统一配置了前缀和后缀，需进行添加（但注意如果配置了前缀，上面生成的驼峰式类名首字母要改为大写）
        if (string.IsNullOrEmpty(AppValues.ExportCsvClassClassNamePrefix))
            className = string.Concat(fileName, "Data");
        else
            className = string.Concat(AppValues.ExportCsvClassClassNamePrefix, fileName, "Data");

        if (string.IsNullOrEmpty(AppValues.ExportCsvClassClassNamePostfix) == false)
            className = className + AppValues.ExportCsvClassClassNamePostfix;

        return _ExportTableToGoHeader(tableInfo, allFieldInfo, keyType, className, fileName, out errorString);
    }


    private static bool _ExportTableToGoHeader(TableInfo tableInfo, List<FieldInfo> allFieldInfo, string keyType, string className, string fileName, out string errorString)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("package excel").AppendLine();
        stringBuilder.Append("import (").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).Append("\"encoding/json\"").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).Append("\"io/ioutil\"").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).Append("\"sync\"").AppendLine();
        stringBuilder.AppendLine(")").AppendLine();
        ////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////


        StringBuilder feildBuilder = new StringBuilder();
        stringBuilder.AppendFormat("type {0} struct {{", className).AppendLine();
        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            string valTypeName = _GetGoDataTypeString(fieldInfo.DataType, fieldInfo.JsonDetailType);
            string fieldName = string.Concat(char.ToUpper(fieldInfo.FieldName[0]), fieldInfo.FieldName.Substring(1));

            if (_IsLocalCache(fieldInfo.DataType, fieldInfo.JsonDetailType))
            {
                feildBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("{0} {1}", fieldName, valTypeName).AppendLine();
            }
        }
        
        stringBuilder.Append(feildBuilder);

        // 闭合类定义
        stringBuilder.AppendLine("}");


        // 定义相关接口函数
        string LockName = string.Concat(fileName, "Lock");
        stringBuilder.AppendFormat("var {0} map[{1}]*{2}", fileName, keyType, className).AppendLine();
        stringBuilder.AppendFormat("var {0} sync.RWMutex", LockName).AppendLine().AppendLine();


        ///////////////////////////////////////////////////////////////
        stringBuilder.AppendFormat("func Load{0}() {{", fileName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("{0}.Lock()", LockName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("defer {0}.Unlock()", LockName).AppendLine().AppendLine();

        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("data, err := ioutil.ReadFile(\"../res/excel/{0}.json\")", fileName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("if err != nil {{").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).Append(_GO_CLASS_INDENTATION_STRING).Append("panic(err)").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendLine("}").AppendLine();

        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("err = json.Unmarshal(data, &{0})", fileName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("if err != nil {{").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).Append(_GO_CLASS_INDENTATION_STRING).Append("panic(err)").AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendLine("}").AppendLine();

        stringBuilder.AppendLine("}").AppendLine();

        ///////////////////////////////////////////////////////////////
        stringBuilder.AppendFormat("func Get{0}Map() map[{1}]*{2} {{",fileName, keyType, className).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("return {0}", fileName).AppendLine();
        stringBuilder.AppendLine("}").AppendLine();


        ///////////////////////////////////////////////////////////////
        stringBuilder.AppendFormat("func Get{0}(key {1}) (*{2}, bool) {{", fileName, keyType, className).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("{0}.RLock()", LockName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("defer {0}.RUnlock()", LockName).AppendLine().AppendLine();

        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("val, ok := {0}[key]", fileName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("return val, ok").AppendLine();

        stringBuilder.AppendLine("}").AppendLine();



        ///////////////////////////////////////////////////////////////
        stringBuilder.AppendFormat("func Get{0}MapLen() int {{", fileName).AppendLine();
        stringBuilder.Append(_GO_CLASS_INDENTATION_STRING).AppendFormat("return len({0})", fileName).AppendLine();
        stringBuilder.AppendLine("}").AppendLine();


        if (Utils.SaveGoClassFile(tableInfo.TableName, fileName + ".go", stringBuilder.ToString()) == true)
        {
            errorString = null;
            return true;
        }
        else
        {
            errorString = "保存csv对应Go文件失败\n";
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
                return true;
            case DataType.Json:
                return JsonDetailType != null;
            default:
                return false;
        }
    }

    private static string _getGoJsonTypeString(JsonDetail jsonDetail)
    {
        if (null == jsonDetail) return null;

        switch (jsonDetail.ContentType)
        {
            case DataType.Array:
                return string.Concat("[]", _GetGoDataTypeString(jsonDetail.ValueType, null));
            case DataType.Dict:
                return string.Concat("map[", _GetGoDataTypeString(jsonDetail.KeyType, null), "]", _GetGoDataTypeString(jsonDetail.ValueType, null));
            default:
                break;
        }

        return null;
    }

    private static string _GetGoDataTypeString(DataType dataType, JsonDetail JsonDetailType)
    {
        switch (dataType)
        {
            case DataType.Int:
                return "int32";
            case DataType.Long:
                return "int64";
            case DataType.Float:
                return "float32";
            case DataType.Bool:
                return "bool";
            case DataType.String:
                return "string";
            case DataType.Json:
                {
                    string tyStr = _getGoJsonTypeString(JsonDetailType);
                    if (tyStr != null)
                        return tyStr;
                    break;
                }
            default:
                break;
        }

        return "string";
    }

}

