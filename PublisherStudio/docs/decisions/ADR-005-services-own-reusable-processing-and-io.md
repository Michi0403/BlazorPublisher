# ADR-005: Services own reusable processing and technical I/O

**Status:** Accepted

Components, Controllers, Hubs and HostedServices all reuse Services. Services own general data processing, application capabilities, persistence and technical I/O including files, networks, FFmpeg, devices and operating-system APIs.

Controllers are request-driven backend entry points. Hubs are persistent-connection entry points. HostedServices are thin scheduling and application-lifecycle adapters. None of those roots should contain reusable processing that another caller needs, and Services must not depend back on those caller roots.

The former `Backend` root duplicated the meaning of Controllers and fragmented reusable technical code. It is removed; its streaming implementation is organized beneath `Services/Streaming` instead.
