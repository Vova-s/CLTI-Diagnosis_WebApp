using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CLTI.Diagnosis.Core.Domain.Entities;

namespace CLTI.Diagnosis.Data.Entities;

[Table("u_clti_photos")]
public class CltiPhoto
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("CltiCase")]
    public int CltiCaseId { get; set; }

    public Guid CltiCaseGuid { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    public byte[]? CTA { get; set; }
    public byte[]? DSA { get; set; }
    public byte[]? MRA { get; set; }

    [Column("US_of_lower_extremity_arteries")]
    public byte[]? USOfLowerExtremityArteries { get; set; }

    public CltiCase? CltiCase { get; set; }
}
