﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

// 注意：要在本项目属性的“生成”选项卡中将“目标平台”由默认的“Any CPU”改为“x86”，
// 否则即便安装了AccessDatabaseEngine，在64位系统安装32位Office（Microsoft.ACE.OLEDB.12.0也就是32位的），然后64位的VS默认编译为64位程序仍将导致无法连接Excel，提示本机未注册Microsoft.ACE.OLEDB.12.0提供程序

public class Program
{
    /// <summary>
    /// 传入参数中，第1个必须为Excel表格所在目录，第2个必须为存放导出lua文件的目录，第3个参数为项目Client目录的路径（无需文件存在型检查规则则填-noClient），第4个参数为必须为lang文件路径（没有填-noLang）
    /// 可附加参数有：
    /// 1)  -exportIncludeSubfolder（将要导出的Excel文件夹下的各级子文件夹中的Excel文件也进行导出，不声明则仅导出选定文件夹直接下属的Excel文件）
    /// 2)  -exportKeepDirectoryStructure（进行各种导出时将生成的文件按原Excel文件所在的目录结构进行存储，不声明则会将生成的文件均存放在同级目录下）
    /// 3)  -exportMySQL（将表格数据导出到MySQL数据库中，默认不导出）
    /// 4)  -columnInfo（在生成lua文件的最上方用注释形式显示列信息，默认不开启）
    /// 5)  -unchecked（不对表格进行查错，不推荐使用）
    /// 6)  -printEmptyStringWhenLangNotMatching（当lang型数据key在lang文件中找不到对应值时，在lua文件输出字段值为空字符串即xx = ""，默认为输出nil）
    /// 7)  -part（后面在英文小括号内声明本次要导出的Excel文件名，用|分隔，未声明的文件将被本工具忽略）
    /// 8)  -except（后面在英文小括号内声明本次要忽略导出的Excel文件名，用|分隔，注意不允许对同一张表格既声明-part又声明-except）
    /// 9)  -allowedNullNumber（允许int、long、float型字段下填写空值，默认不允许）
    /// 10) 声明要将指定的Excel表导出为csv文件需要以下2个参数：
    ///     -exportCsv（后面在英文小括号内声明本次要额外导出csv文件的Excel文件名，用|分隔，或者用$all表示全部。注意如果-part参数中未指定本次要导出某个Excel表，即便声明要导出csv文件也不会生效）
    ///     -exportCsvParam（可声明导出csv文件的参数）
    /// 11) 声明要将指定的Excel表导出为csv对应的C#文件需要以下2个参数：
    ///     -exportCsClass（后面在英文小括号内声明本次要额外导出csv对应C#类文件的Excel文件名，用|分隔，或者用$all表示全部。注意如果-part参数中未指定本次要导出某个Excel表，即便声明要导出csv文件也不会生效）
    ///     -exportCsClassParam（可声明导出csv对应C#类文件的参数）
    /// 12) 声明要将指定的Excel表导出为csv对应的Java文件需要以下2个参数：
    ///     -exportJavaClass（后面在英文小括号内声明本次要额外导出csv对应Java类文件的Excel文件名，用|分隔，或者用$all表示全部。注意如果-part参数中未指定本次要导出某个Excel表，即便声明要导出csv文件也不会生效）
    ///     -exportJavaClassParam（可声明导出csv对应Java类文件的参数）
    /// 13) -autoNameCsvClassParam（通过classNamePrefix、classNamePostfix两个子参数设置当导出csv对应的C#或Java类文件时，由本工具自动根据Excel名命名时统一追加前后缀）
    /// 14) 声明要将指定的Excel表导出为json文件需要以下2个参数：
    ///     -exportJson（后面在英文小括号内声明本次要额外导出json文件的Excel文件名，用|分隔，或者用$all表示全部。注意如果-part参数中未指定本次要导出某个Excel表，即便声明要导出json文件也不会生效）
    ///     -exportJsonParam（可声明导出json文件的参数）
    /// </summary>
    static int Main(string[] args)
    {
        int errorLevel = 0;

        // 检查第1个参数（Excel表格所在目录）是否正确
        if (args.Length < 1)
            Utils.LogErrorAndExit("错误：未输入Excel表格所在目录");
        if (!Directory.Exists(args[0]))
            Utils.LogErrorAndExit(string.Format("错误：输入的Excel表格所在目录不存在，路径为{0}", args[0]));

        AppValues.ExcelFolderPath = Path.GetFullPath(args[0]);
        Utils.Log(string.Format("选择的Excel所在路径：{0}", AppValues.ExcelFolderPath));

        // 检查第2个参数（存放导出lua文件的目录）是否正确
        if (args.Length < 2)
            Utils.LogErrorAndExit("错误：未输入要将生成lua文件存放的路径");
        if (!Directory.Exists(args[1]))
            Utils.LogErrorAndExit(string.Format("错误：输入的lua文件导出路径不存在，路径为{0}", args[1]));

        AppValues.ExportLuaFilePath = Path.GetFullPath(args[1]);
        Utils.Log(string.Format("选择的lua文件导出路径：{0}", AppValues.ExportLuaFilePath));
        // 检查第3个参数（项目Client目录的路径）是否正确
        if (args.Length < 3)
            Utils.LogErrorAndExit("错误：未输入项目Client目录的路径，如果不需要请输入参数-noClient");
        if (AppValues.NO_CLIENT_PATH_STRING.Equals(args[2], StringComparison.CurrentCultureIgnoreCase))
        {
            if (AppValues.VerboseModeFlag)
                Utils.LogWarning("警告：你选择了不指定Client文件夹路径，则本工具无法检查表格中填写的图片路径等对应的文件是否存在");
            AppValues.ClientPath = null;
        }
        else if (Directory.Exists(args[2]))
        {
            AppValues.ClientPath = Path.GetFullPath(args[2]);
            Utils.Log(string.Format("Client目录完整路径：{0}", AppValues.ClientPath));
        }
        else
            Utils.LogErrorAndExit(string.Format("错误：请检查输入的Client路径是否正确{0}", args[2]));

        // 检查第4个参数（lang文件路径）是否正确
        if (args.Length < 5)
            Utils.LogErrorAndExit("错误：未输入lang文件路径或未声明不含lang文件（使用-noLang）");
        if (AppValues.NO_LANG_PARAM_STRING.Equals(args[3], StringComparison.CurrentCultureIgnoreCase))
        {
            AppValues.LangFilePath = null;
            Utils.Log("选择的lang文件路径：无");
        }
        else if (File.Exists(args[3]))
        {
            AppValues.LangFilePath = Path.GetFullPath(args[3]);
            Utils.Log(string.Format("选择的lang文件路径：{0}", AppValues.LangFilePath));

            // 解析lang文件
            string errorString = null;

            if (AppValues.LangFilePath.EndsWith("." + AppValues.XLSX_EXTENSION))
            {
                string langName = (args.Length >= 4) ? args[4] : "default";
                AppValues.LangData = XlsxLangReader.ParseXlsxConfigFile(AppValues.LangFilePath, langName, out errorString);
            }
            else
                AppValues.LangData = TxtConfigReader.ParseTxtConfigFile(AppValues.LangFilePath, ":", out errorString);

            if (!string.IsNullOrEmpty(errorString))
                Utils.LogErrorAndExit(errorString);
        }
        else
            Utils.LogErrorAndExit(string.Format("错误：输入的lang文件不存在，路径为{0}", args[3]));

        int ParamStartIndex = (AppValues.LangFilePath == null) ? 4 : 5;

        // 生成Excel文件夹中所有表格文件信息
        List<string> existExcelFileNames = new List<string>();
        // 先判断是否指定要导出Excel文件夹下属子文件夹中的Excel文件，若要导出还需要确保不同文件夹下的Excel文件也不允许同名
        for (int i = ParamStartIndex; i < args.Length; ++i)
        {
            string tempParam = args[i];
            if (tempParam.Equals(AppValues.EXPORT_INCLUDE_SUBFOLDER_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.IsExportIncludeSubfolder = true;

                // 检查Excel文件所在目录及其子目录下都不存在同名文件
                // 记录重名文件所在目录
                Dictionary<string, List<string>> sameExcelNameInfo = new Dictionary<string, List<string>>();
                foreach (string filePath in Directory.GetFiles(AppValues.ExcelFolderPath, "*.xlsx", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (fileName.StartsWith(AppValues.EXCEL_TEMP_FILE_FILE_NAME_START_STRING))
                    {
                        Utils.LogWarning(string.Format("目录中的{0}文件为Excel自动生成的临时文件，将被忽略处理", filePath));
                        continue;
                    }

                    if (AppValues.ExportTableNameAndPath.ContainsKey(fileName))
                    {
                        if (!sameExcelNameInfo.ContainsKey(fileName))
                        {
                            sameExcelNameInfo.Add(fileName, new List<string>());
                            sameExcelNameInfo[fileName].Add(AppValues.ExportTableNameAndPath[fileName]);
                        }

                        sameExcelNameInfo[fileName].Add(filePath);
                    }
                    else
                    {
                        existExcelFileNames.Add(fileName);
                        AppValues.ExportTableNameAndPath.Add(fileName, filePath);
                    }
                }

                if (sameExcelNameInfo.Count > 0)
                {
                    StringBuilder sameExcelNameErrorStringBuilder = new StringBuilder();
                    sameExcelNameErrorStringBuilder.AppendLine("错误：Excel文件夹及其子文件夹中不允许出现同名文件，重名文件如下：");
                    foreach (var item in sameExcelNameInfo)
                    {
                        string fileName = item.Key;
                        List<string> filePath = item.Value;
                        sameExcelNameErrorStringBuilder.AppendFormat("以下路径中存在同名文件（{0}）：\n", fileName);
                        foreach (string oneFilePath in filePath)
                            sameExcelNameErrorStringBuilder.AppendLine(oneFilePath);
                    }

                    Utils.LogErrorAndExit(sameExcelNameErrorStringBuilder.ToString());
                }

                break;
            }
        }
        if (AppValues.IsExportIncludeSubfolder == false)
        {
            foreach (string filePath in Directory.GetFiles(AppValues.ExcelFolderPath, "*.xlsx", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.StartsWith(AppValues.EXCEL_TEMP_FILE_FILE_NAME_START_STRING))
                {
                    Utils.LogWarning(string.Format("目录中的{0}文件为Excel自动生成的临时文件，将被忽略处理", filePath));
                    continue;
                }

                existExcelFileNames.Add(fileName);
                AppValues.ExportTableNameAndPath.Add(fileName, filePath);
            }
        }

        // 记录本次运行是否指定仅导出部分Excel文件
        bool isExportPart = false;

        // 检查其他参数
        for (int i = ParamStartIndex; i < args.Length; ++i)
        {
            string param = args[i];

            if (param.Equals(AppValues.EXPORT_INCLUDE_SUBFOLDER_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                continue;
            else if (param.Equals(AppValues.EXPORT_KEEP_DIRECTORY_STRUCTURE_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                if (AppValues.IsExportIncludeSubfolder == false)
                {
                    string warningString = string.Format("警告：只有通过设置{0}参数，将要导出的Excel文件夹下的各级子文件夹中的Excel文件也进行导出时，指定{1}参数设置将生成的文件按原Excel文件所在的目录结构进行存储才有意义，请检查是否遗漏声明{0}参数", AppValues.EXPORT_INCLUDE_SUBFOLDER_PARAM_STRING, AppValues.EXPORT_KEEP_DIRECTORY_STRUCTURE_PARAM_STRING);
                    Utils.LogWarning(warningString);
                }
                else
                    AppValues.IsExportKeepDirectoryStructure = true;
            }
            else if (param.Equals(AppValues.UNCHECKED_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.IsNeedCheck = false;
                if (AppValues.VerboseModeFlag)
                    Utils.Log("警告：你选择了不进行表格检查，请务必自己保证表格的正确性", ConsoleColor.Cyan);
            }
            else if (param.Equals(AppValues.NEED_COLUMN_INFO_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.IsNeedColumnInfo = true;
                if (AppValues.VerboseModeFlag)
                    Utils.Log("你选择了在生成的lua文件最上方用注释形式显示列信息", ConsoleColor.Cyan);
            }
            else if (param.Equals(AppValues.EXPORT_MYSQL_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.IsExportMySQL = true;
                Utils.LogWarning("你选择了导出表格数据到MySQL数据库");
            }
            else if (param.StartsWith(AppValues.EXCEPT_EXPORT_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                // 解析声明的本次忽略导出的Excel名
                string errorString = null;
                string[] fileNames = Utils.GetExcelFileNames(param, out errorString);
                if (errorString != null)
                    Utils.LogErrorAndExit(string.Format("错误：声明忽略导出部分Excel表格的参数{0}后{1}", AppValues.EXCEPT_EXPORT_PARAM_STRING, errorString));
                else
                {
                    foreach (string fileName in fileNames)
                        AppValues.ExceptExportTableNames.Add(fileName.Trim());

                    // 检查要忽略导出的Excel文件是否存在
                    foreach (string exceptExportExcelFileName in AppValues.ExceptExportTableNames)
                    {
                        if (!existExcelFileNames.Contains(exceptExportExcelFileName))
                            Utils.LogErrorAndExit(string.Format("设置要忽略导出的Excel文件（{0}）不存在，请检查后重试并注意区分大小写", Utils.CombinePath(AppValues.ExcelFolderPath, string.Concat(exceptExportExcelFileName, ".xlsx"))));
                    }

                    Utils.LogWarning(string.Format("警告：本次将忽略以下Excel文件：\n{0}\n", Utils.CombineString(AppValues.ExceptExportTableNames, ", ")));
                }
            }
            else if (param.StartsWith(AppValues.PART_EXPORT_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                isExportPart = true;

                // 解析声明的本次要导出的Excel名
                string errorString = null;
                string[] fileNames = Utils.GetExcelFileNames(param, out errorString);
                List<string> tempExportTableName = new List<string>();
                if (errorString != null)
                    Utils.LogErrorAndExit(string.Format("错误：声明导出部分Excel表格的参数{0}后{1}", AppValues.PART_EXPORT_PARAM_STRING, errorString));
                else
                {
                    foreach (string inputFileName in fileNames)
                    {
                        string fileName = inputFileName.Trim();
                        if (!tempExportTableName.Contains(fileName))
                            tempExportTableName.Add(fileName);
                    }

                    // 检查指定导出的Excel文件是否存在
                    foreach (string exportExcelFileName in tempExportTableName)
                    {
                        if (!existExcelFileNames.Contains(exportExcelFileName))
                            Utils.LogErrorAndExit(string.Format("要求导出的Excel文件（{0}）不存在，请检查后重试并注意区分大小写", Utils.CombinePath(AppValues.ExcelFolderPath, string.Concat(exportExcelFileName, ".xlsx"))));
                    }

                    Utils.LogWarning(string.Format("警告：本次将仅检查并导出以下Excel文件：\n{0}\n", Utils.CombineString(tempExportTableName, ", ")));
                    Dictionary<string, string> tempExportTableNameAndPath = new Dictionary<string, string>();
                    foreach (string exportExcelFileName in tempExportTableName)
                        tempExportTableNameAndPath.Add(exportExcelFileName, AppValues.ExportTableNameAndPath[exportExcelFileName]);

                    AppValues.ExportTableNameAndPath = tempExportTableNameAndPath;
                }
            }
            // 对额外导出csv对应C#或Java类文件自动命名类名时统一添加的前后缀配置
            else if (param.StartsWith(AppValues.AUTO_NAME_CSV_CLASS_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                string paramString;
                if (!Utils.GetInnerBracketParam(param, out paramString))
                {
                    Utils.LogErrorAndExit(string.Format("错误：声明自动命名导出csv对应的C#或Java类名规则的参数{0}后必须在英文小括号内声明各个具体参数", AppValues.AUTO_NAME_CSV_CLASS_PARAM_STRING));
                }
                else if (string.IsNullOrEmpty(paramString))
                {
                    Utils.LogWarning(string.Format("警告：声明的{0}参数没有在小括号中一并声明下属的子参数{1}和{2}来配置自动命名导出csv对应的C#或Java类名时统一添加的前后缀，若不想设置，可以直接不配置此参数，而不是将参数值留空", AppValues.AUTO_NAME_CSV_CLASS_PARAM_STRING, AppValues.AUTO_NAME_CSV_CLASS_PARAM_CLASS_NAME_PREFIX_PARAM_STRING, AppValues.AUTO_NAME_CSV_CLASS_PARAM_CLASS_NAME_POSTFIX_PARAM_STRING));
                }  
                else
                {
                    // 通过|分隔各个参数
                    string[] paramStringList = paramString.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    // 解析各个具体参数
                    foreach (string oneParamString in paramStringList)
                    {
                        string[] keyAndValue = oneParamString.Split(new char[] { '=' });
                        if (keyAndValue.Length != 2)
                            Utils.LogErrorAndExit(string.Format("声明的{0}参数下属的参数字符串{1}错误，参数名和配置值之间应用=分隔", AppValues.AUTO_NAME_CSV_CLASS_PARAM_STRING, oneParamString));
                        else
                        {
                            string key = keyAndValue[0].Trim();
                            string value = keyAndValue[1].Trim();
                            if (AppValues.AUTO_NAME_CSV_CLASS_PARAM_CLASS_NAME_PREFIX_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                AppValues.ExportCsvClassClassNamePrefix = value;
                                Utils.LogWarning(string.Format("导出csv对应的C#或Java类文件时，若进行类名的自动命名，将统一添加类名前缀为“{0}”", value));
                            }
                            else if (AppValues.AUTO_NAME_CSV_CLASS_PARAM_CLASS_NAME_POSTFIX_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                AppValues.ExportCsvClassClassNamePostfix = value;
                                Utils.LogWarning(string.Format("导出csv对应的C#或Java类文件时，若进行类名的自动命名，将统一添加类名后缀为“{0}”", value));
                            }
                            else
                                Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}非法", AppValues.AUTO_NAME_CSV_CLASS_PARAM_STRING, key));
                        }
                    }
                }
            }
            // 注意：-exportCsv与-exportCsvParam均以-exportCsv开头，故要先判断-exportCsvParam分支。这里将-exportCsvParam的解析放到-exportCsv的解析之中是为了只有声明了进行csv文件导出时才解析导出参数
            else if (param.StartsWith(AppValues.EXPORT_CSV_PARAM_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                continue;
            else if (param.StartsWith(AppValues.EXPORT_CSV_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                // 首先解析并判断配置的csv文件导出参数是否正确
                string exportCsvParamString = null;
                for (int j = ParamStartIndex; j < args.Length; ++j)
                {
                    string tempParam = args[j];
                    if (tempParam.StartsWith(AppValues.EXPORT_CSV_PARAM_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                    {
                        exportCsvParamString = tempParam;
                        break;
                    }
                }

                if (exportCsvParamString == null)
                {
                    Utils.LogErrorAndExit(string.Format("错误：声明要额外导出指定Excel文件为csv文件，就必须同时声明用于配置csv文件导出参数的{0}", AppValues.EXPORT_CSV_PARAM_PARAM_STRING));
                }

                string innerBracketParam;
                if(!Utils.GetInnerBracketParam(exportCsvParamString, out innerBracketParam))
                {
                    Utils.LogErrorAndExit(string.Format("错误：声明导出csv文件的参数{0}后必须在英文小括号内声明各个具体参数", AppValues.EXPORT_CSV_PARAM_PARAM_STRING));
                }
                else
                {
                    // 通过|分隔各个参数，但因为用户设置的csv文件中的字段分隔符本身可能为|，本工具采用\|配置进行转义，故需要自行从头遍历查找真正的参数分隔符
                    // 记录参数分隔符的下标位置
                    List<int> splitParamCharIndex = new List<int>();
                    for (int index = 0; index < innerBracketParam.Length; ++index)
                    {
                        char c = innerBracketParam[index];
                        if (c == '|' && (index < 1 || (index > 1 && innerBracketParam[index - 1] != '\\')))
                            splitParamCharIndex.Add(index);
                    }
                    // 通过识别的参数分隔符，分隔各个参数
                    List<string> paramStringList = new List<string>();
                    int lastSplitParamChatIndex = -1;
                    foreach (int index in splitParamCharIndex)
                    {
                        paramStringList.Add(innerBracketParam.Substring(lastSplitParamChatIndex + 1, index - lastSplitParamChatIndex - 1));
                        lastSplitParamChatIndex = index;
                    }
                    // 还要加上最后一个|后面的参数
                    if (lastSplitParamChatIndex == -1)
                        paramStringList.Add(innerBracketParam);
                    else if (lastSplitParamChatIndex + 1 < innerBracketParam.Length - 1)
                        paramStringList.Add(innerBracketParam.Substring(lastSplitParamChatIndex + 1));
                    // 解析各个具体参数
                    foreach (string oneParamString in paramStringList)
                    {
                        if (string.IsNullOrEmpty(oneParamString))
                            continue;

                        string[] keyAndValue = oneParamString.Split(new char[] { '=' });
                        if (keyAndValue.Length != 2)
                            Utils.LogErrorAndExit(string.Format("声明的{0}参数下属的参数字符串{1}错误，参数名和配置值之间应用=分隔", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, oneParamString));
                        else
                        {
                            string key = keyAndValue[0].Trim();
                            string value = keyAndValue[1];
                            if (AppValues.EXPORT_CSV_PARAM_EXPORT_PATH_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                // 检查导出路径是否存在
                                if (!Directory.Exists(value))
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的csv文件导出路径不存在", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, AppValues.EXPORT_CSV_PARAM_EXPORT_PATH_PARAM_STRING));
                                else
                                    AppValues.ExportCsvPath = Path.GetFullPath(value);
                            }
                            else if (AppValues.EXPORT_CSV_PARAM_EXTENSION_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if (string.IsNullOrEmpty(value))
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的导出csv文件的扩展名不允许为空", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, AppValues.EXPORT_CSV_PARAM_EXTENSION_PARAM_STRING));
                                if (value.StartsWith("."))
                                    value = value.Substring(1);

                                AppValues.ExportCsvExtension = value;
                            }
                            else if (AppValues.EXPORT_CSV_PARAM_SPLIT_STRING_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Replace("\\|", "|");
                                AppValues.ExportCsvSplitString = value;
                            }
                            else if (AppValues.EXPORT_CSV_PARAM_IS_EXPORT_COLUMN_NAME_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if ("true".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportCsvIsExportColumnName = true;
                                else if ("false".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportCsvIsExportColumnName = false;
                                else
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的值错误，必须为true或false", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, AppValues.EXPORT_CSV_PARAM_IS_EXPORT_COLUMN_NAME_PARAM_STRING));
                            }
                            else if (AppValues.EXPORT_CSV_PARAM_IS_EXPORT_COLUMN_DATA_TYPE_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if ("true".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportCsvIsExportColumnDataType = true;
                                else if ("false".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportCsvIsExportColumnDataType = false;
                                else
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的值错误，必须为true或false", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, AppValues.EXPORT_CSV_PARAM_IS_EXPORT_COLUMN_DATA_TYPE_PARAM_STRING));
                            }
                            else
                                Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}非法", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, key));
                        }
                    }
                    // 要求必须含有exportPath参数
                    if (AppValues.ExportCsvPath == null)
                        Utils.LogErrorAndExit(string.Format("错误：声明要额外导出指定Excel文件为csv文件，就必须同时在{0}参数下声明用于配置csv文件导出路径的参数{1}", AppValues.EXPORT_CSV_PARAM_PARAM_STRING, AppValues.EXPORT_CSV_PARAM_EXPORT_PATH_PARAM_STRING));
                }    

                // 解析配置的要额外导出csv文件的Excel文件名
                // 先判断是否声明对所有文件进行导出
                if (!Utils.ParserIncludeFileNameList(param, ref existExcelFileNames, AppValues.EXPORT_CSV_PARAM_STRING, ref AppValues.ExportCsvTableNames))
                {
                    Utils.LogErrorAndExit(
                       string.Format("必须在英文小括号内声明要导出为csv文件的Excel表格名，若要全部导出，请配置为{0}参数",
                       AppValues.EXPORT_ALL_TO_EXTRA_FILE_PARAM_STRING));
                    continue;
                }
            }
            else if (param.StartsWith(AppValues.EXPORT_LANG_PATH_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                string paramString;
                if (!Utils.GetInnerBracketParam(param, out paramString))
                {
                    Utils.LogErrorAndExit(string.Format("错误：声明导出csv对应Lang文件的参数{0}后必须在英文小括号内声明输出路径", AppValues.EXPORT_LANG_PATH_PARAM_STRING));
                }
                else
                {
                    if (!Directory.Exists(paramString))
                        Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下所配置的Lang文件导出路径不存在", AppValues.EXPORT_LANG_PATH_PARAM_STRING));
                    else
                        AppValues.ExportLangPath = Path.GetFullPath(paramString);
                }
            }
            else if (param.StartsWith(AppValues.EXPORT_VERBOSE_LOG_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.VerboseModeFlag = true;
                continue;
            }
            else if (param.StartsWith(AppValues.EXPORT_UE_FILE_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.ExportUERef = true;

                string UERefPath;
                if(Utils.GetInnerBracketParam(param, out UERefPath))
                {
                    AppValues.UEFileRefPath = UERefPath;
                }
                continue;
            }
            else if (param.StartsWith(AppValues.EXPORT_GO_FLAG_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                continue;
            else if (param.StartsWith(AppValues.EXPORT_GO_FLAG_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                // 首先解析并判断配置的csv对应Java类文件导出参数是否正确
                string exportGoParamString = null;
                for (int j = ParamStartIndex; j < args.Length; ++j)
                {
                    string tempParam = args[j];
                    if (tempParam.StartsWith(AppValues.EXPORT_GO_FLAG_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                    {
                        exportGoParamString = tempParam;
                        break;
                    }
                }
                if (exportGoParamString != null)
                {
                    string innerBracketParam;
                    if (!Utils.GetInnerBracketParam(exportGoParamString, out innerBracketParam))
                    {
                        Utils.LogErrorAndExit(string.Format("错误：声明导出csv对应Go类文件的参数{0}后必须在英文小括号内声明各个具体参数", AppValues.EXPORT_GO_FLAG_PARAM_STRING));
                    }
                    else
                    {
                        // 通过|分隔各个参数
                        string[] paramStringList = innerBracketParam.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        // 解析各个具体参数
                        foreach (string oneParamString in paramStringList)
                        {
                            string[] keyAndValue = oneParamString.Split(new char[] { '=' });
                            if (keyAndValue.Length != 2)
                                Utils.LogErrorAndExit(string.Format("声明的{0}参数下属的参数字符串{1}错误，参数名和配置值之间应用=分隔", AppValues.EXPORT_GO_FLAG_PARAM_STRING, oneParamString));
                            else
                            {
                                string key = keyAndValue[0].Trim();
                                string value = keyAndValue[1];
                                if (AppValues.EXPORT_GO_PARAM_EXPORT_PATH_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    // 检查导出路径是否存在
                                    if (!Directory.Exists(value))
                                        Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的导出csv对应Go文件导出路径不存在", AppValues.EXPORT_GO_FLAG_STRING, AppValues.EXPORT_GO_PARAM_EXPORT_PATH_PARAM_STRING));
                                    else
                                        AppValues.ExportGoPath = Path.GetFullPath(value);
                                }
                                else
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}非法", AppValues.EXPORT_GO_FLAG_PARAM_STRING, key));
                            }
                        }
                        // 要求必须含有exportPath参数
                        if (AppValues.ExportGoPath == null)
                            Utils.LogErrorAndExit(string.Format("错误：声明要额外导出csv对应Go类文件，就必须同时在{0}参数下声明用于配置导出路径的参数{1}", AppValues.EXPORT_GO_FLAG_PARAM_STRING, AppValues.EXPORT_GO_PARAM_EXPORT_PATH_PARAM_STRING));
                    }
                }
                else
                    Utils.LogErrorAndExit(string.Format("错误：声明要额外导出指定Excel文件为csv对应Go类文件，就必须同时声明用于配置Java类文件导出参数的{0}", AppValues.EXPORT_GO_FLAG_PARAM_STRING));

                // 解析配置的要额外导出csv对应Java类文件的Excel文件名
                // 先判断是否声明对所有文件进行导出
                if (!Utils.ParserIncludeFileNameList(param, ref existExcelFileNames, AppValues.EXPORT_GO_FLAG_STRING, ref AppValues.ExportGoTableNames))
                {
                    Utils.LogErrorAndExit(string.Format("必须在英文小括号内声明要导出为csv对应Go类文件的Excel表格名，若要全部导出，请配置为{0}参数", AppValues.EXPORT_ALL_TO_EXTRA_FILE_PARAM_STRING));
                }
            }
            // 注意：-exportJson与-exportJsonParam均以-exportJson开头，故要先判断-exportJsonParam分支。这里将-exportJsonParam的解析放到-exportJson的解析之中是为了只有声明了进行json文件导出时才解析导出参数
            else if (param.StartsWith(AppValues.EXPORT_JSON_PARAM_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                continue;
            else if (param.StartsWith(AppValues.EXPORT_JSON_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                // 首先解析并判断配置的json文件导出参数是否正确
                string exportJsonParamString = null;
                for (int j = ParamStartIndex; j < args.Length; ++j)
                {
                    string tempParam = args[j];
                    if (tempParam.StartsWith(AppValues.EXPORT_JSON_PARAM_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                    {
                        exportJsonParamString = tempParam;
                        break;
                    }
                }
                if (exportJsonParamString != null)
                {
                    string paramString;
                    if (!Utils.GetInnerBracketParam(exportJsonParamString, out paramString) || paramString == null)
                    {
                        Utils.LogErrorAndExit(string.Format("错误：声明导出json文件的参数{0}后必须在英文小括号内声明各个具体参数", AppValues.EXPORT_JSON_PARAM_PARAM_STRING));
                        continue;
                    }

                    // 通过|分隔各个参数
                    string[] paramStringList = paramString.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    // 解析各个具体参数
                    foreach (string oneParamString in paramStringList)
                    {
                        string[] keyAndValue = oneParamString.Split(new char[] { '=' });
                        if (keyAndValue.Length != 2)
                            Utils.LogErrorAndExit(string.Format("声明的{0}参数下属的参数字符串{1}错误，参数名和配置值之间应用=分隔", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, oneParamString));
                        else
                        {
                            string key = keyAndValue[0].Trim();
                            string value = keyAndValue[1];
                            if (AppValues.EXPORT_JSON_PARAM_EXPORT_PATH_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                // 检查导出路径是否存在
                                if (!Directory.Exists(value))
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的json文件导出路径不存在", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, AppValues.EXPORT_JSON_PARAM_EXPORT_PATH_PARAM_STRING));
                                else
                                    AppValues.ExportJsonPath = Path.GetFullPath(value);
                            }
                            else if (AppValues.EXPORT_JSON_PARAM_EXTENSION_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if (string.IsNullOrEmpty(value))
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的导出json文件的扩展名不允许为空", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, AppValues.EXPORT_JSON_PARAM_EXTENSION_PARAM_STRING));
                                if (value.StartsWith("."))
                                    value = value.Substring(1);

                                AppValues.ExportJsonExtension = value;
                            }
                            else if (AppValues.EXPORT_JSON_PARAM_IS_FORMAT_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if ("true".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportJsonIsFormat = true;
                                else if ("false".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportJsonIsFormat = false;
                                else
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的值错误，必须为true或false", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, AppValues.EXPORT_JSON_PARAM_IS_FORMAT_PARAM_STRING));
                            }
                            else if (AppValues.EXPORT_JSON_PARAM_IS_EXPORT_JSON_ARRAY_FORMAT_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if ("true".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportJsonIsExportJsonArrayFormat = true;
                                else if ("false".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportJsonIsExportJsonArrayFormat = false;
                                else
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的值错误，必须为true或false", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, AppValues.EXPORT_JSON_PARAM_IS_EXPORT_JSON_ARRAY_FORMAT_PARAM_STRING));
                            }
                            else if (AppValues.EXPORT_JSON_PARAM_IS_MAP_INCLUDE_KEY_COLUMN_VALUE_PARAM_STRING.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = value.Trim();
                                if ("true".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportJsonIsExportJsonMapIncludeKeyColumnValue = true;
                                else if ("false".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                                    AppValues.ExportJsonIsExportJsonMapIncludeKeyColumnValue = false;
                                else
                                    Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}所配置的值错误，必须为true或false", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, AppValues.EXPORT_JSON_PARAM_IS_MAP_INCLUDE_KEY_COLUMN_VALUE_PARAM_STRING));
                            }
                            else
                                Utils.LogErrorAndExit(string.Format("错误：声明的{0}参数下属的参数{1}非法", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, key));
                        }
                    }
                    // 要求必须含有exportPath参数
                    if (AppValues.ExportJsonPath == null)
                        Utils.LogErrorAndExit(string.Format("错误：声明要额外导出指定Excel文件为json文件，就必须同时在{0}参数下声明用于配置json文件导出路径的参数{1}", AppValues.EXPORT_JSON_PARAM_PARAM_STRING, AppValues.EXPORT_JSON_PARAM_EXPORT_PATH_PARAM_STRING));
                }
                else
                    Utils.LogErrorAndExit(string.Format("错误：声明要额外导出指定Excel文件为json文件，就必须同时声明用于配置json文件导出参数的{0}", AppValues.EXPORT_JSON_PARAM_PARAM_STRING));

                // 解析配置的要额外导出json文件的Excel文件名
                string errorString = null;
                // 先判断是否声明对所有文件进行导出
                string innerParam;
                if (!Utils.GetInnerBracketParam(param, out innerParam))
                {
                    Utils.LogErrorAndExit(string.Format("必须在英文小括号内声明要导出为json文件的Excel表格名，若要全部导出，请配置为{0}参数", AppValues.EXPORT_ALL_TO_EXTRA_FILE_PARAM_STRING));
                    continue;
                }
                if (innerParam == null || innerParam.Equals(AppValues.EXPORT_ALL_TO_EXTRA_FILE_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
                    AppValues.ExportJsonTableNames = existExcelFileNames;
                else
                {
                    string[] fileNames = Utils.GetExcelFileNamesByInnerParam(innerParam, out errorString);
                    if (errorString != null)
                        Utils.LogErrorAndExit(string.Format("错误：声明额外导出为json文件的参数{0}后{1}", AppValues.EXPORT_JSON_PARAM_STRING, errorString));
                    else
                    {
                        // 检查指定导出的Excel文件是否存在
                        foreach (string fileName in fileNames)
                        {
                            if (!existExcelFileNames.Contains(fileName))
                                Utils.LogErrorAndExit(string.Format("要求额外导出为json文件的Excel表（{0}）不存在，请检查后重试并注意区分大小写", Utils.CombinePath(AppValues.ExportJsonPath, string.Concat(fileName, ".xlsx"))));
                            else
                                AppValues.ExportJsonTableNames.Add(fileName);
                        }
                    }
                }
            }
            else if (param.StartsWith(AppValues.ALLOWED_NULL_NUMBER_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                AppValues.IsAllowedNullNumber = true;
                if (AppValues.VerboseModeFlag)
                    Utils.LogWarning("警告：你选择了允许int、long、float字段中存在空值，建议为逻辑上不允许为空的数值型字段声明使用notEmpty检查规则");
            }
            else if (param.StartsWith(AppValues.EXPORT_GROUP_PARAM_STRING, StringComparison.CurrentCultureIgnoreCase))
            {
                //AppValues.ExportExcludeGroupNames
                string errorString;
                string[] groupList = Utils.GetExcelFileNames(param, out errorString);
                if (errorString != null || groupList == null || groupList.Length == 0)
                {
                    Utils.LogErrorAndExit(string.Format("错误：声明要额外导出指定Excel文件为csv对应Java类文件，就必须同时声明用于配置Java类文件导出参数的{0}", AppValues.EXPORT_JAVA_CLASS_PARAM_PARAM_STRING));
                }
                else
                {
                    AppValues.ExportGroupNames.AddRange(groupList);
                }
            }
            else
                Utils.LogErrorAndExit(string.Format("错误：未知的指令参数{0}", param));
        }

        // 如果设置了部分导出，则检查是否对同一个表格既声明了-part又声明了-except
        if (isExportPart == true)
        {
            List<string> errorConfigTableNames = new List<string>();
            foreach (string tableName in AppValues.ExportTableNameAndPath.Keys)
            {
                if (AppValues.ExceptExportTableNames.Contains(tableName))
                    errorConfigTableNames.Add(tableName);
            }

            if (errorConfigTableNames.Count > 0)
                Utils.LogErrorAndExit(string.Format("错误：以下表格既声明要进行导出，又声明要忽略导出：{0}，请修正配置后重试", Utils.CombineString(errorConfigTableNames, ",")));
        }

        // 排除本次设置为忽略导出的Excel文件
        foreach (string exceptTableName in AppValues.ExceptExportTableNames)
            AppValues.ExportTableNameAndPath.Remove(exceptTableName);

        // 如果声明要额外导出为csv文件的Excel表本身在本次被忽略，需要进行警告
        List<string> warnExportCsvTableNames = new List<string>();
        foreach (string exportCsvTableName in AppValues.ExportCsvTableNames)
        {
            if (!AppValues.ExportTableNameAndPath.ContainsKey(exportCsvTableName))
                warnExportCsvTableNames.Add(exportCsvTableName);
        }
        if (warnExportCsvTableNames.Count > 0)
        {
            Utils.LogWarning(string.Format("警告：以下Excel表声明为要额外导出为csv文件，但在{0}参数中未声明本次要对其进行导出，本工具将不对这些表格执行导出csv文件的操作\n{1}", AppValues.PART_EXPORT_PARAM_STRING, Utils.CombineString(warnExportCsvTableNames, ", ")));
            foreach (string tableName in warnExportCsvTableNames)
                AppValues.ExportCsvTableNames.Remove(tableName);
        }
        if (AppValues.ExportCsvTableNames.Count > 0)
            Utils.Log(string.Format("本次将以下Excel表额外导出为csv文件：\n{0}\n", Utils.CombineString(AppValues.ExportCsvTableNames, ", ")));

        // 如果声明要额外导出为json文件的Excel表本身在本次被忽略，需要进行警告
        List<string> warnExportJsonTableNames = new List<string>();
        foreach (string exportJsonTableName in AppValues.ExportJsonTableNames)
        {
            if (!AppValues.ExportTableNameAndPath.ContainsKey(exportJsonTableName))
                warnExportJsonTableNames.Add(exportJsonTableName);
        }
        if (warnExportJsonTableNames.Count > 0)
        {
            Utils.LogWarning(string.Format("警告：以下Excel表声明为要额外导出为json文件，但在{0}参数中未声明本次要对其进行导出，本工具将不对这些表格执行导出json文件的操作\n{1}", AppValues.PART_EXPORT_PARAM_STRING, Utils.CombineString(warnExportJsonTableNames, ", ")));
            foreach (string tableName in warnExportJsonTableNames)
                AppValues.ExportJsonTableNames.Remove(tableName);
        }
        if (AppValues.ExportJsonTableNames.Count > 0)
            Utils.Log(string.Format("本次将以下Excel表额外导出为json文件：\n{0}\n", Utils.CombineString(AppValues.ExportJsonTableNames, ", ")));

        // 解析本工具所在目录下的config文件
        string configFilePath = Utils.CombinePath(AppValues.PROGRAM_FOLDER_PATH, AppValues.CONFIG_FILE_NAME);
        if (File.Exists(configFilePath))
        {
            string errorString = null;
            AppValues.ConfigData = TxtConfigReader.ParseTxtConfigFile(configFilePath, ":", out errorString);
            if (!string.IsNullOrEmpty(errorString))
                Utils.LogErrorAndExit(errorString);
        }
        else if (AppValues.VerboseModeFlag)
            Utils.LogWarning(string.Format("警告：找不到本工具所在路径下的{0}配置文件，请确定是否真的不需要自定义配置", AppValues.CONFIG_FILE_NAME));

        // 读取部分配置项并进行检查
        const string ERROR_STRING_FORMAT = "配置项\"{0}\"所设置的值\"{1}\"非法：{2}\n";
        StringBuilder errorStringBuilder = new StringBuilder();
        string tempErrorString = null;
        if (AppValues.ConfigData.ContainsKey(AppValues.APP_CONFIG_KEY_DEFAULT_DATE_INPUT_FORMAT))
        {
            AppValues.DefaultDateInputFormat = AppValues.ConfigData[AppValues.APP_CONFIG_KEY_DEFAULT_DATE_INPUT_FORMAT].Trim();
            if (TableCheckHelper.CheckDateInputDefine(AppValues.DefaultDateInputFormat, out tempErrorString) == false)
                errorStringBuilder.AppendFormat(ERROR_STRING_FORMAT, AppValues.APP_CONFIG_KEY_DEFAULT_DATE_INPUT_FORMAT, AppValues.DefaultDateInputFormat, tempErrorString);
        }
        if (AppValues.ConfigData.ContainsKey(AppValues.APP_CONFIG_KEY_DEFAULT_DATE_TO_LUA_FORMAT))
        {
            AppValues.DefaultDateToLuaFormat = AppValues.ConfigData[AppValues.APP_CONFIG_KEY_DEFAULT_DATE_TO_LUA_FORMAT].Trim();
            if (TableCheckHelper.CheckDateToLuaDefine(AppValues.DefaultDateToLuaFormat, out tempErrorString) == false)
                errorStringBuilder.AppendFormat(ERROR_STRING_FORMAT, AppValues.APP_CONFIG_KEY_DEFAULT_DATE_TO_LUA_FORMAT, AppValues.DefaultDateToLuaFormat, tempErrorString);
        }
        if (AppValues.ConfigData.ContainsKey(AppValues.APP_CONFIG_KEY_DEFAULT_DATE_TO_DATABASE_FORMAT))
        {
            AppValues.DefaultDateToDatabaseFormat = AppValues.ConfigData[AppValues.APP_CONFIG_KEY_DEFAULT_DATE_TO_DATABASE_FORMAT].Trim();
            if (TableCheckHelper.CheckDateToDatabaseDefine(AppValues.DefaultDateToDatabaseFormat, out tempErrorString) == false)
                errorStringBuilder.AppendFormat(ERROR_STRING_FORMAT, AppValues.APP_CONFIG_KEY_DEFAULT_DATE_TO_DATABASE_FORMAT, AppValues.DefaultDateToDatabaseFormat, tempErrorString);
        }
        if (AppValues.ConfigData.ContainsKey(AppValues.APP_CONFIG_KEY_DEFAULT_TIME_INPUT_FORMAT))
        {
            AppValues.DefaultTimeInputFormat = AppValues.ConfigData[AppValues.APP_CONFIG_KEY_DEFAULT_TIME_INPUT_FORMAT].Trim();
            if (TableCheckHelper.CheckTimeDefine(AppValues.DefaultTimeInputFormat, out tempErrorString) == false)
                errorStringBuilder.AppendFormat(ERROR_STRING_FORMAT, AppValues.APP_CONFIG_KEY_DEFAULT_TIME_INPUT_FORMAT, AppValues.DefaultTimeInputFormat, tempErrorString);
        }
        if (AppValues.ConfigData.ContainsKey(AppValues.APP_CONFIG_KEY_DEFAULT_TIME_TO_LUA_FORMAT))
        {
            AppValues.DefaultTimeToLuaFormat = AppValues.ConfigData[AppValues.APP_CONFIG_KEY_DEFAULT_TIME_TO_LUA_FORMAT].Trim();
            if (TableCheckHelper.CheckTimeDefine(AppValues.DefaultTimeToLuaFormat, out tempErrorString) == false)
                errorStringBuilder.AppendFormat(ERROR_STRING_FORMAT, AppValues.APP_CONFIG_KEY_DEFAULT_TIME_TO_LUA_FORMAT, AppValues.DefaultTimeToLuaFormat, tempErrorString);
        }
        if (AppValues.ConfigData.ContainsKey(AppValues.APP_CONFIG_KEY_DEFAULT_TIME_TO_DATABASE_FORMAT))
        {
            AppValues.DefaultTimeToDatabaseFormat = AppValues.ConfigData[AppValues.APP_CONFIG_KEY_DEFAULT_TIME_TO_DATABASE_FORMAT].Trim();
            if (TableCheckHelper.CheckTimeDefine(AppValues.DefaultTimeToDatabaseFormat, out tempErrorString) == false)
                errorStringBuilder.AppendFormat(ERROR_STRING_FORMAT, AppValues.APP_CONFIG_KEY_DEFAULT_TIME_TO_DATABASE_FORMAT, AppValues.DefaultTimeToDatabaseFormat, tempErrorString);
        }

        string errorConfigString = errorStringBuilder.ToString();
        if (!string.IsNullOrEmpty(errorConfigString))
        {
            errorConfigString = string.Concat("配置文件中存在以下错误，请修正后重试\n", errorConfigString);
            Utils.LogErrorAndExit(errorConfigString);
        }

        // 读取给定的Excel所在目录下的所有Excel文件，然后解析成本工具所需的数据结构
        Utils.Log(string.Format("开始解析Excel所在目录下的所有Excel文件（{0}）：", AppValues.IsExportIncludeSubfolder == true ? "包含子目录中的Excel文件" : "不包含子目录中的Excel文件"));
        Stopwatch stopwatch = new Stopwatch();
        // 注意：不管Excel表格本次是否需要进行导出，都要进行解析，因为其他表格中可能含有对那些表格的引用检查
        string[] excelFilePath = Directory.GetFiles(AppValues.ExcelFolderPath, "*.xlsx", AppValues.IsExportIncludeSubfolder == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        foreach (string filePath in excelFilePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.StartsWith(AppValues.EXCEL_TEMP_FILE_FILE_NAME_START_STRING))
                continue;

            if (AppValues.VerboseModeFlag)
                Utils.Log(string.Format("解析表格\"{0}\"：", fileName), ConsoleColor.Green);

            stopwatch.Reset();
            stopwatch.Start();

            string errorString = null;
            DataSet ds = XlsxReader.ReadXlsxFile(filePath, out errorString);
            stopwatch.Stop();
            //Utils.Log(string.Format("成功，耗时：{0}毫秒", stopwatch.ElapsedMilliseconds));
            if (string.IsNullOrEmpty(errorString))
            {
                Dictionary<string, List<string>> tableConfig = null;

                do
                {
                    // 如果有表格配置进行解析
                    if (ds.Tables[AppValues.EXCEL_CONFIG_SHEET_NAME] != null)
                    {
                        tableConfig = TableAnalyzeHelper.GetTableConfig(ds.Tables[AppValues.EXCEL_CONFIG_SHEET_NAME], out errorString);
                        if (!string.IsNullOrEmpty(errorString))
                        {
                            Utils.LogErrorAndExit(string.Format("错误：解析表格{0}的配置失败\n{1}", fileName, errorString));
                            break;
                        }
                    }

                    foreach (DataTable tb in ds.Tables)
                    {
                        if (AppValues.EXCEL_DATA_SHEET_NAME.Equals(tb.TableName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (analyzeIsGroupIgnore(ref tableConfig, fileName))
                            {
                                Utils.Log(string.Format("跳过导出表{0}", fileName), ConsoleColor.Blue);
                                AppValues.ExportTableNameAndPath.Remove(fileName);
                                continue;
                            }
                            if (!analyzeTable(ref tableConfig, tb, fileName, out errorString))
                                Utils.LogErrorAndExit(string.Format("错误：解析{0}失败\n{1}", filePath, errorString));
                            AppValues.ExportTableNameAndFileName.Add(fileName, fileName);
                            continue;
                        }

                        if (AppValues.EXCEL_CONFIG_SHEET_NAME.Equals(tb.TableName, StringComparison.CurrentCultureIgnoreCase))
                            continue;

                        string tableName = tb.TableName.Replace("$", "");
                        
                        if (analyzeIsGroupIgnore(ref tableConfig, tableName))
                        {
                            Utils.Log(string.Format("跳过导出表{0}", tableName), ConsoleColor.Blue);
                            continue;
                        }                

                        if (!analyzeTable(ref tableConfig, tb, tableName, out errorString))
                        {
                            Utils.LogErrorAndExit(string.Format("错误：解析{0}失败\n{1}", filePath, errorString));
                            break;
                        }
                        AppValues.ExportTableNameAndPath.Add(tableName, filePath);
                        AppValues.ExportTableNameAndFileName.Add(tableName, fileName);
                    }

                } while (false);
            }
            else
                Utils.LogErrorAndExit(string.Format("错误：读取{0}失败\n{1}", filePath, errorString));
        }

        // 进行表格检查
        bool isTableAllRight = true;
        if (AppValues.IsNeedCheck == true)
        {
            if (AppValues.VerboseModeFlag)
                Utils.Log("\n下面开始进行表格检查：");

            foreach (string tableName in AppValues.ExportTableNameAndPath.Keys)
            {
                if (!AppValues.TableInfo.ContainsKey(tableName))
                    continue;
                TableInfo tableInfo = AppValues.TableInfo[tableName];
                string errorString = null;
                if (AppValues.VerboseModeFlag)
                {
                    Utils.Log(string.Format("检查表格\"{0}\"：", tableInfo.TableName), ConsoleColor.Green);
                }
                TableCheckHelper.CheckTable(tableInfo, out errorString);
                if (errorString != null)
                {
                    Utils.LogError(errorString);
                    isTableAllRight = false;
                }
                else if (AppValues.VerboseModeFlag)
                    Utils.Log("正确");
            }
        }
        if (isTableAllRight == true)
        {
            int tableIndex = -1;
            if (AppValues.VerboseModeFlag)
                Utils.Log("\n表格检查完毕，没有发现错误，开始导出为lua文件\n");
            // 进行表格导出
            foreach (var item in AppValues.ExportTableNameAndFileName)
            {
                ++tableIndex;
                string tableName = item.Key;
                string fileName = item.Value;

                TableInfo tableInfo = AppValues.TableInfo[tableName];
                string errorString = null;
                
                if (AppValues.VerboseModeFlag)
                {
                    Utils.Log(string.Format("导出表格\"{0}\"：", tableInfo.TableName), ConsoleColor.Green);
                }

                bool isNeedExportOriginalTable = true;
                // 判断是否设置了特殊导出规则
                if (tableInfo.TableConfig != null && tableInfo.TableConfig.ContainsKey(AppValues.CONFIG_NAME_EXPORT))
                {
                    List<string> inputParams = tableInfo.TableConfig[AppValues.CONFIG_NAME_EXPORT];
                    if (inputParams.Contains(AppValues.CONFIG_PARAM_NOT_EXPORT_ORIGINAL_TABLE))
                    {
                        isNeedExportOriginalTable = false;
                        if (inputParams.Count <= 1)
                            Utils.LogWarning(string.Format("警告：你设置了不对表格\"{0}\"按默认方式进行导出，而又没有指定任何其他自定义导出规则，本工具对此表格不进行任何导出，请确认是否真要如此", tableInfo.TableName));
                        else
                            Utils.Log("你设置了不对此表进行默认规则导出");
                    }
                    // 执行设置的特殊导出规则
                    foreach (string param in inputParams)
                    {
                        if (!AppValues.CONFIG_PARAM_NOT_EXPORT_ORIGINAL_TABLE.Equals(param, StringComparison.CurrentCultureIgnoreCase))
                        {
                            Utils.Log(string.Format("对此表格按\"{0}\"自定义规则进行导出：", param));
                            TableExportToLuaHelper.SpecialExportTableToLua(tableInfo, param, out errorString);
                            if (errorString != null)
                                Utils.LogErrorAndExit(string.Format("导出失败：\n{0}\n", errorString));
                            else
                                Utils.Log("成功");
                        }
                    }
                }
                // 对表格按默认方式导出（除非通过参数设置不执行此操作）
                if (isNeedExportOriginalTable == true)
                {
                    TableExportToLuaHelper.ExportTableToLua(tableInfo, out errorString);
                    if (errorString != null)
                        Utils.LogErrorAndExit(errorString);
                }
                // 判断是否要额外导出为csv文件
                if (AppValues.ExportCsvTableNames.Contains(tableName))
                {
                    TableExportToCsvHelper.ExportTableToCsv(tableInfo, out errorString);
                    if (errorString != null)
                        Utils.LogErrorAndExit(errorString);
                    else
                        Utils.Log("额外导出为csv文件成功");
                }
                // 判断是否要额外导出为csv对应Go文件
                if (AppValues.ExportGoTableNames.Contains(fileName))
                {
                    TableExportToGoClassHelper.ExportTableToGoClass(tableInfo, out errorString);
                    if (errorString != null)
                        Utils.LogErrorAndExit(errorString);
                }

                // 判断是否要额外导出为csv对应Lang文件
                if (!string.IsNullOrEmpty(AppValues.ExportLangPath))
                {
                    TableExportLangHelper.ExportTableLang(tableInfo, tableIndex == 0, out errorString);
                    if (errorString != null)
                        Utils.LogErrorAndExit(errorString);
                    else
                        Utils.Log(string.Format("额外导出为csv {0} 对应Lang文件成功\n", tableName));
                }
                // 判断是否要额外导出为json文件
                if (AppValues.ExportJsonTableNames.Contains(fileName))
                {
                    TableExportToJsonHelper.ExportTableToJson(tableInfo, out errorString);
                    if (errorString != null)
                        Utils.LogErrorAndExit(errorString);
                }
            }
            // 进行数据库导出
            if (AppValues.IsExportMySQL == true)
            {
                Utils.Log("\n导出表格数据到MySQL数据库\n");


                Utils.Log("\n导出到数据库完毕\n");
            }

            if (AppValues.ExportUERef)
            {
                string errorString = null;
                UEFileReference.ExportUEFileRefrenceCSV(AppValues.TableInfo, out errorString);
                if (errorString != null)
                    Utils.LogErrorAndExit(errorString);
            }
        }
        else
        {
            Utils.LogError("\n表格检查完毕，但存在上面所列错误，必须全部修正后才可以进行表格导出\n");
            // 将错误信息全部输出保存到txt文件中
            Utils.SaveErrorInfoToFile();
            errorLevel = -1;
        }

        {
            string errorString = null;
            TableExportToLuaHelper.ExportLangTableToLua(AppValues.LangData, out errorString);
            if (errorString != null)
                Utils.LogErrorAndExit(errorString);
            else if (AppValues.VerboseModeFlag)
                Utils.Log("导出lang.lua成功", ConsoleColor.Green);
        }


        Utils.Log("\n导出完毕", ConsoleColor.Green);
        return errorLevel;
    }

    private static bool analyzeIsGroupIgnore(ref Dictionary<string, List<string>> tableConfig, string tableName)
    {
        if (tableConfig == null || AppValues.ExportGroupNames == null || AppValues.ExportGroupNames.Count == 0)
        {
            return false;
        }

        if (tableName != null && tableName.Length > 0)
        {
            string tableGroupFeild = string.Concat(AppValues.CONFIG_NAME_GROUP, "|", tableName);
            if (tableConfig.ContainsKey(tableGroupFeild))
            {
                List<string> groupCfg = tableConfig[tableGroupFeild];
                if (groupCfg.Count == 0)
                    return false;

                foreach (string group in AppValues.ExportGroupNames)
                {
                    if (groupCfg.Contains(group))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        if (tableConfig.ContainsKey(AppValues.CONFIG_NAME_GROUP))
        {
            List<string> groupCfg = tableConfig[AppValues.CONFIG_NAME_GROUP];
            if (groupCfg.Count == 0)
                return false;

            foreach (string group in AppValues.ExportGroupNames)
            {
                if (groupCfg.Contains(group))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    private static bool analyzeTable(ref Dictionary<string, List<string>> tableConfig, DataTable dataTable, string name, out string errorString)
    {
        TableInfo tableInfo = TableAnalyzeHelper.AnalyzeTable(dataTable, name, out errorString);
        if (errorString != null)
        {
            return false;
        }

        if (tableConfig != null)
        {
            tableInfo.TableConfig = tableConfig;
        }
        AppValues.TableInfo.Add(tableInfo.TableName, tableInfo);

        errorString = null;
        return true;
    }
}