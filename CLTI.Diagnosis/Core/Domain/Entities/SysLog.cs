using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Core.Domain.Entities;

[Table("sys_log")]
public class SysLog
{
    [Key]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string? Thread { get; set; }

    public string? Level { get; set; }

    public string? Logger { get; set; }

    public string? Message { get; set; }

    public string? Exception { get; set; }

    public int? UserId { get; set; }

    public int? ProcessId { get; set; }

    [Column("Logger_namespace")]
    public string? LoggerNamespace { get; set; }
}
