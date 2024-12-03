using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharedResources
{
    public static partial class ExcelOperations
    {
        public static JObject XlsToJson(byte[] file, bool removeHeaderSpacing = false, string[] stringOverrides = null)
        {
            using (var stream = new MemoryStream(file))
                return XlsToJson(stream, removeHeaderSpacing, stringOverrides);
        }

        public static JObject XlsToJson(Stream file, bool removeHeaderSpacing = false, string[] stringOverrides = null)
        {
            var worksheets = new JObject();
            IWorkbook workbook = WorkbookFactory.Create(file);
            DataFormatter dataFormatter = new DataFormatter();

            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var worksheet = workbook.GetSheetAt(i);
                var headers = worksheet.GetRow(worksheet.FirstRowNum)
                    .Cells
                    .ToDictionary(k => k.ToString().Trim(), v => v.ColumnIndex);

                var wsArr = new JArray();
                for (int rowNum = worksheet.FirstRowNum + 1; rowNum <= worksheet.LastRowNum; rowNum++)
                {
                    var row = worksheet.GetRow(rowNum);
                    if (row != null)
                    {
                        var wsObj = new JObject();
                        foreach (var header in headers)
                        {
                            ICell cell = row.GetCell(header.Value);
                            if (cell == null)
                                wsObj[(removeHeaderSpacing ? header.Key.Replace(" ", "") : header.Key)] = new JValue("");
                            else
                            {
                                if (stringOverrides?.Contains(header.Key, StringComparer.InvariantCultureIgnoreCase) == true)
                                    cell.SetCellType(CellType.String);

                                wsObj[(removeHeaderSpacing ? header.Key.Replace(" ", "") : header.Key)] =
                                    cell.CellType == CellType.String ? new JValue(cell.StringCellValue)
                                    : cell.CellType == CellType.Boolean ? new JValue(cell.BooleanCellValue)
                                    : cell.CellType == CellType.Numeric ?
                                        DateUtil.IsCellDateFormatted(cell) ? new JValue(DateTime.FromOADate(cell.NumericCellValue))
                                            : cell.NumericCellValue.IsInt() ? new JValue((long)cell.NumericCellValue)
                                                : new JValue(cell.NumericCellValue)
                                                : new JValue(cell?.ToString());
                            }
                        }
                        wsArr.Add(wsObj);
                    }
                }

                worksheets[(removeHeaderSpacing ? worksheet.SheetName.Replace(" ", "") : worksheet.SheetName).Trim()] = wsArr;
            }
            return worksheets;
        }

        public static byte[] ToXLSX(this JObject jObject, string[] sheetOrder = null, string[] ignoreSheets = null, bool useXLS = false)
            => JsonToXLSX(jObject, sheetOrder, ignoreSheets, useXLS);

        public static byte[] JsonToXLSX(JObject jObject, string[] sheetOrder = null, string[] ignoreSheets = null, bool useXLS = false)
        {
            IWorkbook workbook = useXLS ? (IWorkbook)new HSSFWorkbook() : new XSSFWorkbook();

            var sheets = (sheetOrder ?? Array.Empty<string>())
            ?.Concat(jObject
                ?.Properties()
                ?.Where(p => p.Value.Type == JTokenType.Array)
                ?.Select(prop => prop.Name) ?? Array.Empty<string>())
            ?.Distinct()
            ?.Where(h => ignoreSheets?.Contains(h) != true)
            ?.ToArray();

            foreach (var sheet in sheets)
            {

                var wkSheet = workbook.CreateSheet(Regex.Replace(sheet.Length <= 31 ? sheet : sheet.Substring(0, 28) + "...", @"[:?*/\\]", "_"));
                var rows = jObject[sheet] as JArray;
                var wkStRow = wkSheet.CreateRow(0);

                var headerDict = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                var headers = rows
                    ?.Cast<JObject>()
                    ?.SelectMany((JObject row) => row.Properties().Select(prop => prop.Name))
                    ?.Distinct()
                    ?.ToArray();

                for (int i = 0; i < headers.Length; i++)
                {
                    wkStRow.CreateCell(i).SetCellValue(headers[i]);
                    headerDict.Add(headers[i], i);
                }

                var rowIndex = 1;
                foreach (var row in rows)
                {
                    wkStRow = wkSheet.CreateRow(rowIndex++);
                    foreach (var header in headerDict)
                        wkStRow.CreateCell(header.Value).SetCellValue((string)row?[header.Key] ?? "");
                }
            }

            using (var data = new MemoryStream())
            {
                workbook.Write(data);
                return data.ToArray();
            }
        }
    }
}
