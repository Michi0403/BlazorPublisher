# PublisherStudio v1.0.87 release

See `CHANGELOG-v1.0.87.md`, `SOURCE-CHANGES-v1.0.87.txt`, `TEST-RESULTS-v1.0.87.txt` and `docs/architecture/task-ledger.md`.

v1.0.87 is a focused compiler-unblocking release on top of v1.0.86. It fixes `CS9007` by separating the C# raw-string boundary from the embedded JavaScript interpolation syntax. The serialized blob configuration is inserted through one explicit marker using ordinal replacement; generated runtime behavior and the public interface remain compatible.

Application and installer version is `1.0.87`. Publication format remains `1.55`. Native .NET/Razor/DevExpress validation must continue on the licensed developer workstation; repository contracts, generated-JavaScript syntax, JavaScript, JSON, XML and archive checks are recorded in the test report.
