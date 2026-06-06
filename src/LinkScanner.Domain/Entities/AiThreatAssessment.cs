using LinkScanner.Domain.Enums;

namespace LinkScanner.Domain.Entities;

public sealed class AiThreatAssessment
{
    public bool IsSuspicious { get; init; }
    public float Probability { get; init; }
    public ThreatLevel ThreatLevel { get; init; }
    public string PredictedLabel { get; init; } = string.Empty;
    public string ModelVersion { get; init; } = string.Empty;
    public List<string> TopReasons { get; init; } = new();
}