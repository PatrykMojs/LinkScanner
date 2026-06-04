using LinkScanner.Domain.Entities;

namespace LinkScanner.Application.Abstractions;

public interface IThreatClassifier
{
    Task<AiThreatAssessment> PredictAsync(LinkScanResult scanResult, CancellationToken cancellationToken = default);
 }