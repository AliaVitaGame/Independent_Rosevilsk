namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    static class MultiplayConfigTemplate
    {
        public const string Yaml = @"version: 1.0
builds:
  my build: # replace with the name for your build
    executableName: Build.x86_64 # the name of your build executable
    buildPath: Builds/Multiplay # the location of the build files
buildConfigurations:
  my build configuration: # replace with the name for your build configuration
    build: my build # replace with the name for your build
    queryType: sqp # sqp or a2s, delete if you do not have logs to query
    binaryPath: Build.x86_64 # the name of your build executable
    commandLine: -port $$port$$ -queryport $$query_port$$ -log $$log_dir$$/Engine.log # launch parameters for your server
    variables: {}
    cores: 1 # number of cores per server
    speedMhz: 750 # launch parameters for your server
    memoryMiB: 800 # launch parameters for your server
fleets:
  my fleet: # replace with the name for your fleet
    buildConfigurations:
      - my build configuration # replace with the names of your build configuration
    regions:
      North America: # North America, Europe, Asia, South America, Australia
        minAvailable: 0 # minimum number of servers running in the region
        maxServers: 1 # maximum number of servers running in the region
";
    }
}
