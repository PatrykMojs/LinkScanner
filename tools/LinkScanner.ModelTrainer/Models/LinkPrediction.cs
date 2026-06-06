using Microsoft.ML.Data;

namespace LinkScanner.ModelTrainer.Models;

public sealed class LinkPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsSuspicious { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}