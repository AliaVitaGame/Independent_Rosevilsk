# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.1] - 2025-09-24
### Fixed 
- Fixed assets being loaded despite being of the incorrect type

## [1.3.0] - 2025-09-16

### Added
- Add migration path validation to warn when incompatible packages are detected.

### Changed
- Update documentation to replace “Game Server Hosting” with “Multiplay Hosting.”
- Update the minimum supported Unity Editor version to `2021.3`.
- Update `com.unity.services.wire` to `1.4.0`.

### Fixed
- Fix broken links.

## [1.2.5] - 2024-08-20

### Added
- Add API to interact with the Admin API.
- Add API to interact with deployment capabilities.

### Changed
- Make uploads more robust in cases of partial success.

### Fixed
- Fix issue where `gsh deploy upload` could fail in some cases.
  - A partial upload would not be retried and a subsequent build version would fail to be created.
- Fix upload command not waiting for sync when there was nothing to do.

## [1.2.2] - 2024-04-30

### Changed
- Update Apple privacy manifest content.

## [1.2.1] - 2024-04-30

### Added
- Add Apple privacy manifest.

### Changed
- Update `com.unity.services.core` to `1.12.5`.
- Update `com.unity.services.wire` to `1.2.5`.

## [1.2.0] - 2024-04-05

### Changed
- Disable this package’s authoring component if the Multiplayer SDK is present, preferring that one instead.
  - Multiplay will own the integration for versions in [1, 1.2).
  - The unified package will own it from 1.2.0 onward.

## [1.1.1] - 2023-11-08

### Changed
- When creating a build, avoid switching targets (which can cause a domain reload); emit a warning instead.

## [1.1.0] - 2023-10-30

### Added
- Add support for editor authoring of Game Server Hosting files.
- Add support for Deployment Window.

## [1.0.5] - 2023-06-22

### Fixed
- Fix deserialization errors in `ReadyServerForPlayersAsync` calls for Unity Editor versions above 2020.3.

## [1.0.4] - 2023-06-01

### Fixed
- Fix exception when deserializing a payload allocation while used with Matchmaker.

## [1.0.3] - 2023-05-16

### Fixed
- Prevent Newtonsoft JSON default settings from overriding SDK serialization behavior.

## [1.0.0-pre.10] - 2023-04-28

### Changed
- Improve stability of the `com.unity.services.wire` dependency.
- Update `com.unity.services.core` to `1.8.2`.

## [1.0.0-pre.9] - 2023-04-13

### Added
- Add server IP address property to `ServerConfig` (`ServerConfig.IpAddress`).

## [1.0.0-pre.8] - 2023-03-21

### Changed
- Add support for the renamed Game Server Hosting CLI module.
- Change the config file extension from `.mps` to `.gsh`.

## [1.0.0-pre.7] - 2023-01-04

### Fixed
- Fix a race condition when subscribing to server events that could cause events to be missed.

## [1.0.0-pre.6] - 2022-08-12

### Added
- Add support for Unity Editor `2020.3`.

## [1.0.0-pre.5] - 2022-08-03

### Changed
- Update supported Unity Editor version.

## [1.0.0-pre.4] - 2022-08-02

### Fixed
- Fix documentation and `README`.

### Changed
- Update package dependencies.

## [1.0.0-pre.3] - 2022-07-22

### Changed
- Improve XML documentation for readiness.
- Rename `IServerCheckManager` to `IServerQueryHandler`.
- Mark `ServerConfig` with the `Preserve` attribute to protect it from code stripping.
- Rename `AllocatedUuid` to `AllocationId`.

### Fixed
- Fix a potential null reference exception when deserializing a JSON response.

## [1.0.0-pre.2] - 2022-06-24

### Changed
- Rename functions:
  - `ServerReadyForPlayersAsync` -> `ReadyServerForPlayersAsync`.
  - `ServerUnreadyAsync` -> `UnreadyServerAsync`.
  - `ConnectToServerCheckAsync` -> `StartServerQueryHandlerAsync`.
- On Windows, use `HOMEPATH` instead of `HOME` to read `server.json`.

### Fixed
- Fix connection to `payloadproxy`.

## [1.0.0-pre.1] - 2022-03-28

### Added
- Initial version of the Multiplay SDK package.
