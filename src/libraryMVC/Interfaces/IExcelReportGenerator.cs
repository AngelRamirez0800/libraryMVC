using libraryMVC.DTOs.Reports;

namespace libraryMVC.Interfaces;

public interface IExcelReportGenerator
{
    byte[] Generate(TabularReport report, string worksheetName);
}
