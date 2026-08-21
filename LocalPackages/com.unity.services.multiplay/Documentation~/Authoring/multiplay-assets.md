# Multiplay Hosting assets

## Creation
Right-click in the `Project Window` then select `Create > Services > Multiplay Config` to create a Multiplay Hosting config file.

### Multiplay Hosting Config file

The Multiplay Hosting Config file is a YAML consisting of three collections:
* Builds
* Build Configurations
* Fleets

More details on each collection will be elaborated on below.
By default, the file is setup to run a single Multiplay Hosting server without
any scaling settings.

### Builds
Builds constitute the binary to be uploaded to Multiplay Hosting of the game.  

The name will be the name used to be displayed in the dashboard as well as other parts
of the pipeline.

You can modify the executable and output path name as well.

The `Linux Dedicated Server Build Support` module needs to be installed to be able to create the
binary build.

More details in the [GSH Build documentation][1].

### Build Configurations

The Build Configuration specifies how the Build will be run.

The Build that is configured must be specified.

This includes the size of the machine that will be running the build,
as well as the command line that will be ran on the executable.

More details can be found in the [GSH Build Configuration documentation][2].

### Fleets

The fleets are sets of machines assigned to run Build Configurations.  
They may have specialized scaling settings, depending on your requirements.

By default, fleets are only set to run 1 server at a time for development purposes.

More details can be found in the [GSH Fleet documentation][3].

[1]:https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/concepts/builds
[2]:https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/concepts/build-configurations
[3]:https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/concepts/fleets
