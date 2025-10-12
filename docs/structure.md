# Proposed project layout (backend-first)

The repo should be split by responsibility so that domain logic, delivery mechanisms, and legacy code never bleed into each other. The table below captures the target structure we will migrate toward.

| Folder | Purpose | Notes |
|--------|---------|-------|
| `backend/` | Source for all .NET code. Contains a single solution (`MovieTickets.Backend.sln`). | Keeps a cohesive module for tutors who open the project in VS2022. |
| `backend/src/MovieTickets.Core/` | Domain entities, services, storage adapters, TMDB integration. | No UI types; high-cohesion services consume JSON or API dependencies via interfaces. |
| `backend/src/MovieTickets.Api/` | ASP.NET host exposing the core over REST/SignalR for the Electron shell (or future clients). | References `MovieTickets.Core` only. |
| `backend/tests/MovieTickets.Tests/` | NUnit coverage to satisfy assignment requirements. | Organised by feature (catalog, screenings, seating). |
| `frontend/` | Vite + React UI implementing the booking flow. | Talks to the API during production; can hit mocked data in dev if needed. |
| `desktop/` | Electron wrapper. Boots the frontend and manages the packaged backend process. | Development mode points at Vite dev server. |
| `storage/` | JSON datasets (movies, screenings, cinemas, seating templates). | Loaded through repository interfaces so it can be swapped for EF later. |
| `legacy/winforms/` | Frozen WinForms prototype retained for reference only. | No longer part of the active build. |
| `docs/` | Specs, architecture notes, and decision records. | Use this to keep the tutor in sync with the migration story. |
| `scripts/` | Automation (build/run helpers). | `run-desktop.sh` builds Electron; `run-winforms.sh` still launches the legacy app via Wine. |

Goal: the backend compiles independently, Electron supplies the GUI, and the old WinForms code remains quarantined.
