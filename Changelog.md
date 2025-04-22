# NEventStore.Domain Versions

## 10.1.0

- Updated NEventStore reference to version 10.1.0
- `IRepository.Save` and `IRepository.SaveAsync` now return the persisted `ICommit`.

## 10.0.0

- Async Methods [#17](https://github.com/NEventStore/NEventStore.Domain/issues/17)
- Updated NEventStore reference to version 10.0.0

### Breaking Changes

- Simplified `IRepository` interface, many overloaded methods are now extension methods in `RepositoryExtensions`.

## 9.1.1

- Updated NEventStore reference to version 9.1.0
- Target Frameworks: netstandard2.0, net462.

## 9.0.1

- Updated NEventStore reference to version 9.0.1
- Added documentation files to NuGet packages (improved intellisense support) [#15](https://github.com/NEventStore/NEventStore.Domain/issues/15)

## 9.0.0

- Updated NEventStore reference to version 9.0.0
- net6.0 supported.

## 8.0.0

- Updated NEventStore reference to version 8.0.0
- net5.0, net461 supported.

### Breaking Changes

- dropped net45.


## 7.0.0

- Updated NEventStore reference to version 7.0.0

There are no known backwards breaking changes in this release.

## 6.0.0

- Updated NEventStore reference to version 6.0.0

There are no known backwards breaking changes in this release.

## 6.0.0-rc-0

__Version 6.x is not backwards compatible with version 5.x.__ Updating to NEventStore 6.x without doing some preparation work will result in problems.
