# aws-sdk-dotnet-core
AWS SDK 3.3x with DotNet Core 2.x Console App examples for EC2 Info, Start &amp; Stop

- Connects to AWS using `default` profile with a region set in `appsettings.Development.json`
- Retrives all instances and spits out info about each instance to the console
- Stops (or Starts) any instance found in a `Running` (or `Stopped`) state using a short form of long polling (with a timeout)
