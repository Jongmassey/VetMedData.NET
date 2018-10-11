# VetMedData.NET
A .NET Core class library for obtaining structured data on UK-licensed Veterinary Medicines

[![Build status](https://jongmassey.visualstudio.com/VetMedData.NET%20Build/_apis/build/status/VetMedData.NET%20Build-CI)](https://jongmassey.visualstudio.com/VetMedData.NET%20Build/_build/latest?definitionId=1)

## Building
This library is provided as a Microsoft Visual Studio 2017 solution and can be build from within this IDE.

For other platforms, the following command within the VetMedData.NET folder will build the project
```dotnet build -c Release```

## Using

### Getting the VMD PID
VMDPIDFactory is a factory class for the VMDPID class hierarchy. After a successful GET and parse of the xml feed from the Veterinary Medicines Directorate (VMD), the factory class will retain the instance of the VMDPID class