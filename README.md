# VetMedData.NET
A .NET Core class library for obtaining structured data on UK-licensed Veterinary Medicines


## Building
This library is provided as a Microsoft Visual Studio 2017 solution and can be build from within this IDE.

For other platforms, the following command within the VetMedData.NET folder will build the project
```dotnet build -c Release```

## Using
### Getting the VMD PID
VMDPIDFactory is a factory class for the VMDPID class hierarchy. After a successful GET and parse of the xml feed from the Veterinary Medicines Directorate (VMD), the factory class will retain the instance of the VMDPID class. 
If the Persistence option is provided, then a temporary file will be created containing a cached copy of the PID.
The reference data from the VMD does not provide certain fields for Expired products, such as Target Species information. If this
is required then the GetTargetSpeciesForExpiredVmdProduct option will enable the downloading and target species extraction of Summary Product Characteristics documents
from the VMD website for all Expired products licensed directly by the VMD. For products licensed by the European Medicines Agency, 
the GetTargetSpeciesForExpiredEmaProduct option will enable a similar behaviour for SPC documents from the EMA website.

### Product Matching
#### Product Name Similarity
VetMedData.NET uses a novel string similarity metric based on the [Monge-Elkan metric](http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.28.8405).
This novel ProductNameMetric has a number of configuration options, which are defined by extending the ProductNameMetricConfig class 
DefaultProductNameMetricConfig provides sensible defaults. Once configured, the GetSimilarity() method of ProductNameMetric will
provide a measure of similarity between two strings ranging from 0 to 1.

#### Product Match Filtering
Once the name similarity between an input product and those in a reference set has been ascertained, then these results can be filtered
to ascertain those of interest. This is acheived by extension of the ProductMatchResultFilter class, with some useful examples provided.

#### Product Match Disambiguation
In cases where an input product is indistinguishable between multiple reference products by name alone, the Diambiguator class can be configured 
(via a DisambiguatorConfig) to select the most likely candidate based on application of result filters.

#### Product Match Runner
Similarity estimation, filtering and disambiguation can all be wrapped up into a single configured match runner
which allows for a single reference product match for an input product to be returned. 

## Examples
A sample .NET Core console application is provided in the VetMedData.CLI Project.
