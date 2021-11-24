# NServiceBus.Gateway.RavenDB

The official [NServiceBus](https://github.com/Particular/NServiceBus) Gateway persistence implementation for [RavenDB.](https://ravendb.net/)

Learn more about NServiceBus.Gateway.RavenDB through our [documentation.](https://docs.particular.net/nservicebus/gateway/ravendb/)

If you are interested in contributing, please follow the instructions [here.](https://github.com/Particular/NServiceBus/blob/develop/CONTRIBUTING.md)

## Running the tests

Running the tests requires RavenDB 4.2 and an environment variable named `CommaSeparatedRavenClusterUrls` containing the connection URLs, separated by commas if testing a cluster. The tests can be run with a RavenDB server hosted on a Docker container.

### Spinning up the necessary infrastructure

This assumes docker and docker-compose are properly setup. It works currently on Windows with Docker Desktop but not on docker hosted in WSL2 only.

1. [Acquire a developer license](https://ravendb.net/license/request/dev)
1. Convert the multi-line license JSON to a single line JSON and set the `LICENSE` variable. Alternatively the license can be set using [an `.env` file](https://docs.docker.com/compose/environment-variables/).
1. Inside the root directory of the repository issue the following command: `docker-compose up -d`.

The single node server is reachable under [`http://localhost:8080`](http://localhost:8080). The cluster leader is reachable under [`http://localhost:8081`](http://localhost:8081).