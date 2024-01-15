# Changelog

[_vNext_](https://github.com/sketch7/SignalR.Orleans/compare/1.0.0...1.1.0) (2024-X-X)

## [5.0.0](https://github.com/sketch7/SignalR.Orleans/compare/4.0.4...5.0.0) (2024-01-15)

### Features

- **deps:** update to .NET7 and Orleans 7

## [4.0.4](https://github.com/sketch7/SignalR.Orleans/compare/4.0.3...4.0.4) (2023-08-08)

### Features

- **hub context:** add `HubContext` non generic version (for dynamic usages)

## [4.0.3](https://github.com/sketch7/SignalR.Orleans/compare/4.0.2...4.0.3) (2023-06-09)
No changes CI switching testing

## [4.0.2](https://github.com/sketch7/SignalR.Orleans/compare/4.0.1...4.0.2) (2023-06-09)
No changes CI switching testing

## [4.0.1](https://github.com/sketch7/SignalR.Orleans/compare/4.0.0...4.0.1) (2023-02-06)

### Bug Fixes

- **orleans:** generate stream replica consistent instead of random, in order to reuse same stream

## [4.0.0](https://github.com/sketch7/SignalR.Orleans/compare/3.1.0...4.0.0) (2022-03-08)

### Features

- **netcore:** netcore 6.0 support
- **orleans:** Orleans 3.6.0 update
- **deps:** update `OrleansStreamsUtils` to v11.0.0 which consumes Orleans 3.6.0

## [3.1.0](https://github.com/sketch7/SignalR.Orleans/compare/3.0.0...3.1.0) (2022-03-08)

### Features

- **orleans:** Orleans 3.5.1 update
- **client:** add log when connection not found
- **orleans:** add log if stream fails to send on `ClientGrain`
- **deps:** update `OrleansStreamsUtils` to v10.0.0 which consumes Orleans 3.5.1

## [3.0.0](https://github.com/sketch7/SignalR.Orleans/compare/2.0.0...3.0.0) (2021-04-29)

### Features

- **netcore:** netcore 5.0 support
- **orleans:** Orleans 3.4.2 update
- **signalr:** SignalR 5.0.5 update

## [2.0.0](https://github.com/sketch7/SignalR.Orleans/compare/2.0.0-rc6...2.0.0) (2020-11-16)
No new changes - release as stable release

## [2.0.0-rc6](https://github.com/sketch7/SignalR.Orleans/compare/2.0.0-rc5...2.0.0-rc6) (2020-08-26)

### Bug Fixes

- **client:** regression server disconnect stream null check

## [2.0.0-rc5](https://github.com/sketch7/SignalR.Orleans/compare/2.0.0-rc4...2.0.0-rc5) (2020-08-18)

### Bug Fixes

- **client:** attempt to fix extension not installed in grain

## [2.0.0-rc4](https://github.com/sketch7/SignalR.Orleans/compare/2.0.0-rc3...2.0.0-rc4) (2020-06-11)

### Features

- **netcore:** netcore 3.1 support
- **orleans:** Orleans 3.2 update
- **client:** Add logs on `OnConnectedAsync`

### Bug Fixes

- **client:** handle subscribe correctly for `server-disconnected`
- **connection:** remove `Task` pooling and instead use one way invokes - was noticing timeouts stating after 30sec before 30sec (e.g. ~100ms) in several cases
- **connection:** on client disconnect defer `Remove` which potentially avoiding deadlock
- **connection:** fix key parsing when key was containing `:` group was incorrect
- **server directory:** dispose timer on deactivate
- **connection:** cleanup streams via timer instead of deferred timer (since it was timing out PubSub) and not triggering deactivation on connection remove
- **connection:** cleanup streams subscriptions when resume
- **client:** cleanup streams subscriptions when resume
- **client:** only clean Client Grain state when `hub-disconnect` gracefully, otherwise don't so it might be possible to recover grain - if for some reason the hub connection is still active but the grain doesn't
- **client:** avoid reusing stream subscription handlers - attempt fix for `Handle is no longer valid. It has been used to unsubscribe or resume.`

### BREAKING CHANGES

- **connection:** `Send*` will not await a response and now its fire and forget

## [2.0.0-rc3](https://github.com/sketch7/SignalR.Orleans/compare/2.0.0-rc2...2.0.0-rc3) (2019-11-21)

### Features

- **perf:** streams sharding for sending messages to servers - Should fix issue when having high throughput to a specific server it starts timing out

## 2.0.0-rc2 (2019-11-21)

### Features

- **netcore:** netcore 3 support

## 1.0.0-rc1 (2019-11-21)

### Features

- **netcore:** netcore2 support synced with 2.x

## 1.0.0-dev4 (2019-11-19)

### Features

- **netcore:** down version to netcore2.0