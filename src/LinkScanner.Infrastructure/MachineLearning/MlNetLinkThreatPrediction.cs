using Microsoft.ML.Data;

namespace LinkScanner.Infrastructure.MachineLearning;

public sealed class MlNetLinkThreatPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsSuspicious { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}