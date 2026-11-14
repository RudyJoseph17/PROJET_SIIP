namespace IdentificationProjet.Application.Contracts;

/// <summary>Limites de la section 5 (identiques aux constantes de PKG_IP_IDENTIFICATION).</summary>
public static class LimitesAnnexes
{
    public const int NombreMax = 3;
    public const long TailleMaxOctets = 10 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
        };

    public static string Accept => string.Join(",", Extensions.Keys);
}
