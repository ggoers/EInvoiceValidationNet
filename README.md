# Problem
To validate e-invoices, there is currently a [reference implementation from KoSIT](https://github.com/itplr-kosit/validator) written in Java.
Most of the logic for schema validation or the enforcement of business rules via Schematron is already defined in
independent formats (XSD, XSLT). The challenge arises when one’s own application/backend is written in a language
like .NET: in such cases, the Java-based validator can only be used if a Java runtime is installed, with the validator
running either as a separate process (which is slow) or as a daemon (which requires additional service management).

# Overview

This project contains a .NET implementation of the java KoSIT Validator.
It supports the same validator configurations as the original KoSIT Validator.
You can specify a validator configuration with the scenarios.xml file from the configuration:

```csharp
var validator = ValidatorFactory.CreateValidator();
var configuration = ConfigurationBuilder.FromScenariosXml(scenariosXmlPath, configurationRootPath, null);
var validationResult = validator.Validate(pathToInvoiceXml, configuration);
```

It is also possible to create the validation configuration entirely in code
using a fluent API:
```csharp
var validator = ValidatorFactory.CreateValidator();
var configurationBuilder = new ConfigurationBuilder();
//minimal example
var configuration = configurationBuilder
    .AddScenario(scenarioBuilder => scenarioBuilder
        .AddXPathMatchExpression("myFancyXPathMatchExpression")
        .SetSchemaValidation("/path/to/mainXsdFile")
        .AddSchematronValidation("/path/to/schematronXsltFile")
        .SetReportTransformation("/path/to/reportTransformation", outputDirectory: "path/to/outputDirectory"))
    .Build();
var validationResult = validator.Validate(pathToInvoiceXml, configuration);
```

The whole project is inspired by the reference implementation from KoSIT (Thanks!), but i also added my own ideas:
- independence from the concrete XML processing implementation
- the possibility to declare configurations entirely in code, without using a scenarios.xml file
- the possibility to access validation results directly as typed C# objects through the ValidationResult,
without having to generate a validation report

# Roadmap

**Currently I work on the first stable release**.  
Feedback is welcome.

Some planned improvements:
- [x] implement caching for the xslt transformations
- [ ] comparison test with the java KoSIT validator
- [ ] add tests with other configurations than XRechnung
- [ ] add CI/CD in github
- [ ] first release

# Architecture
The XSLT implementation from Microsoft currently only supports XSLT 1.0.
For this reason this project uses the [Saxon CS HE library](https://www.saxonica.com/html/products/latest.html) for
the XSLT transformations and XPath Queries.

However the project is written in a way so it is independent from this third party library.
To achieve this, the core (.netstandard 2.0) defines a port (IXmlProcessing) which is implemented in 
another project (XmlProcessing), which acts like an adapter. So the whole core does not depend on the specific
XmlProcessing implementation, therefore it could easily be swapped later. The port is small and simple and only contains the
XPath evaluation to a Boolean (used for match and accept expressions in the scenario) and the XSLT
Transformation (used for schematron validation and report creation).

I might implement later an adapter with a .netstandard 2.0 implementation if possible to offer 
a .netstandard 2.0 version of this nuget.

# Tests
**EInvoiceValidation.Core.Tests**  
- inludes unit tests for building the configuration and the validation (also the 
validation steps are separately tested)  

**EInvoiceValidation.Integration.Tests**  
- E2E tests with "real" Invoices (CII and UBL) and the real xrechnung configuration

**EInvoiceValidation.XmlProcessing.Tests**
- tests the adapter implementation

# License
This Project is free to use and licensed under the MIT license.
