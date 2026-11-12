using libraryMVC.DTOs.Reports;

namespace libraryMVC.Interfaces;

public interface IPdfReportGenerator
{
    byte[] Generate(TabularReport report);
}
