using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
public class XlsxLangReader
{
    public static Dictionary<string, string> ParseXlsxConfigFile(string filePath, string sheetName, out string errorString)
    {
        if (!File.Exists(filePath))
        {
            errorString = string.Format("错误：TxtConfig文件不存在，输入路径为{0}", filePath);
            return null;
        }

        Dictionary<string, string> result = new Dictionary<string, string>();

        DataSet ds = XlsxReader.ReadXlsxFile(filePath, out errorString);
        if(!string.IsNullOrEmpty(errorString))
        {
            return null;
        }

        DataTable dt = ds.Tables[sheetName+"$"];
        if (dt == null)
        {
            errorString = "国际表内不含制定名称的工作表:"+ sheetName;
            return null;
        }

        if (dt.Rows.Count <= AppValues.DATA_FIELD_LANG_DATA_START_INDEX)
        {
            errorString = "表格格式不符合要求，必须在表格首行声明字段名";
            return null;
        }
        if (dt.Columns.Count < 2)
        {
            errorString = "表格中至少需要配置2个字段";
            return null;
        }

        const int KEY_COLUMN_IDX = 0;
        const int VALUE_COLUMN_IDX = 1;

        for (int row = AppValues.DATA_FIELD_LANG_DATA_START_INDEX; row < dt.Rows.Count; ++row)
        {
            string inputKey = dt.Rows[row][KEY_COLUMN_IDX].ToString().Trim();
            if (string.IsNullOrEmpty(inputKey))
            {
                continue;
            }

            string inputVal = dt.Rows[row][VALUE_COLUMN_IDX].ToString().Trim();
            result.Add(inputKey, inputVal);
        }

        errorString = null;
        return result;
    }    
}
