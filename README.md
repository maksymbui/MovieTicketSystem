# Movie Tickets – Assignment 2

## File Structure

- `/backend`: Contains all backend logic, including API, business logic, data models, and tests.
  - `src/MovieTickets.Api/`: ASP.NET Core API project (entry point, endpoints, API services).
  - `src/MovieTickets.Core/`: Core business logic, entities, models, and data storage.
  - `src/MovieTickets.Tests/`: NUnit test project for backend logic.
- `/frontend`: React + TypeScript frontend application, all user interface code and pages.
  - `src/pages/`: Main UI screens (seat selection, deals, checkout, etc.).
  - `src/hooks/`, `src/utils/`: API calls and utility functions.
- `/desktop`: Electron desktop app version (cross-platform desktop support).
- `/storage`: JSON files for persistent data (movies, bookings, users, etc.).
- `/tools/SeedGenerator`: Utility for generating and seeding initial data.
- `start.sh`, `flake.nix`, `flake.lock`: Scripts and Nix files for reproducible development environments.

> [!WARNING]
> After navigating towards the seat selection screen, the 'Quick Book' option is there for demo/testing, it auto-fills your current selection with adults and calls the same booking endpoint. It is not indicative of actual performance (nor is the rest of data)

## File References

- High cohesion/low coupling: `backend/src/MovieTickets.Core/Logic/*`, `Entities/*`, `Models/*`
- Polymorphism: `backend/src/MovieTickets.Core/Logic/BookingService.cs` (method overriding), `Entities/User.cs` (if inheritance used)
- Interfaces: `backend/src/MovieTickets.Core/Logic/IUserRepository.cs`, `MessageService.cs`, `DealService.cs`
- NUnit tests: `backend/src/MovieTickets.Tests/DealServiceTests.cs`
- Anonymous method with LINQ/Lambda: `backend/src/MovieTickets.Core/Logic/RewardService.cs`, `DealService.cs`, `BookingService.cs`
- Generics/Generic Collections: `List<T>` and `Dictionary<K,V>` in `DataStore.cs`, `BookingService.cs`, `PricingService.cs`
- Multiple GUI screens: `frontend/src/pages/SeatSelectionPage.tsx`, `CheckoutPage.tsx`, `DealsPage.tsx`, `LoginPage.tsx`, `RegisterPage.tsx`, `MailBoxPage.tsx`, `MovieDetailsPage.tsx`, `MyOrdersPage.tsx`, `AdminMenuPage.tsx`, `ManageDealsPage.tsx`
- Multiple UI element categories: Buttons, headings, dropdowns, images, lists, modals, alerts, tooltips, etc. (see above pages)
- Core features, error handling, input validation, data structures/algorithms:
  - Backend: `BookingService.cs`, `PricingService.cs`, `RewardService.cs`, `DataStore.cs`, `DealService.cs`, `MessageService.cs`
  - Frontend: All main pages in `frontend/src/pages/`
- Use of ASP.NET Core (not WinForms): `backend/src/MovieTickets.Api/`
- Use of external APIs: TMDB API integration in `backend/src/MovieTickets.Api/Services/TmdbImportService.cs`
- JSON data: `/storage/*.json` (auditoriums, bookings, cinemas, messages, movies, screenings, ticket_types)

## Initial setup:

```bash
Open terminal:
- `nix develop`
- `dotnet run --project backend/src/MovieTickets.Api/MovieTickets.Api.csproj` # Expected: "Now listening on: https://localhost:####"

Open another terminal:
- `nix develop`
- `export TMDB__APIKEY="df0b8bc6934d37266ef32754dfa21420"`
- `curl -X POST http://localhost:####/api/admin/import/tmdb` # Note: Change localhost port number with whatever is set.

- `dotnet run --project tools/SeedGenerator`# Expected: [seed] Wrote # cinemas, # auditoriums, # screenings, # bookings
```

## Run:

```bash
- `chmod +x start.sh`
- `./start.sh`
```

> [!NOTE]
> To refresh the movie catalog from TMDB, set `TMDB__APIKEY` (or edit `backend/src/MovieTickets.Api/appsettings.json`) and call `POST http://localhost:####/api/admin/import/tmdb`... and rerun: `dotnet run --project tools/SeedGenerator`.

## For Windows:

WSL with Ubuntu: (doesnt matter, nix is distro-agnostic)

```bash
WSL:
- `wsl --install -d Ubuntu-24.04` # Onetime, just for installing
- `wsl --shutdown` # Actually shutdown
- `wsl.exe -d Ubuntu-24.04` # Open

- `sudo apt update && sudo apt upgrade -y`

- `sh <(curl -L https://nixos.org/nix/install) --no-daemon . ~/.nix-profile/etc/profile.d/nix.sh` # installs nix
OR
- `sh <(curl -L https://nixos.org/nix/install) --daemon` # Should work...

Add to group:
- `sudo usermod -aG nix-users $USER` # this should be the main one
- `sudo usermod -aG nixbld $USER` # but I also have this just in case.... adds ur user group, make sure to run `wsl --shutdown` in Powershell (admin) and rerun with `wsl.exe -d Ubuntu-24.04` or whatever you use.

IF, groups are still missing (run: `grep nixbld /etc/group` and check, or just `group`):
- `sudo groupadd nixbld`
- `sudo usermod -aG nixbld $USER`

Enabling flakes:
- `mkdir -p ~/.config/nix`
- `echo "experimental-features = nix-command flakes" >> ~/.config/nix/nix.conf`

Checks:
- `nix --version` # verify it even exists
- `sudo systemctl start nix-daemon` # just in case its sleeping
- `sudo ls -ld /nix/var/nix /nix/var/nix/daemon-socket /nix/var/nix/daemon-socket/socket` # final check, for if you have perms to run nix commands or not
```
