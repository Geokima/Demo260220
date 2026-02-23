using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.Json;
using System.Text.Json.Nodes;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            // 默认目录
            string excelDir = "Excel";
            string outputDir = "Export";
            
            // 如果提供了参数，使用参数
            if (args.Length >= 1)
                excelDir = args[0];
            if (args.Length >= 2)
                outputDir = args[1];
            
            if (!Directory.Exists(excelDir))
            {
                Console.WriteLine($"错误: Excel目录不存在 - {excelDir}");
                return 1;
            }
            
            Directory.CreateDirectory(outputDir);
            
            // 获取所有Excel文件
            var excelFiles = Directory.GetFiles(excelDir, "*.xls")
                .Concat(Directory.GetFiles(excelDir, "*.xlsx"))
                .ToArray();
            
            if (excelFiles.Length == 0)
            {
                Console.WriteLine($"在目录 {excelDir} 中没有找到Excel文件");
                return 1;
            }
            
            Console.WriteLine($"找到 {excelFiles.Length} 个Excel文件:");
            foreach (var file in excelFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            
            int totalFiles = 0;
            int totalWorksheets = 0;
            
            foreach (var excelPath in excelFiles)
            {
                try
                {
                    Console.WriteLine($"\n处理文件: {Path.GetFileName(excelPath)}");
                    totalFiles++;
                    
                    IWorkbook workbook;
                    using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
                    {
                        // 根据文件扩展名选择合适的工作簿类型
                        if (Path.GetExtension(excelPath).ToLower() == ".xls")
                            workbook = new HSSFWorkbook(fs);
                        else
                            workbook = new XSSFWorkbook(fs);
                    }
                    
                    // 只处理第一个工作表
                    if (workbook.NumberOfSheets > 0)
                    {
                        ISheet worksheet = workbook.GetSheetAt(0);
                        
                        if (worksheet.PhysicalNumberOfRows < 3)
                        {
                            Console.WriteLine($"  跳过工作表 {worksheet.SheetName}: 行数不足 (需要至少3行)");
                            continue;
                        }
                        
                        // 获取第一行标题（字段名）
                        var headerRow = worksheet.GetRow(0);
                        if (headerRow == null) 
                        {
                            Console.WriteLine("  错误: 无法获取标题行");
                            continue;
                        }
                        
                        var headers = new List<string>();
                        
                        for (int j = 0; j < headerRow.LastCellNum; j++)
                        {
                            var cell = headerRow.GetCell(j);
                            if (cell != null)
                            {
                                var name = GetCellStringValue(cell).Trim();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    headers.Add(name);
                                }
                            }
                        }
                        
                        if (headers.Count == 0)
                        {
                            Console.WriteLine($"  跳过工作表 {worksheet.SheetName}: 没有有效的标题");
                            continue;
                        }
                        
                        // 处理数据行（跳过前两行标题）
                        var jsonArray = new JsonArray();
                        int dataRowCount = 0;
                        
                        for (int i = 2; i < worksheet.PhysicalNumberOfRows; i++)
                        {
                            var row = worksheet.GetRow(i);
                            if (row == null) continue;
                            
                            // 检查是否是注释行（以#开头）
                            var firstCell = row.GetCell(0);
                            if (firstCell != null)
                            {
                                var firstCellValue = GetCellStringValue(firstCell).Trim();
                                if (firstCellValue.StartsWith("#"))
                                {
                                    Console.WriteLine($"  跳过注释行: {firstCellValue}");
                                    continue;
                                }
                            }
                            
                            var jsonObject = new JsonObject();
                            
                            for (int j = 0; j < headers.Count; j++)
                            {
                                var cell = row.GetCell(j);
                                var value = GetCellValue(cell);
                                jsonObject[headers[j]] = value;
                            }
                            
                            jsonArray.Add(jsonObject);
                            dataRowCount++;
                        }
                        
                        // 写入JSON文件，使用文件名作为JSON文件名的一部分
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(excelPath);
                        var outputPath = Path.Combine(outputDir, fileNameWithoutExt + ".json");
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText(outputPath, jsonArray.ToJsonString(options));
                        
                        Console.WriteLine($"  已导出: {fileNameWithoutExt}.json ({dataRowCount} 行)");
                        
                        // 验证JSON结构
                        ValidateJsonStructure(jsonArray, headers);
                        
                        totalWorksheets++;
                    }
                    
                    workbook.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  错误: 处理文件 {Path.GetFileName(excelPath)} 时出错 - {ex.Message}");
                }
            }
            
            Console.WriteLine($"\n导出完成! 共处理 {totalFiles} 个文件");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            return 1;
        }
    }
    
    static JsonNode? GetCellValue(ICell? cell)
    {
        if (cell == null) return null;
        
        return cell.CellType switch
        {
            CellType.Numeric => cell.NumericCellValue % 1 == 0 
                ? JsonValue.Create((int)cell.NumericCellValue)
                : JsonValue.Create(cell.NumericCellValue),
            CellType.Boolean => JsonValue.Create(cell.BooleanCellValue),
            CellType.String => JsonValue.Create(cell.StringCellValue),
            CellType.Formula => JsonValue.Create(GetCellStringValue(cell)),
            CellType.Blank => null,
            _ => JsonValue.Create(GetCellStringValue(cell))
        };
    }
    
    static string GetCellStringValue(ICell cell)
    {
        if (cell == null) return string.Empty;
        
        switch (cell.CellType)
        {
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Numeric:
                return cell.NumericCellValue.ToString();
            case CellType.Boolean:
                return cell.BooleanCellValue.ToString();
            case CellType.Formula:
                switch (cell.CachedFormulaResultType)
                {
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString();
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    default:
                        return cell.ToString();
                }
            default:
                return cell.ToString();
        }
    }
    
    static void ValidateJsonStructure(JsonArray jsonArray, List<string> headers)
    {
        if (jsonArray.Count == 0)
        {
            Console.WriteLine("\n没有数据记录");
            return;
        }
        
        // 计算每列的最大宽度，用于格式化输出
        var columnWidths = new Dictionary<string, int>();
        foreach (var header in headers)
        {
            columnWidths[header] = Math.Max(header.Length, 10); // 最小宽度为10
        }
        
        // 遍历所有数据，计算每列的最大宽度
        for (int i = 0; i < jsonArray.Count; i++)
        {
            if (jsonArray[i] is JsonObject record)
            {
                foreach (var header in headers)
                {
                    if (record.ContainsKey(header))
                    {
                        var value = record[header]?.ToString() ?? "null";
                        if (value.Length > 50) // 限制最大宽度为50
                            value = value.Substring(0, 47) + "...";
                        columnWidths[header] = Math.Max(columnWidths[header], value.Length);
                    }
                }
            }
        }
        
        // 打印表头
        Console.WriteLine();
        foreach (var header in headers)
        {
            var formattedHeader = header.PadRight(columnWidths[header]);
            Console.Write($"| {formattedHeader} ");
        }
        Console.WriteLine("|");
        
        // 打印分隔线
        foreach (var header in headers)
        {
            var line = new string('-', columnWidths[header]);
            Console.Write($"|-{line}-");
        }
        Console.WriteLine("|");
        
        // 打印所有数据行
        for (int i = 0; i < jsonArray.Count; i++)
        {
            if (jsonArray[i] is JsonObject record)
            {
                foreach (var header in headers)
                {
                    string value;
                    if (record.ContainsKey(header))
                    {
                        value = record[header]?.ToString() ?? "null";
                        if (value.Length > 50)
                            value = value.Substring(0, 47) + "...";
                    }
                    else
                    {
                        value = "[缺失]";
                    }
                    
                    var formattedValue = value.PadRight(columnWidths[header]);
                    Console.Write($"| {formattedValue} ");
                }
                Console.WriteLine("|");
            }
        }
        
        // 简单显示记录数
        Console.WriteLine($"\n共 {jsonArray.Count} 条记录");
    }
}