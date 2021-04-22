using System;
using System.Collections.Generic;
using System.Text;

public class TableExportToUEBPCookHelper
{
    private static string _CPP_CLASS_INDENTATION_STRING = "\t";

    public static bool ExportTableToUEBPCookList(ref Dictionary<string, string> ExportTableNameAndFileName, ref Dictionary<string, TableInfo> tableInfoMap, out string errorString)
    {
        errorString = null;
        Dictionary<string, List<List<string>>> AllPath = new Dictionary<string, List<List<string>>>();
        foreach (var item in ExportTableNameAndFileName)
        {
            string tableName = item.Key;
            string fileName = item.Value;
            TableInfo tableInfo = tableInfoMap[tableName];
            Dictionary<string, List<string>> tableConfigRef = tableInfo.TableConfig;
            if (AppValues.AnalyzeIsGroupIgnore(ref tableConfigRef, fileName))
                continue;

            int dataCount = tableInfo.GetKeyColumnFieldInfo().Data.Count;
            if (dataCount == 0)
            {
                continue;
            }
            List<FieldInfo> allField = tableInfo.GetAllClientFieldInfo();
            if (allField.Count == 0)
            {
                continue;
            }
            List<List<string>> PathOfTable = new List<List<string>>();
            AllPath.Add(tableName, PathOfTable);

            foreach(FieldInfo field in allField)
            {
                if (field.DataType != DataType.UEPath)
                    continue;
                List<string> feildList = new List<string>();
                PathOfTable.Add(feildList);

                for (int Idx = 0; Idx < dataCount; ++Idx)
                {
                    string filePath = (string)field.Data[Idx];
                    filePath = filePath.Trim();
                    if (filePath.Length == 0)
                        continue;

                    feildList.Add(filePath);
                }
            }


        }
        return true;

//         try
//         {
//             string savePath = Utils.CombinePath(AppValues.ExportBPCookPath, fileName);
//             StreamWriter writer = new StreamWriter(savePath, false, new UTF8Encoding(false));
//             writer.Write(content);
//             writer.Flush();
//             writer.Close();
//             return true;
//         }
//         catch
//         {
//             return false;
//         }
    }


    private static bool _ExportTableToUESluaHeader(TableInfo tableInfo, List<FieldInfo> allFieldInfo, string keyType, string className, string fileName, out string errorString)
    {
        StringBuilder stringBuilder = new StringBuilder();
        errorString = null;
        return false;
    }
    
}
