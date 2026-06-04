using LinkScanner.Application.Abstractions;
using LinkScanner.Domain.Entities;
using LinkScanner.Domain.Enums;

namespace LinkScanner.Infrastructure.MachineLearning;

public sealed class NoOpThreatClassifier : IThreatClassifier
{
    private readonly IThreatFeatureExtractor _featureExtractor;

    public NoOpThreatClassifier(IThreatFeatureExtractor featureExtractor)
    {
        _featureExtractor = featureExtractor;   
    }

    public Task<AiThreatAssessment> PredictAsync(LinkScanResult scanResult, CancellationToken cancellationToken = default)
    {
        var features = _featureExtractor.Extract(scanResult);

        var topReasons = new List<string>
        {
            "AI classifier is not trained yet.",
            $"Extracted URL length: {features.UrlLength}",
            $"Extracted risk score: {features.RiskScore}",
            $"Extracted HTTPS flag: {features.UsesHttps}"
        };

        var assessment = new AiThreatAssessment
        {
            IsSuspicious = false,
            Probability = 0.0f,
            ThreatLevel = ThreatLevel.Low,
            PredictedLabel = "NotEvaluated",
            ModelVersion = "no-op-v0",
            TopReasons = topReasons
        };

        return Task.FromResult(assessment);
    }
}