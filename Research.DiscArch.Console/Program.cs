// See https://aka.ms/new-console-template for more information
using Research.DiscArch.Console;

//var experiment = new OprimizationExperiment();
//experiment.Run();

//var experiment = new CategorizedReqsExperiment();
//experiment.Run(Research.DiscArch.TestData.SystemNames.Messaging);

var experiment = new CategorizedReqsExperiment();
experiment.Run(Research.DiscArch.TestData.SystemNames.SmallMessagingSystem);

//var experiment = new ProvidedQAWeightsExperiment();
//experiment.Run(Research.DiscArch.TestData.SystemNames.OfficerDispatcher);

Console.ReadLine();

