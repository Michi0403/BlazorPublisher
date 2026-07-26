# ADR-013: Loopback browser automation and screenshots

**Status:** Accepted

Local AI collaborators use controller-backed queues and an active browser runtime to execute DOM input and screenshots. This stays inside the existing monolith and browser security boundary. Operating-system-global input is not implied by this API.
