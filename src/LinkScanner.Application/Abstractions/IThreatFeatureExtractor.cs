using LinkScanner.Application.ThreatIntelligence;
using LinkScanner.Domain.Entities;

namespace LinkScanner.Application.Abstractions;

public interface IThreatFeatureExtractor
{
    LinkThreatFeatures Extract(LinkScanResult scanResult);
}