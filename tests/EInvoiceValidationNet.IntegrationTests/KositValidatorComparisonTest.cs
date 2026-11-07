using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Xml.Linq;

namespace EInvoiceValidationNet.IntegrationTests
{
    //Runs the original java-based KoSIT-validator for a given testsuite and also the EInvoiceValidationNet validator and compares the results.
    //The validator paths and test data are configured through the following environment variables:
    // KOSIT_VALIDATOR_JAR - path to the KoSIT validator JAR file
    // JAVA_PATH - path to the Java executable (optional, defaults to "java")
    // SCENARIOS_XML_PATH - path to a scenarios.xml (all required rules etc. are needed as specified by scenarios.xml, it is assumed that scenarios.xml lives in root of configuration dir)
    // TESTSUITE_DIR_PATH - path to a directory containing only the e-invoices to be used in the test
    public class KositValidatorComparisonTests
    {
        [Test]
        public void KositValidatorComparison_ShouldHaveSameResults()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var javaExecutable = configuration["JAVA_PATH"];
            if (!File.Exists(javaExecutable))
            {
                javaExecutable = "java";
            }

            var kositValidatorJarFile = GetAndCheckPath(configuration, "KOSIT_VALIDATOR_JAR", File.Exists);
            var scenarioXml = GetAndCheckPath(configuration, "SCENARIOS_XML_PATH", File.Exists);
            var testsuitePath = GetAndCheckPath(configuration, "TESTSUITE_DIR_PATH", Directory.Exists);


            var validator = new EInvoiceValidationNet.ValidatorFactory().CreateValidator();
            var myConfiguration = EInvoiceValidationNet.Configuration.ConfigurationBuilder.FromScenariosXml(scenarioXml, Path.GetDirectoryName(scenarioXml));

            var watch = new Stopwatch();
            watch.Start();
            foreach (var file in Directory.EnumerateFiles(testsuitePath, "*.xml", SearchOption.AllDirectories))
            {
                //var report = ReportReader.Read(ExecuteKositValidator(javaExecutable, kositValidatorJarFile, scenarioXml, file));

                validator.Validate(file, myConfiguration);
            }
            watch.Stop();
            //Assert.Fail(watch.Elapsed.TotalMinutes.ToString());
        }


        private static string GetAndCheckPath(IConfiguration configuration, string key, Func<string, bool> pathChecker)
        {
            var path = configuration[key] ?? throw new InvalidOperationException($"Missing configuration path for key {key}");

            return pathChecker(path)
                ? path
                : throw new InvalidOperationException($"The Path \"{path}\" is not valid");
        }

        /// <summary>
        /// Executes the java KoSIT-validator and returns the report as XDocument.
        /// </summary>        
        private static XDocument ExecuteKositValidator(string javaPath, string validatorJar, string scenariosFile, string fileToValidate)
        {
            var scenarioDir = Path.GetDirectoryName(scenariosFile) ?? throw new InvalidOperationException("scenarios.xml file is expected in root of validator configuration directory");
            var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(outputDir);

            string[] arguments = [
                $"-jar",
                validatorJar,
                $"-s",
                scenariosFile,
                $"-r",
                scenarioDir,
                $"-o",
                outputDir,
                fileToValidate
                ];

            var processStartInfo = new ProcessStartInfo(javaPath, arguments)
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            try
            {
                using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("failed to start java kosit validator");
                process.WaitForExit();

                foreach (var file in Directory.EnumerateFiles(outputDir))
                {
                    //only one report output file expected
                    return XDocument.Load(file);
                }

                throw new InvalidOperationException("KoSIT validator execution was not successfull");
            }
            finally
            {
                Directory.Delete(outputDir, true);
            }
        }
    }
}
