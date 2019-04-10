using System;
using System.Collections.Generic;
using System.Text;

public class TableExportLangHelper
{
    private static string _LANG_SPLICE_STRING = ":";
    private static string _LANG_EMPTY_STRING = "---";

    public static bool ExportTableLang(TableInfo tableInfo, bool firstTable, out string errorString)
    {
        List<FieldInfo> allFieldInfo = tableInfo.GetAllClientFieldInfo();
        allFieldInfo.RemoveAt(0);
        string fileName = string.Concat(char.ToUpper(tableInfo.TableName[0]), tableInfo.TableName.Substring(1), "Table");


        StringBuilder stringBuilder = new StringBuilder();
        if (string.IsNullOrEmpty(AppValues.ExportLangPath))
        {
            errorString = "Export Lang 输出路径为空\n";
            return false;
        }

        foreach (FieldInfo fieldInfo in allFieldInfo)
        {
            if (fieldInfo.DataType != DataType.Lang)
                continue;

            for (int Idx = 0; Idx < fieldInfo.LangKeys.Count; ++Idx)
            {
                string defaultVal = fieldInfo.LangDefaultValues[Idx];
                if (string.IsNullOrEmpty(defaultVal))
                {
                    defaultVal = _LANG_EMPTY_STRING;
                }

                stringBuilder.AppendFormat("{0}{1}{2}", fieldInfo.LangKeys[Idx], _LANG_SPLICE_STRING, defaultVal).AppendLine();
            }
        }

        if (Utils.SaveLangFile(stringBuilder.ToString(), !firstTable) == true)
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
}
