using LinkScanner.ModelTrainer.Models;
using Microsoft.ML;

var mlContext = new MLContext(seed: 42);

var baseDirectory = AppContext.BaseDirectory;

var dataPath = Path.GetFullPath(
    Path.Combine(baseDirectory, "..", "..", "..", "Data", "phishing-training-data.csv"));

var modelOutputPath = Path.GetFullPath(
    Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "src", "LinkScanner.Infrastructure", "MachineLearning", "Models", "phishing-url-model.zip"));

Directory.CreateDirectory(Path.GetDirectoryName(modelOutputPath)!);

Console.WriteLine("Loading data...");
Console.WriteLine($"Data path: {dataPath}");

var data = mlContext.Data.LoadFromTextFile<TrainingLinkData>(
    path: dataPath,
    hasHeader: true,
    separatorChar: ',');

var pipeline = mlContext.Transforms.Concatenate(
        "Features",
        nameof(TrainingLinkData.UrlLength),
        nameof(TrainingLinkData.DotsCount),
        nameof(TrainingLinkData.HyphenCount),
        nameof(TrainingLinkData.DigitsCount),
        nameof(TrainingLinkData.SpecialCharactersCount),
        nameof(TrainingLinkData.UsesHttps),
        nameof(TrainingLinkData.HasIpAddressAsHost),
        nameof(TrainingLinkData.HasAtSymbol),
        nameof(TrainingLinkData.SubdomainCount),
        nameof(TrainingLinkData.SuspiciousKeywordCount))
    .Append(mlContext.BinaryClassification.Trainers.FastTree(
        labelColumnName: nameof(TrainingLinkData.Label),
        featureColumnName: "Features"));

Console.WriteLine("Training model...");

var model = pipeline.Fit(data);

Console.WriteLine("Evaluating model...");

var predictions = model.Transform(data);

var metrics = mlContext.BinaryClassification.Evaluate(
    predictions,
    labelColumnName: nameof(TrainingLinkData.Label));

Console.WriteLine();
Console.WriteLine("Model metrics:");
Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");
Console.WriteLine();

Console.WriteLine("Saving model...");
Console.WriteLine($"Model output path: {modelOutputPath}");

mlContext.Model.Save(model, data.Schema, modelOutputPath);

Console.WriteLine("Done.");