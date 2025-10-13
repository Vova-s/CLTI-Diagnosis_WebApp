using System.ComponentModel.DataAnnotations;

namespace CLTI.Diagnosis.Core.Domain.Entities.ValueObjects;

public class WifiCriteria
{
    // W критерії (рівень втрати тканини)
    public bool W1 { get; set; }
    public bool W2 { get; set; }
    public bool W3 { get; set; }

    // I критерії (рівень ішемії)
    public bool I0 { get; set; }
    public bool I1 { get; set; }
    public bool I2 { get; set; }
    public bool I3 { get; set; }

    // FI критерії (рівень інфекції стопи)
    public bool FI0 { get; set; }
    public bool FI1 { get; set; }
    public bool FI2 { get; set; }
    public bool FI3 { get; set; }
}

public class GlassCriteria
{
    // Аорто-ілеальний сегмент
    public bool AidI { get; set; }    // Стадія I
    public bool AidII { get; set; }   // Стадія II
    public bool AidA { get; set; }    // Підстадія A
    public bool AidB { get; set; }    // Підстадія B

    // Стегново-підколінний сегмент (0-4)
    [Range(0, 4)]
    public int Fps { get; set; }

    // Інфрапоплітеальний сегмент (0-4)
    [Range(0, 4)]
    public int Ips { get; set; }

    // Фінальна інфраінгвінальна стадія
    public bool Iid { get; set; }
    public bool IidI { get; set; }
    public bool IidII { get; set; }
    public bool IidIII { get; set; }

    // Дескриптор підкісточкової хвороби
    public bool ImdP0 { get; set; }
    public bool ImdP1 { get; set; }
    public bool ImdP2 { get; set; }
}
