| **Package** |**Version** |**Downloads** |
| --- | --- | --- |
| `SimplyWorks.Infolink` | [![NuGet](https://img.shields.io/nuget/v/SimplyWorks.Infolink.Sdk)](https://www.nuget.org/packages/SimplyWorks.Infolink.Sdk/) | [![Nuget](https://img.shields.io/nuget/dt/SimplyWorks.Infolink.Sdk)](https://nuget.org/packages/SimplyWorks.Infolink) |


[![Build Status](https://dev.azure.com/simplify9/Github%20Pipelines/_apis/build/status/simplify9.Infolink?branchName=master)](https://dev.azure.com/simplify9/Github%20Pipelines/_build/latest?definitionId=168&branchName=master) ![Azure DevOps tests](https://img.shields.io/azure-devops/tests/Simplify9/Github%20Pipelines/168?) [![Generic badge](https://img.shields.io/badge/contributions-WELCOME-<green>.svg)](https://shields.io/)

## Infolink 

Infolink is an abstracted message handling framework, that makes it extremely convenient to
add message handling into an existing project. Infolink sits on top of a queue and filters it into
different **Documents**, passing them into different **Subscribers** where each **Subscriber** is
directly related to a **Partner**.

### Documents

A **Document** is essentially a message that can be correlated with a *class* name. Whenever it
is published using the `Publisher` from [SW.Bus](https://github.com/simplify9/Bus/) that can be
injected into `IPublish` from [SW.PrimitiveTypes](https://github.com/simplify9/PrimitiveTypes/). You
can define `Promoted Properties` from a document to allow you to filter them to different
subscribers.

### Partners

A **Partner** is simply a way to associated a subscriber with a specific entity. Simply name one to
add it to the system, so when a subscriber is created it can be associated with one.

### Subscriber

A **Subscriber** is a *listner*. It keeps watching the queue for a certain **Document**. If
a document is found, it will pass through the following stages.

#### Filter:
The subscriber can ignore a document matches certain requirements based on the *promoted
properties* of the **Document**.

#### Mapper:
The mapping phase is where one form of a message is transformed into another. Possibly from csv to
json, a certain model of json to another or any other format.

The mapper is, in facem a **serverless adapter**- explained below.

#### Handler
The handling phase is where a certain action is performed using the mapped(if a mapper was given)
data. Like, for example, making a POST request to a certain URL.

Note, that the handler is also a **serverless adapter**.

---

### Serverless adapter


A serverless adapter is console application hosted on the cloud, that will be downloaded and ran at
runtime. For more information on adapters, their installation and details please take a look at the
[serverless repository](https://github.com/simplify9/serverless)






## Getting support 👷 
If you encounter any bugs, don't hesitate to submit an [issue](https://github.com/simplify9/Infolink/issues). We'll get back to you promptly!
