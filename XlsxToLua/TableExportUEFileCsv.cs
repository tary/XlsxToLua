using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


public class UEFileReference
{
    public static bool ExportUEFileRefrenceCSV(Dictionary<string, TableInfo> TableInfoDic, out string errorString)
    {
        errorString = null;

        //<table.field, ref list>
        Dictionary<string, List<string>> refOfTable = new Dictionary<string, List<string>>();
        foreach (TableInfo table in TableInfoDic.Values)
        {
            List<FieldInfo> allField = table.GetAllFieldInfo();
            if (allField.Count == 0)
                continue;

            foreach (FieldInfo field in allField)
            {
                if (field.Data.Count == 0 || field.CheckRule == null || 
                    (field.DataType != DataType.String && !field.IsJsonArrayOfType(DataType.String) && !field.IsJsonDictOfType(DataType.String)))
                    continue;

                bool isStringArray = field.IsJsonArrayOfType(DataType.String);
                bool isStringMap = field.IsJsonDictOfType(DataType.String);
                if (field.DataType != DataType.String && !isStringArray && !isStringMap)
                    continue;

                if (field.CheckRule.IndexOf(AppValues.CheckRuleUEFileFlag, StringComparison.CurrentCultureIgnoreCase) == -1)
                    continue;

                List<string> refOfField = new List<string>();

                if (isStringArray || isStringMap)
                {
                    foreach (object obj in field.Data)
                    {
                        JsonData jsonData = obj as JsonData;
                        if (jsonData == null)
                            continue;

                        if (jsonData.IsArray || jsonData.IsObject)
                        {
                            for (int idx = 0; idx < jsonData.Count; ++idx)
                            {
                                string inputFileName = jsonData[idx].ToString().Trim();
                                if (string.IsNullOrEmpty(inputFileName))
                                    continue;
                                if (inputFileName.Length == 0 || refOfField.Contains(inputFileName))
                                    continue;
                                refOfField.Add(inputFileName);
                            }
                        }
                    }
                }
                else
                {
                    foreach (string value in field.Data)
                    {
                        string path = value.Trim();
                        if (path.Length == 0 || refOfField.Contains(path))
                            continue;
                        refOfField.Add(path);
                    }
                }                

                if (refOfField.Count == 0)
                    continue;

                string refDesc = table.TableName + "." + field.FieldName;
                refOfTable.Add(refDesc, refOfField);

                foreach(string path in refOfField)
                {
                    List<string> history = null;
                    if (refOfTable.ContainsKey(path))
                    {
                        history = refOfTable[path];
                    }
                    else
                    {
                        history = new List<string>();
                        refOfTable.Add(path, history);
                    }

                    history.Add(refDesc);
                }
            }
        }

        StreamWriter writer = new StreamWriter(Path.Combine(AppValues.UEFileRefPath, "UETableDepondence.csv"), false, new UTF8Encoding(false));
        writer.WriteLine("---,UEFile,Ref");
        foreach (string path in refOfTable.Keys)
        {
            int startIdx = path.IndexOf('\'');
            int endIdx = path.LastIndexOf('\'');
            if (startIdx >= endIdx)
                continue;

            string depPath = path.Substring(startIdx +1, endIdx - startIdx-1);

            writer.Write(depPath);
            writer.Write(",\"");
            writer.Write(depPath);
            writer.Write("\",\"(\"\"");
            List<string> Ref = refOfTable[path];
            string refStr = String.Join("\"\",\"\"", Ref);
            writer.Write(refStr);
            writer.WriteLine("\"\")\"");
        }

        writer.Flush();
        writer.Close();
        return true;
    }
}