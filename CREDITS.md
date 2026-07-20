# Open Source Credits and Third-Party Acknowledgements

StrongKingJames is built on top of a large ecosystem of open-source software and open data.
This project itself is licensed under the **MIT License** and is copyright **AdvanGeneration Pty Ltd**.

The following projects and data sets are used either directly in the codebase, at build/test time,
or as runtime infrastructure/data for the application.

## Open-Source Software

### .NET platform and Microsoft libraries

- **.NET 10 SDK, ASP.NET Core, Blazor, Entity Framework Core, .NET Aspire**
  - Copyright © Microsoft Corporation
  - Licensed under the **MIT License**
  - https://dotnet.microsoft.com/

- **Microsoft.EntityFrameworkCore**, **Microsoft.EntityFrameworkCore.Relational**, **Microsoft.EntityFrameworkCore.Design** (10.0.10)
  - Copyright © Microsoft Corporation
  - Licensed under the **MIT License**
  - https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/

- **Microsoft.Extensions.Http.Resilience** (10.6.0)
  - Copyright © Microsoft Corporation
  - Licensed under the **MIT License**

- **Microsoft.Extensions.ServiceDiscovery** (10.6.0)
  - Copyright © Microsoft Corporation
  - Licensed under the **MIT License**

- **Microsoft.NET.Test.Sdk** (17.14.1)
  - Copyright © Microsoft Corporation
  - Licensed under the **MIT License**

### PostgreSQL ecosystem

- **PostgreSQL**
  - Copyright © PostgreSQL Global Development Group
  - Licensed under the **PostgreSQL License**
  - https://www.postgresql.org/

- **Npgsql.EntityFrameworkCore.PostgreSQL** (10.0.3)
  - Copyright © The Npgsql Development Team
  - Licensed under the **PostgreSQL License**
  - https://www.npgsql.org/

- **pgvector**
  - Copyright © Andrew Kane
  - Licensed under the **PostgreSQL License**
  - https://github.com/pgvector/pgvector

- **Pgvector.EntityFrameworkCore** (0.3.0)
  - Copyright © Andrew Kane
  - Licensed under the **PostgreSQL License**
  - https://github.com/pgvector/pgvector-dotnet

- **Testcontainers.PostgreSql** (4.13.0)
  - Copyright © Testcontainers contributors
  - Licensed under the **MIT License**
  - https://testcontainers.com/

### Aspire and observability

- **Aspire.AppHost.Sdk** and **Aspire.Hosting.PostgreSQL** (13.4.6)
  - Copyright © Microsoft Corporation
  - Licensed under the **MIT License**
  - https://aka.ms/dotnet/aspire

- **OpenTelemetry.Exporter.OpenTelemetryProtocol**, **OpenTelemetry.Extensions.Hosting**, **OpenTelemetry.Instrumentation.AspNetCore**, **OpenTelemetry.Instrumentation.Http**, **OpenTelemetry.Instrumentation.Runtime**
  - Copyright © OpenTelemetry Authors
  - Licensed under the **Apache License 2.0**
  - https://opentelemetry.io/

### Testing

- **xUnit.net** (xunit 2.9.3, xunit.runner.visualstudio 3.1.4)
  - Copyright © .NET Foundation
  - Licensed under the **Apache License 2.0**
  - https://xunit.net/

- **coverlet.collector** (6.0.4)
  - Copyright © Toni Solarin-Sodara and coverlet contributors
  - Licensed under the **MIT License**
  - https://github.com/coverlet-coverage/coverlet

### Runtime infrastructure

- **Ollama**
  - Copyright © Ollama, Inc.
  - Licensed under the **MIT License**
  - https://ollama.com/

- **Docker** and **Docker Compose**
  - Copyright © Docker Inc.
  - Licensed under the **Apache License 2.0**
  - https://www.docker.com/

## Bible Text and Lexicon Data

The application requires Bible text and Strong's lexicon data that must be downloaded separately.
None of these data files are bundled in this repository.

### Primary source (recommended)

- **1John419/kjs**
  - KJV Bible text with per-word Strong's numbers for the whole Bible (OT Hebrew `H####`, NT Greek `G####`),
    plus a combined Hebrew+Greek Strong's dictionary.
  - Copyright © Clayton Carney
  - Licensed under the **GNU General Public License v3.0 (GPL-3.0)**
  - https://github.com/1John419/kjs

### Public-domain text

- **King James Version (KJV) of the Bible**
  - The underlying English translation is in the **public domain** in many jurisdictions.
  - The kjs JSON file combines this public-domain text with Strong's numbering and is itself
    distributed under GPL-3.0.

### Alternative source

- **openscriptures/strongs**
  - Strong's Hebrew dictionary in XML format (used only when the optional OSIS import path is enabled).
  - Copyright © Open Scriptures contributors
  - Licensed under a **Creative Commons Attribution-ShareAlike** license
  - https://github.com/openscriptures/strongs

## Trademarks

All product names, logos, and brands mentioned above are property of their respective owners.
Use of these names, logos, and brands does not imply endorsement.

---

If you believe any attribution is missing or incorrect, please open an issue and we will correct it.
