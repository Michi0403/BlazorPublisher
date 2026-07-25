# PublisherStudio v1.0.73 release

See `CHANGELOG-v1.0.73.md`, `SOURCE-CHANGES-v1.0.73.txt`, `TEST-RESULTS-v1.0.73.txt`, `docs/architecture/video-project-import-doctrine.md`, `docs/architecture/interchange-formats.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

v1.0.73 introduces the canonical open video-project interchange foundation. Video Studio can import OpenTimelineIO/OTIOZ, MLT XML including Kdenlive and Shotcut projects, XGES, OpenShot OSP, and CMX 3600 EDL. Imported tracks, gaps, placements, source ranges/rates, speeds, media references, markers and transitions are retained in `VideoProjectDocument`; missing media remains visible and can be batch relinked.

The existing selected-clip workflow remains intact: temporal selections, multiple cut sections, layer-bound frame regions, chroma key and live filters edit the selected clip on the active video-track projection. Other imported tracks are preserved, but simultaneous multitrack compositing/audio mixing is deliberately not claimed yet.

Application and installer version is `1.0.73`. Publication format is `1.52`; Picture Studio format remains `1.4`; dependency versions are unchanged.
