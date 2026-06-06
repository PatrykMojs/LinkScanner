using LinkScanner.Application.Abstractions;
using LinkScanner.Domain.Entities;
using LinkScanner.Domain.Enums;
using Microsoft.ML;

namespace LinkScanner.Infrastructure.MachineLearning;

public sealed class MlNetThreatClassifier : IThreatClassifier
{
    private const string ModelVersion = "mlnet-demo-v0.2";

    private readonly IThreatFeatureExtractor _featureExtractor;
    private readonly PredictionEngine<MlNetLinkThreatInput, MlNetLinkThreatPrediction> _predictionEngine;

    public MlNetThreatClassifier(IThreatFeatureExtractor featureExtractor)
    {
        _featureExtractor = featureExtractor;

        var mlContext = new MLContext(seed: 42);

        var modelPath = Path.Combine(
            AppContext.BaseDirectory,
            "MachineLearning",
            "Models",
            "phishing-url-model.zip");

        if(!File.Exists(modelPath))
            throw new FileNotFoundException($"ML.NET model file was not found. Expected path: {modelPath}", modelPath);
    
        var model = mlContext.Model.Load(modelPath, out _);

        _predictionEngine = mlContext.Model.CreatePredictionEngine<MlNetLinkThreatInput, MlNetLinkThreatPrediction>(model);
    }

    public Task<AiThreatAssessment> PredictAsync(LinkScanResult scanResult, CancellationToken cancellationToken = default)
    {
        var features = _featureExtractor.Extract(scanResult);

        var input = new MlNetLinkThreatInput
        {
            UrlLength = features.UrlLength,
            DotsCount = features.DotsCount,
            HyphenCount = features.HyphenCount,
            DigitsCount = features.DigitsCount,
            SpecialCharactersCount = features.SpecialCharactersCount,
            UsesHttps = features.UsesHttps ? 1f : 0f,
            HasIpAddressAsHost = features.HasIpAddressAsHost ? 1f : 0f,
            HasAtSymbol = features.HasAtSymbol ? 1f : 0f,
            SubdomainCount = features.SubdomainCount,
            SuspiciousKeywordCount = features.SuspiciousKeywordCount
        };

        var prediction = _predictionEngine.Predict(input);

        var probability = NormalizeProbability(prediction.Probability);

        var assessment = new AiThreatAssessment
        {
            IsSuspicious = prediction.IsSuspicious,
            Probability = probability,
            ThreatLevel = GetThreatLevel(probability),
            PredictedLabel = prediction.IsSuspicious ? "Podejrzany" : "Niskie ryzyko",
            ModelVersion = ModelVersion,
            TopReasons = BuildTopReasons(features, prediction, probability)
        };

        return Task.FromResult(assessment);
    }

    private static float NormalizeProbability(float probability)
    {
        if (float.IsNaN(probability))
        {
            return 0;
        }

        if (probability < 0)
        {
            return 0;
        }

        if (probability > 1)
        {
            return 1;
        }

        return probability;
    }

    private static ThreatLevel GetThreatLevel(float probability)
    {
        return probability switch
        {
            >= 0.90f => ThreatLevel.Critical,
            >= 0.70f => ThreatLevel.High,
            >= 0.40f => ThreatLevel.Medium,
            _ => ThreatLevel.Low
        };
    }

    private static List<string> BuildTopReasons(
        LinkScanner.Application.ThreatIntelligence.LinkThreatFeatures features,
        MlNetLinkThreatPrediction prediction,
        float probability)
    {
        var reasons = new List<string>
        {
            $"Model ML.NET ocenił prawdopodobieństwo podejrzanego linku na {probability:P2}."
        };

        if (prediction.IsSuspicious && probability >= 0.90f)
        {
            reasons.Add("Model ma bardzo wysoką pewność, że ten adres URL może być podejrzany.");
        }
        else if (prediction.IsSuspicious && probability >= 0.70f)
        {
            reasons.Add("Model wykrył kilka wzorców często spotykanych w podejrzanych linkach.");
        }
        else if (probability >= 0.40f)
        {
            reasons.Add("Model wykrył pewne potencjalnie podejrzane wzorce, ale poziom pewności jest umiarkowany.");
        }
        else
        {
            reasons.Add("Model nie wykrył silnych wzorców phishingowych w strukturze adresu URL.");
        }

        reasons.Add($"Wynik modelu: {prediction.Score:0.0000}");

        if (!features.UsesHttps)
        {
            reasons.Add("Adres URL nie korzysta z HTTPS.");
        }

        if (features.HasAtSymbol)
        {
            reasons.Add("Adres URL zawiera znak '@', który bywa używany w mylących linkach.");
        }

        if (features.HasIpAddressAsHost)
        {
            reasons.Add("Adres URL używa adresu IP zamiast nazwy domeny.");
        }

        if (features.SuspiciousKeywordCount > 0)
        {
            reasons.Add($"Adres URL zawiera {features.SuspiciousKeywordCount} podejrzanych słów kluczowych.");
        }

        if (features.SubdomainCount >= 3)
        {
            reasons.Add($"Adres URL ma dużą liczbę subdomen: {features.SubdomainCount}.");
        }

        if (features.UrlLength >= 70)
        {
            reasons.Add($"Adres URL jest nietypowo długi: {features.UrlLength} znaków.");
        }

        return reasons;
    }
}